using System;
using System.IO;
using LibVLCSharp.Shared;

namespace ArenaApp.Services
{
    /// <summary>
    /// Видеосервис на базе LibVLC. Включает декодеры в составе приложения (без кодеков у пользователя).
    /// </summary>
    public sealed class VlcVideoService : IDisposable
    {
        public LibVLC LibVlc { get; }
        public MediaPlayer MainPlayer { get; }
        public MediaPlayer SecondaryPlayer { get; }

        public event Action? Playing;
        public event Action? Paused;
        public event Action? Stopped;
        public event Action? EndReached;
        public event Action<string>? Error;

        private DateTime _lastMainSyncUtc = DateTime.MinValue;

        public VlcVideoService()
        {
            // Важно: инициализация LibVLCSharp
            Core.Initialize();

            // Базовые опции для более стабильного воспроизведения на Windows
            // (минимально-инвазивные — без агрессивных кэшей/латентности)
            LibVlc = new LibVLC(
                "--no-video-title-show",
                "--avcodec-hw=d3d11va"
            );
            MainPlayer = new MediaPlayer(LibVlc);
            SecondaryPlayer = new MediaPlayer(LibVlc);

            Wire(MainPlayer);
            Wire(SecondaryPlayer);

            // По умолчанию: звук идет только из основного плеера
            TrySetMute(SecondaryPlayer, true);
            TrySetMute(MainPlayer, false);
        }

        private void Wire(MediaPlayer player)
        {
            player.Playing += (_, _) => Playing?.Invoke();
            player.Paused += (_, _) => Paused?.Invoke();
            player.Stopped += (_, _) => Stopped?.Invoke();
            player.EndReached += (_, _) => EndReached?.Invoke();
            player.EncounteredError += (_, _) => Error?.Invoke("LibVLC: EncounteredError");
        }

        public void Load(string path, bool forSecondary = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Путь к видео пустой", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException($"Файл не найден: {path}", path);

            var media = new Media(LibVlc, new Uri(path));
            var player = forSecondary ? SecondaryPlayer : MainPlayer;
            player.Media = media;
        }

        public bool Play(bool forSecondary = false)
        {
            var player = forSecondary ? SecondaryPlayer : MainPlayer;
            return player.Play();
        }

        public void Pause(bool forSecondary = false)
        {
            var player = forSecondary ? SecondaryPlayer : MainPlayer;
            player.Pause();
        }

        public void Stop(bool forSecondary = false)
        {
            var player = forSecondary ? SecondaryPlayer : MainPlayer;
            player.Stop();
        }

        public TimeSpan GetPosition(bool forSecondary = false)
        {
            var player = forSecondary ? SecondaryPlayer : MainPlayer;
            var ms = player.Time;
            return ms >= 0 ? TimeSpan.FromMilliseconds(ms) : TimeSpan.Zero;
        }

        public void SetPosition(TimeSpan position, bool forSecondary = false)
        {
            var player = forSecondary ? SecondaryPlayer : MainPlayer;
            player.Time = (long)Math.Max(0, position.TotalMilliseconds);
        }

        public TimeSpan GetDuration(bool forSecondary = false)
        {
            var player = forSecondary ? SecondaryPlayer : MainPlayer;
            var ms = player.Length;
            return ms > 0 ? TimeSpan.FromMilliseconds(ms) : TimeSpan.Zero;
        }

        public bool IsPlaying(bool forSecondary = false)
        {
            var player = forSecondary ? SecondaryPlayer : MainPlayer;
            return player.IsPlaying;
        }

        public bool HasMedia(bool forSecondary = false)
        {
            var player = forSecondary ? SecondaryPlayer : MainPlayer;
            return player.Media != null;
        }
        
        /// <summary>
        /// Получает путь к текущему медиа файлу
        /// </summary>
        public string? GetMediaPath(bool forSecondary = false)
        {
            var player = forSecondary ? SecondaryPlayer : MainPlayer;
            if (player.Media == null) return null;
            
            try
            {
                var mrl = player.Media.Mrl;
                if (string.IsNullOrWhiteSpace(mrl)) return null;
                
                // Если это URI (file://), преобразуем в локальный путь
                if (Uri.TryCreate(mrl, UriKind.Absolute, out var uri))
                {
                    if (uri.IsFile)
                    {
                        return uri.LocalPath;
                    }
                    return mrl; // Если не file://, возвращаем как есть
                }
                
                // Если это уже путь, возвращаем как есть
                return mrl;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Перезапускает медиа с начала (для зацикливания)
        /// </summary>
        public void Restart(bool forSecondary = false)
        {
            var player = forSecondary ? SecondaryPlayer : MainPlayer;
            if (player.Media == null) return;
            
            try
            {
                // Получаем путь к текущему медиа
                var mediaPath = GetMediaPath(forSecondary);
                if (string.IsNullOrWhiteSpace(mediaPath))
                {
                    System.Diagnostics.Debug.WriteLine($"VLC Restart: Не удалось получить путь к медиа");
                    return;
                }
                
                // Проверяем существование файла (только для локальных путей)
                if (mediaPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase) || 
                    (mediaPath.Length > 2 && mediaPath[1] == ':'))
                {
                    if (!File.Exists(mediaPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"VLC Restart: Файл не найден: {mediaPath}");
                        return;
                    }
                }
                
                // Останавливаем
                player.Stop();
                
                // Небольшая задержка для завершения остановки
                System.Threading.Thread.Sleep(50);
                
                // Перезагружаем медиа
                var media = new Media(LibVlc, new Uri(mediaPath));
                player.Media = media;
                
                // Запускаем с начала
                player.Play();
                
                System.Diagnostics.Debug.WriteLine($"VLC Restart: Медиа перезапущено с начала: {mediaPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VLC Restart error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Синхронизирует позицию вторичного плеера с основным
        /// </summary>
        public void SyncSecondaryPlayer()
        {
            if (!HasMedia() || !HasMedia(forSecondary: true)) return;
            
            try
            {
                var currentPos = GetPosition();
                var secondaryPos = GetPosition(forSecondary: true);
                if (Math.Abs((currentPos - secondaryPos).TotalSeconds) > 0.1)
                {
                    SetPosition(currentPos, forSecondary: true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VLC sync error: {ex.Message}");
            }
        }

        /// <summary>
        /// Режим "второй экран = мастер": звук идет только из SecondaryPlayer, а MainPlayer — беззвучное превью.
        /// </summary>
        public void SetDualScreenAudioMode(bool enabled)
        {
            // Если enabled=true: звук только у secondary
            TrySetMute(MainPlayer, enabled);
            TrySetMute(SecondaryPlayer, !enabled);
        }

        /// <summary>
        /// Подстраивает основной плеер к вторичному (чтобы корректировки не дергали звук/видео на выходе).
        /// Делает редкие корректировки (throttle), без постоянных "прыжков" каждую итерацию таймера.
        /// </summary>
        public void SyncMainToSecondaryThrottled(TimeSpan driftThreshold, TimeSpan minInterval)
        {
            if (!HasMedia(forSecondary: false) || !HasMedia(forSecondary: true)) return;
            if (!IsPlaying(forSecondary: false) || !IsPlaying(forSecondary: true)) return;

            var now = DateTime.UtcNow;
            if (now - _lastMainSyncUtc < minInterval) return;

            var mainPos = GetPosition(forSecondary: false);
            var secondaryPos = GetPosition(forSecondary: true);
            var drift = secondaryPos - mainPos;

            if (Math.Abs(drift.TotalMilliseconds) >= Math.Abs(driftThreshold.TotalMilliseconds))
            {
                // Корректируем ТОЛЬКО основной плеер (превью), чтобы не портить звук/видео на выходе
                SetPosition(secondaryPos, forSecondary: false);
                _lastMainSyncUtc = now;
            }
        }

        private static void TrySetMute(MediaPlayer player, bool mute)
        {
            try
            {
                player.Mute = mute;
            }
            catch
            {
                // ignore
            }
        }

        public void Dispose()
        {
            try { SecondaryPlayer.Dispose(); } catch { /* ignore */ }
            try { MainPlayer.Dispose(); } catch { /* ignore */ }
            try { LibVlc.Dispose(); } catch { /* ignore */ }
        }
    }
}



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
        public event Action<string>? Error;

        public VlcVideoService()
        {
            // Важно: инициализация LibVLCSharp
            Core.Initialize();

            LibVlc = new LibVLC();
            MainPlayer = new MediaPlayer(LibVlc);
            SecondaryPlayer = new MediaPlayer(LibVlc);

            Wire(MainPlayer);
            Wire(SecondaryPlayer);
        }

        private void Wire(MediaPlayer player)
        {
            player.Playing += (_, _) => Playing?.Invoke();
            player.Paused += (_, _) => Paused?.Invoke();
            player.Stopped += (_, _) => Stopped?.Invoke();
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

        public void Dispose()
        {
            try { SecondaryPlayer.Dispose(); } catch { /* ignore */ }
            try { MainPlayer.Dispose(); } catch { /* ignore */ }
            try { LibVlc.Dispose(); } catch { /* ignore */ }
        }
    }
}



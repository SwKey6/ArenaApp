using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления основными операциями с медиа (загрузка, воспроизведение, остановка, закрытие)
    /// </summary>
    public class MediaControlService
    {
        private MediaStateService? _mediaStateService;
        
        // Делегаты для доступа к UI элементам
        public Func<MediaElement>? GetMainMediaElement { get; set; }
        public Action? SyncPauseWithSecondaryScreen { get; set; }
        
        // Делегаты для работы с состоянием
        public Func<string?>? GetCurrentMainMedia { get; set; }
        public Func<string?>? GetCurrentAudioContent { get; set; }
        public Func<string, MediaElement?>? TryGetAudioSlot { get; set; }
        public Func<string, TimeSpan>? GetSlotPosition { get; set; }
        public Action<string, TimeSpan>? SaveSlotPosition { get; set; }
        public Func<string, TimeSpan>? GetMediaResumePosition { get; set; }
        public Action<bool>? SetIsVideoPlaying { get; set; }
        public Action<bool>? SetIsAudioPlaying { get; set; }
        
        public void SetMediaStateService(MediaStateService service)
        {
            _mediaStateService = service;
        }
        
        /// <summary>
        /// Загружает медиа файл через диалог выбора файла
        /// </summary>
        public void LoadMedia()
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            if (mediaElement == null) return;
            
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Video files|*.mp4;*.avi;*.mov;*.wmv;*.flv;*.mkv";
            if (openFileDialog.ShowDialog() == true)
            {
                mediaElement.Source = new Uri(openFileDialog.FileName);
            }
        }
        
        /// <summary>
        /// Воспроизводит медиа
        /// </summary>
        public void PlayMedia()
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            if (mediaElement == null) return;
            
            var currentMainMedia = GetCurrentMainMedia?.Invoke();
            
            // Если нет активного триггера, работаем с основным плеером
            if (currentMainMedia == null || currentMainMedia.StartsWith("Slot_"))
            {
                // Возобновляем с сохраненной позиции
                if (mediaElement.Source != null)
                {
                    var resumePosition = GetMediaResumePosition?.Invoke(mediaElement.Source.LocalPath);
                    if (resumePosition != null && resumePosition.Value > TimeSpan.Zero)
                    {
                        mediaElement.Position = resumePosition.Value;
                    }
                }
                mediaElement.Play();
                SetIsVideoPlaying?.Invoke(true);
            }
            
            // Также возобновляем активное аудио, если есть
            var currentAudioContent = GetCurrentAudioContent?.Invoke();
            if (currentAudioContent != null)
            {
                var audioElement = TryGetAudioSlot?.Invoke(currentAudioContent);
                if (audioElement != null && audioElement.Source != null)
                {
                    var audioResume = GetMediaResumePosition?.Invoke(audioElement.Source.LocalPath);
                    if (audioResume != null && audioResume.Value > TimeSpan.Zero)
                    {
                        audioElement.Position = audioResume.Value;
                    }
                    audioElement.Play();
                    SetIsAudioPlaying?.Invoke(true);
                }
            }
        }
        
        /// <summary>
        /// Приостанавливает воспроизведение медиа
        /// </summary>
        public void StopMedia()
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            if (mediaElement == null) return;
            
            var currentMainMedia = GetCurrentMainMedia?.Invoke();
            
            // Если нет активного триггера, работаем с основным плеером
            if (currentMainMedia == null || currentMainMedia.StartsWith("Slot_"))
            {
                // Сохраняем позицию слота перед паузой
                if (currentMainMedia != null)
                {
                    var position = mediaElement.Position;
                    SaveSlotPosition?.Invoke(currentMainMedia, position);
                }
                mediaElement.Pause();
                SyncPauseWithSecondaryScreen?.Invoke();
                SetIsVideoPlaying?.Invoke(false);
            }
            
            // Также приостанавливаем активное аудио, если есть
            var currentAudioContent = GetCurrentAudioContent?.Invoke();
            if (currentAudioContent != null)
            {
                var audioElement = TryGetAudioSlot?.Invoke(currentAudioContent);
                if (audioElement != null)
                {
                    // Сохраняем позицию аудио слота перед паузой
                    var audioPosition = audioElement.Position;
                    SaveSlotPosition?.Invoke(currentAudioContent, audioPosition);
                    audioElement.Pause();
                    SetIsAudioPlaying?.Invoke(false);
                }
            }
        }
        
        /// <summary>
        /// Закрывает медиа (останавливает и очищает источник)
        /// </summary>
        public void CloseMedia()
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            if (mediaElement == null) return;
            
            var currentMainMedia = GetCurrentMainMedia?.Invoke();
            
            // Если нет активного триггера, работаем с основным плеером
            // Сохраняем позицию перед остановкой
            if (currentMainMedia != null)
            {
                var position = mediaElement.Position;
                SaveSlotPosition?.Invoke(currentMainMedia, position);
            }
            
            mediaElement.Stop();
            mediaElement.Source = null;
        }
    }
}


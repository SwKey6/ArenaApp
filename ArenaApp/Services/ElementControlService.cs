using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления воспроизведением элементов (Play, Stop, Restart)
    /// </summary>
    public class ElementControlService
    {
        private MediaStateService? _mediaStateService;
        private VideoDisplayService? _videoDisplayService;
        
        // Делегаты для доступа к UI элементам
        public Func<MediaElement>? GetMainMediaElement { get; set; }
        public Func<Border>? GetMediaBorder { get; set; }
        public Func<Grid>? GetTextOverlayGrid { get; set; }
        public Func<Grid>? GetMainContentGrid { get; set; }
        public Func<Dispatcher>? GetDispatcher { get; set; }
        public Func<MediaElement?>? GetSecondaryMediaElement { get; set; }
        public Func<Window?>? GetSecondaryScreenWindow { get; set; }
        
        // Делегаты для работы с состоянием
        public Func<string?>? GetCurrentMainMedia { get; set; }
        public Action<string?>? SetCurrentMainMedia { get; set; }
        public Func<string?>? GetCurrentAudioContent { get; set; }
        public Func<string?>? GetCurrentVisualContent { get; set; }
        public Func<bool>? GetIsVideoPaused { get; set; }
        public Action<bool>? SetIsVideoPaused { get; set; }
        public Action<bool>? SetIsVideoPlaying { get; set; }
        public Action<bool>? SetIsAudioPlaying { get; set; }
        
        // Делегаты для работы с аудио слотами
        public Func<string, MediaElement?>? TryGetAudioSlot { get; set; }
        public Func<Dictionary<string, MediaElement>>? GetAllAudioSlots { get; set; }
        public Func<Dictionary<string, Grid>>? GetAllAudioContainers { get; set; }
        public Func<Grid>? GetBottomPanel { get; set; }
        
        // Делегаты для работы с позициями
        public Func<string, TimeSpan>? GetMediaResumePosition { get; set; }
        public Action<string, TimeSpan>? SaveMediaResumePosition { get; set; }
        
        // Делегаты для работы с UI
        public Action? UpdateAllSlotButtonsHighlighting { get; set; }
        public Action? ApplyTextSettings { get; set; }
        public Action? SyncPlayWithSecondaryScreen { get; set; }
        public Action? SyncPauseWithSecondaryScreen { get; set; }
        
        public void SetMediaStateService(MediaStateService service)
        {
            _mediaStateService = service;
        }
        
        public void SetVideoDisplayService(VideoDisplayService service)
        {
            _videoDisplayService = service;
        }
        
        /// <summary>
        /// Воспроизводит выбранный элемент
        /// </summary>
        public void PlayElement(MediaSlot slot, string slotKey)
        {
            if (slot == null || string.IsNullOrEmpty(slotKey)) return;
            
            var mediaElement = GetMainMediaElement?.Invoke();
            var currentMainMedia = GetCurrentMainMedia?.Invoke();
            var currentAudioContent = GetCurrentAudioContent?.Invoke();
            var activeAudioSlots = GetAllAudioSlots?.Invoke() ?? new Dictionary<string, MediaElement>();
            var isVideoPaused = GetIsVideoPaused?.Invoke() ?? false;
            var isVideoPlaying = false; // Будет установлено ниже
            var isAudioPlaying = false; // Будет установлено ниже
            
            // Проверяем, играет ли уже этот элемент
            bool isCurrentlyPlaying = false;
            bool isCurrentlyPaused = false;
            
            if (slot.Type == MediaType.Video || slot.Type == MediaType.Image)
            {
                isCurrentlyPlaying = currentMainMedia == slotKey && mediaElement?.Source != null;
                isCurrentlyPaused = isCurrentlyPlaying && !isVideoPlaying;
            }
            else if (slot.Type == MediaType.Audio)
            {
                if (activeAudioSlots != null && activeAudioSlots.TryGetValue(slotKey, out MediaElement? audioElement))
                {
                    isCurrentlyPlaying = currentAudioContent == slotKey;
                    isCurrentlyPaused = isCurrentlyPlaying && !isAudioPlaying;
                }
            }
            else if (slot.Type == MediaType.Text)
            {
                isCurrentlyPlaying = currentMainMedia == slotKey;
                isCurrentlyPaused = false; // Текстовые элементы не имеют состояния паузы
            }
            
            // Если элемент уже играет - ставим на паузу/возобновляем
            if (isCurrentlyPlaying)
            {
                if (isCurrentlyPaused)
                {
                    // Возобновляем воспроизведение с сохраненной позиции
                    if (slot.Type == MediaType.Video || slot.Type == MediaType.Image)
                    {
                        if (mediaElement?.Source != null)
                        {
                            var resume = GetMediaResumePosition?.Invoke(mediaElement.Source.LocalPath) ?? TimeSpan.Zero;
                            if (resume > TimeSpan.Zero)
                            {
                                mediaElement.Position = resume;
                                // Синхронизируем позицию со вторым экраном
                                var secondaryElement = GetSecondaryMediaElement?.Invoke();
                                if (secondaryElement?.Source != null)
                                {
                                    try
                                    {
                                        secondaryElement.Position = resume;
                                    }
                                    catch { }
                                }
                            }
                        }
                        mediaElement?.Play();
                        SyncPlayWithSecondaryScreen?.Invoke();
                        SetIsVideoPlaying?.Invoke(true);
                    }
                    else if (slot.Type == MediaType.Audio && activeAudioSlots != null && activeAudioSlots.TryGetValue(slotKey, out MediaElement? audioElement))
                    {
                        var resume = GetMediaResumePosition?.Invoke(audioElement.Source?.LocalPath ?? "") ?? TimeSpan.Zero;
                        if (resume > TimeSpan.Zero)
                        {
                            audioElement.Position = resume;
                        }
                        audioElement.Play();
                        SetIsAudioPlaying?.Invoke(true);
                    }
                    else if (slot.Type == MediaType.Text)
                    {
                        ApplyTextSettings?.Invoke();
                    }
                }
                else
                {
                    // Ставим на паузу и сохраняем позицию
                    if (slot.Type == MediaType.Video || slot.Type == MediaType.Image)
                    {
                        if (mediaElement?.Source != null)
                        {
                            var position = mediaElement.Position;
                            SaveMediaResumePosition?.Invoke(mediaElement.Source.LocalPath, position);
                        }
                        mediaElement?.Pause();
                        SyncPauseWithSecondaryScreen?.Invoke();
                        SetIsVideoPlaying?.Invoke(false);
                    }
                    else if (slot.Type == MediaType.Audio && activeAudioSlots != null && activeAudioSlots.TryGetValue(slotKey, out MediaElement? audioElement))
                    {
                        if (audioElement.Source != null)
                        {
                            var position = audioElement.Position;
                            SaveMediaResumePosition?.Invoke(audioElement.Source.LocalPath, position);
                        }
                        audioElement.Pause();
                        SetIsAudioPlaying?.Invoke(false);
                    }
                }
                UpdateAllSlotButtonsHighlighting?.Invoke();
                return;
            }
            
            // Если элемент не играет - запускаем заново
            if (slot.Type == MediaType.Video)
            {
                _videoDisplayService?.LoadAndPlayVideo(slot, slotKey);
            }
            else if (slot.Type == MediaType.Image)
            {
                LoadAndPlayImage(slot, slotKey);
            }
            else if (slot.Type == MediaType.Audio)
            {
                LoadAndPlayAudio(slot, slotKey);
            }
            else if (slot.Type == MediaType.Text)
            {
                SetCurrentMainMedia?.Invoke(slotKey);
                ApplyTextSettings?.Invoke();
            }
            
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
        
        /// <summary>
        /// Останавливает выбранный элемент
        /// </summary>
        public void StopElement(MediaSlot slot, string slotKey)
        {
            if (slot == null || string.IsNullOrEmpty(slotKey)) return;
            
            if (slot.Type == MediaType.Video || slot.Type == MediaType.Image)
            {
                StopCurrentMainMedia();
            }
            else if (slot.Type == MediaType.Audio)
            {
                StopAudioInSlot(slotKey);
            }
            
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
        
        /// <summary>
        /// Перезапускает выбранный элемент
        /// </summary>
        public async Task RestartElement(MediaSlot slot, string slotKey)
        {
            if (slot == null || string.IsNullOrEmpty(slotKey)) return;
            
            StopElement(slot, slotKey);
            
            var dispatcher = GetDispatcher?.Invoke();
            if (dispatcher != null)
            {
                await Task.Delay(100);
                dispatcher.Invoke(() => PlayElement(slot, slotKey));
            }
        }
        
        /// <summary>
        /// Загружает и воспроизводит изображение
        /// </summary>
        private void LoadAndPlayImage(MediaSlot slot, string slotKey)
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            var mediaBorder = GetMediaBorder?.Invoke();
            var textOverlayGrid = GetTextOverlayGrid?.Invoke();
            
            if (mediaElement == null || mediaBorder == null || textOverlayGrid == null) return;
            
            mediaElement.Stop();
            mediaElement.Source = null;
            
            // Создаем Image элемент
            var imageElement = new Image
            {
                Source = new BitmapImage(new Uri(slot.MediaPath)),
                Stretch = Stretch.Uniform,
                Width = 600,
                Height = 400
            };
            
            // Обновляем содержимое Grid, сохраняя textOverlayGrid
            if (mediaBorder.Child is Grid mainGrid)
            {
                // Удаляем старые элементы
                var oldMediaElement = mainGrid.Children.OfType<MediaElement>().FirstOrDefault();
                if (oldMediaElement != null)
                {
                    mainGrid.Children.Remove(oldMediaElement);
                }
                var oldImages = mainGrid.Children.OfType<Image>().ToList();
                foreach (var oldImage in oldImages)
                {
                    mainGrid.Children.Remove(oldImage);
                }
                
                // Добавляем новое изображение
                mainGrid.Children.Insert(0, imageElement);
                
                // Убеждаемся, что textOverlayGrid остается
                if (!mainGrid.Children.Contains(textOverlayGrid))
                {
                    mainGrid.Children.Add(textOverlayGrid);
                }
                
                // Делаем textOverlayGrid невидимым если в нем нет текста
                if (textOverlayGrid.Children.Count == 0)
                {
                    textOverlayGrid.Visibility = Visibility.Hidden;
                }
                else
                {
                    textOverlayGrid.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Если нет Grid, создаем новый
                var newGrid = new Grid();
                newGrid.Children.Add(imageElement);
                newGrid.Children.Add(textOverlayGrid);
                mediaBorder.Child = newGrid;
                
                // Делаем textOverlayGrid невидимым если в нем нет текста
                if (textOverlayGrid.Children.Count == 0)
                {
                    textOverlayGrid.Visibility = Visibility.Hidden;
                }
                else
                {
                    textOverlayGrid.Visibility = Visibility.Visible;
                }
            }
            
            SetCurrentMainMedia?.Invoke(slotKey);
            SetIsVideoPlaying?.Invoke(false); // Изображения не "играют"
        }
        
        /// <summary>
        /// Загружает и воспроизводит аудио
        /// </summary>
        private void LoadAndPlayAudio(MediaSlot slot, string slotKey)
        {
            var activeAudioSlots = GetAllAudioSlots?.Invoke();
            var activeAudioContainers = GetAllAudioContainers?.Invoke();
            var bottomPanel = GetBottomPanel?.Invoke();
            
            if (bottomPanel == null || activeAudioSlots == null || activeAudioContainers == null) return;
            
            // Для аудио создаем отдельный MediaElement
            if (!activeAudioSlots.ContainsKey(slotKey))
            {
                var audioElement = new MediaElement
                {
                    LoadedBehavior = MediaState.Manual,
                    Source = new Uri(slot.MediaPath),
                    Volume = slot.Volume,
                    SpeedRatio = slot.PlaybackSpeed
                };
                
                // Создаем контейнер для аудио элемента
                var audioContainer = new Grid
                {
                    Width = 1,
                    Height = 1,
                    Visibility = Visibility.Hidden
                };
                audioContainer.Children.Add(audioElement);
                
                bottomPanel.Children.Add(audioContainer);
                
                activeAudioSlots[slotKey] = audioElement;
                activeAudioContainers[slotKey] = audioContainer;
            }
            
            var element = activeAudioSlots[slotKey];
            element.Play();
            SetCurrentAudioContent?.Invoke(slotKey);
            SetIsAudioPlaying?.Invoke(true);
        }
        
        /// <summary>
        /// Останавливает текущее основное медиа
        /// </summary>
        private void StopCurrentMainMedia()
        {
            _videoDisplayService?.StopCurrentMainMedia();
            SetCurrentVisualContent?.Invoke(null);
        }
        
        /// <summary>
        /// Останавливает аудио в слоте
        /// </summary>
        private void StopAudioInSlot(string slotKey)
        {
            var activeAudioSlots = GetAllAudioSlots?.Invoke();
            var activeAudioContainers = GetAllAudioContainers?.Invoke();
            var bottomPanel = GetBottomPanel?.Invoke();
            
            if (activeAudioSlots != null && activeAudioSlots.TryGetValue(slotKey, out MediaElement? audioElement))
            {
                audioElement.Stop();
                activeAudioSlots.Remove(slotKey);
                
                if (activeAudioContainers != null && activeAudioContainers.TryGetValue(slotKey, out Grid? container) && bottomPanel != null)
                {
                    bottomPanel.Children.Remove(container);
                    activeAudioContainers.Remove(slotKey);
                }
            }
        }
    }
}


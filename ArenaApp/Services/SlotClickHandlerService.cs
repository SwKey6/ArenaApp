using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для обработки кликов по слотам медиа
    /// </summary>
    public class SlotClickHandlerService
    {
        private ProjectManager? _projectManager;
        private MediaStateService? _mediaStateService;
        private VideoDisplayService? _videoDisplayService;
        private MediaControlService? _mediaControlService;
        private DialogService? _dialogService;
        
        // Делегаты для доступа к UI элементам
        public Func<MediaElement>? GetMainMediaElement { get; set; }
        public Func<Border>? GetMediaBorder { get; set; }
        public Func<Grid>? GetTextOverlayGrid { get; set; }
        public Func<Window?>? GetSecondaryScreenWindow { get; set; }
        
        // Делегаты для работы с состоянием
        public Func<string?>? GetCurrentMainMedia { get; set; }
        public Action<string?>? SetCurrentMainMedia { get; set; }
        public Func<string?>? GetCurrentVisualContent { get; set; }
        public Func<string?>? GetCurrentAudioContent { get; set; }
        public Func<bool>? GetIsVideoPaused { get; set; }
        public Action<bool>? SetIsVideoPaused { get; set; }
        public Func<Dictionary<string, MediaElement>>? GetAllAudioSlots { get; set; }
        
        // Делегаты для действий
        public Action<MediaSlot>? LoadMediaFromSlotSelective { get; set; }
        public Action<int, int>? ShowSlotOptionsDialog { get; set; }
        public Action? UpdateAllSlotButtonsHighlighting { get; set; }
        public Action? SyncPlayWithSecondaryScreen { get; set; }
        public Action? SyncPauseWithSecondaryScreen { get; set; }
        public Action<MediaElement>? RestoreMediaElement { get; set; }
        
        public void SetProjectManager(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        
        public void SetMediaStateService(MediaStateService mediaStateService)
        {
            _mediaStateService = mediaStateService;
        }
        
        public void SetVideoDisplayService(VideoDisplayService videoDisplayService)
        {
            _videoDisplayService = videoDisplayService;
        }
        
        public void SetMediaControlService(MediaControlService mediaControlService)
        {
            _mediaControlService = mediaControlService;
        }
        
        public void SetDialogService(DialogService dialogService)
        {
            _dialogService = dialogService;
        }
        
        /// <summary>
        /// Обрабатывает клик по слоту
        /// </summary>
        public void HandleSlotClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            
            string? tag = btn.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            // Парсим тег для получения координат слота
            string[] parts = tag.Replace("Slot_", "").Split('_');
            if (parts.Length != 2 || 
                !int.TryParse(parts[0], out int column) || 
                !int.TryParse(parts[1], out int row))
            {
                return;
            }

            // Проверяем, есть ли медиа в этом слоте
            var mediaSlot = _projectManager?.GetMediaSlot(column, row);
            if (mediaSlot == null || (string.IsNullOrEmpty(mediaSlot.MediaPath) && mediaSlot.Type != MediaType.Text))
            {
                // Предлагаем загрузить медиа или создать текстовый блок в этот слот
                ShowSlotOptionsDialog?.Invoke(column, row);
                return;
            }

            string slotKey = $"Slot_{column}_{row}";
            var mediaElement = GetMainMediaElement?.Invoke();
            var mediaBorder = GetMediaBorder?.Invoke();
            var textOverlayGrid = GetTextOverlayGrid?.Invoke();
            
            if (mediaElement == null || mediaBorder == null || textOverlayGrid == null) return;
            
            // Проверяем, воспроизводится ли этот слот сейчас
            bool isMainMedia = IsSlotCurrentlyPlaying(mediaSlot, slotKey, mediaElement, mediaBorder, textOverlayGrid);
            bool isAudioMedia = GetCurrentAudioContent?.Invoke() == slotKey && 
                               GetAllAudioSlots?.Invoke()?.ContainsKey(slotKey) == true;

            if (isMainMedia || isAudioMedia)
            {
                // Если этот слот уже воспроизводится - ставим на паузу/возобновляем или останавливаем
                HandlePlayingSlotClick(mediaSlot, slotKey, isMainMedia, isAudioMedia, mediaElement, mediaBorder, textOverlayGrid);
            }
            else
            {
                // Если этот слот не воспроизводится - запускаем его
                System.Diagnostics.Debug.WriteLine($"Slot_Click: Запускаем слот {column}-{row}, Type={mediaSlot.Type}, Path={mediaSlot.MediaPath}");
                LoadMediaFromSlotSelective?.Invoke(mediaSlot);
            }
        }
        
        private bool IsSlotCurrentlyPlaying(MediaSlot mediaSlot, string slotKey, MediaElement mediaElement, Border mediaBorder, Grid textOverlayGrid)
        {
            if (GetCurrentMainMedia?.Invoke() != slotKey) return false;
            
            return mediaSlot.Type switch
            {
                MediaType.Text => textOverlayGrid.Children.Count > 0,
                MediaType.Image => IsImageCurrentlyDisplayed(mediaSlot, mediaBorder),
                _ => mediaElement.Source != null
            };
        }
        
        private bool IsImageCurrentlyDisplayed(MediaSlot mediaSlot, Border mediaBorder)
        {
            bool hasImage = false;
            bool imagePathMatches = false;
            
            if (mediaBorder.Child is Grid mainGrid)
            {
                var images = mainGrid.Children.OfType<Image>().ToList();
                hasImage = images.Any();
                if (hasImage && !string.IsNullOrEmpty(mediaSlot.MediaPath))
                {
                    imagePathMatches = images.Any(img => 
                        img.Source is BitmapImage bitmap && 
                        bitmap.UriSource != null && 
                        bitmap.UriSource.LocalPath.Equals(mediaSlot.MediaPath, StringComparison.OrdinalIgnoreCase));
                }
            }
            else if (mediaBorder.Child is Image currentImage)
            {
                hasImage = true;
                if (!string.IsNullOrEmpty(mediaSlot.MediaPath) && currentImage.Source is BitmapImage bitmap)
                {
                    imagePathMatches = bitmap.UriSource != null && 
                        bitmap.UriSource.LocalPath.Equals(mediaSlot.MediaPath, StringComparison.OrdinalIgnoreCase);
                }
            }
            
            return hasImage && (GetCurrentVisualContent?.Invoke() == $"Slot_{mediaSlot.Column}_{mediaSlot.Row}" || imagePathMatches);
        }
        
        private void HandlePlayingSlotClick(MediaSlot mediaSlot, string slotKey, bool isMainMedia, bool isAudioMedia, 
            MediaElement mediaElement, Border mediaBorder, Grid textOverlayGrid)
        {
            if (isMainMedia)
            {
                HandleMainMediaClick(mediaSlot, slotKey, mediaElement, mediaBorder, textOverlayGrid);
            }
            
            if (isAudioMedia)
            {
                HandleAudioMediaClick(slotKey);
            }
        }
        
        private void HandleMainMediaClick(MediaSlot mediaSlot, string slotKey, MediaElement mediaElement, Border mediaBorder, Grid textOverlayGrid)
        {
            // Для изображений и текста - останавливаем (гасим) при повторном клике
            if (mediaSlot.Type == MediaType.Image)
            {
                StopImage(mediaElement, mediaBorder, slotKey);
                return;
            }
            
            if (mediaSlot.Type == MediaType.Text)
            {
                StopText(textOverlayGrid, slotKey);
                return;
            }
            
            // Для видео - управляем паузой/возобновлением
            bool isPaused = GetIsVideoPaused?.Invoke() ?? false;
            if (isPaused)
            {
                // Видео на паузе - возобновляем
                mediaElement.Play();
                SyncPlayWithSecondaryScreen?.Invoke();
                SetIsVideoPaused?.Invoke(false);
            }
            else
            {
                // Видео воспроизводится - ставим на паузу
                mediaElement.Pause();
                SyncPauseWithSecondaryScreen?.Invoke();
                SetIsVideoPaused?.Invoke(true);
            }
        }
        
        private void StopImage(MediaElement mediaElement, Border mediaBorder, string slotKey)
        {
            // Останавливаем изображение - очищаем визуальный контент
            mediaElement.Stop();
            mediaElement.Source = null;
            SetCurrentMainMedia?.Invoke(null);
            
            // Восстанавливаем MediaElement если был заменен на Image
            if (mediaBorder.Child != mediaElement)
            {
                RestoreMediaElement?.Invoke(mediaElement);
                mediaElement.Visibility = Visibility.Visible;
            }
            
            // Очищаем изображение на втором экране
            var secondaryWindow = GetSecondaryScreenWindow?.Invoke();
            if (secondaryWindow?.Content is Image)
            {
                secondaryWindow.Content = null;
            }
            else if (secondaryWindow?.Content is Grid secondaryGrid)
            {
                var secondaryImages = secondaryGrid.Children.OfType<Image>().ToList();
                foreach (var img in secondaryImages)
                {
                    secondaryGrid.Children.Remove(img);
                }
            }
            
            // Обновляем подсветку кнопок
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
        
        private void StopText(Grid textOverlayGrid, string slotKey)
        {
            // Останавливаем текст - очищаем textOverlayGrid
            textOverlayGrid.Children.Clear();
            textOverlayGrid.Visibility = Visibility.Hidden;
            SetCurrentMainMedia?.Invoke(null);
            
            // Очищаем текст на втором экране, если он там есть
            var secondaryWindow = GetSecondaryScreenWindow?.Invoke();
            if (secondaryWindow?.Content is Grid secondaryGrid)
            {
                var secondaryTextBlocks = secondaryGrid.Children.OfType<TextBlock>().ToList();
                foreach (var textBlock in secondaryTextBlocks)
                {
                    secondaryGrid.Children.Remove(textBlock);
                }
            }
            
            // Обновляем подсветку кнопок
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
        
        private void HandleAudioMediaClick(string slotKey)
        {
            // Управляем аудио - безопасно получаем элемент
            if (_mediaStateService?.TryGetAudioSlot(slotKey, out var audioElement) == true && audioElement != null)
            {
                bool isAudioPaused = _mediaStateService.IsAudioPaused(slotKey);
                
                if (isAudioPaused)
                {
                    // Аудио на паузе - возобновляем
                    audioElement.Play();
                    _mediaStateService.SetAudioPaused(slotKey, false);
                }
                else
                {
                    // Аудио воспроизводится - ставим на паузу
                    audioElement.Pause();
                    _mediaStateService.SetAudioPaused(slotKey, true);
                }
                
                UpdateAllSlotButtonsHighlighting?.Invoke();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА: Аудио элемент не найден для слота {slotKey} в Slot_Click");
            }
        }
    }
}


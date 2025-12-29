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
    /// Сервис для управления воспроизведением медиа
    /// </summary>
    public class MediaPlayerService
    {
        // Зависимости от других сервисов
        private MediaStateService? _mediaStateService;
        private TransitionService? _transitionService;
        private SettingsManager? _settingsManager;
        private DeviceManager? _deviceManager;
        
        // Делегаты для доступа к UI элементам
        public Func<MediaElement>? GetMainMediaElement { get; set; }
        public Func<Border>? GetMediaBorder { get; set; }
        public Func<Grid>? GetTextOverlayGrid { get; set; }
        public Func<MediaElement>? GetSecondaryMediaElement { get; set; }
        public Func<Window>? GetSecondaryScreenWindow { get; set; }
        public Func<Grid>? GetMainContentGrid { get; set; }
        public Func<Dispatcher>? GetDispatcher { get; set; }
        
        // Делегаты для работы с состоянием
        public Func<string>? GetCurrentMainMedia { get; set; }
        public Action<string>? SetCurrentMainMedia { get; set; }
        public Func<string>? GetCurrentVisualContent { get; set; }
        public Action<string>? SetCurrentVisualContent { get; set; }
        public Func<string>? GetCurrentAudioContent { get; set; }
        public Action<string>? SetCurrentAudioContent { get; set; }
        public Func<bool>? GetUseUniformToFill { get; set; }
        public Func<bool>? GetIsVideoPaused { get; set; }
        public Action<bool>? SetIsVideoPaused { get; set; }
        public Action<bool>? SetIsVideoPlaying { get; set; }
        public Action<bool>? SetIsAudioPlaying { get; set; }
        
        // Делегаты для работы с позициями и медиа
        public Func<string, TimeSpan>? GetSlotPosition { get; set; }
        public Action<string, TimeSpan>? SaveSlotPosition { get; set; }
        public Action<string>? RegisterActiveMediaFile { get; set; }
        public Func<string, bool>? IsMediaFileAlreadyPlaying { get; set; }
        public Func<string, MediaType, string, bool>? ShouldBlockMediaFile { get; set; }
        
        // Делегаты для работы с аудио слотами
        public Func<string, MediaElement?>? TryGetAudioSlot { get; set; }
        public Func<string, Grid?>? TryGetAudioContainer { get; set; }
        public Action<string, MediaElement, Grid>? AddAudioSlot { get; set; }
        public Action<string>? StopActiveAudio { get; set; }
        
        // Делегаты для работы с текстом
        public Func<string, TextAlignment>? GetTextAlignment { get; set; }
        public Func<string, VerticalAlignment>? GetVerticalAlignment { get; set; }
        public Func<string, HorizontalAlignment>? GetHorizontalAlignment { get; set; }
        
        // Делегаты для работы с настройками и UI
        public Action? UpdateAllSlotButtonsHighlighting { get; set; }
        public Action<MediaSlot, string>? SelectElementForSettings { get; set; }
        public Action<MediaSlot, string>? ApplyElementSettings { get; set; }
        public Action? ApplyGlobalSettings { get; set; }
        public Func<GlobalSettings?>? GetGlobalSettings { get; set; }
        public Action<string>? AutoPlayNextAudioElement { get; set; }
        public Action? ConfigureAudioDevice { get; set; }
        
        // Делегаты для работы с вторичным экраном
        public Action<MediaElement>? SetSecondaryMediaElement { get; set; }
        
        // Методы для установки зависимостей
        public void SetMediaStateService(MediaStateService service)
        {
            _mediaStateService = service;
        }
        
        public void SetTransitionService(TransitionService service)
        {
            _transitionService = service;
        }
        
        public void SetSettingsManager(SettingsManager service)
        {
            _settingsManager = service;
        }
        
        public void SetDeviceManager(DeviceManager service)
        {
            _deviceManager = service;
        }
        
        /// <summary>
        /// Обновляет MediaElement, сохраняя текстовые блоки
        /// </summary>
        public void UpdateMediaElement(MediaElement mediaElement)
        {
            var mediaBorder = GetMediaBorder?.Invoke();
            var textOverlayGrid = GetTextOverlayGrid?.Invoke();
            
            if (mediaBorder?.Child is Grid mainGrid && textOverlayGrid != null)
            {
                // Устанавливаем правильный Stretch для медиа
                mediaElement.Stretch = Stretch.Uniform;
                mediaElement.HorizontalAlignment = HorizontalAlignment.Center;
                mediaElement.VerticalAlignment = VerticalAlignment.Center;
                
                // Удаляем старый MediaElement если есть
                var oldMediaElement = mainGrid.Children.OfType<MediaElement>().FirstOrDefault();
                if (oldMediaElement != null)
                {
                    mainGrid.Children.Remove(oldMediaElement);
                }
                
                // Удаляем старые Image элементы если есть
                var oldImages = mainGrid.Children.OfType<Image>().ToList();
                foreach (var oldImage in oldImages)
                {
                    mainGrid.Children.Remove(oldImage);
                }
                
                // Добавляем новый MediaElement в начало (под текстом)
                mainGrid.Children.Insert(0, mediaElement);
                
                // Убеждаемся, что textOverlayGrid остается в Grid
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
            else if (mediaBorder != null && textOverlayGrid != null)
            {
                // Если mediaBorder.Child не Grid, создаем новый Grid
                var newGrid = new Grid();
                mediaElement.Stretch = Stretch.Uniform;
                mediaElement.HorizontalAlignment = HorizontalAlignment.Center;
                mediaElement.VerticalAlignment = VerticalAlignment.Center;
                newGrid.Children.Add(mediaElement);
                newGrid.Children.Add(textOverlayGrid);
                mediaBorder.Child = newGrid;
            }
        }
        
        /// <summary>
        /// Создает Image элемент для отображения изображения
        /// </summary>
        public Image CreateImageElement(string imagePath)
        {
            var imageElement = new Image
            {
                Source = new BitmapImage(new Uri(imagePath)),
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Размер будет установлен после добавления в визуальное дерево через событие Loaded
            // Это предотвращает проблемы с ActualWidth/ActualHeight до добавления элемента
            
            return imageElement;
        }
        
        /// <summary>
        /// Заменяет MediaElement на Image в Border
        /// </summary>
        public void ReplaceMediaElementWithImage(string imagePath)
        {
            var mediaBorder = GetMediaBorder?.Invoke();
            var textOverlayGrid = GetTextOverlayGrid?.Invoke();
            
            if (mediaBorder == null || textOverlayGrid == null) return;
            
            var imageElement = CreateImageElement(imagePath);
            
            // Обновляем содержимое Grid, сохраняя textOverlayGrid
            if (mediaBorder.Child is Grid mainGrid)
            {
                // Устанавливаем ClipToBounds чтобы изображение не вылезало за рамки
                // Это основной способ ограничения размера, так как RenderTransform может увеличивать элемент
                mainGrid.ClipToBounds = true;
                
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
                
                // Обновляем размер изображения после добавления в визуальное дерево
                imageElement.Loaded += (s, e) =>
                {
                    if (mediaBorder.ActualWidth > 0 && mediaBorder.ActualHeight > 0)
                    {
                        imageElement.Width = mediaBorder.ActualWidth;
                        imageElement.Height = mediaBorder.ActualHeight;
                    }
                };
                
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
                // Если mediaBorder.Child не Grid, создаем новый Grid
                var newGrid = new Grid();
                // Устанавливаем ClipToBounds чтобы изображение не вылезало за рамки
                newGrid.ClipToBounds = true;
                
                // Обновляем размер изображения на основе фактического размера mediaBorder
                if (mediaBorder.ActualWidth > 0 && mediaBorder.ActualHeight > 0)
                {
                    imageElement.Width = mediaBorder.ActualWidth;
                    imageElement.Height = mediaBorder.ActualHeight;
                }
                
                newGrid.Children.Add(imageElement);
                newGrid.Children.Add(textOverlayGrid);
                mediaBorder.Child = newGrid;
                
                // Обновляем размер изображения после добавления в визуальное дерево
                imageElement.Loaded += (s, e) =>
                {
                    if (mediaBorder.ActualWidth > 0 && mediaBorder.ActualHeight > 0)
                    {
                        imageElement.Width = mediaBorder.ActualWidth;
                        imageElement.Height = mediaBorder.ActualHeight;
                    }
                };
                
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
        }
        
        /// <summary>
        /// Восстанавливает MediaElement в Border
        /// </summary>
        public void RestoreMediaElement(MediaElement mediaElement)
        {
            var mediaBorder = GetMediaBorder?.Invoke();
            if (mediaBorder?.Child != mediaElement)
            {
                UpdateMediaElement(mediaElement);
                mediaElement.Visibility = Visibility.Visible;
            }
        }
        
        /// <summary>
        /// Синхронизирует медиа со вторым экраном
        /// </summary>
        public void SyncMediaToSecondaryScreen(string mediaPath, MediaType mediaType)
        {
            var secondaryWindow = GetSecondaryScreenWindow?.Invoke();
            var useUniformToFill = GetUseUniformToFill?.Invoke() ?? false;
            
            if (secondaryWindow == null) return;
            
            if (mediaType == MediaType.Video)
            {
                var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
                if (secondaryMediaElement == null)
                {
                    // Создаем MediaElement для второго экрана если его нет
                    secondaryMediaElement = new MediaElement
                    {
                        LoadedBehavior = MediaState.Manual,
                        Stretch = useUniformToFill ? Stretch.UniformToFill : Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Margin = new Thickness(0),
                        Volume = 0 // Отключаем звук на втором экране
                    };
                }
                
                secondaryWindow.Content = secondaryMediaElement;
                secondaryMediaElement.Source = new Uri(mediaPath);
            }
            else if (mediaType == MediaType.Image)
            {
                var secondaryImageElement = new Image
                {
                    Source = new BitmapImage(new Uri(mediaPath)),
                    Stretch = useUniformToFill ? Stretch.UniformToFill : Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(0)
                };
                secondaryWindow.Content = secondaryImageElement;
            }
        }
        
        /// <summary>
        /// Останавливает воспроизведение медиа
        /// </summary>
        public void StopMedia()
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            if (mediaElement != null)
            {
                mediaElement.Stop();
                mediaElement.Source = null;
            }
            
            var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
            if (secondaryMediaElement != null)
            {
                secondaryMediaElement.Stop();
                secondaryMediaElement.Source = null;
            }
        }
        
        /// <summary>
        /// Приостанавливает воспроизведение медиа
        /// </summary>
        public void PauseMedia()
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            mediaElement?.Pause();
            
            var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
            secondaryMediaElement?.Pause();
        }
        
        /// <summary>
        /// Возобновляет воспроизведение медиа
        /// </summary>
        public void ResumeMedia()
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            mediaElement?.Play();
            
            var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
            secondaryMediaElement?.Play();
        }
        
        /// <summary>
        /// Загружает медиа из слота с поддержкой всех типов медиа
        /// </summary>
        public async Task LoadMediaFromSlotSelective(MediaSlot mediaSlot)
        {
            try
            {
                string slotKey = $"Slot_{mediaSlot.Column}_{mediaSlot.Row}";
                
                // Настраиваем аудиоустройство если нужно
                ConfigureAudioDevice?.Invoke();
                
                var mediaElement = GetMainMediaElement?.Invoke();
                var mediaBorder = GetMediaBorder?.Invoke();
                var textOverlayGrid = GetTextOverlayGrid?.Invoke();
                
                if (mediaElement == null || mediaBorder == null || textOverlayGrid == null) return;
                
                // Сохраняем позицию текущего медиа перед переключением на новый слот
                var currentMainMedia = GetCurrentMainMedia?.Invoke();
                if (currentMainMedia != null && mediaElement.Source != null)
                {
                    var currentPosition = mediaElement.Position;
                    var currentPath = mediaElement.Source.LocalPath;
                    
                    if (_mediaStateService != null)
                    {
                        _mediaStateService.SaveMediaResumePosition(currentPath, currentPosition);
                        System.Diagnostics.Debug.WriteLine($"СОХРАНЕНИЕ ПОЗИЦИИ LoadMediaFromSlotSelective: {currentPath} -> {currentPosition}");
                        
                        // Также сохраняем позицию для текущего слота
                        var slotPosition = GetSlotPosition?.Invoke(currentMainMedia);
                        if (slotPosition.HasValue)
                        {
                            SaveSlotPosition?.Invoke(currentMainMedia, slotPosition.Value);
                        }
                    }
                }
                
                // Сбрасываем состояние паузы при запуске нового медиа
                SetIsVideoPaused?.Invoke(false);
                if (_mediaStateService != null)
                {
                    _mediaStateService.ResetAudioPaused(slotKey);
                }
                
                // Проверяем, нужно ли блокировать запуск (только для аудио)
                if (ShouldBlockMediaFile != null && ShouldBlockMediaFile(mediaSlot.MediaPath, mediaSlot.Type, slotKey))
                {
                    MessageBox.Show($"Аудио файл уже воспроизводится в другом слоте или триггере. SlotKey: {slotKey}, CurrentAudio: {GetCurrentAudioContent?.Invoke()}, CurrentMain: {GetCurrentMainMedia?.Invoke()}", 
                        "Дублирование аудиофайла", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                switch (mediaSlot.Type)
                {
                    case MediaType.Video:
                        await LoadVideoFromSlot(mediaSlot, slotKey, mediaElement, mediaBorder, textOverlayGrid);
                        break;
                        
                    case MediaType.Image:
                        await LoadImageFromSlot(mediaSlot, slotKey, mediaBorder, textOverlayGrid);
                        break;
                        
                    case MediaType.Audio:
                        LoadAudioFromSlot(mediaSlot, slotKey);
                        break;
                        
                    case MediaType.Text:
                        LoadTextFromSlot(mediaSlot, slotKey, mediaBorder, textOverlayGrid);
                        break;
                }
                
                // Применяем настройки к только что загруженному элементу
                ApplyElementSettings?.Invoke(mediaSlot, slotKey);
                
                // Применяем общие настройки после загрузки медиа
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Вызываем ApplyGlobalSettings() после загрузки медиа, _currentMainMedia={GetCurrentMainMedia?.Invoke()}");
                ApplyGlobalSettings?.Invoke();
                
                // Дополнительно принудительно применяем общие настройки к основному медиа элементу
                var globalSettings = GetGlobalSettings?.Invoke();
                if (globalSettings != null && globalSettings.UseGlobalOpacity)
                {
                    var finalOpacity = globalSettings.GlobalOpacity;
                    System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Принудительно применяем прозрачность {finalOpacity} к основному медиа элементу");
                    
                    if (mediaSlot.Type == MediaType.Image && mediaBorder != null)
                    {
                        mediaBorder.Opacity = finalOpacity;
                        System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Применена прозрачность {finalOpacity} к mediaBorder для изображения");
                    }
                    else if (mediaSlot.Type == MediaType.Video && mediaElement != null)
                    {
                        mediaElement.Opacity = finalOpacity;
                        System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Применена прозрачность {finalOpacity} к mediaElement для видео");
                    }
                    else if (mediaSlot.Type == MediaType.Text && textOverlayGrid != null)
                    {
                        textOverlayGrid.Opacity = finalOpacity;
                        System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Применена прозрачность {finalOpacity} к textOverlayGrid для текста");
                    }
                }
                
                // Выбираем элемент для панели настроек
                SelectElementForSettings?.Invoke(mediaSlot, slotKey);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке медиа: {ex.Message}", "Ошибка");
            }
        }
        
        // Приватные вспомогательные методы для загрузки разных типов медиа
        private async Task LoadVideoFromSlot(MediaSlot mediaSlot, string slotKey, MediaElement mediaElement, Border mediaBorder, Grid textOverlayGrid)
        {
            SetCurrentVisualContent?.Invoke(slotKey);
            SetCurrentMainMedia?.Invoke(slotKey);
            
            // Принудительно применяем общие настройки прозрачности сразу после установки _currentMainMedia
            var globalSettings = GetGlobalSettings?.Invoke();
            if (globalSettings != null && globalSettings.UseGlobalOpacity)
            {
                var finalOpacity = globalSettings.GlobalOpacity;
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective (Video): Принудительно применяем прозрачность {finalOpacity} к mediaElement");
                mediaElement.Opacity = finalOpacity;
            }
            
            // Обновляем подсветку кнопок
            UpdateAllSlotButtonsHighlighting?.Invoke();
            
            // Применяем переход при смене медиа с поддержкой второго экрана
            if (_transitionService != null)
            {
                // Скрываем элемент перед переходом, чтобы избежать мигания
                mediaElement.Visibility = Visibility.Hidden;
                
                await _transitionService.ApplyTransition(
                    () =>
                    {
                        // Очищаем предыдущий источник внутри перехода
                        mediaElement.Stop();
                        mediaElement.Source = null;
                        
                        // Обновляем медиа, сохраняя текстовые блоки
                        UpdateMediaElement(mediaElement);
                        mediaElement.Source = new Uri(mediaSlot.MediaPath);
                        mediaElement.Visibility = Visibility.Visible;
                        RegisterActiveMediaFile?.Invoke(mediaSlot.MediaPath);
                    },
                    () => SyncVideoToSecondaryScreen(mediaSlot, slotKey)
                );
            }
            else
            {
                // Очищаем предыдущий источник перед загрузкой нового
                mediaElement.Stop();
                mediaElement.Source = null;
                
                UpdateMediaElement(mediaElement);
                mediaElement.Source = new Uri(mediaSlot.MediaPath);
                RegisterActiveMediaFile?.Invoke(mediaSlot.MediaPath);
                SyncVideoToSecondaryScreen(mediaSlot, slotKey);
            }
            
            // Возобновляем с сохраненной позиции слота
            var slotPosition = GetSlotPosition?.Invoke(slotKey) ?? TimeSpan.Zero;
            if (slotPosition > TimeSpan.Zero)
            {
                mediaElement.MediaOpened += (s, e) =>
                {
                    var duration = mediaElement.NaturalDuration;
                    if (duration.HasTimeSpan)
                    {
                        var remainingTime = duration.TimeSpan - slotPosition;
                        if (remainingTime.TotalSeconds < 0.4)
                        {
                            mediaElement.Position = TimeSpan.Zero;
                            var secondaryElement = GetSecondaryMediaElement?.Invoke();
                            if (secondaryElement != null)
                            {
                                secondaryElement.Position = TimeSpan.Zero;
                            }
                            System.Diagnostics.Debug.WriteLine($"ВИДЕО ЗАПУЩЕНО С НАЧАЛА (осталось {remainingTime.TotalSeconds:F2}с): {slotKey}");
                        }
                        else
                        {
                            mediaElement.Position = slotPosition;
                            var secondaryElement = GetSecondaryMediaElement?.Invoke();
                            if (secondaryElement != null)
                            {
                                secondaryElement.Position = slotPosition;
                            }
                            System.Diagnostics.Debug.WriteLine($"ВОССТАНОВЛЕНИЕ ПОЗИЦИИ ВИДЕО LoadMediaFromSlotSelective: {slotKey} -> {slotPosition}");
                        }
                    }
                    else
                    {
                        mediaElement.Position = slotPosition;
                        var secondaryElement = GetSecondaryMediaElement?.Invoke();
                        if (secondaryElement != null)
                        {
                            secondaryElement.Position = slotPosition;
                        }
                        System.Diagnostics.Debug.WriteLine($"ВОССТАНОВЛЕНИЕ ПОЗИЦИИ ВИДЕО LoadMediaFromSlotSelective: {slotKey} -> {slotPosition}");
                    }
                };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"НЕТ СОХРАНЕННОЙ ПОЗИЦИИ ВИДЕО LoadMediaFromSlotSelective: {slotKey}");
            }
            
            mediaElement.Play();
            
            // Синхронизируем воспроизведение с дополнительным экраном
            var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
            if (secondaryMediaElement != null)
            {
                secondaryMediaElement.Play();
                System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ВОСПРОИЗВЕДЕНИЯ: Запущено воспроизведение на дополнительном экране");
            }
            
            SetIsVideoPlaying?.Invoke(true);
        }
        
        private void SyncVideoToSecondaryScreen(MediaSlot mediaSlot, string slotKey)
        {
            var secondaryWindow = GetSecondaryScreenWindow?.Invoke();
            if (secondaryWindow == null) return;
            
            var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
            var useUniformToFill = GetUseUniformToFill?.Invoke() ?? false;
            
            if (secondaryMediaElement == null)
            {
                secondaryMediaElement = new MediaElement
                {
                    LoadedBehavior = MediaState.Manual,
                    Stretch = useUniformToFill ? Stretch.UniformToFill : Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(0),
                    Volume = 0
                };
                SetSecondaryMediaElement?.Invoke(secondaryMediaElement);
            }
            
            secondaryWindow.Content = secondaryMediaElement;
            secondaryMediaElement.Source = new Uri(mediaSlot.MediaPath);
            
            var slotPosition = GetSlotPosition?.Invoke(slotKey) ?? TimeSpan.Zero;
            secondaryMediaElement.MediaOpened += (s, e) =>
            {
                if (slotPosition > TimeSpan.Zero)
                {
                    var duration = secondaryMediaElement.NaturalDuration;
                    if (duration.HasTimeSpan)
                    {
                        var remainingTime = duration.TimeSpan - slotPosition;
                        if (remainingTime.TotalSeconds < 0.4)
                        {
                            secondaryMediaElement.Position = TimeSpan.Zero;
                        }
                        else
                        {
                            secondaryMediaElement.Position = slotPosition;
                        }
                    }
                    else
                    {
                        secondaryMediaElement.Position = slotPosition;
                    }
                }
                secondaryMediaElement.Play();
            };
            
            System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ МЕДИА: Передан файл {mediaSlot.MediaPath} на дополнительный экран");
        }
        
        private async Task LoadImageFromSlot(MediaSlot mediaSlot, string slotKey, Border mediaBorder, Grid textOverlayGrid)
        {
            SetCurrentVisualContent?.Invoke(slotKey);
            SetCurrentMainMedia?.Invoke(slotKey);
            
            // Принудительно применяем общие настройки прозрачности
            var globalSettings = GetGlobalSettings?.Invoke();
            if (globalSettings != null && globalSettings.UseGlobalOpacity)
            {
                var finalOpacity = globalSettings.GlobalOpacity;
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective (Image): Принудительно применяем прозрачность {finalOpacity} к mediaBorder");
                mediaBorder.Opacity = finalOpacity;
            }
            
            UpdateAllSlotButtonsHighlighting?.Invoke();
            
            // Применяем переход при смене на изображение с поддержкой второго экрана
            if (_transitionService != null)
            {
                await _transitionService.ApplyTransition(
                    () => ReplaceMediaElementWithImage(mediaSlot.MediaPath),
                    () => SyncImageToSecondaryScreen(mediaSlot)
                );
            }
            else
            {
                ReplaceMediaElementWithImage(mediaSlot.MediaPath);
                SyncImageToSecondaryScreen(mediaSlot);
            }
        }
        
        private void SyncImageToSecondaryScreen(MediaSlot mediaSlot)
        {
            var secondaryWindow = GetSecondaryScreenWindow?.Invoke();
            if (secondaryWindow == null) return;
            
            var useUniformToFill = GetUseUniformToFill?.Invoke() ?? false;
            var secondaryImageElement = new Image
            {
                Source = new BitmapImage(new Uri(mediaSlot.MediaPath)),
                Stretch = useUniformToFill ? Stretch.UniformToFill : Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0)
            };
            secondaryWindow.Content = secondaryImageElement;
            System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ИЗОБРАЖЕНИЯ: Передано изображение {mediaSlot.MediaPath} на дополнительный экран");
        }
        
        private void LoadAudioFromSlot(MediaSlot mediaSlot, string slotKey)
        {
            // Проверяем, играет ли уже это аудио в триггере
            bool audioAlreadyPlayingInTrigger = (IsMediaFileAlreadyPlaying?.Invoke(mediaSlot.MediaPath) ?? false) &&
                                              GetCurrentAudioContent?.Invoke() != null &&
                                              GetCurrentAudioContent.Invoke()!.StartsWith("Trigger_");
            
            if (!audioAlreadyPlayingInTrigger)
            {
                // Останавливаем предыдущее аудио при запуске нового
                StopActiveAudio?.Invoke(slotKey);
            }
            
            MediaElement? audioElement = null;
            Grid? tempGrid = null;
            
            if (audioAlreadyPlayingInTrigger)
            {
                // Используем существующий аудио элемент из триггера
                var currentAudioContent = GetCurrentAudioContent?.Invoke();
                if (currentAudioContent != null)
                {
                    audioElement = TryGetAudioSlot?.Invoke(currentAudioContent);
                    tempGrid = TryGetAudioContainer?.Invoke(currentAudioContent);
                    
                    // Обновляем текущий контент
                    SetCurrentAudioContent?.Invoke(slotKey);
                }
            }
            
            if (audioElement == null)
            {
                // Создаем новый аудио элемент
                audioElement = new MediaElement
                {
                    Source = new Uri(mediaSlot.MediaPath),
                    LoadedBehavior = MediaState.Manual
                };
                
                // Возобновляем с сохраненной позиции слота
                var slotPosition = GetSlotPosition?.Invoke(slotKey) ?? TimeSpan.Zero;
                if (slotPosition > TimeSpan.Zero)
                {
                    audioElement.MediaOpened += (s, e) =>
                    {
                        audioElement.Position = slotPosition;
                    };
                }
                
                // Добавляем автопереход для аудио
                var dispatcher = GetDispatcher?.Invoke();
                if (dispatcher != null && AutoPlayNextAudioElement != null)
                {
                    audioElement.MediaEnded += (s, e) =>
                    {
                        dispatcher.Invoke(() => AutoPlayNextAudioElement(slotKey));
                    };
                }
                
                tempGrid = new Grid { Visibility = Visibility.Hidden };
                tempGrid.Children.Add(audioElement);
                
                var mainContentGrid = GetMainContentGrid?.Invoke();
                if (mainContentGrid != null)
                {
                    mainContentGrid.Children.Add(tempGrid);
                }
                
                if (audioElement != null && tempGrid != null)
                {
                    AddAudioSlot?.Invoke(slotKey, audioElement, tempGrid);
                }
                
                SetCurrentAudioContent?.Invoke(slotKey);
                RegisterActiveMediaFile?.Invoke(mediaSlot.MediaPath);
                
                audioElement.Play();
            }
            
            SetIsAudioPlaying?.Invoke(true);
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
        
        private void LoadTextFromSlot(MediaSlot mediaSlot, string slotKey, Border mediaBorder, Grid textOverlayGrid)
        {
            SetCurrentMainMedia?.Invoke(slotKey);
            
            // Проверяем, есть ли уже текстовый элемент в textOverlayGrid
            var existingTextElement = textOverlayGrid.Children.OfType<TextBlock>().FirstOrDefault();
            
            if (existingTextElement == null)
            {
                // Создаем новый текстовый элемент
                var textElement = new TextBlock
                {
                    Text = mediaSlot.TextContent,
                    FontFamily = new FontFamily(mediaSlot.FontFamily),
                    FontSize = mediaSlot.FontSize,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.FontColor)),
                    Background = mediaSlot.BackgroundColor == "Transparent" ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.BackgroundColor)),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = GetTextAlignment?.Invoke(mediaSlot.TextPosition) ?? TextAlignment.Center,
                    VerticalAlignment = GetVerticalAlignment?.Invoke(mediaSlot.TextPosition) ?? VerticalAlignment.Center,
                    HorizontalAlignment = GetHorizontalAlignment?.Invoke(mediaSlot.TextPosition) ?? HorizontalAlignment.Center,
                    Width = 600,
                    Height = 400,
                    Padding = new Thickness(20)
                };
                
                // Применяем ручную настройку положения если включена
                if (mediaSlot.UseManualPosition)
                {
                    textElement.Margin = new Thickness(mediaSlot.TextX, mediaSlot.TextY, 0, 0);
                    textElement.HorizontalAlignment = HorizontalAlignment.Left;
                    textElement.VerticalAlignment = VerticalAlignment.Top;
                }
                
                // Добавляем текст поверх существующего контента
                if (mediaBorder.Child is Grid existingGrid)
                {
                    textElement.SetValue(Grid.ZIndexProperty, 10);
                    textOverlayGrid.Children.Add(textElement);
                    textOverlayGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    var grid = new Grid();
                    var existingChild = mediaBorder.Child;
                    
                    mediaBorder.Child = null;
                    
                    if (existingChild != null)
                    {
                        grid.Children.Add(existingChild);
                    }
                    
                    textElement.SetValue(Grid.ZIndexProperty, 10);
                    textOverlayGrid.Children.Add(textElement);
                    grid.Children.Add(textOverlayGrid);
                    mediaBorder.Child = grid;
                    textOverlayGrid.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Обновляем существующий текстовый элемент
                existingTextElement.Text = mediaSlot.TextContent;
                existingTextElement.FontFamily = new FontFamily(mediaSlot.FontFamily);
                existingTextElement.FontSize = mediaSlot.FontSize;
                existingTextElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.FontColor));
                existingTextElement.Background = mediaSlot.BackgroundColor == "Transparent" ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.BackgroundColor));
                existingTextElement.TextAlignment = GetTextAlignment?.Invoke(mediaSlot.TextPosition) ?? TextAlignment.Center;
                existingTextElement.VerticalAlignment = GetVerticalAlignment?.Invoke(mediaSlot.TextPosition) ?? VerticalAlignment.Center;
                existingTextElement.HorizontalAlignment = GetHorizontalAlignment?.Invoke(mediaSlot.TextPosition) ?? HorizontalAlignment.Center;
                
                if (mediaSlot.UseManualPosition)
                {
                    existingTextElement.Margin = new Thickness(mediaSlot.TextX, mediaSlot.TextY, 0, 0);
                    existingTextElement.HorizontalAlignment = HorizontalAlignment.Left;
                    existingTextElement.VerticalAlignment = VerticalAlignment.Top;
                }
                else
                {
                    existingTextElement.Margin = new Thickness(0);
                }
                
                textOverlayGrid.Visibility = Visibility.Visible;
            }
            
            // Синхронизируем с дополнительным экраном
            var secondaryWindow = GetSecondaryScreenWindow?.Invoke();
            if (secondaryWindow != null)
            {
                TextBlock? existingSecondaryTextElement = null;
                if (secondaryWindow.Content is Grid secondaryGrid)
                {
                    existingSecondaryTextElement = secondaryGrid.Children.OfType<TextBlock>().FirstOrDefault();
                }
                
                if (existingSecondaryTextElement == null)
                {
                    var secondaryTextElement = new TextBlock
                    {
                        Text = mediaSlot.TextContent,
                        FontFamily = new FontFamily(mediaSlot.FontFamily),
                        FontSize = mediaSlot.FontSize,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.FontColor)),
                        Background = mediaSlot.BackgroundColor == "Transparent" ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.BackgroundColor)),
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = GetTextAlignment?.Invoke(mediaSlot.TextPosition) ?? TextAlignment.Center,
                        VerticalAlignment = GetVerticalAlignment?.Invoke(mediaSlot.TextPosition) ?? VerticalAlignment.Center,
                        HorizontalAlignment = GetHorizontalAlignment?.Invoke(mediaSlot.TextPosition) ?? HorizontalAlignment.Center,
                        Padding = new Thickness(20)
                    };
                    
                    if (mediaSlot.UseManualPosition)
                    {
                        secondaryTextElement.Margin = new Thickness(mediaSlot.TextX, mediaSlot.TextY, 0, 0);
                        secondaryTextElement.HorizontalAlignment = HorizontalAlignment.Left;
                        secondaryTextElement.VerticalAlignment = VerticalAlignment.Top;
                    }
                    
                    if (secondaryWindow.Content is Grid existingSecondaryGrid)
                    {
                        secondaryTextElement.SetValue(Grid.ZIndexProperty, 10);
                        existingSecondaryGrid.Children.Add(secondaryTextElement);
                    }
                    else
                    {
                        var newSecondaryGrid = new Grid();
                        var existingContent = secondaryWindow.Content;
                        secondaryWindow.Content = null;
                        
                        if (existingContent is UIElement uiElement)
                        {
                            newSecondaryGrid.Children.Add(uiElement);
                        }
                        
                        secondaryTextElement.SetValue(Grid.ZIndexProperty, 10);
                        newSecondaryGrid.Children.Add(secondaryTextElement);
                        secondaryWindow.Content = newSecondaryGrid;
                    }
                }
                else
                {
                    existingSecondaryTextElement.Text = mediaSlot.TextContent;
                    existingSecondaryTextElement.FontFamily = new FontFamily(mediaSlot.FontFamily);
                    existingSecondaryTextElement.FontSize = mediaSlot.FontSize;
                    existingSecondaryTextElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.FontColor));
                    existingSecondaryTextElement.Background = mediaSlot.BackgroundColor == "Transparent" ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.BackgroundColor));
                    existingSecondaryTextElement.TextAlignment = GetTextAlignment?.Invoke(mediaSlot.TextPosition) ?? TextAlignment.Center;
                    existingSecondaryTextElement.VerticalAlignment = GetVerticalAlignment?.Invoke(mediaSlot.TextPosition) ?? VerticalAlignment.Center;
                    existingSecondaryTextElement.HorizontalAlignment = GetHorizontalAlignment?.Invoke(mediaSlot.TextPosition) ?? HorizontalAlignment.Center;
                    
                    if (mediaSlot.UseManualPosition)
                    {
                        existingSecondaryTextElement.Margin = new Thickness(mediaSlot.TextX, mediaSlot.TextY, 0, 0);
                        existingSecondaryTextElement.HorizontalAlignment = HorizontalAlignment.Left;
                        existingSecondaryTextElement.VerticalAlignment = VerticalAlignment.Top;
                    }
                    else
                    {
                        existingSecondaryTextElement.Margin = new Thickness(0);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ТЕКСТА: Передан текст '{mediaSlot.TextContent}' на дополнительный экран");
            }
            
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
    }
}


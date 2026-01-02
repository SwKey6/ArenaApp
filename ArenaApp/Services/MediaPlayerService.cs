using System;
using System.Collections.Generic;
using System.IO;
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
        public Func<MediaElement?>? GetSecondaryMediaElement { get; set; }
        public Func<Window?>? GetSecondaryScreenWindow { get; set; }
        public Func<Grid>? GetMainContentGrid { get; set; }
        public Func<Dispatcher>? GetDispatcher { get; set; }
        
        // Делегаты для работы с состоянием
        public Func<string?>? GetCurrentMainMedia { get; set; }
        public Action<string?>? SetCurrentMainMedia { get; set; }
        public Func<string?>? GetCurrentVisualContent { get; set; }
        public Action<string?>? SetCurrentVisualContent { get; set; }
        public Func<string?>? GetCurrentAudioContent { get; set; }
        public Action<string?>? SetCurrentAudioContent { get; set; }
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
        public Func<string, string>? GetAbsoluteMediaPath { get; set; }  // Получение абсолютного пути с учетом режима хранения

        // VLC видео (если задано — видео воспроизводим через LibVLC, а не через WPF MediaElement)
        public Action? VlcShowMainVideo { get; set; }
        public Action? VlcHideMainVideo { get; set; }
        public Action<string>? VlcLoadMainVideo { get; set; }          // absolute path
        public Func<bool>? VlcPlayMainVideo { get; set; }
        public Action? VlcStopMainVideo { get; set; }
        public Action<TimeSpan>? VlcSetMainVideoPosition { get; set; }
        public Func<TimeSpan>? VlcGetMainVideoDuration { get; set; }
        public Func<bool>? VlcHasMainVideo { get; set; }

        public Action<string>? VlcLoadSecondaryVideo { get; set; }     // absolute path
        public Func<bool>? VlcPlaySecondaryVideo { get; set; }
        public Action? VlcStopSecondaryVideo { get; set; }
        public Action<TimeSpan>? VlcSetSecondaryVideoPosition { get; set; }
        
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
        /// Создает URI для медиафайла с правильной обработкой путей
        /// </summary>
        private Uri CreateMediaUri(string mediaPath)
        {
            System.Diagnostics.Debug.WriteLine($"CreateMediaUri: Начало, mediaPath={mediaPath}");
            
            if (string.IsNullOrEmpty(mediaPath))
            {
                System.Diagnostics.Debug.WriteLine($"CreateMediaUri: ОШИБКА - путь пустой");
                throw new ArgumentException("Путь к медиафайлу пустой");
            }
            
            // Получаем абсолютный путь с учетом режима хранения
            string absolutePath = GetAbsoluteMediaPath?.Invoke(mediaPath) ?? mediaPath;
            System.Diagnostics.Debug.WriteLine($"CreateMediaUri: После GetAbsoluteMediaPath, absolutePath={absolutePath}");
            
            // Нормализуем путь
            try
            {
                absolutePath = Path.GetFullPath(absolutePath);
                System.Diagnostics.Debug.WriteLine($"CreateMediaUri: После нормализации, absolutePath={absolutePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА при нормализации пути: {ex.Message}, Path={absolutePath}");
                throw new ArgumentException($"Некорректный путь: {absolutePath}", ex);
            }
            
            // Проверяем существование файла
            if (!File.Exists(absolutePath))
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА: Файл не существует: {absolutePath}");
                throw new FileNotFoundException($"Файл не найден: {absolutePath}");
            }
            
            // Создаем URI: для файловой системы .NET сам корректно формирует file:/// и экранирует пробелы и т.п.
            try
            {
                var uri = new Uri(absolutePath, UriKind.Absolute);
                System.Diagnostics.Debug.WriteLine($"CreateMediaUri: URI создан успешно: {uri}");
                return uri;
            }
            catch (UriFormatException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateMediaUri: ОШИБКА создания URI: {ex.Message}, Path={absolutePath}");
                throw new ArgumentException($"Невозможно создать URI из пути: {absolutePath}", ex);
            }
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
                // КРИТИЧНО: НЕ трогаем MediaElement если он уже правильно настроен в XAML
                // Манипуляции с элементом в визуальном дереве могут нарушить рендеринг
                bool mediaElementInGrid = mainGrid.Children.Contains(mediaElement);
                
                // Проверяем, нужно ли что-то делать
                if (!mediaElementInGrid)
                {
                    // MediaElement НЕ в Grid - это проблема, нужно добавить
                    // Удаляем старые MediaElement и Image элементы
                    var oldMediaElements = mainGrid.Children.OfType<MediaElement>().ToList();
                    foreach (var old in oldMediaElements)
                    {
                        mainGrid.Children.Remove(old);
                    }
                    
                    var oldImages = mainGrid.Children.OfType<Image>().ToList();
                    foreach (var oldImage in oldImages)
                    {
                        mainGrid.Children.Remove(oldImage);
                    }
                    
                    // Добавляем MediaElement
                    mainGrid.Children.Insert(0, mediaElement);
                    System.Diagnostics.Debug.WriteLine($"UpdateMediaElement: MediaElement добавлен в Grid");
                }
                
                // КРИТИЧНО: НЕ трогаем Width, Height, HorizontalAlignment, VerticalAlignment!
                // Они уже правильно установлены в XAML. Любые изменения могут нарушить layout.
                // Устанавливаем только видимость и прозрачность
                mediaElement.Visibility = Visibility.Visible;
                
                // Убеждаемся, что прозрачность правильная
                if (mediaElement.Opacity <= 0)
                {
                    mediaElement.Opacity = 1.0;
                }
                
                mediaBorder.Visibility = Visibility.Visible;
                mediaBorder.Opacity = 1.0;
                
                System.Diagnostics.Debug.WriteLine($"UpdateMediaElement: MediaElement - Visibility={mediaElement.Visibility}, Opacity={mediaElement.Opacity}. НЕ трогаем размеры и alignment!");
                
                // Убеждаемся, что textOverlayGrid в Grid
                if (!mainGrid.Children.Contains(textOverlayGrid))
                {
                    mainGrid.Children.Add(textOverlayGrid);
                    System.Diagnostics.Debug.WriteLine($"UpdateMediaElement: textOverlayGrid добавлен в Grid");
                }
                
                // Убеждаемся, что textOverlayGrid полностью прозрачен и не перекрывает видео
                textOverlayGrid.Background = new SolidColorBrush(Colors.Transparent);
                textOverlayGrid.IsHitTestVisible = false;
                
                // Делаем textOverlayGrid невидимым если в нем нет текста
                if (textOverlayGrid.Children.Count == 0)
                {
                    textOverlayGrid.Visibility = Visibility.Hidden;
                }
                else
                {
                    textOverlayGrid.Visibility = Visibility.Visible;
                }
                
                System.Diagnostics.Debug.WriteLine($"UpdateMediaElement: Завершено. MediaElement.ActualWidth={mediaElement.ActualWidth}, ActualHeight={mediaElement.ActualHeight}");
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
            // Используем CreateMediaUri для правильной обработки путей
            Uri imageUri = CreateMediaUri(imagePath);
            
            var imageElement = new Image
            {
                Source = new BitmapImage(imageUri),
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
                secondaryMediaElement.Source = CreateMediaUri(mediaPath);
            }
            else if (mediaType == MediaType.Image)
            {
                var secondaryImageElement = new Image
                {
                    Source = new BitmapImage(CreateMediaUri(mediaPath)),
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
                System.Diagnostics.Debug.WriteLine($"=== LoadMediaFromSlotSelective НАЧАЛО ===");
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Проверка 1 - метод начался");
                System.Diagnostics.Debug.WriteLine($"Type={mediaSlot.Type}, Path={mediaSlot.MediaPath}, Column={mediaSlot.Column}, Row={mediaSlot.Row}");
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Проверка 2 - параметры выведены");
                
                string slotKey = $"Slot_{mediaSlot.Column}_{mediaSlot.Row}";
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: slotKey={slotKey}");
                
                // Настраиваем аудиоустройство если нужно
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Вызываем ConfigureAudioDevice");
                ConfigureAudioDevice?.Invoke();
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: ConfigureAudioDevice завершен");
                
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Получаем элементы UI");
                var mediaElement = GetMainMediaElement?.Invoke();
                var mediaBorder = GetMediaBorder?.Invoke();
                var textOverlayGrid = GetTextOverlayGrid?.Invoke();
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Элементы получены - mediaElement={mediaElement != null}, mediaBorder={mediaBorder != null}, textOverlayGrid={textOverlayGrid != null}");
                
                if (mediaElement == null || mediaBorder == null || textOverlayGrid == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ОШИБКА: Элементы null - mediaElement={mediaElement == null}, mediaBorder={mediaBorder == null}, textOverlayGrid={textOverlayGrid == null}");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"Элементы получены: mediaElement.Visibility={mediaElement.Visibility}, Opacity={mediaElement.Opacity}, Source={mediaElement.Source?.LocalPath}");
                System.Diagnostics.Debug.WriteLine($"mediaBorder.Visibility={mediaBorder.Visibility}, Child={mediaBorder.Child?.GetType().Name}");
                
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
                        System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Загружаем ВИДЕО, slotKey={slotKey}");
                        try
                        {
                            await LoadVideoFromSlot(mediaSlot, slotKey, mediaElement, mediaBorder, textOverlayGrid);
                            System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: ВИДЕО загружено, mediaElement.Visibility={mediaElement.Visibility}, mediaElement.Opacity={mediaElement.Opacity}, Source={mediaElement.Source?.LocalPath}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: ОШИБКА в LoadVideoFromSlot - {ex.GetType().Name}: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: StackTrace - {ex.StackTrace}");
                            throw;
                        }
                        // Для видео настройки применяются внутри LoadVideoFromSlot после загрузки
                        break;
                        
                    case MediaType.Image:
                        await LoadImageFromSlot(mediaSlot, slotKey, mediaBorder, textOverlayGrid);
                        // Применяем настройки к изображению
                        ApplyElementSettings?.Invoke(mediaSlot, slotKey);
                        break;
                        
                    case MediaType.Audio:
                        LoadAudioFromSlot(mediaSlot, slotKey);
                        // Применяем настройки к аудио
                        ApplyElementSettings?.Invoke(mediaSlot, slotKey);
                        break;
                        
                    case MediaType.Text:
                        LoadTextFromSlot(mediaSlot, slotKey, mediaBorder, textOverlayGrid);
                        // Применяем настройки к тексту
                        ApplyElementSettings?.Invoke(mediaSlot, slotKey);
                        break;
                }
                
                // Применяем общие настройки после загрузки медиа
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: Вызываем ApplyGlobalSettings() после загрузки медиа, _currentMainMedia={GetCurrentMainMedia?.Invoke()}");
                ApplyGlobalSettings?.Invoke();
                
                // Выбираем элемент для панели настроек
                SelectElementForSettings?.Invoke(mediaSlot, slotKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: ИСКЛЮЧЕНИЕ - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective: StackTrace - {ex.StackTrace}");
                MessageBox.Show($"Ошибка при загрузке медиа: {ex.Message}", "Ошибка");
            }
        }
        
        // Приватные вспомогательные методы для загрузки разных типов медиа
        private async Task LoadVideoFromSlot(MediaSlot mediaSlot, string slotKey, MediaElement mediaElement, Border mediaBorder, Grid textOverlayGrid)
        {
            System.Diagnostics.Debug.WriteLine($"=== LoadVideoFromSlot НАЧАЛО ===");
            System.Diagnostics.Debug.WriteLine($"slotKey={slotKey}, Path={mediaSlot.MediaPath}");
            
            SetCurrentVisualContent?.Invoke(slotKey);
            SetCurrentMainMedia?.Invoke(slotKey);
            
            // ВАЖНО: Устанавливаем видимость и прозрачность сразу
            mediaBorder.Visibility = Visibility.Visible;
            mediaBorder.Opacity = 1.0; // Border всегда непрозрачен
            mediaElement.Visibility = Visibility.Visible;
            
            // ВАЖНО: Убеждаемся, что textOverlayGrid прозрачен и не перекрывает видео
            if (textOverlayGrid != null)
            {
                textOverlayGrid.Background = new SolidColorBrush(Colors.Transparent);
                textOverlayGrid.Opacity = 1.0; // Прозрачность текста управляется отдельно
                // КРИТИЧНО: Убеждаемся, что textOverlayGrid не перехватывает события мыши и не перекрывает видео
                textOverlayGrid.IsHitTestVisible = false; // Позволяет событиям проходить сквозь к mediaElement
            }
            
            System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: Установлена видимость - mediaBorder.Visibility={mediaBorder.Visibility}, mediaElement.Visibility={mediaElement.Visibility}");
            
            // Принудительно применяем общие настройки прозрачности сразу после установки _currentMainMedia
            var globalSettings = GetGlobalSettings?.Invoke();
            if (globalSettings != null && globalSettings.UseGlobalOpacity)
            {
                var finalOpacity = globalSettings.GlobalOpacity;
                // Убеждаемся, что прозрачность не равна 0
                if (finalOpacity <= 0)
                {
                    finalOpacity = 1.0;
                    System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: Глобальная прозрачность была 0, устанавливаем 1.0");
                }
                System.Diagnostics.Debug.WriteLine($"LoadMediaFromSlotSelective (Video): Принудительно применяем прозрачность {finalOpacity} к mediaElement");
                mediaElement.Opacity = finalOpacity;
            }
            else
            {
                // Если глобальная прозрачность не используется, убеждаемся, что прозрачность не равна 0
                if (mediaElement.Opacity <= 0)
                {
                    mediaElement.Opacity = 1.0;
                    System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: Прозрачность mediaElement была 0, устанавливаем 1.0");
                }
            }
            
            // Обновляем подсветку кнопок
            UpdateAllSlotButtonsHighlighting?.Invoke();

            // -------------------------
            // VLC PATH: воспроизводим видео через LibVLC (без MediaElement)
            // -------------------------
            if (VlcLoadMainVideo != null && VlcPlayMainVideo != null)
            {
                // Прячем legacy MediaElement
                try
                {
                    mediaElement.Stop();
                    mediaElement.Source = null;
                    mediaElement.Visibility = Visibility.Collapsed;
                }
                catch { /* ignore */ }

                // Показываем VLC рендер
                VlcShowMainVideo?.Invoke();

                // Получаем абсолютный путь с учетом режима хранения
                string absolutePath = GetAbsoluteMediaPath?.Invoke(mediaSlot.MediaPath) ?? mediaSlot.MediaPath;
                absolutePath = Path.GetFullPath(absolutePath);
                if (!File.Exists(absolutePath))
                {
                    throw new FileNotFoundException($"Файл не найден: {absolutePath}", absolutePath);
                }

                // Загружаем и запускаем
                VlcLoadMainVideo(absolutePath);
                VlcPlayMainVideo();

                // Восстанавливаем позицию слота, если есть
                var vlcSlotPosition = GetSlotPosition?.Invoke(slotKey) ?? TimeSpan.Zero;
                if (vlcSlotPosition > TimeSpan.Zero)
                {
                    VlcSetMainVideoPosition?.Invoke(vlcSlotPosition);
                    VlcSetSecondaryVideoPosition?.Invoke(vlcSlotPosition);
                }

                // Второй экран (если открыт) — пробуем запустить тоже
                var secondaryWindow = GetSecondaryScreenWindow?.Invoke();
                if (secondaryWindow != null && VlcLoadSecondaryVideo != null && VlcPlaySecondaryVideo != null)
                {
                    VlcLoadSecondaryVideo(absolutePath);
                    VlcPlaySecondaryVideo();
                }

                RegisterActiveMediaFile?.Invoke(absolutePath);
                System.Diagnostics.Debug.WriteLine($"VLC: Видео запущено: {absolutePath}");
                return;
            }
            
            // Применяем переход при смене медиа с поддержкой второго экрана
            if (_transitionService != null)
            {
                // НЕ скрываем элемент - оставляем видимым для плавного перехода
                // mediaElement.Visibility = Visibility.Visible; // Уже установлено выше
                
                await _transitionService.ApplyTransition(
                    () =>
                    {
                        // КРИТИЧНО: Проверяем, что mediaElement не null и сохраняем в локальную переменную
                        var element = mediaElement;
                        if (element == null) return;
                        
                        // Очищаем предыдущий источник внутри перехода
                        element.Stop();
                        element.Source = null;
                        
                        // ВАЖНО: Устанавливаем LoadedBehavior ПЕРЕД установкой Source
                        element.LoadedBehavior = MediaState.Manual;
                        
                        // НЕ вызываем UpdateMediaElement - он нарушает layout!
                        // MediaElement уже правильно настроен в XAML
                        
                        // Получаем абсолютный путь с учетом режима хранения
                        string absolutePath = GetAbsoluteMediaPath?.Invoke(mediaSlot.MediaPath) ?? mediaSlot.MediaPath;
                        
                        // Проверяем и нормализуем путь
                        if (string.IsNullOrEmpty(absolutePath))
                        {
                            System.Diagnostics.Debug.WriteLine($"ОШИБКА: Путь пустой для MediaPath={mediaSlot.MediaPath}");
                            throw new ArgumentException($"Путь к медиафайлу пустой");
                        }
                        
                        // Нормализуем путь
                        try
                        {
                            absolutePath = Path.GetFullPath(absolutePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ОШИБКА при нормализации пути: {ex.Message}, Path={absolutePath}");
                            throw new ArgumentException($"Некорректный путь: {absolutePath}", ex);
                        }
                        
                        // Устанавливаем Source с проверкой пути
                        if (!File.Exists(absolutePath))
                        {
                            System.Diagnostics.Debug.WriteLine($"ОШИБКА: Файл не существует: {absolutePath}");
                            throw new FileNotFoundException($"Файл не найден: {absolutePath}");
                        }
                        
                        // Создаем URI с правильным форматом
                        Uri uri;
                        try
                        {
                            // Используем file:/// для локальных файлов
                            if (!absolutePath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                            {
                                uri = new Uri(absolutePath, UriKind.Absolute);
                            }
                            else
                            {
                                uri = new Uri(absolutePath);
                            }
                        }
                        catch (UriFormatException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ОШИБКА создания URI: {ex.Message}, Path={absolutePath}");
                            // Пробуем через file:///
                            uri = new Uri(new Uri("file:///"), absolutePath);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: Устанавливаем Source: {uri}");
                        element.Source = uri;
                        
                        // Устанавливаем только видимость и прозрачность
                        element.Visibility = Visibility.Visible;
                        element.Opacity = 1.0;
                        
                        System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (Transition): MediaElement - Visibility={element.Visibility}, Opacity={element.Opacity}. Grid размер {mediaBorder?.ActualWidth}x{mediaBorder?.ActualHeight}");
                        
                        if (mediaBorder != null)
                        {
                            mediaBorder.Visibility = Visibility.Visible;
                            mediaBorder.Opacity = 1.0;
                        }
                        
                        // КРИТИЧНО: Убеждаемся, что textOverlayGrid не перекрывает видео
                        if (textOverlayGrid != null)
                        {
                            textOverlayGrid.Background = new SolidColorBrush(Colors.Transparent);
                            textOverlayGrid.IsHitTestVisible = false;
                            if (textOverlayGrid.Children.Count == 0)
                            {
                                textOverlayGrid.Visibility = Visibility.Collapsed;
                            }
                        }
                        
                        RegisterActiveMediaFile?.Invoke(mediaSlot.MediaPath);
                        
                        System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (Transition): mediaElement.Visibility={element.Visibility}, Opacity={element.Opacity}, Source={element.Source?.LocalPath}");
                        System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (Transition): mediaElement.Width={element.Width}, Height={element.Height}, Stretch={element.Stretch}");
                        
                        // НЕ вызываем Play() здесь - он будет вызван после завершения перехода
                    },
                    () => SyncVideoToSecondaryScreen(mediaSlot, slotKey)
                );
                
                // КРИТИЧНО: Play() теперь вызывается в MainWindow.xaml.cs после загрузки
                // Это решает проблему с нулевым размером MediaElement
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (After Transition): Загрузка завершена, Play() будет вызван в MainWindow.xaml.cs");
            }
            else
            {
                // Очищаем предыдущий источник перед загрузкой нового
                mediaElement.Stop();
                mediaElement.Source = null;
                
                // ВАЖНО: Устанавливаем LoadedBehavior ПЕРЕД установкой Source
                mediaElement.LoadedBehavior = MediaState.Manual;
                
                // НЕ вызываем UpdateMediaElement - он нарушает layout!
                // MediaElement уже правильно настроен в XAML
                
                // Получаем абсолютный путь с учетом режима хранения
                string absolutePath = GetAbsoluteMediaPath?.Invoke(mediaSlot.MediaPath) ?? mediaSlot.MediaPath;
                
                // Проверяем и нормализуем путь
                if (string.IsNullOrEmpty(absolutePath))
                {
                    System.Diagnostics.Debug.WriteLine($"ОШИБКА: Путь пустой для MediaPath={mediaSlot.MediaPath}");
                    throw new ArgumentException($"Путь к медиафайлу пустой");
                }
                
                // Нормализуем путь
                try
                {
                    absolutePath = Path.GetFullPath(absolutePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ОШИБКА при нормализации пути: {ex.Message}, Path={absolutePath}");
                    throw new ArgumentException($"Некорректный путь: {absolutePath}", ex);
                }
                
                // Устанавливаем Source с проверкой пути
                if (!File.Exists(absolutePath))
                {
                    System.Diagnostics.Debug.WriteLine($"ОШИБКА: Файл не существует: {absolutePath}");
                    throw new FileNotFoundException($"Файл не найден: {absolutePath}");
                }
                
                // Создаем URI с правильным форматом
                Uri uri;
                try
                {
                    uri = new Uri(absolutePath, UriKind.Absolute);
                }
                catch (UriFormatException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ОШИБКА создания URI: {ex.Message}, Path={absolutePath}");
                    // Пробуем через file:///
                    uri = new Uri(new Uri("file:///"), absolutePath);
                }
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: Устанавливаем Source: {uri}");
                mediaElement.Source = uri;
                
                // Устанавливаем только видимость и прозрачность
                mediaElement.Visibility = Visibility.Visible;
                mediaElement.Opacity = 1.0;
                
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (No Transition): MediaElement - Visibility={mediaElement.Visibility}, Opacity={mediaElement.Opacity}. Grid размер {mediaBorder?.ActualWidth}x{mediaBorder?.ActualHeight}");
                
                if (mediaBorder != null)
                {
                    mediaBorder.Visibility = Visibility.Visible;
                    mediaBorder.Opacity = 1.0;
                }
                
                // КРИТИЧНО: Убеждаемся, что textOverlayGrid не перекрывает видео
                if (textOverlayGrid != null)
                {
                    textOverlayGrid.Background = new SolidColorBrush(Colors.Transparent);
                    textOverlayGrid.IsHitTestVisible = false;
                    if (textOverlayGrid.Children.Count == 0)
                    {
                        textOverlayGrid.Visibility = Visibility.Collapsed;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (No Transition): mediaElement.Visibility={mediaElement.Visibility}, Opacity={mediaElement.Opacity}, Source={mediaElement.Source?.LocalPath}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (No Transition): mediaElement.Width={mediaElement.Width}, Height={mediaElement.Height}, Stretch={mediaElement.Stretch}");
                
                RegisterActiveMediaFile?.Invoke(mediaSlot.MediaPath);
                SyncVideoToSecondaryScreen(mediaSlot, slotKey);
                
                // КРИТИЧНО: Play() теперь вызывается в MainWindow.xaml.cs после загрузки
                // Это решает проблему с нулевым размером MediaElement
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (No Transition): Загрузка завершена, Play() будет вызван в MainWindow.xaml.cs");
            }
            
            // Возобновляем с сохраненной позиции слота и запускаем воспроизведение
            var slotPosition = GetSlotPosition?.Invoke(slotKey) ?? TimeSpan.Zero;
            
            RoutedEventHandler? mediaOpenedHandler = null;
            mediaOpenedHandler = (s, e) =>
            {
                // Отписываемся от события, чтобы избежать повторных вызовов
                mediaElement.MediaOpened -= mediaOpenedHandler;
                
                // ВАЖНО: Убеждаемся, что все элементы видимы и непрозрачны
                mediaElement.Visibility = Visibility.Visible;
                if (mediaBorder != null)
                {
                    mediaBorder.Visibility = Visibility.Visible;
                    mediaBorder.Opacity = 1.0;
                }
                
                // ВАЖНО: Убеждаемся, что textOverlayGrid прозрачен и не перекрывает видео
                if (textOverlayGrid != null)
                {
                    textOverlayGrid.Background = new SolidColorBrush(Colors.Transparent);
                    textOverlayGrid.Opacity = 1.0; // Прозрачность текста управляется отдельно
                    // КРИТИЧНО: Убеждаемся, что textOverlayGrid не перехватывает события мыши и не перекрывает видео
                    textOverlayGrid.IsHitTestVisible = false; // Позволяет событиям проходить сквозь к mediaElement
                }
                
                // КРИТИЧНО: Проверяем размеры и позицию элементов
                var mainGrid = mediaBorder?.Child as Grid;
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaElement.Visibility={mediaElement.Visibility}, Opacity={mediaElement.Opacity}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaBorder.Visibility={mediaBorder?.Visibility}, textOverlayGrid.Background={textOverlayGrid?.Background}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaElement.Width={mediaElement.Width}, Height={mediaElement.Height}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaElement.ActualWidth={mediaElement.ActualWidth}, ActualHeight={mediaElement.ActualHeight}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaElement.HorizontalAlignment={mediaElement.HorizontalAlignment}, VerticalAlignment={mediaElement.VerticalAlignment}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaElement.Stretch={mediaElement.Stretch}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaElement.NaturalVideoWidth={mediaElement.NaturalVideoWidth}, NaturalVideoHeight={mediaElement.NaturalVideoHeight}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaBorder.ActualWidth={mediaBorder?.ActualWidth}, ActualHeight={mediaBorder?.ActualHeight}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mainGrid.ActualWidth={mainGrid?.ActualWidth}, ActualHeight={mainGrid?.ActualHeight}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaElement в Grid={mainGrid?.Children.Contains(mediaElement) ?? false}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): Индекс mediaElement в Grid={mainGrid?.Children.IndexOf(mediaElement) ?? -1}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): Индекс textOverlayGrid в Grid={mainGrid?.Children.IndexOf(textOverlayGrid) ?? -1}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): Всего детей в Grid={mainGrid?.Children.Count ?? 0}");
                if (mainGrid != null)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): Дети Grid: {string.Join(", ", mainGrid.Children.Cast<UIElement>().Select(c => $"{c.GetType().Name}(Z={mainGrid.Children.IndexOf(c)})"))}");
                }
                
                // КРИТИЧНО: В обработчике MediaOpened НЕ трогаем размеры и alignment!
                // XAML уже правильно настроил MediaElement, любые изменения здесь нарушают layout
                
                // Устанавливаем только видимость и прозрачность
                mediaElement.Visibility = Visibility.Visible;
                if (mediaElement.Opacity <= 0)
                {
                    mediaElement.Opacity = 1.0;
                    System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: Прозрачность mediaElement была 0 в MediaOpened, устанавливаем 1.0");
                }
                
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): MediaElement - НЕ изменяем свойства, оставляем как в XAML");
                
                // Проверяем размеры для диагностики ПОСЛЕ UpdateLayout
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaElement - ActualWidth={mediaElement.ActualWidth}, ActualHeight={mediaElement.ActualHeight}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaElement - Width={mediaElement.Width}, Height={mediaElement.Height}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaElement - Visibility={mediaElement.Visibility}, Opacity={mediaElement.Opacity}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mediaBorder - ActualWidth={mediaBorder?.ActualWidth}, ActualHeight={mediaBorder?.ActualHeight}, Visibility={mediaBorder?.Visibility}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): mainGrid - ActualWidth={mainGrid?.ActualWidth}, ActualHeight={mainGrid?.ActualHeight}");
                
                // НЕ перемещаем элементы - это нарушает layout
                // Убеждаемся, что textOverlayGrid полностью прозрачен
                if (textOverlayGrid != null)
                {
                    textOverlayGrid.Background = new SolidColorBrush(Colors.Transparent);
                    textOverlayGrid.IsHitTestVisible = false;
                    
                    // Если в textOverlayGrid нет детей, скрываем его
                    if (textOverlayGrid.Children.Count == 0)
                    {
                        textOverlayGrid.Visibility = Visibility.Collapsed;
                    }
                }
                
                if (slotPosition > TimeSpan.Zero)
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
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"НЕТ СОХРАНЕННОЙ ПОЗИЦИИ ВИДЕО LoadMediaFromSlotSelective: {slotKey}");
                }
                
                // Убеждаемся, что элементы видимы
                mediaElement.Visibility = Visibility.Visible;
                if (mediaBorder != null)
                {
                    mediaBorder.Visibility = Visibility.Visible;
                }
                
                // Убеждаемся, что прозрачность не равна 0
                var currentOpacity = mediaElement.Opacity;
                if (currentOpacity <= 0)
                {
                    mediaElement.Opacity = 1.0;
                    System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ LoadVideoFromSlot: Прозрачность была {currentOpacity}, устанавливаем 1.0");
                }
                
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): Устанавливаем позицию и запускаем, Source={mediaElement.Source?.LocalPath}, Opacity={mediaElement.Opacity}, Visibility={mediaElement.Visibility}");
                
                // КРИТИЧНО: Play() теперь вызывается в MainWindow.xaml.cs после загрузки
                // Это решает проблему с нулевым размером MediaElement
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot (MediaOpened): Загрузка завершена, Play() будет вызван в MainWindow.xaml.cs");
            };
            
            mediaElement.MediaOpened += mediaOpenedHandler;
            
            // Если медиа уже загружено, запускаем сразу
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                mediaElement.Visibility = Visibility.Visible;
                if (mediaBorder != null)
                {
                    mediaBorder.Visibility = Visibility.Visible;
                }
                
                // Убеждаемся, что прозрачность не равна 0
                var currentOpacity = mediaElement.Opacity;
                if (currentOpacity <= 0)
                {
                    mediaElement.Opacity = 1.0;
                    System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ LoadVideoFromSlot (уже загружено): Прозрачность была {currentOpacity}, устанавливаем 1.0");
                }
                
                if (slotPosition > TimeSpan.Zero)
                {
                    var duration = mediaElement.NaturalDuration;
                    if (duration.HasTimeSpan)
                    {
                        var remainingTime = duration.TimeSpan - slotPosition;
                        if (remainingTime.TotalSeconds < 0.4)
                        {
                            mediaElement.Position = TimeSpan.Zero;
                        }
                        else
                        {
                            mediaElement.Position = slotPosition;
                        }
                    }
                    else
                    {
                        mediaElement.Position = slotPosition;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: Запускаем уже загруженное видео, Source={mediaElement.Source?.LocalPath}, Opacity={mediaElement.Opacity}, Visibility={mediaElement.Visibility}");
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: LoadedBehavior={mediaElement.LoadedBehavior}, NaturalDuration.HasTimeSpan={mediaElement.NaturalDuration.HasTimeSpan}");
                
                // КРИТИЧНО: Убеждаемся, что LoadedBehavior установлен правильно
                if (mediaElement.LoadedBehavior != MediaState.Manual)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: Исправляем LoadedBehavior с {mediaElement.LoadedBehavior} на Manual");
                    mediaElement.LoadedBehavior = MediaState.Manual;
                }
                
                // КРИТИЧНО: Проверяем текущее состояние перед Play()
                try
                {
                    var clock = mediaElement.Clock;
                    var currentState = clock?.CurrentState;
                    System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: Состояние перед Play() - Clock={clock != null}, CurrentState={currentState}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: Не удалось проверить состояние - {ex.Message}");
                }
                
                // КРИТИЧНО: Вызываем Play() и проверяем результат
                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: Вызываем Play() для уже загруженного видео");
                mediaElement.Play();
                SetIsVideoPlaying?.Invoke(true);
                
                // КРИТИЧНО: Проверяем состояние сразу после Play()
                var checkDispatcher = GetDispatcher?.Invoke();
                if (checkDispatcher != null)
                {
                    _ = checkDispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            var clock = mediaElement.Clock;
                            var currentState = clock?.CurrentState;
                            System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: Состояние после Play() - Clock={clock != null}, CurrentState={currentState}, Position={mediaElement.Position}");
                            
                            // Если видео не играет, пытаемся запустить еще раз
                            if (currentState != System.Windows.Media.Animation.ClockState.Active)
                            {
                                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: ВИДЕО НЕ ИГРАЕТ! Состояние={currentState}, пытаемся запустить снова");
                                mediaElement.Play();
                                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: Повторный Play() вызван");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: ВИДЕО ИГРАЕТ! Position={mediaElement.Position}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"LoadVideoFromSlot: ОШИБКА при проверке состояния - {ex.Message}");
                        }
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
                
                // КРИТИЧНО: Play() для дополнительного экрана теперь вызывается в MainWindow.xaml.cs
                // Применяем настройки элемента ПОСЛЕ загрузки видео
                ApplyElementSettings?.Invoke(mediaSlot, slotKey);
            }
        }
        
        private void SyncVideoToSecondaryScreen(MediaSlot mediaSlot, string slotKey)
        {
            var secondaryWindow = GetSecondaryScreenWindow?.Invoke();
            if (secondaryWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("SyncVideoToSecondaryScreen: secondaryWindow == null");
                return;
            }
            
            var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
            var useUniformToFill = GetUseUniformToFill?.Invoke() ?? false;
            
            // Если MediaElement еще не создан, создаем его
            if (secondaryMediaElement == null)
            {
                System.Diagnostics.Debug.WriteLine("SyncVideoToSecondaryScreen: Создаем новый MediaElement для второго экрана");
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
            
            // Убеждаемся, что MediaElement правильно настроен
            secondaryMediaElement.LoadedBehavior = MediaState.Manual;
            secondaryMediaElement.Stretch = useUniformToFill ? Stretch.UniformToFill : Stretch.Uniform;
            secondaryMediaElement.HorizontalAlignment = HorizontalAlignment.Stretch;
            secondaryMediaElement.VerticalAlignment = VerticalAlignment.Stretch;
            secondaryMediaElement.Margin = new Thickness(0);
            secondaryMediaElement.Visibility = Visibility.Visible;
            
            // Если окно уже имеет MediaElement как Content напрямую, используем его
            if (secondaryWindow.Content == secondaryMediaElement)
            {
                System.Diagnostics.Debug.WriteLine("SyncVideoToSecondaryScreen: MediaElement уже является Content окна");
            }
            else if (secondaryWindow.Content is Grid existingGrid)
            {
                // Если Content - Grid, проверяем, есть ли там MediaElement
                var existingMedia = existingGrid.Children.OfType<MediaElement>().FirstOrDefault();
                if (existingMedia != secondaryMediaElement)
                {
                    // Удаляем старые MediaElement и Image элементы
                    var oldMediaElements = existingGrid.Children.OfType<MediaElement>().ToList();
                    foreach (var oldMedia in oldMediaElements)
                    {
                        existingGrid.Children.Remove(oldMedia);
                    }
                    var oldImages = existingGrid.Children.OfType<System.Windows.Controls.Image>().ToList();
                    foreach (var oldImage in oldImages)
                    {
                        existingGrid.Children.Remove(oldImage);
                    }
                    
                    // Добавляем наш MediaElement
                    if (!existingGrid.Children.Contains(secondaryMediaElement))
                    {
                        existingGrid.Children.Insert(0, secondaryMediaElement);
                    }
                }
            }
            else
            {
                // Если Content другой, устанавливаем MediaElement напрямую
                System.Diagnostics.Debug.WriteLine("SyncVideoToSecondaryScreen: Устанавливаем MediaElement как Content окна");
                secondaryWindow.Content = secondaryMediaElement;
            }
            
            // Останавливаем предыдущее воспроизведение перед установкой нового Source
            if (secondaryMediaElement.Source != null)
            {
                secondaryMediaElement.Stop();
                secondaryMediaElement.Source = null;
            }
            
            // ВАЖНО: Устанавливаем LoadedBehavior ПЕРЕД установкой Source
            secondaryMediaElement.LoadedBehavior = MediaState.Manual;
            
            // Получаем абсолютный путь с учетом режима хранения
            string absolutePath = GetAbsoluteMediaPath?.Invoke(mediaSlot.MediaPath) ?? mediaSlot.MediaPath;
            
            // Устанавливаем Source
            try
            {
                secondaryMediaElement.Source = new Uri(absolutePath);
                System.Diagnostics.Debug.WriteLine($"SyncVideoToSecondaryScreen: Установлен Source={absolutePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SyncVideoToSecondaryScreen: ОШИБКА при установке Source - {ex.Message}");
                return;
            }
            
            var slotPosition = GetSlotPosition?.Invoke(slotKey) ?? TimeSpan.Zero;
            
            // Обработчик MediaOpened для запуска воспроизведения
            RoutedEventHandler? mediaOpenedHandler = null;
            mediaOpenedHandler = (s, e) =>
            {
                var element = secondaryMediaElement;
                if (element == null) return;
                
                // Удаляем обработчик, чтобы не вызывать его повторно
                element.MediaOpened -= mediaOpenedHandler;
                
                System.Diagnostics.Debug.WriteLine($"SyncVideoToSecondaryScreen: MediaOpened, Visibility={element.Visibility}, Opacity={element.Opacity}");
                
                // Устанавливаем позицию если нужно
                if (slotPosition > TimeSpan.Zero)
                {
                    var duration = element.NaturalDuration;
                    if (duration.HasTimeSpan)
                    {
                        var remainingTime = duration.TimeSpan - slotPosition;
                        if (remainingTime.TotalSeconds < 0.4)
                        {
                            element.Position = TimeSpan.Zero;
                        }
                        else
                        {
                            element.Position = slotPosition;
                        }
                    }
                    else
                    {
                        element.Position = slotPosition;
                    }
                }
                
                // Убеждаемся, что элемент видим и непрозрачен
                element.Visibility = Visibility.Visible;
                element.Opacity = 1.0;
                
                // Запускаем воспроизведение
                element.Play();
                System.Diagnostics.Debug.WriteLine($"SyncVideoToSecondaryScreen: Запущено воспроизведение на втором экране");
            };
            
            secondaryMediaElement.MediaOpened += mediaOpenedHandler;
            
            // Если медиа уже загружено, запускаем сразу
            if (secondaryMediaElement.NaturalDuration.HasTimeSpan)
            {
                System.Diagnostics.Debug.WriteLine("SyncVideoToSecondaryScreen: Медиа уже загружено, запускаем сразу");
                secondaryMediaElement.Visibility = Visibility.Visible;
                secondaryMediaElement.Opacity = 1.0;
                if (slotPosition > TimeSpan.Zero)
                {
                    secondaryMediaElement.Position = slotPosition;
                }
                secondaryMediaElement.Play();
            }
            
            System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ МЕДИА: Передан файл {mediaSlot.MediaPath} на дополнительный экран");
        }
        
        private async Task LoadImageFromSlot(MediaSlot mediaSlot, string slotKey, Border mediaBorder, Grid textOverlayGrid)
        {
            SetCurrentVisualContent?.Invoke(slotKey);
            SetCurrentMainMedia?.Invoke(slotKey);

            // Если ранее играло VLC-видео — прячем его при переходе на картинку
            VlcHideMainVideo?.Invoke();
            VlcStopMainVideo?.Invoke();
            VlcStopSecondaryVideo?.Invoke();
            
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
            var secondaryImageElement = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(CreateMediaUri(mediaSlot.MediaPath)),
                Stretch = useUniformToFill ? Stretch.UniformToFill : Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0),
                Width = double.NaN, // Auto - растягивается на весь экран
                Height = double.NaN // Auto - растягивается на весь экран
            };
            
            // Получаем или создаем Grid для второго экрана
            Grid? secondaryGrid = null;
            if (secondaryWindow.Content is Grid existingGrid)
            {
                secondaryGrid = existingGrid;
                // Удаляем старые MediaElement и Image элементы, но сохраняем TextBlock
                var oldMediaElements = secondaryGrid.Children.OfType<MediaElement>().ToList();
                foreach (var oldMedia in oldMediaElements)
                {
                    secondaryGrid.Children.Remove(oldMedia);
                }
                var oldImages = secondaryGrid.Children.OfType<System.Windows.Controls.Image>().ToList();
                foreach (var oldImage in oldImages)
                {
                    secondaryGrid.Children.Remove(oldImage);
                }
            }
            else
            {
                // Если Content не Grid, создаем новый Grid
                secondaryGrid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(0)
                };
                
                var existingContent = secondaryWindow.Content;
                secondaryWindow.Content = null;
                
                if (existingContent is UIElement uiElement)
                {
                    secondaryGrid.Children.Add(uiElement);
                }
                
                secondaryWindow.Content = secondaryGrid;
            }
            
            // Убеждаемся, что Grid правильно настроен
            secondaryGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            secondaryGrid.VerticalAlignment = VerticalAlignment.Stretch;
            secondaryGrid.Margin = new Thickness(0);
            
            // Добавляем Image в Grid (под текстом, ZIndex = 0 по умолчанию)
            if (!secondaryGrid.Children.Contains(secondaryImageElement))
            {
                secondaryGrid.Children.Insert(0, secondaryImageElement);
            }
            
            // Убеждаемся, что Grid установлен как Content окна
            if (secondaryWindow.Content != secondaryGrid)
            {
                secondaryWindow.Content = secondaryGrid;
            }
            
            System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ИЗОБРАЖЕНИЯ: Передано изображение {mediaSlot.MediaPath} на дополнительный экран");
            
            System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ИЗОБРАЖЕНИЯ: Передано изображение {mediaSlot.MediaPath} на дополнительный экран");
        }
        
        private void LoadAudioFromSlot(MediaSlot mediaSlot, string slotKey)
        {
            // Если ранее играло VLC-видео — прячем его при переходе на аудио
            VlcHideMainVideo?.Invoke();
            VlcStopMainVideo?.Invoke();
            VlcStopSecondaryVideo?.Invoke();

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
                    Source = CreateMediaUri(mediaSlot.MediaPath),
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
                if (dispatcher != null && AutoPlayNextAudioElement != null && audioElement != null)
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
                
                if (audioElement != null)
                {
                    audioElement.Play();
                }
            }
            else if (audioElement != null)
            {
                // Если используем существующий аудио элемент, просто обновляем состояние
                SetCurrentAudioContent?.Invoke(slotKey);
            }
            
            if (audioElement != null)
            {
                SetIsAudioPlaying?.Invoke(true);
            }
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
        
        private void LoadTextFromSlot(MediaSlot mediaSlot, string slotKey, Border mediaBorder, Grid textOverlayGrid)
        {
            SetCurrentMainMedia?.Invoke(slotKey);

            // Если ранее играло VLC-видео — прячем его при переходе на текст
            VlcHideMainVideo?.Invoke();
            VlcStopMainVideo?.Invoke();
            VlcStopSecondaryVideo?.Invoke();
            
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
            System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ТЕКСТА: secondaryWindow = {(secondaryWindow != null ? "не null" : "null")}");
            
            if (secondaryWindow != null)
            {
                // Получаем или создаем Grid для второго экрана
                Grid? secondaryGrid = null;
                if (secondaryWindow.Content is Grid existingGrid)
                {
                    secondaryGrid = existingGrid;
                    System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ТЕКСТА: Используем существующий Grid. Детей: {existingGrid.Children.Count}");
                }
                else
                {
                    // Если Content не Grid, создаем новый Grid и перемещаем существующий контент
                    secondaryGrid = new Grid();
                    var existingContent = secondaryWindow.Content;
                    
                    System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ТЕКСТА: Создаем новый Grid. Существующий контент: {(existingContent != null ? existingContent.GetType().Name : "null")}");
                    
                    // Сначала отсоединяем существующий контент от Window
                    secondaryWindow.Content = null;
                    
                    // Теперь можем безопасно добавить его в Grid
                    if (existingContent is UIElement uiElement)
                    {
                        secondaryGrid.Children.Add(uiElement);
                    }
                    
                    // Устанавливаем Grid как новый Content
                    secondaryWindow.Content = secondaryGrid;
                }
                
                // Ищем существующий текстовый элемент
                var existingSecondaryTextElement = secondaryGrid.Children.OfType<TextBlock>().FirstOrDefault();
                
                if (existingSecondaryTextElement == null)
                {
                    // Создаем новый текстовый элемент
                    var secondaryTextElement = new TextBlock
                    {
                        Text = mediaSlot.TextContent,
                        FontFamily = new FontFamily(mediaSlot.FontFamily),
                        FontSize = mediaSlot.FontSize,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.FontColor)),
                        Background = mediaSlot.BackgroundColor == "Transparent" ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.BackgroundColor)),
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = GetTextAlignment?.Invoke(mediaSlot.TextPosition) ?? TextAlignment.Center,
                        Padding = new Thickness(20),
                        // Текст должен занимать весь экран на втором мониторе
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };
                    
                    if (mediaSlot.UseManualPosition)
                    {
                        secondaryTextElement.Margin = new Thickness(mediaSlot.TextX, mediaSlot.TextY, 0, 0);
                        secondaryTextElement.HorizontalAlignment = HorizontalAlignment.Left;
                        secondaryTextElement.VerticalAlignment = VerticalAlignment.Top;
                    }
                    
                    // Устанавливаем высокий ZIndex чтобы текст был поверх медиа
                    secondaryTextElement.SetValue(Grid.ZIndexProperty, 10);
                    secondaryTextElement.Visibility = Visibility.Visible;
                    secondaryGrid.Children.Add(secondaryTextElement);
                    
                    System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ТЕКСТА: Создан новый TextBlock на втором экране. Текст: '{mediaSlot.TextContent}', Детей в Grid: {secondaryGrid.Children.Count}");
                }
                else
                {
                    // Обновляем существующий текстовый элемент
                    existingSecondaryTextElement.Text = mediaSlot.TextContent;
                    existingSecondaryTextElement.Visibility = Visibility.Visible;
                    existingSecondaryTextElement.FontFamily = new FontFamily(mediaSlot.FontFamily);
                    existingSecondaryTextElement.FontSize = mediaSlot.FontSize;
                    existingSecondaryTextElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.FontColor));
                    existingSecondaryTextElement.Background = mediaSlot.BackgroundColor == "Transparent" ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString(mediaSlot.BackgroundColor));
                    existingSecondaryTextElement.TextAlignment = GetTextAlignment?.Invoke(mediaSlot.TextPosition) ?? TextAlignment.Center;
                    
                    if (mediaSlot.UseManualPosition)
                    {
                        existingSecondaryTextElement.Margin = new Thickness(mediaSlot.TextX, mediaSlot.TextY, 0, 0);
                        existingSecondaryTextElement.HorizontalAlignment = HorizontalAlignment.Left;
                        existingSecondaryTextElement.VerticalAlignment = VerticalAlignment.Top;
                    }
                    else
                    {
                        existingSecondaryTextElement.Margin = new Thickness(0);
                        // Текст должен занимать весь экран на втором мониторе
                        existingSecondaryTextElement.HorizontalAlignment = HorizontalAlignment.Stretch;
                        existingSecondaryTextElement.VerticalAlignment = VerticalAlignment.Stretch;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ТЕКСТА: Обновлен существующий TextBlock на втором экране. Текст: '{mediaSlot.TextContent}'");
                }
                
                System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ТЕКСТА: Завершено. Детей в Grid: {secondaryGrid.Children.Count}, TextBlock: {secondaryGrid.Children.OfType<TextBlock>().Count()}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ТЕКСТА: secondaryWindow == null, синхронизация пропущена");
            }
            
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
    }
}


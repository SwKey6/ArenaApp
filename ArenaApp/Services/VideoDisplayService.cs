using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления отображением видео и медиа элементов
    /// </summary>
    public class VideoDisplayService
    {
        // Делегаты для доступа к UI элементам
        public Func<MediaElement>? GetMainMediaElement { get; set; }
        public Func<Border>? GetMediaBorder { get; set; }
        public Func<Grid>? GetTextOverlayGrid { get; set; }
        public Func<MediaElement?>? GetSecondaryMediaElement { get; set; }
        
        // Делегаты для работы с состоянием
        public Func<string?>? GetCurrentMainMedia { get; set; }
        public Action<string?>? SetCurrentMainMedia { get; set; }
        public Action<bool>? SetIsVideoPlaying { get; set; }
        public Func<bool>? GetIsVideoPaused { get; set; }
        public Action<bool>? SetIsVideoPaused { get; set; }
        public Action? SyncPlayWithSecondaryScreen { get; set; }
        public Action? SyncPauseWithSecondaryScreen { get; set; }
        
        // Делегаты для работы с позициями
        public Func<string, TimeSpan>? GetMediaResumePosition { get; set; }
        public Action<string, TimeSpan>? SaveMediaResumePosition { get; set; }
        
        // Делегаты для применения настроек
        public Action<MediaSlot, string>? ApplyElementSettings { get; set; }
        
        /// <summary>
        /// Обновляет MediaElement, сохраняя текстовые блоки в textOverlayGrid
        /// </summary>
        public void UpdateMediaElement(MediaElement mediaElement)
        {
            var mediaBorder = GetMediaBorder?.Invoke();
            var textOverlayGrid = GetTextOverlayGrid?.Invoke();
            
            if (mediaBorder == null || textOverlayGrid == null) return;
            
            // НЕ трогаем Stretch, HorizontalAlignment, VerticalAlignment - они установлены в XAML!
            // Устанавливаем только видимость и прозрачность
            mediaElement.Visibility = Visibility.Visible;
            if (mediaElement.Opacity <= 0)
            {
                mediaElement.Opacity = 1.0;
            }
            mediaBorder.Visibility = Visibility.Visible;
            mediaBorder.Opacity = 1.0;
            
                // ВАЖНО: Убеждаемся, что textOverlayGrid прозрачен и не перекрывает видео
                textOverlayGrid.Background = new SolidColorBrush(Colors.Transparent);
                textOverlayGrid.Opacity = 1.0; // Прозрачность текста управляется отдельно
                
                System.Diagnostics.Debug.WriteLine($"UpdateMediaElement: Установлена видимость - mediaElement.Visibility={mediaElement.Visibility}, Opacity={mediaElement.Opacity}");
                System.Diagnostics.Debug.WriteLine($"UpdateMediaElement: textOverlayGrid.Background={textOverlayGrid.Background}, Opacity={textOverlayGrid.Opacity}, Visibility={textOverlayGrid.Visibility}");
            
            // Получаем Grid внутри mediaBorder (который содержит mediaElement и textOverlayGrid)
            if (mediaBorder.Child is Grid mainGrid)
            {
                // Проверяем, есть ли уже mediaElement в Grid
                bool mediaElementInGrid = mainGrid.Children.Contains(mediaElement);
                
                // Удаляем старый MediaElement если есть (но не тот же самый объект)
                var oldMediaElement = mainGrid.Children.OfType<MediaElement>().FirstOrDefault();
                if (oldMediaElement != null && oldMediaElement != mediaElement)
                {
                    mainGrid.Children.Remove(oldMediaElement);
                    System.Diagnostics.Debug.WriteLine($"UpdateMediaElement: Удален старый MediaElement (не тот же объект)");
                }
                
                // Удаляем старые Image элементы если есть
                var oldImages = mainGrid.Children.OfType<Image>().ToList();
                foreach (var oldImage in oldImages)
                {
                    mainGrid.Children.Remove(oldImage);
                    System.Diagnostics.Debug.WriteLine($"UpdateMediaElement: Удален Image элемент");
                }
                
                // Добавляем MediaElement только если его еще нет в Grid
                if (!mediaElementInGrid)
                {
                    mainGrid.Children.Insert(0, mediaElement);
                    System.Diagnostics.Debug.WriteLine($"UpdateMediaElement: Добавлен mediaElement в Grid");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateMediaElement: mediaElement уже в Grid, не добавляем повторно");
                }
                
                // Убеждаемся, что textOverlayGrid остается в Grid
                if (!mainGrid.Children.Contains(textOverlayGrid))
                {
                    mainGrid.Children.Add(textOverlayGrid);
                    System.Diagnostics.Debug.WriteLine("UpdateMediaElement: Добавлен textOverlayGrid в Grid");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("UpdateMediaElement: textOverlayGrid уже присутствует в Grid");
                }
                
                // Делаем textOverlayGrid невидимым если в нем нет текста
                // ВАЖНО: textOverlayGrid должен быть прозрачным, чтобы не перекрывать видео
                textOverlayGrid.Background = new SolidColorBrush(Colors.Transparent);
                if (textOverlayGrid.Children.Count == 0)
                {
                    textOverlayGrid.Visibility = Visibility.Hidden;
                    System.Diagnostics.Debug.WriteLine("UpdateMediaElement: textOverlayGrid скрыт (нет текста)");
                }
                else
                {
                    textOverlayGrid.Visibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine($"UpdateMediaElement: textOverlayGrid видим ({textOverlayGrid.Children.Count} элементов)");
                }
                
                // Отладочная информация
                System.Diagnostics.Debug.WriteLine($"=== UpdateMediaElement ===");
                System.Diagnostics.Debug.WriteLine($"mediaElement.Visibility={mediaElement.Visibility}, Opacity={mediaElement.Opacity}");
                System.Diagnostics.Debug.WriteLine($"mediaElement.Width={mediaElement.Width}, Height={mediaElement.Height}, ActualWidth={mediaElement.ActualWidth}, ActualHeight={mediaElement.ActualHeight}");
                System.Diagnostics.Debug.WriteLine($"mediaBorder.Visibility={mediaBorder.Visibility}, Opacity={mediaBorder.Opacity}");
                System.Diagnostics.Debug.WriteLine($"mediaBorder.ActualWidth={mediaBorder.ActualWidth}, ActualHeight={mediaBorder.ActualHeight}");
                System.Diagnostics.Debug.WriteLine($"mediaElement в Grid: {mainGrid.Children.Contains(mediaElement)}");
                System.Diagnostics.Debug.WriteLine($"textOverlayGrid в Grid: {mainGrid.Children.Contains(textOverlayGrid)}");
                System.Diagnostics.Debug.WriteLine($"Дети Grid: {string.Join(", ", mainGrid.Children.Cast<UIElement>().Select(c => c.GetType().Name))}");
            }
            else
            {
                // Если mediaBorder.Child не Grid, создаем новый Grid
                var newGrid = new Grid();
                newGrid.Children.Add(mediaElement);
                newGrid.Children.Add(textOverlayGrid);
                mediaBorder.Child = newGrid;
                
                // Убеждаемся, что все видимо
                mediaElement.Visibility = Visibility.Visible;
                if (mediaElement.Opacity <= 0)
                {
                    mediaElement.Opacity = 1.0;
                }
                mediaBorder.Visibility = Visibility.Visible;
                mediaBorder.Opacity = 1.0;
            }
        }
        
        /// <summary>
        /// Восстанавливает MediaElement в Border, сохраняя текстовые блоки
        /// </summary>
        public void RestoreMediaElement(MediaElement mediaElement)
        {
            var mediaBorder = GetMediaBorder?.Invoke();
            if (mediaBorder == null) return;
            
            if (mediaBorder.Child != mediaElement)
            {
                UpdateMediaElement(mediaElement);
                mediaElement.Visibility = Visibility.Visible;
            }
        }
        
        /// <summary>
        /// Останавливает текущее медиа в главном плеере
        /// </summary>
        public void StopCurrentMainMedia()
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            if (mediaElement == null) return;
            
            if (mediaElement.Source != null)
            {
                mediaElement.Stop();
                SetCurrentMainMedia?.Invoke(null);
            }
            
            // Останавливаем воспроизведение на дополнительном экране
            var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
            if (secondaryMediaElement != null)
            {
                secondaryMediaElement.Stop();
            }
        }
        
        /// <summary>
        /// Очищает медиа элементы (используется при создании нового проекта)
        /// </summary>
        public void ClearMediaElements()
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            var mediaBorder = GetMediaBorder?.Invoke();
            
            if (mediaElement != null)
            {
                mediaElement.Stop();
                mediaElement.Source = null;
                // Используем Hidden вместо Collapsed, чтобы элемент оставался в визуальном дереве
                mediaElement.Visibility = Visibility.Hidden;
            }
            
            if (mediaBorder != null)
            {
                // Используем Hidden вместо Collapsed, чтобы элемент оставался в визуальном дереве
                mediaBorder.Visibility = Visibility.Hidden;
            }
        }
        
        /// <summary>
        /// Загружает и воспроизводит видео из слота
        /// </summary>
        public void LoadAndPlayVideo(MediaSlot slot, string slotKey)
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            var mediaBorder = GetMediaBorder?.Invoke();
            var textOverlayGrid = GetTextOverlayGrid?.Invoke();
            
            if (mediaElement == null || mediaBorder == null || textOverlayGrid == null) return;
            
            // Сохраняем позицию текущего медиа перед переключением
            if (GetCurrentMainMedia?.Invoke() != null && mediaElement.Source != null)
            {
                var currentPosition = mediaElement.Position;
                var sourcePath = mediaElement.Source.LocalPath;
                SaveMediaResumePosition?.Invoke(sourcePath, currentPosition);
            }
            
            // Для видео используем основной плеер
            mediaElement.Stop();
            mediaElement.Source = null;
            
            // Обновляем содержимое Grid, сохраняя textOverlayGrid
            if (mediaBorder.Child is Grid mainGrid)
            {
                // Удаляем старые элементы
                var oldMediaElement = mainGrid.Children.OfType<MediaElement>().FirstOrDefault();
                if (oldMediaElement != null && oldMediaElement != mediaElement)
                {
                    mainGrid.Children.Remove(oldMediaElement);
                }
                var oldImages = mainGrid.Children.OfType<Image>().ToList();
                foreach (var oldImage in oldImages)
                {
                    mainGrid.Children.Remove(oldImage);
                }
                
                // Добавляем MediaElement
                if (!mainGrid.Children.Contains(mediaElement))
                {
                    mainGrid.Children.Insert(0, mediaElement);
                }
                
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
                newGrid.Children.Add(mediaElement);
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
            
            // ВАЖНО: Устанавливаем видимость mediaBorder сразу, до загрузки видео
            mediaBorder.Visibility = Visibility.Visible;
            mediaBorder.Opacity = 1.0; // Убеждаемся, что border непрозрачен
            
            // Убеждаемся, что MediaElement видим и правильно настроен
            mediaElement.Visibility = Visibility.Visible;
            mediaElement.LoadedBehavior = MediaState.Manual;
            
            // Убеждаемся, что прозрачность mediaElement не равна 0
            if (mediaElement.Opacity <= 0)
            {
                mediaElement.Opacity = 1.0;
                System.Diagnostics.Debug.WriteLine($"LoadAndPlayVideo: Прозрачность была 0, устанавливаем 1.0");
            }
            
            // ВАЖНО: Убеждаемся, что textOverlayGrid прозрачен и не перекрывает видео
            textOverlayGrid.Background = new SolidColorBrush(Colors.Transparent);
            textOverlayGrid.Opacity = 1.0; // Прозрачность текста управляется отдельно
            
            System.Diagnostics.Debug.WriteLine($"LoadAndPlayVideo: Перед загрузкой - mediaElement.Visibility={mediaElement.Visibility}, Opacity={mediaElement.Opacity}, mediaBorder.Visibility={mediaBorder.Visibility}");
            System.Diagnostics.Debug.WriteLine($"LoadAndPlayVideo: textOverlayGrid.Background={textOverlayGrid.Background}, Opacity={textOverlayGrid.Opacity}, Visibility={textOverlayGrid.Visibility}");
            
            mediaElement.Source = new Uri(slot.MediaPath);
            SetCurrentMainMedia?.Invoke(slotKey);
            
            // Восстанавливаем позицию после загрузки медиа
            RoutedEventHandler? mediaOpenedHandler = null;
            mediaOpenedHandler = (s, e) =>
            {
                // Отписываемся от события, чтобы избежать повторных вызовов
                mediaElement.MediaOpened -= mediaOpenedHandler;
                
                // Убеждаемся, что элемент видим
                mediaElement.Visibility = Visibility.Visible;
                
                var resumePosition = GetMediaResumePosition?.Invoke(slot.MediaPath) ?? TimeSpan.Zero;
                if (resumePosition > TimeSpan.Zero)
                {
                    mediaElement.Position = resumePosition;
                    // Синхронизируем позицию со вторым экраном
                    var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
                    if (secondaryMediaElement != null && secondaryMediaElement.Source != null)
                    {
                        try
                        {
                            secondaryMediaElement.Position = resumePosition;
                        }
                        catch { }
                    }
                }
                
                // Убеждаемся, что mediaBorder видим
                mediaBorder.Visibility = Visibility.Visible;
                mediaBorder.Opacity = 1.0;
                
                // Убеждаемся, что прозрачность не равна 0
                var currentOpacity = mediaElement.Opacity;
                if (currentOpacity <= 0)
                {
                    mediaElement.Opacity = 1.0;
                    System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: Прозрачность была {currentOpacity}, устанавливаем 1.0");
                }
                
                System.Diagnostics.Debug.WriteLine($"LoadAndPlayVideo: Запускаем видео, Source={mediaElement.Source?.LocalPath}, Opacity={mediaElement.Opacity}, Visibility={mediaElement.Visibility}");
                System.Diagnostics.Debug.WriteLine($"LoadAndPlayVideo: mediaBorder.Visibility={mediaBorder.Visibility}, mediaBorder.Opacity={mediaBorder.Opacity}");
                System.Diagnostics.Debug.WriteLine($"LoadAndPlayVideo: mediaElement в Grid: {((mediaBorder.Child as Grid)?.Children.Contains(mediaElement) ?? false)}");
                
                // Еще раз убеждаемся, что все видимо перед воспроизведением
                mediaElement.Visibility = Visibility.Visible;
                mediaBorder.Visibility = Visibility.Visible;
                if (mediaElement.Opacity <= 0)
                {
                    mediaElement.Opacity = 1.0;
                }
                mediaBorder.Opacity = 1.0;
                
                mediaElement.Play();
                SyncPlayWithSecondaryScreen?.Invoke();
                SetIsVideoPlaying?.Invoke(true);
                
                // Применяем настройки после загрузки
                ApplyElementSettings?.Invoke(slot, slotKey);
            };
            mediaElement.MediaOpened += mediaOpenedHandler;
            
            // Если медиа уже загружено, запускаем сразу
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                mediaElement.Visibility = Visibility.Visible;
                mediaBorder.Visibility = Visibility.Visible;
                
                // Убеждаемся, что прозрачность не равна 0
                var currentOpacity = mediaElement.Opacity;
                if (currentOpacity <= 0)
                {
                    mediaElement.Opacity = 1.0;
                    System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: Прозрачность была {currentOpacity}, устанавливаем 1.0");
                }
                
                var resumePosition = GetMediaResumePosition?.Invoke(slot.MediaPath) ?? TimeSpan.Zero;
                if (resumePosition > TimeSpan.Zero)
                {
                    mediaElement.Position = resumePosition;
                    // Синхронизируем позицию со вторым экраном
                    var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
                    if (secondaryMediaElement != null && secondaryMediaElement.Source != null)
                    {
                        try
                        {
                            secondaryMediaElement.Position = resumePosition;
                        }
                        catch { }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"LoadAndPlayVideo: Запускаем уже загруженное видео, Source={mediaElement.Source?.LocalPath}, Opacity={mediaElement.Opacity}, Visibility={mediaElement.Visibility}");
                
                mediaElement.Play();
                SyncPlayWithSecondaryScreen?.Invoke();
                SetIsVideoPlaying?.Invoke(true);
                
                // Применяем настройки после загрузки
                ApplyElementSettings?.Invoke(slot, slotKey);
            }
        }
        
        /// <summary>
        /// Устанавливает видимость видео
        /// </summary>
        public void SetVideoVisibility(bool visible)
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            var mediaBorder = GetMediaBorder?.Invoke();
            
            if (mediaElement != null)
            {
                mediaElement.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            }
            
            if (mediaBorder != null)
            {
                mediaBorder.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            }
        }
        
        /// <summary>
        /// Убеждается, что видео видимо и правильно настроено
        /// </summary>
        public void EnsureVideoVisible()
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            var mediaBorder = GetMediaBorder?.Invoke();
            
            if (mediaElement != null)
            {
                mediaElement.Visibility = Visibility.Visible;
                
                // Убеждаемся, что прозрачность не равна 0
                if (mediaElement.Opacity <= 0)
                {
                    mediaElement.Opacity = 1.0;
                    System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: Прозрачность была {mediaElement.Opacity}, устанавливаем 1.0");
                }
            }
            
            if (mediaBorder != null)
            {
                mediaBorder.Visibility = Visibility.Visible;
            }
        }
    }
}


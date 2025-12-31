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
            
            // Устанавливаем правильный Stretch для медиа, чтобы оно помещалось в зону, а не растягивалось
            mediaElement.Stretch = Stretch.Uniform;
            mediaElement.HorizontalAlignment = HorizontalAlignment.Center;
            mediaElement.VerticalAlignment = VerticalAlignment.Center;
            
            // Получаем Grid внутри mediaBorder (который содержит mediaElement и textOverlayGrid)
            if (mediaBorder.Child is Grid mainGrid)
            {
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
                    System.Diagnostics.Debug.WriteLine("UpdateMediaElement: Добавлен textOverlayGrid в Grid");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("UpdateMediaElement: textOverlayGrid уже присутствует в Grid");
                }
                
                // Делаем textOverlayGrid невидимым если в нем нет текста
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
            }
            else
            {
                // Если mediaBorder.Child не Grid, создаем новый Grid
                var newGrid = new Grid();
                newGrid.Children.Add(mediaElement);
                newGrid.Children.Add(textOverlayGrid);
                mediaBorder.Child = newGrid;
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
            
            // Убеждаемся, что MediaElement видим и правильно настроен
            mediaElement.Visibility = Visibility.Visible;
            mediaElement.LoadedBehavior = MediaState.Manual;
            
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
                
                // Убеждаемся, что прозрачность не равна 0
                var currentOpacity = mediaElement.Opacity;
                if (currentOpacity <= 0)
                {
                    mediaElement.Opacity = 1.0;
                    System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: Прозрачность была {currentOpacity}, устанавливаем 1.0");
                }
                
                System.Diagnostics.Debug.WriteLine($"LoadAndPlayVideo: Запускаем видео, Source={mediaElement.Source?.LocalPath}, Opacity={mediaElement.Opacity}, Visibility={mediaElement.Visibility}");
                
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


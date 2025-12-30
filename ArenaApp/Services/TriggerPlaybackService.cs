using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления воспроизведением триггеров (параллельное воспроизведение видео/изображений с аудио)
    /// </summary>
    public class TriggerPlaybackService
    {
        private MediaStateService? _mediaStateService;
        private TriggerManager? _triggerManager;
        
        // Делегаты для доступа к UI элементам
        public Func<MediaElement>? GetMainMediaElement { get; set; }
        public Func<Border>? GetMediaBorder { get; set; }
        public Func<Grid>? GetMainContentGrid { get; set; }
        public Func<Dispatcher>? GetDispatcher { get; set; }
        
        // Делегаты для работы с состоянием
        public Func<string?>? GetCurrentMainMedia { get; set; }
        public Action<string?>? SetCurrentMainMedia { get; set; }
        public Func<string?>? GetCurrentVisualContent { get; set; }
        public Action<string?>? SetCurrentVisualContent { get; set; }
        public Func<string?>? GetCurrentAudioContent { get; set; }
        public Action<string?>? SetCurrentAudioContent { get; set; }
        public Func<string, bool>? IsMediaFileAlreadyPlaying { get; set; }
        public Action<string>? RegisterActiveMediaFile { get; set; }
        public Func<string, TimeSpan>? GetSlotPosition { get; set; }
        public Action<string, TimeSpan>? SaveSlotPosition { get; set; }
        public Func<string, MediaElement?>? TryGetAudioSlot { get; set; }
        public Func<string, Grid?>? TryGetAudioContainer { get; set; }
        public Action<string, MediaElement, Grid>? AddAudioSlot { get; set; }
        public Func<int, TriggerState>? GetTriggerState { get; set; }
        public Action<int, TriggerState>? SetTriggerState { get; set; }
        public Func<int?>? GetActiveTriggerColumn { get; set; }
        public Action<int?>? SetActiveTriggerColumn { get; set; }
        public Func<int?>? GetLastUsedTriggerColumn { get; set; }
        public Action<int?>? SetLastUsedTriggerColumn { get; set; }
        public Action<bool>? SetIsVideoPlaying { get; set; }
        public Action<bool>? SetIsAudioPlaying { get; set; }
        public Action? UpdateAllSlotButtonsHighlighting { get; set; }
        
        // Временные словари для хранения активных аудио элементов триггеров
        private readonly System.Collections.Generic.Dictionary<int, MediaElement> _activeAudioElements = new();
        private readonly System.Collections.Generic.Dictionary<int, Grid> _tempContainers = new();
        
        public void SetMediaStateService(MediaStateService service)
        {
            _mediaStateService = service;
        }
        
        public void SetTriggerManager(TriggerManager service)
        {
            _triggerManager = service;
        }
        
        /// <summary>
        /// Запускает воспроизведение видео с аудио для триггера
        /// </summary>
        public void StartVideoWithAudio(int column, MediaSlot videoSlot, MediaSlot audioSlot)
        {
            try
            {
                var mediaElement = GetMainMediaElement?.Invoke();
                if (mediaElement == null) return;
                
                // Проверяем, играет ли аудио в отдельном слоте ПЕРЕД вызовом SmartStopTriggers
                bool wasAudioPlayingInSlot = (IsMediaFileAlreadyPlaying?.Invoke(audioSlot.MediaPath) ?? false) && 
                                            GetCurrentAudioContent?.Invoke() != null && 
                                            GetCurrentAudioContent.Invoke()!.StartsWith("Slot_") &&
                                            _mediaStateService != null &&
                                            _mediaStateService.TryGetAudioSlot(GetCurrentAudioContent.Invoke()!, out _);
                
                // Проверяем, играет ли аудио в триггере (включая тот же триггер)
                bool wasAudioPlayingInTrigger = (IsMediaFileAlreadyPlaying?.Invoke(audioSlot.MediaPath) ?? false) && 
                                               GetCurrentAudioContent?.Invoke() != null && 
                                               GetCurrentAudioContent.Invoke()!.StartsWith("Trigger_") &&
                                               _mediaStateService != null &&
                                               _mediaStateService.TryGetAudioSlot(GetCurrentAudioContent.Invoke()!, out _);
                
                // Сохраняем ссылки на аудио элемент ДО вызова SmartStopTriggers
                MediaElement? existingAudioElement = null;
                Grid? existingAudioContainer = null;
                string? existingAudioSlotKey = null;
                
                var currentAudioContent = GetCurrentAudioContent?.Invoke();
                if (wasAudioPlayingInSlot && currentAudioContent != null)
                {
                    existingAudioElement = TryGetAudioSlot?.Invoke(currentAudioContent);
                    existingAudioContainer = TryGetAudioContainer?.Invoke(currentAudioContent);
                    existingAudioSlotKey = currentAudioContent;
                    System.Diagnostics.Debug.WriteLine($"StartVideoWithAudio: Сохранили ссылки на существующий аудио элемент из слота");
                }
                else if (wasAudioPlayingInTrigger && currentAudioContent != null)
                {
                    existingAudioElement = TryGetAudioSlot?.Invoke(currentAudioContent);
                    existingAudioContainer = TryGetAudioContainer?.Invoke(currentAudioContent);
                    existingAudioSlotKey = currentAudioContent;
                    System.Diagnostics.Debug.WriteLine($"StartVideoWithAudio: Сохранили ссылки на существующий аудио элемент из триггера");
                }
                
                bool audioAlreadyPlayingInSlot = wasAudioPlayingInSlot;
                bool audioAlreadyPlayingInTrigger = wasAudioPlayingInTrigger;
                bool audioAlreadyPlaying = audioAlreadyPlayingInSlot || audioAlreadyPlayingInTrigger;
                
                if (audioAlreadyPlaying)
                {
                    // Аудио уже играет - используем его, не создаем новый
                    // Не останавливаем аудио, только меняем визуальную часть
                    if (GetCurrentMainMedia?.Invoke() == $"Trigger_{column}")
                    {
                        mediaElement.Stop();
                        mediaElement.Source = null;
                        SetCurrentMainMedia?.Invoke(null!);
                    }
                }
                
                // Загружаем видео в основной плеер
                mediaElement.Source = new Uri(videoSlot.MediaPath);
                string triggerKey = $"Trigger_{column}";
                mediaElement.MediaOpened += (s2, e2) =>
                {
                    var slotPosition = GetSlotPosition?.Invoke(triggerKey) ?? TimeSpan.Zero;
                    if (slotPosition > TimeSpan.Zero)
                    {
                        mediaElement.Position = slotPosition;
                    }
                };
                RegisterActiveMediaFile?.Invoke(videoSlot.MediaPath);
                
                MediaElement audioElement;
                Grid tempGrid;
                
                if (audioAlreadyPlaying && existingAudioElement != null)
                {
                    // Используем сохраненный аудио элемент - НЕ ТРОГАЕМ его!
                    audioElement = existingAudioElement;
                    tempGrid = existingAudioContainer!;
                    
                    System.Diagnostics.Debug.WriteLine($"StartVideoWithAudio: НЕ ТРОГАЕМ существующий аудио элемент - он уже играет");
                }
                else
                {
                    // Создаем новый MediaElement для аудио
                    audioElement = new MediaElement
                    {
                        Source = new Uri(audioSlot.MediaPath),
                        LoadedBehavior = MediaState.Manual
                    };
                    RegisterActiveMediaFile?.Invoke(audioSlot.MediaPath);

                    // Восстанавливаем позицию аудио (если оно играло раньше)
                    audioElement.MediaOpened += (s2, e2) =>
                    {
                        // Используем сохраненную позицию триггера
                        var slotPosition = GetSlotPosition?.Invoke(triggerKey) ?? TimeSpan.Zero;
                        if (slotPosition > TimeSpan.Zero)
                        {
                            audioElement.Position = slotPosition;
                        }
                    };

                    // Создаем временный контейнер для аудио
                    tempGrid = new Grid { Visibility = Visibility.Hidden };
                    tempGrid.Children.Add(audioElement);
                    
                    var mainContentGrid = GetMainContentGrid?.Invoke();
                    if (mainContentGrid != null)
                    {
                        mainContentGrid.Children.Add(tempGrid);
                    }
                }

                // Сохраняем ссылки
                _activeAudioElements[column] = audioElement;
                _tempContainers[column] = tempGrid;

                // Подписываемся на событие окончания воспроизведения
                audioElement.MediaEnded += (s, e) => 
                {
                    // ЗАКОММЕНТИРОВАНО - триггеры отключены
                    // var dispatcher = GetDispatcher?.Invoke();
                    // dispatcher?.Invoke(() => StopParallelMedia(column, triggerButton));
                };
                
                mediaElement.MediaEnded += (s, e) => 
                {
                    // ЗАКОММЕНТИРОВАНО - триггеры отключены
                    // var dispatcher = GetDispatcher?.Invoke();
                    // dispatcher?.Invoke(() => StopParallelMedia(column, triggerButton));
                };

                // Синхронизируем воспроизведение
                mediaElement.Play();
                
                if (!audioAlreadyPlaying)
                {
                    // Запускаем аудио только если создали новый элемент
                    audioElement.Play();
                    System.Diagnostics.Debug.WriteLine($"StartVideoWithAudio: Запускаем новый аудио элемент");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"StartVideoWithAudio: НЕ ЗАПУСКАЕМ аудио - оно уже играет");
                }
                SetIsVideoPlaying?.Invoke(true);
                SetIsAudioPlaying?.Invoke(true);

                // Устанавливаем что основной плеер принадлежит этому триггеру
                SetCurrentMainMedia?.Invoke($"Trigger_{column}");
                SetCurrentVisualContent?.Invoke($"Trigger_{column}");
                
                // Регистрируем аудио как активное
                AddAudioSlot?.Invoke(triggerKey, audioElement, tempGrid);
                SetCurrentAudioContent?.Invoke(triggerKey);

                // Обновляем состояние
                SetTriggerState?.Invoke(column, TriggerState.Playing);
                SetActiveTriggerColumn?.Invoke(column);
                SetLastUsedTriggerColumn?.Invoke(column);
                
                // Отладочная информация
                System.Diagnostics.Debug.WriteLine($"Установлено состояние Playing для триггера колонки {column}");
                
                // Обновляем подсветку всех кнопок
                UpdateAllSlotButtonsHighlighting?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при параллельном воспроизведении: {ex.Message}", "Ошибка");
            }
        }
        
        /// <summary>
        /// Запускает воспроизведение изображения с аудио для триггера
        /// </summary>
        public void StartImageWithAudio(int column, MediaSlot imageSlot, MediaSlot audioSlot)
        {
            try
            {
                var mediaElement = GetMainMediaElement?.Invoke();
                var mediaBorder = GetMediaBorder?.Invoke();
                if (mediaElement == null || mediaBorder == null) return;
                
                // Проверяем, играет ли аудио в отдельном слоте ПЕРЕД вызовом SmartStopTriggers
                bool wasAudioPlayingInSlot = (IsMediaFileAlreadyPlaying?.Invoke(audioSlot.MediaPath) ?? false) && 
                                            GetCurrentAudioContent?.Invoke() != null && 
                                            GetCurrentAudioContent.Invoke()!.StartsWith("Slot_") &&
                                            _mediaStateService != null &&
                                            _mediaStateService.TryGetAudioSlot(GetCurrentAudioContent.Invoke()!, out _);
                
                // Проверяем, играет ли аудио в триггере (включая тот же триггер)
                bool wasAudioPlayingInTrigger = (IsMediaFileAlreadyPlaying?.Invoke(audioSlot.MediaPath) ?? false) && 
                                               GetCurrentAudioContent?.Invoke() != null && 
                                               GetCurrentAudioContent.Invoke()!.StartsWith("Trigger_") &&
                                               _mediaStateService != null &&
                                               _mediaStateService.TryGetAudioSlot(GetCurrentAudioContent.Invoke()!, out _);
                
                // Сохраняем ссылки на аудио элемент ДО вызова SmartStopTriggers
                MediaElement? existingAudioElement = null;
                Grid? existingAudioContainer = null;
                string? existingAudioSlotKey = null;
                
                var currentAudioContent = GetCurrentAudioContent?.Invoke();
                if (wasAudioPlayingInSlot && currentAudioContent != null)
                {
                    existingAudioElement = TryGetAudioSlot?.Invoke(currentAudioContent);
                    existingAudioContainer = TryGetAudioContainer?.Invoke(currentAudioContent);
                    existingAudioSlotKey = currentAudioContent;
                    System.Diagnostics.Debug.WriteLine($"StartImageWithAudio: Сохранили ссылки на существующий аудио элемент из слота");
                }
                else if (wasAudioPlayingInTrigger && currentAudioContent != null)
                {
                    existingAudioElement = TryGetAudioSlot?.Invoke(currentAudioContent);
                    existingAudioContainer = TryGetAudioContainer?.Invoke(currentAudioContent);
                    existingAudioSlotKey = currentAudioContent;
                    System.Diagnostics.Debug.WriteLine($"StartImageWithAudio: Сохранили ссылки на существующий аудио элемент из триггера");
                }
                
                bool audioAlreadyPlayingInSlot = wasAudioPlayingInSlot;
                bool audioAlreadyPlayingInTrigger = wasAudioPlayingInTrigger;
                bool audioAlreadyPlaying = audioAlreadyPlayingInSlot || audioAlreadyPlayingInTrigger;
                
                if (audioAlreadyPlaying)
                {
                    // Аудио уже играет - используем его, не создаем новый
                    // Не останавливаем аудио, только меняем визуальную часть
                    if (GetCurrentMainMedia?.Invoke() == $"Trigger_{column}")
                    {
                        mediaElement.Stop();
                        mediaElement.Source = null;
                        SetCurrentMainMedia?.Invoke(null!);
                    }
                }
                
                // Очищаем MediaElement и создаем Image для показа картинки
                mediaElement.Stop();
                mediaElement.Source = null;
                mediaElement.Visibility = Visibility.Hidden;
                
                var imageElement = new Image
                {
                    Source = new BitmapImage(new Uri(imageSlot.MediaPath)),
                    Stretch = Stretch.Uniform, // Изменено с UniformToFill на Uniform чтобы не обрезать
                    Width = 600,
                    Height = 400
                };
                
                // Заменяем MediaElement на изображение в Border
                mediaBorder.Child = imageElement;
                
                MediaElement audioElement;
                Grid tempGrid;
                string triggerKey = $"Trigger_{column}";
                
                if (audioAlreadyPlaying && existingAudioElement != null)
                {
                    // Используем сохраненный аудио элемент - НЕ ТРОГАЕМ его!
                    audioElement = existingAudioElement;
                    tempGrid = existingAudioContainer!;
                    
                    System.Diagnostics.Debug.WriteLine($"StartImageWithAudio: НЕ ТРОГАЕМ существующий аудио элемент - он уже играет");
                }
                else
                {
                    // Создаем новый MediaElement для аудио
                    audioElement = new MediaElement
                    {
                        Source = new Uri(audioSlot.MediaPath),
                        LoadedBehavior = MediaState.Manual
                    };
                    audioElement.MediaOpened += (s2, e2) =>
                    {
                        var slotPosition = GetSlotPosition?.Invoke(triggerKey) ?? TimeSpan.Zero;
                        if (slotPosition > TimeSpan.Zero)
                        {
                            audioElement.Position = slotPosition;
                        }
                    };
                    RegisterActiveMediaFile?.Invoke(audioSlot.MediaPath);

                    // Создаем временный контейнер для аудио
                    tempGrid = new Grid { Visibility = Visibility.Hidden };
                    tempGrid.Children.Add(audioElement);
                    
                    var mainContentGrid = GetMainContentGrid?.Invoke();
                    if (mainContentGrid != null)
                    {
                        mainContentGrid.Children.Add(tempGrid);
                    }
                }

                // Сохраняем ссылки
                _activeAudioElements[column] = audioElement;
                _tempContainers[column] = tempGrid;

                // Подписываемся на событие окончания аудио
                audioElement.MediaEnded += (s, e) => 
                {
                    // ЗАКОММЕНТИРОВАНО - триггеры отключены
                    // var dispatcher = GetDispatcher?.Invoke();
                    // dispatcher?.Invoke(() => StopParallelMedia(column, triggerButton));
                };

                // Воспроизводим аудио (только если создали новый элемент)
                if (!audioAlreadyPlaying)
                {
                    audioElement.Play();
                    System.Diagnostics.Debug.WriteLine($"StartImageWithAudio: Запускаем новый аудио элемент");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"StartImageWithAudio: НЕ ЗАПУСКАЕМ аудио - оно уже играет");
                }
                SetIsAudioPlaying?.Invoke(true);

                // Устанавливаем что основной плеер принадлежит этому триггеру
                SetCurrentMainMedia?.Invoke($"Trigger_{column}");
                SetCurrentVisualContent?.Invoke($"Trigger_{column}");
                
                // Регистрируем аудио как активное
                AddAudioSlot?.Invoke(triggerKey, audioElement, tempGrid);
                SetCurrentAudioContent?.Invoke(triggerKey);

                // Обновляем состояние
                SetTriggerState?.Invoke(column, TriggerState.Playing);
                SetActiveTriggerColumn?.Invoke(column);
                SetLastUsedTriggerColumn?.Invoke(column);
                
                // Отладочная информация
                System.Diagnostics.Debug.WriteLine($"Установлено состояние Playing для триггера колонки {column}");
                
                // Обновляем подсветку всех кнопок
                UpdateAllSlotButtonsHighlighting?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при параллельном воспроизведении: {ex.Message}", "Ошибка");
            }
        }
        
        /// <summary>
        /// Запускает воспроизведение одного медиа для триггера
        /// </summary>
        public void StartSingleMedia(int column, MediaSlot mediaSlot)
        {
            try
            {
                var mediaElement = GetMainMediaElement?.Invoke();
                if (mediaElement == null) return;
                
                // Для триггеров: аудио из отдельных слотов уже остановлено в SmartStopTriggers
                // Если аудио играет в триггере - продолжаем его воспроизведение
                
                // Останавливаем предыдущее воспроизведение, если есть
                // Но не останавливаем аудио, если оно должно продолжить играть
                if (mediaSlot.Type == MediaType.Audio && 
                    (IsMediaFileAlreadyPlaying?.Invoke(mediaSlot.MediaPath) ?? false) && 
                    GetCurrentAudioContent?.Invoke() != null && 
                    GetCurrentAudioContent.Invoke()!.StartsWith("Trigger_"))
                {
                    // Аудио уже играет в триггере - не останавливаем его
                    // Останавливаем только визуальную часть
                    if (GetCurrentMainMedia?.Invoke() == $"Trigger_{column}")
                    {
                        mediaElement.Stop();
                        mediaElement.Source = null;
                        SetCurrentMainMedia?.Invoke(null!);
                    }
                }
                
                // Загружаем медиа в основной плеер
                mediaElement.Source = new Uri(mediaSlot.MediaPath);
                RegisterActiveMediaFile?.Invoke(mediaSlot.MediaPath);
                
                // Подписываемся на событие окончания воспроизведения
                mediaElement.MediaEnded += (s, e) => 
                {
                    // ЗАКОММЕНТИРОВАНО - триггеры отключены
                    // var dispatcher = GetDispatcher?.Invoke();
                    // dispatcher?.Invoke(() => StopParallelMedia(column, triggerButton));
                };

                // Воспроизводим
                mediaElement.Play();
                SetIsVideoPlaying?.Invoke(true);

                // Устанавливаем что основной плеер принадлежит этому триггеру
                SetCurrentMainMedia?.Invoke($"Trigger_{column}");
                SetCurrentVisualContent?.Invoke($"Trigger_{column}");

                // Обновляем состояние
                SetTriggerState?.Invoke(column, TriggerState.Playing);
                SetActiveTriggerColumn?.Invoke(column);
                SetLastUsedTriggerColumn?.Invoke(column);
                
                // Отладочная информация
                System.Diagnostics.Debug.WriteLine($"Установлено состояние Playing для триггера колонки {column}");
                
                // Обновляем подсветку всех кнопок
                UpdateAllSlotButtonsHighlighting?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при воспроизведении: {ex.Message}", "Ошибка");
            }
        }
    }
}


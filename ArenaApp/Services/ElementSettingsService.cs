using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для применения настроек элементов к медиа
    /// </summary>
    public class ElementSettingsService
    {
        private SettingsManager? _settingsManager;
        
        // Делегаты для доступа к UI элементам
        public Func<MediaElement>? GetMainMediaElement { get; set; }
        public Func<Border>? GetMediaBorder { get; set; }
        public Func<Grid>? GetTextOverlayGrid { get; set; }
        public Func<MediaElement?>? GetSecondaryMediaElement { get; set; }
        public Func<Window?>? GetSecondaryScreenWindow { get; set; }
        
        // Делегаты для работы с состоянием
        public Func<GlobalSettings?>? GetGlobalSettings { get; set; }
        
        public void SetSettingsManager(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }
        
        /// <summary>
        /// Применяет настройки элемента к активным медиа
        /// </summary>
        public void ApplyElementSettings(MediaSlot slot, string slotKey)
        {
            if (slot == null || string.IsNullOrEmpty(slotKey) || _settingsManager == null) return;
            
            // Получаем финальные значения с учетом общих настроек
            var finalVolume = _settingsManager.GetFinalVolume(slot.Volume);
            var finalOpacity = _settingsManager.GetFinalOpacity(slot.Opacity);
            var finalScale = _settingsManager.GetFinalScale(slot.Scale);
            var finalRotation = _settingsManager.GetFinalRotation(slot.Rotation);
            
            System.Diagnostics.Debug.WriteLine($"ApplyElementSettings: Slot={slotKey}, Type={slot.Type}, FinalOpacity={finalOpacity}");
            
            var mediaElement = GetMainMediaElement?.Invoke();
            var mediaBorder = GetMediaBorder?.Invoke();
            var textOverlayGrid = GetTextOverlayGrid?.Invoke();
            var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
            var secondaryScreenWindow = GetSecondaryScreenWindow?.Invoke();
            var currentMainMedia = GetCurrentMainMedia?.Invoke();
            var activeSlotMedia = GetActiveSlotMedia?.Invoke() ?? new Dictionary<string, MediaElement>();
            var activeAudioSlots = GetAllAudioSlots?.Invoke() ?? new Dictionary<string, MediaElement>();
            
            // Применяем настройки к активному медиа элементу
            if (activeSlotMedia.TryGetValue(slotKey, out MediaElement? slotMediaElement))
            {
                slotMediaElement.SpeedRatio = slot.PlaybackSpeed;
                slotMediaElement.Opacity = finalOpacity;
                slotMediaElement.Volume = finalVolume;
                _settingsManager.ApplyScaleAndRotation(slotMediaElement, finalScale, finalRotation);
            }
            
            // Если это главный плеер
            if (currentMainMedia == slotKey && mediaElement != null)
            {
                mediaElement.SpeedRatio = slot.PlaybackSpeed;
                mediaElement.Volume = finalVolume;
                
                // Синхронизируем настройки со вторым экраном
                if (secondaryMediaElement != null)
                {
                    secondaryMediaElement.SpeedRatio = slot.PlaybackSpeed;
                    secondaryMediaElement.Volume = 0; // Отключаем звук на втором экране
                    _settingsManager.ApplyScaleAndRotation(secondaryMediaElement, finalScale, finalRotation);
                    
                    System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ НАСТРОЕК: Скорость={slot.PlaybackSpeed:F1}x, Звук отключен на втором экране");
                }
                
                // Для изображений применяем прозрачность к Border контейнеру
                if (slot.Type == MediaType.Image && mediaBorder != null)
                {
                    mediaBorder.Opacity = finalOpacity;
                    _settingsManager.ApplyScaleAndRotation(mediaBorder, finalScale, finalRotation);
                    
                    System.Diagnostics.Debug.WriteLine($"ApplyElementSettings: Применена прозрачность {finalOpacity} к mediaBorder для изображения");
                    
                    // Синхронизируем прозрачность изображения на втором экране
                    if (secondaryScreenWindow?.Content is FrameworkElement secondaryElement)
                    {
                        secondaryElement.Opacity = finalOpacity;
                        _settingsManager.ApplyScaleAndRotation(secondaryElement, finalScale, finalRotation);
                    }
                }
                else if (slot.Type == MediaType.Video)
                {
                    // Для видео применяем прозрачность к MediaElement
                    mediaElement.Visibility = Visibility.Visible;
                    
                    // Проверяем, что прозрачность не равна 0
                    if (finalOpacity <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: Прозрачность равна {finalOpacity}, устанавливаем 1.0");
                        finalOpacity = 1.0;
                    }
                    
                    mediaElement.Opacity = finalOpacity;
                    
                    // Убеждаемся, что mediaBorder видим
                    if (mediaBorder != null)
                    {
                        mediaBorder.Visibility = Visibility.Visible;
                        mediaBorder.Opacity = 1.0; // Border всегда непрозрачен
                        _settingsManager.ApplyScaleAndRotation(mediaBorder, finalScale, finalRotation);
                    }
                    
                    // Синхронизируем прозрачность, масштаб и поворот видео на втором экране
                    if (secondaryMediaElement != null)
                    {
                        secondaryMediaElement.Visibility = Visibility.Visible;
                        secondaryMediaElement.Opacity = finalOpacity;
                        _settingsManager.ApplyScaleAndRotation(secondaryMediaElement, finalScale, finalRotation);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"ApplyElementSettings (Video): Opacity={finalOpacity}, Visibility=Visible, SlotKey={slotKey}");
                }
                
                // Для текстовых блоков применяем прозрачность, масштаб и поворот
                if (slot.Type == MediaType.Text && textOverlayGrid != null)
                {
                    textOverlayGrid.Opacity = finalOpacity;
                    var textElement = textOverlayGrid.Children.OfType<TextBlock>().FirstOrDefault();
                    if (textElement != null)
                    {
                        _settingsManager.ApplyScaleAndRotation(textElement, finalScale, finalRotation);
                    }
                    
                    // Синхронизируем настройки текста на втором экране
                    if (secondaryScreenWindow?.Content is Grid secondaryGrid)
                    {
                        var secondaryTextElement = secondaryGrid.Children.OfType<TextBlock>().FirstOrDefault();
                        if (secondaryTextElement != null)
                        {
                            secondaryTextElement.Opacity = finalOpacity;
                            _settingsManager.ApplyScaleAndRotation(secondaryTextElement, finalScale, finalRotation);
                        }
                    }
                }
            }
            
            // Применяем к аудио элементам
            if (activeAudioSlots.TryGetValue(slotKey, out MediaElement? audioElement))
            {
                audioElement.SpeedRatio = slot.PlaybackSpeed;
                audioElement.Volume = finalVolume;
            }
        }
        
        // Делегаты для получения данных
        public Func<ProjectManager?>? GetProjectManager { get; set; }
        public Func<string?>? GetCurrentMainMedia { get; set; }
        public Func<Dictionary<string, MediaElement>>? GetAllAudioSlots { get; set; }
        public Func<Dictionary<string, MediaElement>>? GetActiveSlotMedia { get; set; }
        
        /// <summary>
        /// Применяет общие настройки ко всем активным медиа элементам
        /// </summary>
        public void ApplyGlobalSettings()
        {
            if (_settingsManager == null) return;
            
            var projectManager = GetProjectManager?.Invoke();
            if (projectManager?.CurrentProject?.GlobalSettings == null)
            {
                System.Diagnostics.Debug.WriteLine("ApplyGlobalSettings: GlobalSettings is null, returning");
                return;
            }
            
            var globalSettings = projectManager.CurrentProject.GlobalSettings;
            System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: UseGlobalOpacity={globalSettings.UseGlobalOpacity}, GlobalOpacity={globalSettings.GlobalOpacity}");
            
            var mediaElement = GetMainMediaElement?.Invoke();
            var mediaBorder = GetMediaBorder?.Invoke();
            var textOverlayGrid = GetTextOverlayGrid?.Invoke();
            var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
            var secondaryScreenWindow = GetSecondaryScreenWindow?.Invoke();
            var currentMainMedia = GetCurrentMainMedia?.Invoke();
            var activeAudioSlots = GetAllAudioSlots?.Invoke() ?? new Dictionary<string, MediaElement>();
            var activeSlotMedia = GetActiveSlotMedia?.Invoke() ?? new Dictionary<string, MediaElement>();
            
            // Применяем к главному плееру (основной медиа элемент)
            if (currentMainMedia != null)
            {
                var slot = projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => 
                    (s.IsTrigger ? $"Trigger_{s.Column}" : $"Slot_{s.Column}_{s.Row}") == currentMainMedia);
                
                if (slot != null)
                {
                    var finalVolume = _settingsManager.GetFinalVolume(slot.Volume);
                    var finalOpacity = _settingsManager.GetFinalOpacity(slot.Opacity);
                    var finalScale = _settingsManager.GetFinalScale(slot.Scale);
                    var finalRotation = _settingsManager.GetFinalRotation(slot.Rotation);
                    
                    System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Main media - Slot={currentMainMedia}, FinalOpacity={finalOpacity}, FinalScale={finalScale}, FinalRotation={finalRotation}");
                    
                    if (mediaElement != null)
                    {
                        mediaElement.Volume = finalVolume;
                    }
                    
                    if (slot.Type == MediaType.Image && mediaBorder != null)
                    {
                        // Для изображений применяем прозрачность к mediaBorder
                        mediaBorder.Opacity = finalOpacity;
                        
                        // Применяем масштаб и поворот к mediaBorder для правильного центра поворота
                        _settingsManager.ApplyScaleAndRotation(mediaBorder, finalScale, finalRotation);
                        
                        System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied opacity {finalOpacity} to mediaBorder for image");
                    }
                    else if (slot.Type == MediaType.Video && mediaElement != null)
                    {
                        // Для видео применяем прозрачность к mediaElement
                        // Убеждаемся, что прозрачность не равна 0
                        if (finalOpacity <= 0)
                        {
                            finalOpacity = 1.0;
                            System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: Прозрачность была {finalOpacity}, устанавливаем 1.0 для видео");
                        }
                        
                        // Убеждаемся, что элементы видимы
                        mediaElement.Visibility = Visibility.Visible;
                        if (mediaBorder != null)
                        {
                            mediaBorder.Visibility = Visibility.Visible;
                            mediaBorder.Opacity = 1.0; // Border всегда непрозрачен
                        }
                        
                        mediaElement.Opacity = finalOpacity;
                        
                        // Применяем масштаб и поворот к mediaBorder для правильного центра поворота
                        if (mediaBorder != null)
                        {
                            _settingsManager.ApplyScaleAndRotation(mediaBorder, finalScale, finalRotation);
                        }
                        
                        // Синхронизируем прозрачность, масштаб и поворот видео на втором экране
                        if (secondaryMediaElement != null)
                        {
                            secondaryMediaElement.Visibility = Visibility.Visible;
                            secondaryMediaElement.Opacity = finalOpacity;
                            _settingsManager.ApplyScaleAndRotation(secondaryMediaElement, finalScale, finalRotation);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied opacity {finalOpacity} to mediaElement for video");
                    }
                    else if (slot.Type == MediaType.Text && textOverlayGrid != null)
                    {
                        // Для текстовых блоков применяем прозрачность к textOverlayGrid
                        textOverlayGrid.Opacity = finalOpacity;
                        
                        // Применяем масштаб и поворот к текстовому блоку
                        var textElement = textOverlayGrid.Children.OfType<TextBlock>().FirstOrDefault();
                        if (textElement != null)
                        {
                            _settingsManager.ApplyScaleAndRotation(textElement, finalScale, finalRotation);
                        }
                        System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied opacity {finalOpacity} to textOverlayGrid for text");
                    }
                }
            }
            
            // Применяем к аудио элементам
            foreach (var kvp in activeAudioSlots)
            {
                var slotKey = kvp.Key;
                var audioElement = kvp.Value;
                
                var slot = projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => 
                    (s.IsTrigger ? $"Trigger_{s.Column}" : $"Slot_{s.Column}_{s.Row}") == slotKey);
                
                if (slot != null)
                {
                    var finalVolume = _settingsManager.GetFinalVolume(slot.Volume);
                    audioElement.Volume = finalVolume;
                    System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied volume {finalVolume} to audio element {slotKey}");
                }
            }
            
            // Применяем ко всем активным медиа элементам в _activeSlotMedia
            foreach (var kvp in activeSlotMedia)
            {
                var slotKey = kvp.Key;
                var slotMediaElement = kvp.Value;
                
                // Находим соответствующий слот
                var slot = projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => 
                    (s.IsTrigger ? $"Trigger_{s.Column}" : $"Slot_{s.Column}_{s.Row}") == slotKey);
                
                if (slot != null)
                {
                    var finalVolume = _settingsManager.GetFinalVolume(slot.Volume);
                    var finalOpacity = _settingsManager.GetFinalOpacity(slot.Opacity);
                    var finalScale = _settingsManager.GetFinalScale(slot.Scale);
                    var finalRotation = _settingsManager.GetFinalRotation(slot.Rotation);
                    
                    slotMediaElement.Volume = finalVolume;
                    slotMediaElement.Opacity = finalOpacity;
                    _settingsManager.ApplyScaleAndRotation(slotMediaElement, finalScale, finalRotation);
                    System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied final settings to slot media {slotKey} - Opacity={finalOpacity}, Scale={finalScale}, Rotation={finalRotation}");
                }
            }
            
            // Применяем ко второму экрану если он активен
            if (secondaryScreenWindow != null)
            {
                var secondaryFinalOpacity = globalSettings.UseGlobalOpacity ? globalSettings.GlobalOpacity : 1.0;
                secondaryScreenWindow.Opacity = secondaryFinalOpacity;
                System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied opacity {secondaryFinalOpacity} to secondary screen");
            }
        }
    }
}


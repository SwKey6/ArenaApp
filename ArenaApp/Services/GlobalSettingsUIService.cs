using System;
using System.Windows;
using System.Windows.Controls;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления UI элементами глобальных настроек
    /// </summary>
    public class GlobalSettingsUIService
    {
        private ProjectManager? _projectManager;
        private SettingsManager? _settingsManager;
        private TransitionService? _transitionService;
        
        // Делегаты для доступа к UI элементам
        public Func<CheckBox>? GetUseGlobalVolumeCheckBox { get; set; }
        public Func<Slider>? GetGlobalVolumeSlider { get; set; }
        public Func<TextBlock>? GetGlobalVolumeValueText { get; set; }
        
        public Func<CheckBox>? GetUseGlobalOpacityCheckBox { get; set; }
        public Func<Slider>? GetGlobalOpacitySlider { get; set; }
        public Func<TextBlock>? GetGlobalOpacityValueText { get; set; }
        
        public Func<CheckBox>? GetUseGlobalScaleCheckBox { get; set; }
        public Func<Slider>? GetGlobalScaleSlider { get; set; }
        public Func<TextBlock>? GetGlobalScaleValueText { get; set; }
        
        public Func<CheckBox>? GetUseGlobalRotationCheckBox { get; set; }
        public Func<Slider>? GetGlobalRotationSlider { get; set; }
        public Func<TextBlock>? GetGlobalRotationValueText { get; set; }
        
        public Func<ComboBox>? GetTransitionTypeComboBox { get; set; }
        public Func<Slider>? GetTransitionDurationSlider { get; set; }
        public Func<TextBlock>? GetTransitionDurationValueText { get; set; }
        
        public Func<CheckBox>? GetAutoPlayNextCheckBox { get; set; }
        public Func<CheckBox>? GetLoopPlaylistCheckBox { get; set; }
        
        // Делегаты для событий
        public RoutedEventHandler? UseGlobalVolumeCheckBox_Changed { get; set; }
        public RoutedPropertyChangedEventHandler<double>? GlobalVolumeSlider_ValueChanged { get; set; }
        public RoutedEventHandler? UseGlobalOpacityCheckBox_Changed { get; set; }
        public RoutedPropertyChangedEventHandler<double>? GlobalOpacitySlider_ValueChanged { get; set; }
        public RoutedEventHandler? UseGlobalScaleCheckBox_Changed { get; set; }
        public RoutedPropertyChangedEventHandler<double>? GlobalScaleSlider_ValueChanged { get; set; }
        public RoutedEventHandler? UseGlobalRotationCheckBox_Changed { get; set; }
        public RoutedPropertyChangedEventHandler<double>? GlobalRotationSlider_ValueChanged { get; set; }
        public SelectionChangedEventHandler? TransitionTypeComboBox_SelectionChanged { get; set; }
        public RoutedPropertyChangedEventHandler<double>? TransitionDurationSlider_ValueChanged { get; set; }
        public RoutedEventHandler? AutoPlayNextCheckBox_Changed { get; set; }
        public RoutedEventHandler? LoopPlaylistCheckBox_Changed { get; set; }
        
        // Делегаты для сохранения проекта
        public Action? SaveProject { get; set; }
        
        // Делегаты для применения настроек
        public Action? ApplyGlobalSettings { get; set; }
        public Action? ApplyElementSettings { get; set; }
        
        public void SetProjectManager(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        
        public void SetSettingsManager(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }
        
        public void SetTransitionService(TransitionService transitionService)
        {
            _transitionService = transitionService;
        }
        
        /// <summary>
        /// Загружает глобальные настройки в UI
        /// </summary>
        public void LoadGlobalSettings()
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var globalSettings = _projectManager.CurrentProject.GlobalSettings;
            
            // Отключаем события для предотвращения вызова ApplyGlobalSettings
            var useGlobalVolumeCheckBox = GetUseGlobalVolumeCheckBox?.Invoke();
            var globalVolumeSlider = GetGlobalVolumeSlider?.Invoke();
            var useGlobalOpacityCheckBox = GetUseGlobalOpacityCheckBox?.Invoke();
            var globalOpacitySlider = GetGlobalOpacitySlider?.Invoke();
            var useGlobalScaleCheckBox = GetUseGlobalScaleCheckBox?.Invoke();
            var globalScaleSlider = GetGlobalScaleSlider?.Invoke();
            var useGlobalRotationCheckBox = GetUseGlobalRotationCheckBox?.Invoke();
            var globalRotationSlider = GetGlobalRotationSlider?.Invoke();
            var transitionTypeComboBox = GetTransitionTypeComboBox?.Invoke();
            var transitionDurationSlider = GetTransitionDurationSlider?.Invoke();
            var autoPlayNextCheckBox = GetAutoPlayNextCheckBox?.Invoke();
            var loopPlaylistCheckBox = GetLoopPlaylistCheckBox?.Invoke();
            
            if (useGlobalVolumeCheckBox != null && UseGlobalVolumeCheckBox_Changed != null)
            {
                useGlobalVolumeCheckBox.Checked -= UseGlobalVolumeCheckBox_Changed;
                useGlobalVolumeCheckBox.Unchecked -= UseGlobalVolumeCheckBox_Changed;
            }
            if (globalVolumeSlider != null && GlobalVolumeSlider_ValueChanged != null)
                globalVolumeSlider.ValueChanged -= GlobalVolumeSlider_ValueChanged;
            if (useGlobalOpacityCheckBox != null && UseGlobalOpacityCheckBox_Changed != null)
            {
                useGlobalOpacityCheckBox.Checked -= UseGlobalOpacityCheckBox_Changed;
                useGlobalOpacityCheckBox.Unchecked -= UseGlobalOpacityCheckBox_Changed;
            }
            if (globalOpacitySlider != null && GlobalOpacitySlider_ValueChanged != null)
                globalOpacitySlider.ValueChanged -= GlobalOpacitySlider_ValueChanged;
            if (useGlobalScaleCheckBox != null && UseGlobalScaleCheckBox_Changed != null)
            {
                useGlobalScaleCheckBox.Checked -= UseGlobalScaleCheckBox_Changed;
                useGlobalScaleCheckBox.Unchecked -= UseGlobalScaleCheckBox_Changed;
            }
            if (globalScaleSlider != null && GlobalScaleSlider_ValueChanged != null)
                globalScaleSlider.ValueChanged -= GlobalScaleSlider_ValueChanged;
            if (useGlobalRotationCheckBox != null && UseGlobalRotationCheckBox_Changed != null)
            {
                useGlobalRotationCheckBox.Checked -= UseGlobalRotationCheckBox_Changed;
                useGlobalRotationCheckBox.Unchecked -= UseGlobalRotationCheckBox_Changed;
            }
            if (globalRotationSlider != null && GlobalRotationSlider_ValueChanged != null)
                globalRotationSlider.ValueChanged -= GlobalRotationSlider_ValueChanged;
            if (transitionTypeComboBox != null && TransitionTypeComboBox_SelectionChanged != null)
                transitionTypeComboBox.SelectionChanged -= TransitionTypeComboBox_SelectionChanged;
            if (transitionDurationSlider != null && TransitionDurationSlider_ValueChanged != null)
                transitionDurationSlider.ValueChanged -= TransitionDurationSlider_ValueChanged;
            if (autoPlayNextCheckBox != null && AutoPlayNextCheckBox_Changed != null)
            {
                autoPlayNextCheckBox.Checked -= AutoPlayNextCheckBox_Changed;
                autoPlayNextCheckBox.Unchecked -= AutoPlayNextCheckBox_Changed;
            }
            if (loopPlaylistCheckBox != null && LoopPlaylistCheckBox_Changed != null)
            {
                loopPlaylistCheckBox.Checked -= LoopPlaylistCheckBox_Changed;
                loopPlaylistCheckBox.Unchecked -= LoopPlaylistCheckBox_Changed;
            }
            
            // Загружаем значения
            if (useGlobalVolumeCheckBox != null)
                useGlobalVolumeCheckBox.IsChecked = globalSettings.UseGlobalVolume;
            if (globalVolumeSlider != null)
                globalVolumeSlider.Value = globalSettings.GlobalVolume;
            var globalVolumeValueText = GetGlobalVolumeValueText?.Invoke();
            if (globalVolumeValueText != null)
                globalVolumeValueText.Text = $"Общая громкость: {(globalSettings.GlobalVolume * 100):F0}%";
            
            if (useGlobalOpacityCheckBox != null)
                useGlobalOpacityCheckBox.IsChecked = globalSettings.UseGlobalOpacity;
            if (globalOpacitySlider != null)
                globalOpacitySlider.Value = globalSettings.GlobalOpacity;
            var globalOpacityValueText = GetGlobalOpacityValueText?.Invoke();
            if (globalOpacityValueText != null)
                globalOpacityValueText.Text = $"Общая прозрачность: {(globalSettings.GlobalOpacity * 100):F0}%";
            
            if (useGlobalScaleCheckBox != null)
                useGlobalScaleCheckBox.IsChecked = globalSettings.UseGlobalScale;
            if (globalScaleSlider != null)
                globalScaleSlider.Value = globalSettings.GlobalScale;
            var globalScaleValueText = GetGlobalScaleValueText?.Invoke();
            if (globalScaleValueText != null)
                globalScaleValueText.Text = $"Общий масштаб: {(globalSettings.GlobalScale * 100):F0}%";
            
            if (useGlobalRotationCheckBox != null)
                useGlobalRotationCheckBox.IsChecked = globalSettings.UseGlobalRotation;
            if (globalRotationSlider != null)
                globalRotationSlider.Value = globalSettings.GlobalRotation;
            var globalRotationValueText = GetGlobalRotationValueText?.Invoke();
            if (globalRotationValueText != null)
                globalRotationValueText.Text = $"Общий поворот: {globalSettings.GlobalRotation:F0}°";
            
            if (transitionTypeComboBox != null)
                transitionTypeComboBox.SelectedIndex = (int)globalSettings.TransitionType;
            if (transitionDurationSlider != null)
                transitionDurationSlider.Value = globalSettings.TransitionDuration;
            var transitionDurationValueText = GetTransitionDurationValueText?.Invoke();
            if (transitionDurationValueText != null)
                transitionDurationValueText.Text = $"Длительность: {globalSettings.TransitionDuration:F1}с";
            
            if (autoPlayNextCheckBox != null)
                autoPlayNextCheckBox.IsChecked = globalSettings.AutoPlayNext;
            if (loopPlaylistCheckBox != null)
                loopPlaylistCheckBox.IsChecked = globalSettings.LoopPlaylist;
            
            // Обновляем TransitionService с загруженными настройками
            _transitionService?.SetGlobalSettings(globalSettings);
            
            // Включаем события обратно
            if (useGlobalVolumeCheckBox != null && UseGlobalVolumeCheckBox_Changed != null)
            {
                useGlobalVolumeCheckBox.Checked += UseGlobalVolumeCheckBox_Changed;
                useGlobalVolumeCheckBox.Unchecked += UseGlobalVolumeCheckBox_Changed;
            }
            if (globalVolumeSlider != null && GlobalVolumeSlider_ValueChanged != null)
                globalVolumeSlider.ValueChanged += GlobalVolumeSlider_ValueChanged;
            if (useGlobalOpacityCheckBox != null && UseGlobalOpacityCheckBox_Changed != null)
            {
                useGlobalOpacityCheckBox.Checked += UseGlobalOpacityCheckBox_Changed;
                useGlobalOpacityCheckBox.Unchecked += UseGlobalOpacityCheckBox_Changed;
            }
            if (globalOpacitySlider != null && GlobalOpacitySlider_ValueChanged != null)
                globalOpacitySlider.ValueChanged += GlobalOpacitySlider_ValueChanged;
            if (useGlobalScaleCheckBox != null && UseGlobalScaleCheckBox_Changed != null)
            {
                useGlobalScaleCheckBox.Checked += UseGlobalScaleCheckBox_Changed;
                useGlobalScaleCheckBox.Unchecked += UseGlobalScaleCheckBox_Changed;
            }
            if (globalScaleSlider != null && GlobalScaleSlider_ValueChanged != null)
                globalScaleSlider.ValueChanged += GlobalScaleSlider_ValueChanged;
            if (useGlobalRotationCheckBox != null && UseGlobalRotationCheckBox_Changed != null)
            {
                useGlobalRotationCheckBox.Checked += UseGlobalRotationCheckBox_Changed;
                useGlobalRotationCheckBox.Unchecked += UseGlobalRotationCheckBox_Changed;
            }
            if (globalRotationSlider != null && GlobalRotationSlider_ValueChanged != null)
                globalRotationSlider.ValueChanged += GlobalRotationSlider_ValueChanged;
            if (transitionTypeComboBox != null && TransitionTypeComboBox_SelectionChanged != null)
                transitionTypeComboBox.SelectionChanged += TransitionTypeComboBox_SelectionChanged;
            if (transitionDurationSlider != null && TransitionDurationSlider_ValueChanged != null)
                transitionDurationSlider.ValueChanged += TransitionDurationSlider_ValueChanged;
            if (autoPlayNextCheckBox != null && AutoPlayNextCheckBox_Changed != null)
            {
                autoPlayNextCheckBox.Checked += AutoPlayNextCheckBox_Changed;
                autoPlayNextCheckBox.Unchecked += AutoPlayNextCheckBox_Changed;
            }
            if (loopPlaylistCheckBox != null && LoopPlaylistCheckBox_Changed != null)
            {
                loopPlaylistCheckBox.Checked += LoopPlaylistCheckBox_Changed;
                loopPlaylistCheckBox.Unchecked += LoopPlaylistCheckBox_Changed;
            }
            
            // Обновляем SettingsManager с загруженными настройками
            _settingsManager?.SetGlobalSettings(globalSettings);
            
            // Применяем общие настройки к активным медиа элементам после загрузки
            System.Diagnostics.Debug.WriteLine($"LoadGlobalSettings: Вызываем ApplyGlobalSettings() после загрузки настроек");
            ApplyGlobalSettings?.Invoke();
        }
    }
}


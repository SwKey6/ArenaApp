using System;
using System.Windows;
using System.Windows.Controls;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для обработки событий глобальных настроек
    /// </summary>
    public class GlobalSettingsEventHandlerService
    {
        private ProjectManager? _projectManager;
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
        
        // Делегаты для работы с выбранным элементом
        public Func<MediaSlot?>? GetSelectedElementSlot { get; set; }
        public Func<string?>? GetSelectedElementKey { get; set; }
        
        // Делегаты для применения настроек
        public Action? ApplyGlobalSettings { get; set; }
        public Action? ApplyElementSettings { get; set; }
        
        // Делегаты для сохранения проекта
        public Action? SaveProject { get; set; }
        
        public void SetProjectManager(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        
        public void SetTransitionService(TransitionService transitionService)
        {
            _transitionService = transitionService;
        }
        
        /// <summary>
        /// Обработчик изменения чекбокса использования глобальной громкости
        /// </summary>
        public void OnUseGlobalVolumeCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var useGlobalVolumeCheckBox = GetUseGlobalVolumeCheckBox?.Invoke();
            if (useGlobalVolumeCheckBox != null)
            {
                _projectManager.CurrentProject.GlobalSettings.UseGlobalVolume = useGlobalVolumeCheckBox.IsChecked == true;
                ApplyGlobalSettings?.Invoke();
                SaveProject?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения слайдера глобальной громкости
        /// </summary>
        public void OnGlobalVolumeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var globalVolumeSlider = GetGlobalVolumeSlider?.Invoke();
            var globalVolumeValueText = GetGlobalVolumeValueText?.Invoke();
            
            if (globalVolumeSlider != null)
            {
                _projectManager.CurrentProject.GlobalSettings.GlobalVolume = globalVolumeSlider.Value;
                if (globalVolumeValueText != null)
                {
                    globalVolumeValueText.Text = $"Общая громкость: {(globalVolumeSlider.Value * 100):F0}%";
                }
                ApplyGlobalSettings?.Invoke();
                SaveProject?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик клика по пресету глобальной громкости
        /// </summary>
        public void OnGlobalVolumePresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && double.TryParse(button.Tag?.ToString(), out double volume))
            {
                var globalVolumeSlider = GetGlobalVolumeSlider?.Invoke();
                if (globalVolumeSlider != null)
                {
                    globalVolumeSlider.Value = volume;
                }
            }
        }
        
        /// <summary>
        /// Обработчик изменения чекбокса использования глобального масштаба
        /// </summary>
        public void OnUseGlobalScaleCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var useGlobalScaleCheckBox = GetUseGlobalScaleCheckBox?.Invoke();
            if (useGlobalScaleCheckBox != null)
            {
                _projectManager.CurrentProject.GlobalSettings.UseGlobalScale = useGlobalScaleCheckBox.IsChecked == true;
                ApplyGlobalSettings?.Invoke();
                
                // Также применяем настройки к выбранному элементу если он есть
                if (GetSelectedElementSlot?.Invoke() != null && !string.IsNullOrEmpty(GetSelectedElementKey?.Invoke()))
                {
                    ApplyElementSettings?.Invoke();
                }
                
                SaveProject?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения слайдера глобального масштаба
        /// </summary>
        public void OnGlobalScaleSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var globalScaleSlider = GetGlobalScaleSlider?.Invoke();
            var globalScaleValueText = GetGlobalScaleValueText?.Invoke();
            
            if (globalScaleSlider != null)
            {
                _projectManager.CurrentProject.GlobalSettings.GlobalScale = globalScaleSlider.Value;
                if (globalScaleValueText != null)
                {
                    globalScaleValueText.Text = $"Общий масштаб: {(globalScaleSlider.Value * 100):F0}%";
                }
                ApplyGlobalSettings?.Invoke();
                
                // Также применяем настройки к выбранному элементу если он есть
                if (GetSelectedElementSlot?.Invoke() != null && !string.IsNullOrEmpty(GetSelectedElementKey?.Invoke()))
                {
                    ApplyElementSettings?.Invoke();
                }
                
                SaveProject?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик клика по пресету глобального масштаба
        /// </summary>
        public void OnGlobalScalePresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && double.TryParse(button.Tag?.ToString(), out double scale))
            {
                var globalScaleSlider = GetGlobalScaleSlider?.Invoke();
                if (globalScaleSlider != null)
                {
                    globalScaleSlider.Value = scale;
                }
            }
        }
        
        /// <summary>
        /// Обработчик изменения чекбокса использования глобального поворота
        /// </summary>
        public void OnUseGlobalRotationCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var useGlobalRotationCheckBox = GetUseGlobalRotationCheckBox?.Invoke();
            if (useGlobalRotationCheckBox != null)
            {
                _projectManager.CurrentProject.GlobalSettings.UseGlobalRotation = useGlobalRotationCheckBox.IsChecked == true;
                ApplyGlobalSettings?.Invoke();
                
                // Также применяем настройки к выбранному элементу если он есть
                if (GetSelectedElementSlot?.Invoke() != null && !string.IsNullOrEmpty(GetSelectedElementKey?.Invoke()))
                {
                    ApplyElementSettings?.Invoke();
                }
                
                SaveProject?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения слайдера глобального поворота
        /// </summary>
        public void OnGlobalRotationSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var globalRotationSlider = GetGlobalRotationSlider?.Invoke();
            var globalRotationValueText = GetGlobalRotationValueText?.Invoke();
            
            if (globalRotationSlider != null)
            {
                _projectManager.CurrentProject.GlobalSettings.GlobalRotation = globalRotationSlider.Value;
                if (globalRotationValueText != null)
                {
                    globalRotationValueText.Text = $"Общий поворот: {globalRotationSlider.Value:F0}°";
                }
                ApplyGlobalSettings?.Invoke();
                
                // Также применяем настройки к выбранному элементу если он есть
                if (GetSelectedElementSlot?.Invoke() != null && !string.IsNullOrEmpty(GetSelectedElementKey?.Invoke()))
                {
                    ApplyElementSettings?.Invoke();
                }
                
                SaveProject?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик клика по пресету глобального поворота
        /// </summary>
        public void OnGlobalRotationPresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && double.TryParse(button.Tag?.ToString(), out double rotation))
            {
                var globalRotationSlider = GetGlobalRotationSlider?.Invoke();
                if (globalRotationSlider != null)
                {
                    globalRotationSlider.Value = rotation;
                }
            }
        }
        
        /// <summary>
        /// Обработчик изменения чекбокса использования глобальной прозрачности
        /// </summary>
        public void OnUseGlobalOpacityCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var useGlobalOpacityCheckBox = GetUseGlobalOpacityCheckBox?.Invoke();
            if (useGlobalOpacityCheckBox != null)
            {
                _projectManager.CurrentProject.GlobalSettings.UseGlobalOpacity = useGlobalOpacityCheckBox.IsChecked == true;
                ApplyGlobalSettings?.Invoke();
                
                // Также применяем настройки к выбранному элементу если он есть
                if (GetSelectedElementSlot?.Invoke() != null && !string.IsNullOrEmpty(GetSelectedElementKey?.Invoke()))
                {
                    ApplyElementSettings?.Invoke();
                }
                
                SaveProject?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения слайдера глобальной прозрачности
        /// </summary>
        public void OnGlobalOpacitySliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var globalOpacitySlider = GetGlobalOpacitySlider?.Invoke();
            var globalOpacityValueText = GetGlobalOpacityValueText?.Invoke();
            
            if (globalOpacitySlider != null)
            {
                _projectManager.CurrentProject.GlobalSettings.GlobalOpacity = globalOpacitySlider.Value;
                if (globalOpacityValueText != null)
                {
                    globalOpacityValueText.Text = $"Общая прозрачность: {(globalOpacitySlider.Value * 100):F0}%";
                }
                ApplyGlobalSettings?.Invoke();
                
                // Также применяем настройки к выбранному элементу если он есть
                if (GetSelectedElementSlot?.Invoke() != null && !string.IsNullOrEmpty(GetSelectedElementKey?.Invoke()))
                {
                    ApplyElementSettings?.Invoke();
                }
                
                SaveProject?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения типа перехода
        /// </summary>
        public void OnTransitionTypeComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var transitionTypeComboBox = GetTransitionTypeComboBox?.Invoke();
            if (transitionTypeComboBox != null)
            {
                _projectManager.CurrentProject.GlobalSettings.TransitionType = (TransitionType)transitionTypeComboBox.SelectedIndex;
                
                // Обновляем TransitionService с новыми настройками
                _transitionService?.SetGlobalSettings(_projectManager.CurrentProject.GlobalSettings);
                
                SaveProject?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения длительности перехода
        /// </summary>
        public void OnTransitionDurationSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var transitionDurationSlider = GetTransitionDurationSlider?.Invoke();
            var transitionDurationValueText = GetTransitionDurationValueText?.Invoke();
            
            if (transitionDurationSlider != null)
            {
                _projectManager.CurrentProject.GlobalSettings.TransitionDuration = transitionDurationSlider.Value;
                if (transitionDurationValueText != null)
                {
                    transitionDurationValueText.Text = $"Длительность: {transitionDurationSlider.Value:F1}с";
                }
                
                // Обновляем TransitionService с новыми настройками
                _transitionService?.SetGlobalSettings(_projectManager.CurrentProject.GlobalSettings);
                
                SaveProject?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения чекбокса автоперехода
        /// </summary>
        public void OnAutoPlayNextCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var autoPlayNextCheckBox = GetAutoPlayNextCheckBox?.Invoke();
            if (autoPlayNextCheckBox != null)
            {
                _projectManager.CurrentProject.GlobalSettings.AutoPlayNext = autoPlayNextCheckBox.IsChecked == true;
                SaveProject?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения чекбокса зацикливания плейлиста
        /// </summary>
        public void OnLoopPlaylistCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var loopPlaylistCheckBox = GetLoopPlaylistCheckBox?.Invoke();
            if (loopPlaylistCheckBox != null)
            {
                _projectManager.CurrentProject.GlobalSettings.LoopPlaylist = loopPlaylistCheckBox.IsChecked == true;
                SaveProject?.Invoke();
            }
        }
    }
}


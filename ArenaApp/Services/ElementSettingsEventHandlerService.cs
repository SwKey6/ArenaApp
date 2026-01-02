using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для обработки событий настроек элементов
    /// </summary>
    public class ElementSettingsEventHandlerService
    {
        // Делегаты для доступа к UI элементам
        public Func<Slider>? GetSpeedSlider { get; set; }
        public Func<TextBlock>? GetSpeedValueText { get; set; }
        public Func<Slider>? GetOpacitySlider { get; set; }
        public Func<TextBlock>? GetOpacityValueText { get; set; }
        public Func<Slider>? GetVolumeSlider { get; set; }
        public Func<TextBlock>? GetVolumeValueText { get; set; }
        public Func<Slider>? GetScaleSlider { get; set; }
        public Func<TextBlock>? GetScaleValueText { get; set; }
        public Func<Slider>? GetRotationSlider { get; set; }
        public Func<TextBlock>? GetRotationValueText { get; set; }
        public Func<Button>? GetHideTextButton { get; set; }
        public Func<ComboBox>? GetTextColorComboBox { get; set; }
        public Func<ComboBox>? GetFontFamilyComboBox { get; set; }
        public Func<Slider>? GetFontSizeSlider { get; set; }
        public Func<TextBlock>? GetFontSizeValueText { get; set; }
        public Func<TextBox>? GetTextContentTextBox { get; set; }
        public Func<CheckBox>? GetUseManualPositionCheckBox { get; set; }
        public Func<Panel>? GetManualPositionPanel { get; set; }
        public Func<TextBox>? GetTextXTextBox { get; set; }
        public Func<TextBox>? GetTextYTextBox { get; set; }
        public Func<Button>? GetElementPlayButton { get; set; }
        
        // Делегаты для работы с выбранным элементом
        public Func<MediaSlot?>? GetSelectedElementSlot { get; set; }
        public Func<string?>? GetSelectedElementKey { get; set; }
        
        // Делегаты для применения настроек
        public Action? ApplyElementSettings { get; set; }
        public Action? ApplyTextSettings { get; set; }
        public Action? UpdateElementTitle { get; set; }
        
        // Делегаты для управления элементом
        public Action<MediaSlot, string>? PlayElement { get; set; }
        public Action<MediaSlot, string>? StopElement { get; set; }
        public Func<MediaSlot, string, System.Threading.Tasks.Task>? RestartElement { get; set; }
        
        /// <summary>
        /// Обработчик изменения скорости воспроизведения
        /// </summary>
        public void OnSpeedSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null) return;
            
            var speedSlider = GetSpeedSlider?.Invoke();
            var speedValueText = GetSpeedValueText?.Invoke();
            
            if (speedSlider != null)
            {
                selectedSlot.PlaybackSpeed = speedSlider.Value;
                if (speedValueText != null)
                {
                    speedValueText.Text = $"Скорость: {speedSlider.Value:F1}x";
                }
                
                // Применяем настройки только если слайдер не перетаскивается
                if (!speedSlider.IsMouseCaptured)
                {
                    ApplyElementSettings?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// Обработчик окончания перетаскивания слайдера скорости
        /// </summary>
        public void OnSpeedSliderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot != null)
            {
                ApplyElementSettings?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик клика по пресету скорости
        /// </summary>
        public void OnSpeedPresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && double.TryParse(button.Tag?.ToString(), out double speed))
            {
                var speedSlider = GetSpeedSlider?.Invoke();
                if (speedSlider != null)
                {
                    speedSlider.Value = speed;
                }
            }
        }
        
        /// <summary>
        /// Обработчик изменения прозрачности
        /// </summary>
        public void OnOpacitySliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null) return;
            
            var opacitySlider = GetOpacitySlider?.Invoke();
            var opacityValueText = GetOpacityValueText?.Invoke();
            
            if (opacitySlider != null)
            {
                selectedSlot.Opacity = opacitySlider.Value;
                if (opacityValueText != null)
                {
                    opacityValueText.Text = $"Прозрачность: {(opacitySlider.Value * 100):F0}%";
                }
                ApplyElementSettings?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения громкости
        /// </summary>
        public void OnVolumeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null) return;
            
            var volumeSlider = GetVolumeSlider?.Invoke();
            var volumeValueText = GetVolumeValueText?.Invoke();
            
            if (volumeSlider != null)
            {
                selectedSlot.Volume = volumeSlider.Value;
                if (volumeValueText != null)
                {
                    volumeValueText.Text = $"Звук: {(volumeSlider.Value * 100):F0}%";
                }
                ApplyElementSettings?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик клика по пресету громкости
        /// </summary>
        public void OnVolumePresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && double.TryParse(button.Tag?.ToString(), out double volume))
            {
                var volumeSlider = GetVolumeSlider?.Invoke();
                if (volumeSlider != null)
                {
                    volumeSlider.Value = volume;
                }
            }
        }
        
        /// <summary>
        /// Обработчик изменения масштаба
        /// </summary>
        public void OnScaleSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null) return;
            
            var scaleSlider = GetScaleSlider?.Invoke();
            var scaleValueText = GetScaleValueText?.Invoke();
            
            if (scaleSlider != null)
            {
                selectedSlot.Scale = scaleSlider.Value;
                if (scaleValueText != null)
                {
                    scaleValueText.Text = $"Масштаб: {(scaleSlider.Value * 100):F0}%";
                }
                ApplyElementSettings?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик клика по пресету масштаба
        /// </summary>
        public void OnScalePresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && double.TryParse(button.Tag?.ToString(), out double scale))
            {
                var scaleSlider = GetScaleSlider?.Invoke();
                if (scaleSlider != null)
                {
                    scaleSlider.Value = scale;
                }
            }
        }
        
        /// <summary>
        /// Обработчик изменения поворота
        /// </summary>
        public void OnRotationSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null) return;
            
            var rotationSlider = GetRotationSlider?.Invoke();
            var rotationValueText = GetRotationValueText?.Invoke();
            
            if (rotationSlider != null)
            {
                selectedSlot.Rotation = rotationSlider.Value;
                if (rotationValueText != null)
                {
                    rotationValueText.Text = $"Поворот: {rotationSlider.Value:F0}°";
                }
                ApplyElementSettings?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик клика по пресету поворота
        /// </summary>
        public void OnRotationPresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && double.TryParse(button.Tag?.ToString(), out double rotation))
            {
                var rotationSlider = GetRotationSlider?.Invoke();
                if (rotationSlider != null)
                {
                    rotationSlider.Value = rotation;
                }
            }
        }
        
        /// <summary>
        /// Обработчик клика по кнопке скрытия/показа текста
        /// </summary>
        public void OnHideTextButtonClick(object sender, RoutedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null || selectedSlot.Type != MediaType.Text) return;
            
            selectedSlot.IsTextVisible = !selectedSlot.IsTextVisible;
            
            var hideTextButton = GetHideTextButton?.Invoke();
            if (hideTextButton != null)
            {
                // Обновляем кнопку
                if (selectedSlot.IsTextVisible)
                {
                    hideTextButton.Content = "👁️ Скрыть текст";
                    hideTextButton.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Красный
                }
                else
                {
                    hideTextButton.Content = "👁️ Показать текст";
                    hideTextButton.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зеленый
                }
            }
            
            // Применяем изменения к отображаемому тексту
            ApplyTextSettings?.Invoke();
        }
        
        /// <summary>
        /// Обработчик изменения цвета текста
        /// </summary>
        public void OnTextColorComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null || selectedSlot.Type != MediaType.Text) return;
            
            var textColorComboBox = GetTextColorComboBox?.Invoke();
            if (textColorComboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedSlot.FontColor = selectedItem.Tag?.ToString() ?? "White";
                ApplyTextSettings?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения шрифта
        /// </summary>
        public void OnFontFamilyComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null || selectedSlot.Type != MediaType.Text) return;
            
            var fontFamilyComboBox = GetFontFamilyComboBox?.Invoke();
            if (fontFamilyComboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedSlot.FontFamily = selectedItem.Tag?.ToString() ?? "Arial";
                ApplyTextSettings?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения размера шрифта
        /// </summary>
        public void OnFontSizeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null || selectedSlot.Type != MediaType.Text) return;
            
            var fontSizeSlider = GetFontSizeSlider?.Invoke();
            var fontSizeValueText = GetFontSizeValueText?.Invoke();
            
            if (fontSizeSlider != null)
            {
                selectedSlot.FontSize = fontSizeSlider.Value;
                if (fontSizeValueText != null)
                {
                    fontSizeValueText.Text = $"{fontSizeSlider.Value:F0}px";
                }
                ApplyTextSettings?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения содержимого текста
        /// </summary>
        public void OnTextContentTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null || selectedSlot.Type != MediaType.Text) return;
            
            var textContentTextBox = GetTextContentTextBox?.Invoke();
            if (textContentTextBox != null)
            {
                selectedSlot.TextContent = textContentTextBox.Text;
                ApplyTextSettings?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик включения ручного позиционирования
        /// </summary>
        public void OnUseManualPositionCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null || selectedSlot.Type != MediaType.Text) return;
            
            selectedSlot.UseManualPosition = true;
            var manualPositionPanel = GetManualPositionPanel?.Invoke();
            if (manualPositionPanel != null)
            {
                manualPositionPanel.Visibility = Visibility.Visible;
            }
            ApplyTextSettings?.Invoke();
        }
        
        /// <summary>
        /// Обработчик выключения ручного позиционирования
        /// </summary>
        public void OnUseManualPositionCheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null || selectedSlot.Type != MediaType.Text) return;
            
            selectedSlot.UseManualPosition = false;
            var manualPositionPanel = GetManualPositionPanel?.Invoke();
            if (manualPositionPanel != null)
            {
                manualPositionPanel.Visibility = Visibility.Collapsed;
            }
            ApplyTextSettings?.Invoke();
        }
        
        /// <summary>
        /// Обработчик изменения координаты X текста
        /// </summary>
        public void OnTextXTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null || selectedSlot.Type != MediaType.Text) return;
            
            var textXTextBox = GetTextXTextBox?.Invoke();
            if (textXTextBox != null && double.TryParse(textXTextBox.Text, out double x))
            {
                selectedSlot.TextX = x;
                ApplyTextSettings?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик изменения координаты Y текста
        /// </summary>
        public void OnTextYTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null || selectedSlot.Type != MediaType.Text) return;
            
            var textYTextBox = GetTextYTextBox?.Invoke();
            if (textYTextBox != null && double.TryParse(textYTextBox.Text, out double y))
            {
                selectedSlot.TextY = y;
                ApplyTextSettings?.Invoke();
            }
        }
        
        /// <summary>
        /// Обработчик клика по кнопке воспроизведения элемента
        /// </summary>
        public void OnElementPlayClick(object sender, RoutedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            var selectedKey = GetSelectedElementKey?.Invoke();
            
            if (selectedSlot == null || string.IsNullOrEmpty(selectedKey)) return;
            
            PlayElement?.Invoke(selectedSlot, selectedKey);
            
            var elementPlayButton = GetElementPlayButton?.Invoke();
            if (elementPlayButton != null)
            {
                // Обновляем кнопку
                elementPlayButton.Content = "⏸️";
                elementPlayButton.ToolTip = "Пауза";
            }
        }
        
        /// <summary>
        /// Обработчик клика по кнопке остановки элемента
        /// </summary>
        public void OnElementStopClick(object sender, RoutedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            var selectedKey = GetSelectedElementKey?.Invoke();
            
            if (selectedSlot == null || string.IsNullOrEmpty(selectedKey)) return;
            
            StopElement?.Invoke(selectedSlot, selectedKey);
            
            var elementPlayButton = GetElementPlayButton?.Invoke();
            if (elementPlayButton != null)
            {
                // Сбрасываем состояние кнопки "Продолжить"
                elementPlayButton.Content = "▶️";
                elementPlayButton.ToolTip = "Воспроизвести";
            }
        }
        
        /// <summary>
        /// Обработчик клика по кнопке перезапуска элемента
        /// </summary>
        public async void OnElementRestartClick(object sender, RoutedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            var selectedKey = GetSelectedElementKey?.Invoke();
            
            if (selectedSlot == null || string.IsNullOrEmpty(selectedKey)) return;
            
            if (RestartElement != null)
            {
                await RestartElement(selectedSlot, selectedKey);
            }
            
            var elementPlayButton = GetElementPlayButton?.Invoke();
            if (elementPlayButton != null)
            {
                // Сбрасываем состояние кнопки "Продолжить"
                elementPlayButton.Content = "▶️";
                elementPlayButton.ToolTip = "Воспроизвести";
            }
        }
        
        /// <summary>
        /// Обработчик клика по кнопке переименования элемента
        /// </summary>
        public void OnRenameElementButtonClick(object sender, RoutedEventArgs e)
        {
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null) return;
            
            // Показываем диалог переименования
            string currentName = selectedSlot.DisplayName;
            string? newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите новое имя элемента:", 
                "Переименование элемента", 
                currentName);
                
            if (!string.IsNullOrWhiteSpace(newName) && newName != currentName)
            {
                selectedSlot.DisplayName = newName;
                UpdateElementTitle?.Invoke();
            }
        }
    }
}


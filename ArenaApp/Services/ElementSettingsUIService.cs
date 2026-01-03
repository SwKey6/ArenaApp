using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления UI элементами настроек
    /// </summary>
    public class ElementSettingsUIService
    {
        private ProjectManager? _projectManager;
        
        // Делегаты для доступа к UI элементам
        public Func<TextBlock>? GetNoElementSelectedText { get; set; }
        public Func<Panel>? GetSettingsContentPanel { get; set; }
        public Func<Button>? GetRenameElementButton { get; set; }
        public Func<Button>? GetPreviousElementButton { get; set; }
        public Func<Button>? GetNextElementButton { get; set; }
        public Func<TextBlock>? GetElementTitleText { get; set; }
        
        // Делегаты для слайдеров
        public Func<Slider>? GetSpeedSlider { get; set; }
        public Func<Slider>? GetOpacitySlider { get; set; }
        public Func<Slider>? GetVolumeSlider { get; set; }
        public Func<Slider>? GetScaleSlider { get; set; }
        public Func<Slider>? GetRotationSlider { get; set; }
        
        // Делегаты для текстовых меток
        public Func<TextBlock>? GetSpeedValueText { get; set; }
        public Func<TextBlock>? GetOpacityValueText { get; set; }
        public Func<TextBlock>? GetVolumeValueText { get; set; }
        public Func<TextBlock>? GetScaleValueText { get; set; }
        public Func<TextBlock>? GetRotationValueText { get; set; }
        
        // Делегаты для GroupBox
        public Func<GroupBox>? GetSpeedGroupBox { get; set; }
        public Func<GroupBox>? GetOpacityGroupBox { get; set; }
        public Func<GroupBox>? GetVolumeGroupBox { get; set; }
        public Func<GroupBox>? GetTextSettingsGroupBox { get; set; }
        
        // Делегаты для текстовых настроек
        public Func<ComboBox>? GetTextColorComboBox { get; set; }
        public Func<ComboBox>? GetFontFamilyComboBox { get; set; }
        public Func<Slider>? GetFontSizeSlider { get; set; }
        public Func<TextBlock>? GetFontSizeValueText { get; set; }
        public Func<TextBox>? GetTextContentTextBox { get; set; }
        public Func<CheckBox>? GetUseManualPositionCheckBox { get; set; }
        public Func<Panel>? GetManualPositionPanel { get; set; }
        public Func<TextBox>? GetTextXTextBox { get; set; }
        public Func<TextBox>? GetTextYTextBox { get; set; }
        public Func<Button>? GetHideTextButton { get; set; }
        
        // Делегаты для событий
        public RoutedPropertyChangedEventHandler<double>? SpeedSlider_ValueChanged { get; set; }
        public RoutedPropertyChangedEventHandler<double>? OpacitySlider_ValueChanged { get; set; }
        public RoutedPropertyChangedEventHandler<double>? VolumeSlider_ValueChanged { get; set; }
        public RoutedPropertyChangedEventHandler<double>? ScaleSlider_ValueChanged { get; set; }
        public RoutedPropertyChangedEventHandler<double>? RotationSlider_ValueChanged { get; set; }
        public SelectionChangedEventHandler? TextColorComboBox_SelectionChanged { get; set; }
        public SelectionChangedEventHandler? FontFamilyComboBox_SelectionChanged { get; set; }
        public RoutedPropertyChangedEventHandler<double>? FontSizeSlider_ValueChanged { get; set; }
        public TextChangedEventHandler? TextContentTextBox_TextChanged { get; set; }
        public RoutedEventHandler? UseManualPositionCheckBox_Checked { get; set; }
        public RoutedEventHandler? UseManualPositionCheckBox_Unchecked { get; set; }
        public TextChangedEventHandler? TextXTextBox_TextChanged { get; set; }
        public TextChangedEventHandler? TextYTextBox_TextChanged { get; set; }
        
        // Делегаты для применения настроек
        public Action? ApplyElementSettings { get; set; }
        
        public void SetProjectManager(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        
        /// <summary>
        /// Выбирает элемент для настройки и обновляет UI
        /// </summary>
        public void SelectElementForSettings(MediaSlot slot, string slotKey)
        {
            if (slot == null) return;
            
            // Устанавливаем DisplayName по умолчанию если пустое
            if (string.IsNullOrEmpty(slot.DisplayName))
            {
                slot.DisplayName = System.IO.Path.GetFileNameWithoutExtension(slot.MediaPath);
            }
            
            // Показываем панель настроек
            var noElementSelectedText = GetNoElementSelectedText?.Invoke();
            var settingsContentPanel = GetSettingsContentPanel?.Invoke();
            var renameElementButton = GetRenameElementButton?.Invoke();
            var previousElementButton = GetPreviousElementButton?.Invoke();
            var nextElementButton = GetNextElementButton?.Invoke();
            
            if (noElementSelectedText != null)
                noElementSelectedText.Visibility = Visibility.Collapsed;
            if (settingsContentPanel != null)
                settingsContentPanel.Visibility = Visibility.Visible;
            if (renameElementButton != null)
                renameElementButton.Visibility = Visibility.Visible;
            
            // Показываем кнопки навигации если есть больше одного элемента
            bool hasMultipleElements = _projectManager?.CurrentProject?.MediaSlots?.Count() > 1;
            if (previousElementButton != null)
                previousElementButton.Visibility = hasMultipleElements ? Visibility.Visible : Visibility.Collapsed;
            if (nextElementButton != null)
                nextElementButton.Visibility = hasMultipleElements ? Visibility.Visible : Visibility.Collapsed;
            
            // Загружаем текущие настройки элемента
            LoadElementSettings(slot);
        }
        
        /// <summary>
        /// Снимает выбор элемента и скрывает панель настроек
        /// </summary>
        public void UnselectElement()
        {
            var noElementSelectedText = GetNoElementSelectedText?.Invoke();
            var settingsContentPanel = GetSettingsContentPanel?.Invoke();
            var renameElementButton = GetRenameElementButton?.Invoke();
            var previousElementButton = GetPreviousElementButton?.Invoke();
            var nextElementButton = GetNextElementButton?.Invoke();
            var elementTitleText = GetElementTitleText?.Invoke();
            
            if (noElementSelectedText != null)
                noElementSelectedText.Visibility = Visibility.Visible;
            if (settingsContentPanel != null)
                settingsContentPanel.Visibility = Visibility.Collapsed;
            if (renameElementButton != null)
                renameElementButton.Visibility = Visibility.Collapsed;
            if (previousElementButton != null)
                previousElementButton.Visibility = Visibility.Collapsed;
            if (nextElementButton != null)
                nextElementButton.Visibility = Visibility.Collapsed;
            if (elementTitleText != null)
                elementTitleText.Text = "Настройки элемента";
        }
        
        /// <summary>
        /// Загружает настройки элемента в UI
        /// </summary>
        public void LoadElementSettings(MediaSlot slot)
        {
            if (slot == null) return;
            
            var speedSlider = GetSpeedSlider?.Invoke();
            var opacitySlider = GetOpacitySlider?.Invoke();
            var volumeSlider = GetVolumeSlider?.Invoke();
            var scaleSlider = GetScaleSlider?.Invoke();
            var rotationSlider = GetRotationSlider?.Invoke();
            
            // Устанавливаем значения слайдеров без вызова событий
            if (speedSlider != null && SpeedSlider_ValueChanged != null)
                speedSlider.ValueChanged -= SpeedSlider_ValueChanged;
            if (opacitySlider != null && OpacitySlider_ValueChanged != null)
                opacitySlider.ValueChanged -= OpacitySlider_ValueChanged;
            if (volumeSlider != null && VolumeSlider_ValueChanged != null)
                volumeSlider.ValueChanged -= VolumeSlider_ValueChanged;
            if (scaleSlider != null && ScaleSlider_ValueChanged != null)
                scaleSlider.ValueChanged -= ScaleSlider_ValueChanged;
            if (rotationSlider != null && RotationSlider_ValueChanged != null)
                rotationSlider.ValueChanged -= RotationSlider_ValueChanged;
            
            if (speedSlider != null)
                speedSlider.Value = slot.PlaybackSpeed;
            if (opacitySlider != null)
                opacitySlider.Value = slot.Opacity;
            if (volumeSlider != null)
                volumeSlider.Value = slot.Volume;
            if (scaleSlider != null)
                scaleSlider.Value = slot.Scale;
            if (rotationSlider != null)
                rotationSlider.Value = slot.Rotation;
            
            if (speedSlider != null && SpeedSlider_ValueChanged != null)
                speedSlider.ValueChanged += SpeedSlider_ValueChanged;
            if (opacitySlider != null && OpacitySlider_ValueChanged != null)
                opacitySlider.ValueChanged += OpacitySlider_ValueChanged;
            if (volumeSlider != null && VolumeSlider_ValueChanged != null)
                volumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            if (scaleSlider != null && ScaleSlider_ValueChanged != null)
                scaleSlider.ValueChanged += ScaleSlider_ValueChanged;
            if (rotationSlider != null && RotationSlider_ValueChanged != null)
                rotationSlider.ValueChanged += RotationSlider_ValueChanged;
            
            // Обновляем текстовые метки
            var speedValueText = GetSpeedValueText?.Invoke();
            var opacityValueText = GetOpacityValueText?.Invoke();
            var volumeValueText = GetVolumeValueText?.Invoke();
            var scaleValueText = GetScaleValueText?.Invoke();
            var rotationValueText = GetRotationValueText?.Invoke();
            
            if (speedValueText != null)
                speedValueText.Text = $"Скорость: {slot.PlaybackSpeed:F1}x";
            if (opacityValueText != null)
                opacityValueText.Text = $"Прозрачность: {(slot.Opacity * 100):F0}%";
            if (volumeValueText != null)
                volumeValueText.Text = $"Звук: {(slot.Volume * 100):F0}%";
            if (scaleValueText != null)
                scaleValueText.Text = $"Масштаб: {(slot.Scale * 100):F0}%";
            if (rotationValueText != null)
                rotationValueText.Text = $"Поворот: {slot.Rotation:F0}°";
            
            // Показываем или скрываем секции настроек в зависимости от типа элемента
            var speedGroupBox = GetSpeedGroupBox?.Invoke();
            var opacityGroupBox = GetOpacityGroupBox?.Invoke();
            var volumeGroupBox = GetVolumeGroupBox?.Invoke();
            var textSettingsGroupBox = GetTextSettingsGroupBox?.Invoke();
            
            if (slot.Type == MediaType.Text)
            {
                // Для текстовых элементов скрываем ненужные настройки
                if (speedGroupBox != null)
                    speedGroupBox.Visibility = Visibility.Collapsed;
                if (opacityGroupBox != null)
                    opacityGroupBox.Visibility = Visibility.Collapsed;
                if (volumeGroupBox != null)
                    volumeGroupBox.Visibility = Visibility.Collapsed;
                
                // Показываем настройки текста
                if (textSettingsGroupBox != null)
                    textSettingsGroupBox.Visibility = Visibility.Visible;
                LoadTextSettings(slot);
            }
            else if (slot.Type == MediaType.Image)
            {
                // Для изображений скрываем скорость и громкость (они не применимы)
                if (speedGroupBox != null)
                    speedGroupBox.Visibility = Visibility.Collapsed;
                if (volumeGroupBox != null)
                    volumeGroupBox.Visibility = Visibility.Collapsed;
                
                // Показываем прозрачность и другие настройки
                if (opacityGroupBox != null)
                    opacityGroupBox.Visibility = Visibility.Visible;
                
                // Скрываем настройки текста
                if (textSettingsGroupBox != null)
                    textSettingsGroupBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Для видео и аудио показываем все настройки
                if (speedGroupBox != null)
                    speedGroupBox.Visibility = Visibility.Visible;
                if (opacityGroupBox != null)
                    opacityGroupBox.Visibility = Visibility.Visible;
                if (volumeGroupBox != null)
                    volumeGroupBox.Visibility = Visibility.Visible;
                
                // Скрываем настройки текста
                if (textSettingsGroupBox != null)
                    textSettingsGroupBox.Visibility = Visibility.Collapsed;
            }
            
            // Применяем настройки к активным медиа элементам
            ApplyElementSettings?.Invoke();
            
            // Обновляем заголовок
            UpdateElementTitleInternal(slot);
        }
        
        /// <summary>
        /// Загружает настройки текста в UI
        /// </summary>
        public void LoadTextSettings(MediaSlot slot)
        {
            if (slot == null || slot.Type != MediaType.Text) return;
            
            var textColorComboBox = GetTextColorComboBox?.Invoke();
            var fontFamilyComboBox = GetFontFamilyComboBox?.Invoke();
            var fontSizeSlider = GetFontSizeSlider?.Invoke();
            var fontSizeValueText = GetFontSizeValueText?.Invoke();
            var textContentTextBox = GetTextContentTextBox?.Invoke();
            var useManualPositionCheckBox = GetUseManualPositionCheckBox?.Invoke();
            var manualPositionPanel = GetManualPositionPanel?.Invoke();
            var textXTextBox = GetTextXTextBox?.Invoke();
            var textYTextBox = GetTextYTextBox?.Invoke();
            var hideTextButton = GetHideTextButton?.Invoke();
            
            // Отключаем события чтобы избежать лишних вызовов
            if (textColorComboBox != null && TextColorComboBox_SelectionChanged != null)
                textColorComboBox.SelectionChanged -= TextColorComboBox_SelectionChanged;
            if (fontFamilyComboBox != null && FontFamilyComboBox_SelectionChanged != null)
                fontFamilyComboBox.SelectionChanged -= FontFamilyComboBox_SelectionChanged;
            if (fontSizeSlider != null && FontSizeSlider_ValueChanged != null)
                fontSizeSlider.ValueChanged -= FontSizeSlider_ValueChanged;
            if (textContentTextBox != null && TextContentTextBox_TextChanged != null)
                textContentTextBox.TextChanged -= TextContentTextBox_TextChanged;
            if (useManualPositionCheckBox != null)
            {
                if (UseManualPositionCheckBox_Checked != null)
                    useManualPositionCheckBox.Checked -= UseManualPositionCheckBox_Checked;
                if (UseManualPositionCheckBox_Unchecked != null)
                    useManualPositionCheckBox.Unchecked -= UseManualPositionCheckBox_Unchecked;
            }
            if (textXTextBox != null && TextXTextBox_TextChanged != null)
                textXTextBox.TextChanged -= TextXTextBox_TextChanged;
            if (textYTextBox != null && TextYTextBox_TextChanged != null)
                textYTextBox.TextChanged -= TextYTextBox_TextChanged;
            
            // Загружаем настройки цвета
            if (textColorComboBox != null)
            {
                for (int i = 0; i < textColorComboBox.Items.Count; i++)
                {
                    if (textColorComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == slot.FontColor)
                    {
                        textColorComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            
            // Загружаем шрифт
            if (fontFamilyComboBox != null)
            {
                for (int i = 0; i < fontFamilyComboBox.Items.Count; i++)
                {
                    if (fontFamilyComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == slot.FontFamily)
                    {
                        fontFamilyComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            
            // Загружаем размер шрифта
            if (fontSizeSlider != null)
                fontSizeSlider.Value = slot.FontSize;
            if (fontSizeValueText != null)
                fontSizeValueText.Text = $"{slot.FontSize:F0}px";
            
            // Загружаем содержимое текста
            if (textContentTextBox != null)
                textContentTextBox.Text = slot.TextContent ?? "";
            
            // Загружаем ручную настройку положения
            if (useManualPositionCheckBox != null)
                useManualPositionCheckBox.IsChecked = slot.UseManualPosition;
            if (manualPositionPanel != null)
                manualPositionPanel.Visibility = slot.UseManualPosition ? Visibility.Visible : Visibility.Collapsed;
            if (textXTextBox != null)
                textXTextBox.Text = slot.TextX.ToString();
            if (textYTextBox != null)
                textYTextBox.Text = slot.TextY.ToString();
            
            // Загружаем состояние видимости
            if (hideTextButton != null)
            {
                if (slot.IsTextVisible)
                {
                    hideTextButton.Content = "👁️ Скрыть текст";
                    hideTextButton.Background = new SolidColorBrush(Color.FromRgb(218, 54, 51)); // #DA3633 - DangerBrush
                }
                else
                {
                    hideTextButton.Content = "👁️ Показать текст";
                    hideTextButton.Background = new SolidColorBrush(Color.FromRgb(35, 134, 54)); // #238636 - SuccessBrush
                }
            }
            
            // Включаем события обратно
            if (textColorComboBox != null && TextColorComboBox_SelectionChanged != null)
                textColorComboBox.SelectionChanged += TextColorComboBox_SelectionChanged;
            if (fontFamilyComboBox != null && FontFamilyComboBox_SelectionChanged != null)
                fontFamilyComboBox.SelectionChanged += FontFamilyComboBox_SelectionChanged;
            if (fontSizeSlider != null && FontSizeSlider_ValueChanged != null)
                fontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;
            if (textContentTextBox != null && TextContentTextBox_TextChanged != null)
                textContentTextBox.TextChanged += TextContentTextBox_TextChanged;
            if (useManualPositionCheckBox != null)
            {
                if (UseManualPositionCheckBox_Checked != null)
                    useManualPositionCheckBox.Checked += UseManualPositionCheckBox_Checked;
                if (UseManualPositionCheckBox_Unchecked != null)
                    useManualPositionCheckBox.Unchecked += UseManualPositionCheckBox_Unchecked;
            }
            if (textXTextBox != null && TextXTextBox_TextChanged != null)
                textXTextBox.TextChanged += TextXTextBox_TextChanged;
            if (textYTextBox != null && TextYTextBox_TextChanged != null)
                textYTextBox.TextChanged += TextYTextBox_TextChanged;
        }
        
        /// <summary>
        /// Обновляет заголовок элемента (внутренний метод)
        /// </summary>
        private void UpdateElementTitleInternal(MediaSlot slot)
        {
            UpdateElementTitle(slot);
        }
        
        /// <summary>
        /// Обновляет заголовок элемента
        /// </summary>
        public void UpdateElementTitle(MediaSlot slot)
        {
            if (slot == null) return;
            
            var elementTitleText = GetElementTitleText?.Invoke();
            if (elementTitleText != null)
            {
                elementTitleText.Text = $"Настройки: {slot.DisplayName}";
            }
        }
    }
}


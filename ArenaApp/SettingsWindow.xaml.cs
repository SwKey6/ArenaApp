using System;
using System.Windows;
using System.Windows.Controls;

namespace ArenaApp
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        // Делегаты для сохранения настроек
        public Action<int, int, int, int, double, double>? ApplySettings { get; set; }
        
        // Делегаты для получения текущих настроек
        public Func<(int x, int y)>? GetOutputPosition { get; set; }
        public Func<(int width, int height)>? GetOutputSize { get; set; }
        public Func<(double x, double y)>? GetOutputScale { get; set; }
        
        // Временные настройки (не применяются до нажатия кнопки)
        private int _tempPositionX;
        private int _tempPositionY;
        private int _tempOutputWidth;
        private int _tempOutputHeight;
        private double _tempScaleX;
        private double _tempScaleY;
        
        // Исходные настройки (для отмены)
        private int _originalPositionX;
        private int _originalPositionY;
        private int _originalOutputWidth;
        private int _originalOutputHeight;
        private double _originalScaleX;
        private double _originalScaleY;
        
        public SettingsWindow()
        {
            InitializeComponent();
            // Загружаем настройки после того, как окно загрузится и делегаты будут установлены
            Loaded += (s, e) => LoadSettings();
        }
        
        private void LoadSettings()
        {
            // Загружаем текущие настройки
            if (GetOutputPosition != null)
            {
                var pos = GetOutputPosition();
                _tempPositionX = _originalPositionX = pos.x;
                _tempPositionY = _originalPositionY = pos.y;
                PositionXTextBox.Text = pos.x.ToString();
                PositionYTextBox.Text = pos.y.ToString();
            }
            
            if (GetOutputSize != null)
            {
                var size = GetOutputSize();
                _tempOutputWidth = _originalOutputWidth = size.width;
                _tempOutputHeight = _originalOutputHeight = size.height;
                OutputWidthTextBox.Text = size.width.ToString();
                OutputHeightTextBox.Text = size.height.ToString();
            }
            
            if (GetOutputScale != null)
            {
                var scale = GetOutputScale();
                _tempScaleX = _originalScaleX = scale.x;
                _tempScaleY = _originalScaleY = scale.y;
                ScaleXSlider.Value = scale.x;
                ScaleYSlider.Value = scale.y;
                UpdateScaleText();
            }
        }
        
        private void OutputSectionButton_Click(object sender, RoutedEventArgs e)
        {
            // Показываем раздел "Вывод"
            OutputSection.Visibility = Visibility.Visible;
        }
        
        private void PositionXTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(PositionXTextBox.Text, out int x))
            {
                _tempPositionX = x;
            }
        }
        
        private void PositionYTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(PositionYTextBox.Text, out int y))
            {
                _tempPositionY = y;
            }
        }
        
        private void OutputWidthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(OutputWidthTextBox.Text, out int width))
            {
                _tempOutputWidth = width;
            }
        }
        
        private void OutputHeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(OutputHeightTextBox.Text, out int height))
            {
                _tempOutputHeight = height;
            }
        }
        
        private void ScaleXSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _tempScaleX = ScaleXSlider.Value;
            UpdateScaleText();
        }
        
        private void ScaleYSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _tempScaleY = ScaleYSlider.Value;
            UpdateScaleText();
        }
        
        private void UpdateScaleText()
        {
            if (ScaleXValueText != null && ScaleXSlider != null)
            {
                ScaleXValueText.Text = $"{(int)ScaleXSlider.Value}%";
            }
            if (ScaleYValueText != null && ScaleYSlider != null)
            {
                ScaleYValueText.Text = $"{(int)ScaleYSlider.Value}%";
            }
        }
        
        private void ApplySettingsToSystem()
        {
            ApplySettings?.Invoke(_tempPositionX, _tempPositionY, _tempOutputWidth, _tempOutputHeight, _tempScaleX, _tempScaleY);
            
            // Обновляем исходные значения
            _originalPositionX = _tempPositionX;
            _originalPositionY = _tempPositionY;
            _originalOutputWidth = _tempOutputWidth;
            _originalOutputHeight = _tempOutputHeight;
            _originalScaleX = _tempScaleX;
            _originalScaleY = _tempScaleY;
        }
        
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplySettingsToSystem();
            DialogResult = true;
            Close();
        }
        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ApplySettingsToSystem();
            // Окно остается открытым
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, были ли изменения
            bool hasChanges = _tempPositionX != _originalPositionX ||
                            _tempPositionY != _originalPositionY ||
                            _tempOutputWidth != _originalOutputWidth ||
                            _tempOutputHeight != _originalOutputHeight ||
                            Math.Abs(_tempScaleX - _originalScaleX) > 0.01 ||
                            Math.Abs(_tempScaleY - _originalScaleY) > 0.01;
            
            if (hasChanges)
            {
                var result = MessageBox.Show(
                    "У вас есть несохраненные изменения. Вы уверены, что хотите закрыть окно без сохранения?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                {
                    return; // Отменяем закрытие
                }
            }
            
            // Восстанавливаем исходные значения в полях (для визуального отката)
            PositionXTextBox.Text = _originalPositionX.ToString();
            PositionYTextBox.Text = _originalPositionY.ToString();
            OutputWidthTextBox.Text = _originalOutputWidth.ToString();
            OutputHeightTextBox.Text = _originalOutputHeight.ToString();
            ScaleXSlider.Value = _originalScaleX;
            ScaleYSlider.Value = _originalScaleY;
            
            DialogResult = false;
            Close();
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using ArenaApp.Models;
using NAudio.CoreAudioApi;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления диалогами выбора устройств и слотов
    /// </summary>
    public class DialogService
    {
        private DeviceManager? _deviceManager;
        
        // Делегаты для работы с устройствами
        public Func<List<string>>? GetAudioOutputDevices { get; set; }
        public Action<int>? SetSelectedScreenIndex { get; set; }
        public Action<bool>? SetUseSelectedScreen { get; set; }
        public Action<bool>? SetUseUniformToFill { get; set; }
        public Action<int>? SetSelectedAudioDeviceIndex { get; set; }
        public Action<bool>? SetUseSelectedAudio { get; set; }
        public Func<bool>? GetUseSelectedScreen { get; set; }
        public Func<int>? GetSelectedScreenIndex { get; set; }
        public Func<bool>? GetUseUniformToFill { get; set; }
        public Func<bool>? GetUseSelectedAudio { get; set; }
        public Func<int>? GetSelectedAudioDeviceIndex { get; set; }
        
        // Делегаты для работы с вторичным экраном
        public Action? CreateSecondaryScreenWindow { get; set; }
        public Action? CloseSecondaryScreenWindow { get; set; }
        
        // Делегаты для работы со слотами
        public Action<int, int>? LoadMediaToSlot { get; set; }
        public Action<int, int>? CreateTextBlock { get; set; }
        
        public void SetDeviceManager(DeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }
        
        /// <summary>
        /// Показывает диалог выбора экрана
        /// </summary>
        public void ShowScreenSelectionDialog(int screenIndex)
        {
            try
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                if (screenIndex >= 0 && screenIndex < screens.Length)
                {
                    var screen = screens[screenIndex];
                    
                    var dialog = new Window
                    {
                        Title = "Настройки экрана",
                        Width = 450,
                        Height = 350,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        Background = (System.Windows.Media.Brush)Application.Current.Resources["DarkBackgroundBrush"]
                    };
                    
                    var panel = new StackPanel { Margin = new Thickness(20) };
                    
                    // Название экрана
                    var nameLabel = new TextBlock
                    {
                        Text = $"Экран {screenIndex + 1}: {screen.Bounds.Width}x{screen.Bounds.Height}",
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextPrimaryBrush"],
                        Margin = new Thickness(0, 0, 0, 20)
                    };
                    panel.Children.Add(nameLabel);
                    
                    // Информация об экране
                    var infoText = $"Разрешение: {screen.Bounds.Width}x{screen.Bounds.Height}\n" +
                                  $"Позиция: {screen.Bounds.X}, {screen.Bounds.Y}\n" +
                                  $"Основной: {(screen.Primary ? "Да" : "Нет")}";
                    var infoLabel = new TextBlock
                    {
                        Text = infoText,
                        Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                        Margin = new Thickness(0, 0, 0, 20)
                    };
                    panel.Children.Add(infoLabel);
                    
                    // Чекбокс использования экрана
                    var useScreenCheckBox = new CheckBox
                    {
                        Content = "Воспроизводить на этом экране",
                        IsChecked = GetUseSelectedScreen?.Invoke() == true && GetSelectedScreenIndex?.Invoke() == screenIndex,
                        Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextPrimaryBrush"],
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    panel.Children.Add(useScreenCheckBox);
                    
                    // Чекбокс режима масштабирования
                    var stretchModeCheckBox = new CheckBox
                    {
                        Content = "Заполнить весь экран (может обрезать изображение)",
                        IsChecked = GetUseUniformToFill?.Invoke() == true,
                        Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextPrimaryBrush"],
                        Margin = new Thickness(0, 0, 0, 20),
                        ToolTip = "Если отключено - сохраняет пропорции изображения\nЕсли включено - заполняет весь экран, но может обрезать края"
                    };
                    panel.Children.Add(stretchModeCheckBox);
                    
                    // Предупреждение для основного экрана
                    if (screen.Primary)
                    {
                        var warningLabel = new TextBlock
                        {
                            Text = "⚠️ Нельзя выводить на главный экран",
                            Foreground = (System.Windows.Media.Brush)Application.Current.Resources["WarningBrush"],
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 0, 0, 20)
                        };
                        panel.Children.Add(warningLabel);
                        useScreenCheckBox.IsEnabled = false;
                    }
                    
                    // Кнопки
                    var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                    
                    var okButton = new Button
                    {
                        Content = "OK",
                        Width = 100,
                        Height = 35,
                        Margin = new Thickness(5, 0, 0, 0),
                        Background = (System.Windows.Media.Brush)Application.Current.Resources["PrimaryAccentBrush"],
                        Foreground = System.Windows.Media.Brushes.White,
                        BorderBrush = (System.Windows.Media.Brush)Application.Current.Resources["PrimaryAccentHoverBrush"],
                        BorderThickness = new Thickness(1),
                        FontWeight = FontWeights.SemiBold,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    okButton.Click += (s, e) =>
                    {
                        // Сохраняем настройки режима масштабирования
                        SetUseUniformToFill?.Invoke(stretchModeCheckBox.IsChecked == true);
                        
                        if (!screen.Primary && useScreenCheckBox.IsChecked == true)
                        {
                            SetSelectedScreenIndex?.Invoke(screenIndex);
                            SetUseSelectedScreen?.Invoke(true);
                            System.Diagnostics.Debug.WriteLine($"ВЫБРАН ЭКРАН: Индекс={screenIndex}, Разрешение={screen.Bounds.Width}x{screen.Bounds.Height}, Позиция=({screen.Bounds.X}, {screen.Bounds.Y}), Режим масштабирования={(stretchModeCheckBox.IsChecked == true ? "UniformToFill" : "Uniform")}");
                            MessageBox.Show($"Выбран экран {screenIndex + 1} для вывода медиа.\nРазрешение: {screen.Bounds.Width}x{screen.Bounds.Height}\nПозиция: ({screen.Bounds.X}, {screen.Bounds.Y})\nРежим: {(stretchModeCheckBox.IsChecked == true ? "Заполнить экран" : "Сохранить пропорции")}\n\nОкно должно открыться на экране {screenIndex + 1}!", "Экран выбран");
                            
                            // Создаем окно на дополнительном экране сразу после выбора
                            CreateSecondaryScreenWindow?.Invoke();
                        }
                        else if (useScreenCheckBox.IsChecked == false)
                        {
                            SetUseSelectedScreen?.Invoke(false);
                            System.Diagnostics.Debug.WriteLine("ОТКЛЮЧЕН ВЫВОД НА ДОПОЛНИТЕЛЬНЫЙ ЭКРАН");
                            
                            // Закрываем окно на дополнительном экране при отключении
                            CloseSecondaryScreenWindow?.Invoke();
                        }
                        dialog.Close();
                    };
                    
                    var cancelButton = new Button
                    {
                        Content = "Отмена",
                        Width = 100,
                        Height = 35,
                        Margin = new Thickness(5, 0, 0, 0),
                        Background = (System.Windows.Media.Brush)Application.Current.Resources["DangerBrush"],
                        Foreground = System.Windows.Media.Brushes.White,
                        BorderBrush = (System.Windows.Media.Brush)Application.Current.Resources["DangerBrush"],
                        BorderThickness = new Thickness(1),
                        FontWeight = FontWeights.SemiBold,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    cancelButton.Click += (s, e) => dialog.Close();
                    
                    buttonPanel.Children.Add(cancelButton);
                    buttonPanel.Children.Add(okButton);
                    panel.Children.Add(buttonPanel);
                    
                    dialog.Content = panel;
                    dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при настройке экрана: {ex.Message}", "Ошибка");
            }
        }
        
        /// <summary>
        /// Показывает диалог выбора аудиоустройства
        /// </summary>
        public void ShowAudioSelectionDialog(int deviceIndex)
        {
            try
            {
                var audioDevices = GetAudioOutputDevices?.Invoke() ?? new List<string>();
                if (deviceIndex >= 0 && deviceIndex < audioDevices.Count)
                {
                    var deviceName = audioDevices[deviceIndex];
                    
                    var dialog = new Window
                    {
                        Title = "Настройки аудиоустройства",
                        Width = 400,
                        Height = 250,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize
                    };
                    
                    var panel = new StackPanel { Margin = new Thickness(20) };
                    
                    // Название устройства
                    var nameLabel = new TextBlock
                    {
                        Text = deviceName,
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 20)
                    };
                    panel.Children.Add(nameLabel);
                    
                    // Чекбокс использования устройства
                    var useAudioCheckBox = new CheckBox
                    {
                        Content = "Воспроизводить на этом устройстве",
                        IsChecked = GetUseSelectedAudio?.Invoke() == true && GetSelectedAudioDeviceIndex?.Invoke() == deviceIndex,
                        Margin = new Thickness(0, 0, 0, 20)
                    };
                    panel.Children.Add(useAudioCheckBox);
                    
                    // Кнопки
                    var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                    
                    var okButton = new Button
                    {
                        Content = "OK",
                        Width = 80,
                        Height = 30,
                        Margin = new Thickness(5, 0, 0, 0)
                    };
                    okButton.Click += (s, e) =>
                    {
                        if (useAudioCheckBox.IsChecked == true)
                        {
                            SetSelectedAudioDeviceIndex?.Invoke(deviceIndex);
                            SetUseSelectedAudio?.Invoke(true);
                        }
                        else
                        {
                            SetUseSelectedAudio?.Invoke(false);
                        }
                        dialog.Close();
                    };
                    
                    var cancelButton = new Button
                    {
                        Content = "Отмена",
                        Width = 80,
                        Height = 30,
                        Margin = new Thickness(5, 0, 0, 0)
                    };
                    cancelButton.Click += (s, e) => dialog.Close();
                    
                    buttonPanel.Children.Add(cancelButton);
                    buttonPanel.Children.Add(okButton);
                    panel.Children.Add(buttonPanel);
                    
                    dialog.Content = panel;
                    dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при настройке аудиоустройства: {ex.Message}", "Ошибка");
            }
        }
        
        /// <summary>
        /// Показывает диалог выбора типа контента для слота
        /// </summary>
        public void ShowSlotOptionsDialog(int column, int row)
        {
            var dialog = new ContentTypeDialog();
            if (dialog.ShowDialog() == true)
            {
                switch (dialog.Result)
                {
                    case ContentTypeDialog.ContentTypeResult.Media:
                        LoadMediaToSlot?.Invoke(column, row);
                        break;
                    case ContentTypeDialog.ContentTypeResult.Text:
                        CreateTextBlock?.Invoke(column, row);
                        break;
                    case ContentTypeDialog.ContentTypeResult.Cancel:
                    default:
                        // Ничего не делаем
                        break;
                }
            }
        }
        
        /// <summary>
        /// Показывает диалог выбора медиафайла
        /// </summary>
        public string? ShowOpenMediaFileDialog()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Media files|*.mp4;*.avi;*.mov;*.wmv;*.flv;*.mkv;*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.mp3;*.wav;*.flac;*.aac"
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }
            
            return null;
        }
        
        /// <summary>
        /// Показывает диалог выбора видеофайла
        /// </summary>
        public string? ShowOpenVideoFileDialog()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Video files|*.mp4;*.avi;*.mov;*.wmv;*.flv;*.mkv"
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }
            
            return null;
        }
    }
}


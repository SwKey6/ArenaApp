using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NAudio.CoreAudioApi;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления меню (экраны и аудио устройства)
    /// </summary>
    public class MenuService
    {
        private DeviceManager? _deviceManager;
        
        // Делегаты для доступа к UI элементам
        public Func<MenuItem>? GetScreensMenuItem { get; set; }
        public Func<MenuItem>? GetAudioMenuItem { get; set; }
        
        // Делегаты для получения данных
        public Func<List<string>>? GetAudioOutputDevices { get; set; }
        
        // Делегаты для обработки событий
        public Action<int>? OnScreenMenuItemClick { get; set; }
        public Action<int>? OnAudioMenuItemClick { get; set; }
        
        public void SetDeviceManager(DeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }
        
        /// <summary>
        /// Инициализирует меню экранов
        /// </summary>
        public void InitializeScreensMenu()
        {
            try
            {
                var screensMenuItem = GetScreensMenuItem?.Invoke();
                if (screensMenuItem == null) return;
                
                screensMenuItem.Items.Clear();
                
                // Получаем реальные экраны
                var screens = System.Windows.Forms.Screen.AllScreens;
                
                for (int i = 0; i < screens.Length; i++)
                {
                    var screen = screens[i];
                    var menuItem = new MenuItem
                    {
                        Header = $"Экран {i + 1}: {screen.Bounds.Width}x{screen.Bounds.Height} {(screen.Primary ? "(Основной)" : "")}",
                        Tag = i
                    };
                    menuItem.Click += (s, e) => 
                    {
                        if (menuItem.Tag is int screenIndex)
                        {
                            OnScreenMenuItemClick?.Invoke(screenIndex);
                        }
                    };
                    screensMenuItem.Items.Add(menuItem);
                }
                
                if (screens.Length == 0)
                {
                    var noScreensItem = new MenuItem
                    {
                        Header = "Экраны не найдены",
                        IsEnabled = false
                    };
                    screensMenuItem.Items.Add(noScreensItem);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при инициализации меню экранов: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Инициализирует меню аудио устройств
        /// </summary>
        public void InitializeAudioMenu()
        {
            try
            {
                var audioMenuItem = GetAudioMenuItem?.Invoke();
                if (audioMenuItem == null) return;
                
                audioMenuItem.Items.Clear();
                
                // Получаем все устройства вывода звука
                var audioDevices = GetAudioOutputDevices?.Invoke();
                if (audioDevices == null) return;
                
                for (int i = 0; i < audioDevices.Count; i++)
                {
                    var device = audioDevices[i];
                    var menuItem = new MenuItem
                    {
                        Header = device,
                        Tag = i
                    };
                    menuItem.Click += (s, e) => 
                    {
                        if (menuItem.Tag is int deviceIndex)
                        {
                            OnAudioMenuItemClick?.Invoke(deviceIndex);
                        }
                    };
                    audioMenuItem.Items.Add(menuItem);
                }
                
                if (audioDevices.Count == 0)
                {
                    var noAudioItem = new MenuItem
                    {
                        Header = "Устройства звука не найдены",
                        IsEnabled = false
                    };
                    audioMenuItem.Items.Add(noAudioItem);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при инициализации меню звука: {ex.Message}");
            }
        }
    }
}


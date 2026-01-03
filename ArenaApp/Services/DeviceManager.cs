using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NAudio.CoreAudioApi;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления устройствами (экраны и аудио)
    /// </summary>
    public class DeviceManager
    {
        private int _selectedScreenIndex = 0;
        private int _selectedAudioDeviceIndex = 0;
        private bool _useSelectedScreen = false;
        private bool _useSelectedAudio = false;
        private bool _useUniformToFill = false;
        
        public int SelectedScreenIndex
        {
            get => _selectedScreenIndex;
            set => _selectedScreenIndex = value;
        }
        
        public int SelectedAudioDeviceIndex
        {
            get => _selectedAudioDeviceIndex;
            set => _selectedAudioDeviceIndex = value;
        }
        
        public bool UseSelectedScreen
        {
            get => _useSelectedScreen;
            set => _useSelectedScreen = value;
        }
        
        public bool UseSelectedAudio
        {
            get => _useSelectedAudio;
            set => _useSelectedAudio = value;
        }
        
        public bool UseUniformToFill
        {
            get => _useUniformToFill;
            set => _useUniformToFill = value;
        }
        
        public List<ScreenInfo> GetScreens()
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            return screens.Select((screen, index) => new ScreenInfo
            {
                Index = index,
                Width = screen.Bounds.Width,
                Height = screen.Bounds.Height,
                IsPrimary = screen.Primary,
                Bounds = screen.Bounds
            }).ToList();
        }
        
        public List<string> GetAudioOutputDevices()
        {
            var devices = new List<string>();
            
            try
            {
                // Используем NAudio для получения реальных устройств звука
                var deviceEnumerator = new MMDeviceEnumerator();
                var devicesCollection = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                
                foreach (var device in devicesCollection)
                {
                    devices.Add(device.FriendlyName);
                    device.Dispose();
                }
                
                deviceEnumerator.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении устройств звука: {ex.Message}");
                // Fallback - добавляем стандартное устройство
                devices.Add("Устройство по умолчанию");
            }
            
            return devices;
        }
        
        /// <summary>
        /// Настраивает выбранное аудиоустройство
        /// </summary>
        public void ConfigureAudioDevice()
        {
            try
            {
                if (!_useSelectedAudio) return;
                
                var audioDevices = GetAudioOutputDevices();
                if (_selectedAudioDeviceIndex >= 0 && _selectedAudioDeviceIndex < audioDevices.Count)
                {
                    // Здесь можно добавить логику для настройки конкретного аудиоустройства
                    // Пока что просто выводим информацию
                    System.Diagnostics.Debug.WriteLine($"Выбрано аудиоустройство: {audioDevices[_selectedAudioDeviceIndex]}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при настройке аудиоустройства: {ex.Message}");
            }
        }
    }
    
    public class ScreenInfo
    {
        public int Index { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsPrimary { get; set; }
        public System.Drawing.Rectangle Bounds { get; set; }
    }
}


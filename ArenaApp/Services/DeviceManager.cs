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
                var enumerator = new MMDeviceEnumerator();
                var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                
                foreach (var endpoint in endpoints)
                {
                    devices.Add(endpoint.FriendlyName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении аудиоустройств: {ex.Message}");
            }
            
            return devices;
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


using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления настройками элементов и общими настройками
    /// </summary>
    public class SettingsManager
    {
        private GlobalSettings? _globalSettings;
        
        public void SetGlobalSettings(GlobalSettings? settings)
        {
            _globalSettings = settings;
        }
        
        // Методы для получения финальных значений с учетом общих настроек
        public double GetFinalVolume(double elementVolume)
        {
            if (_globalSettings?.UseGlobalVolume == true)
            {
                return _globalSettings.GlobalVolume;
            }
            return elementVolume;
        }
        
        public double GetFinalOpacity(double elementOpacity)
        {
            if (_globalSettings?.UseGlobalOpacity == true)
            {
                return _globalSettings.GlobalOpacity;
            }
            return elementOpacity;
        }
        
        public double GetFinalScale(double elementScale)
        {
            if (_globalSettings?.UseGlobalScale == true)
            {
                return _globalSettings.GlobalScale;
            }
            return elementScale;
        }
        
        public double GetFinalRotation(double elementRotation)
        {
            if (_globalSettings?.UseGlobalRotation == true)
            {
                return _globalSettings.GlobalRotation;
            }
            return elementRotation;
        }
        
        // Делегаты для доступа к UI элементам
        public Action<MediaElement, double>? ApplyVideoSpeed { get; set; }
        public Action<MediaElement, double>? ApplyVideoOpacity { get; set; }
        public Action<MediaElement, double>? ApplyVideoVolume { get; set; }
        public Action<MediaElement, double, double>? ApplyVideoTransform { get; set; }
        
        public Action<FrameworkElement, double>? ApplyImageOpacity { get; set; }
        public Action<FrameworkElement, double, double>? ApplyImageTransform { get; set; }
        
        public Action<FrameworkElement, double>? ApplyTextOpacity { get; set; }
        public Action<FrameworkElement, double, double>? ApplyTextTransform { get; set; }
        
        public Action<MediaElement, double>? ApplyAudioSpeed { get; set; }
        public Action<MediaElement, double>? ApplyAudioVolume { get; set; }
        
        // Применение настроек к элементу
        public void ApplyElementSettings(MediaSlot slot, MediaElement? videoElement, FrameworkElement? imageElement, 
            FrameworkElement? textElement, MediaElement? audioElement)
        {
            var finalVolume = GetFinalVolume(slot.Volume);
            var finalOpacity = GetFinalOpacity(slot.Opacity);
            var finalScale = GetFinalScale(slot.Scale);
            var finalRotation = GetFinalRotation(slot.Rotation);
            
            // Применяем к видео
            if (videoElement != null)
            {
                ApplyVideoSpeed?.Invoke(videoElement, slot.PlaybackSpeed);
                ApplyVideoOpacity?.Invoke(videoElement, finalOpacity);
                ApplyVideoVolume?.Invoke(videoElement, finalVolume);
                ApplyVideoTransform?.Invoke(videoElement, finalScale, finalRotation);
            }
            
            // Применяем к изображению
            if (imageElement != null)
            {
                ApplyImageOpacity?.Invoke(imageElement, finalOpacity);
                ApplyImageTransform?.Invoke(imageElement, finalScale, finalRotation);
            }
            
            // Применяем к тексту
            if (textElement != null)
            {
                ApplyTextOpacity?.Invoke(textElement, finalOpacity);
                ApplyTextTransform?.Invoke(textElement, finalScale, finalRotation);
            }
            
            // Применяем к аудио
            if (audioElement != null)
            {
                ApplyAudioSpeed?.Invoke(audioElement, slot.PlaybackSpeed);
                ApplyAudioVolume?.Invoke(audioElement, finalVolume);
            }
        }
        
        // Применение общих настроек ко всем активным элементам
        public void ApplyGlobalSettings(MediaElement? videoElement, FrameworkElement? imageElement, 
            FrameworkElement? textElement, MediaElement? audioElement)
        {
            if (_globalSettings == null) return;
            
            var finalVolume = _globalSettings.UseGlobalVolume ? _globalSettings.GlobalVolume : 1.0;
            var finalOpacity = _globalSettings.UseGlobalOpacity ? _globalSettings.GlobalOpacity : 1.0;
            var finalScale = _globalSettings.UseGlobalScale ? _globalSettings.GlobalScale : 1.0;
            var finalRotation = _globalSettings.UseGlobalRotation ? _globalSettings.GlobalRotation : 0.0;
            
            // Применяем к видео
            if (videoElement != null)
            {
                if (_globalSettings.UseGlobalOpacity)
                    ApplyVideoOpacity?.Invoke(videoElement, finalOpacity);
                if (_globalSettings.UseGlobalVolume)
                    ApplyVideoVolume?.Invoke(videoElement, finalVolume);
                if (_globalSettings.UseGlobalScale || _globalSettings.UseGlobalRotation)
                    ApplyVideoTransform?.Invoke(videoElement, finalScale, finalRotation);
            }
            
            // Применяем к изображению
            if (imageElement != null)
            {
                if (_globalSettings.UseGlobalOpacity)
                    ApplyImageOpacity?.Invoke(imageElement, finalOpacity);
                if (_globalSettings.UseGlobalScale || _globalSettings.UseGlobalRotation)
                    ApplyImageTransform?.Invoke(imageElement, finalScale, finalRotation);
            }
            
            // Применяем к тексту
            if (textElement != null)
            {
                if (_globalSettings.UseGlobalOpacity)
                    ApplyTextOpacity?.Invoke(textElement, finalOpacity);
                if (_globalSettings.UseGlobalScale || _globalSettings.UseGlobalRotation)
                    ApplyTextTransform?.Invoke(textElement, finalScale, finalRotation);
            }
            
            // Применяем к аудио
            if (audioElement != null)
            {
                if (_globalSettings.UseGlobalVolume)
                    ApplyAudioVolume?.Invoke(audioElement, finalVolume);
            }
        }
    }
}


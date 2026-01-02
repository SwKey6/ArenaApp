using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления слайдерами видео и аудио
    /// </summary>
    public class SliderService
    {
        private TimerService? _timerService;
        private MediaStateService? _mediaStateService;
        
        // Делегаты для доступа к UI элементам
        public Func<Slider>? GetVideoSlider { get; set; }
        public Func<MediaElement>? GetMainMediaElement { get; set; }
        public Func<MediaElement?>? GetSecondaryMediaElement { get; set; }
        public Func<Slider>? GetAudioSlider { get; set; }

        // Поддержка VLC-видео
        public Func<bool>? IsVlcVideoActive { get; set; }
        public Func<TimeSpan>? GetVlcVideoTotalDuration { get; set; }
        public Action<TimeSpan>? SetVlcVideoPosition { get; set; }
        public Action<TimeSpan>? SetSecondaryVlcVideoPosition { get; set; }
        
        // Делегаты для получения данных
        public Func<TimeSpan>? GetVideoTotalDuration { get; set; }
        public Func<TimeSpan>? GetAudioTotalDuration { get; set; }
        
        public void SetTimerService(TimerService timerService)
        {
            _timerService = timerService;
        }
        
        public void SetMediaStateService(MediaStateService mediaStateService)
        {
            _mediaStateService = mediaStateService;
        }
        
        /// <summary>
        /// Обработчик начала перетаскивания слайдера видео
        /// </summary>
        public void OnVideoSliderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_timerService != null)
            {
                _timerService.IsVideoSliderDragging = true;
            }
        }
        
        /// <summary>
        /// Обработчик окончания перетаскивания слайдера видео
        /// </summary>
        public void OnVideoSliderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_timerService != null)
            {
                _timerService.IsVideoSliderDragging = false;
            }
            
            var videoSlider = GetVideoSlider?.Invoke();
            var isVlc = IsVlcVideoActive?.Invoke() == true;
            var mainMediaElement = GetMainMediaElement?.Invoke();
            var videoTotalDuration = isVlc
                ? (GetVlcVideoTotalDuration?.Invoke() ?? TimeSpan.Zero)
                : (GetVideoTotalDuration?.Invoke() ?? TimeSpan.Zero);
            var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
            
            // Устанавливаем позицию при отпускании слайдера
            if (videoTotalDuration.TotalSeconds > 0 && videoSlider != null)
            {
                var newPosition = TimeSpan.FromSeconds((videoSlider.Value / 100.0) * videoTotalDuration.TotalSeconds);
                
                if (isVlc)
                {
                    SetVlcVideoPosition?.Invoke(newPosition);
                    SetSecondaryVlcVideoPosition?.Invoke(newPosition);
                    System.Diagnostics.Debug.WriteLine($"VLC: Перемотка видео -> {newPosition}");
                    return;
                }

                if (mainMediaElement != null && mainMediaElement.NaturalDuration.HasTimeSpan && mainMediaElement.NaturalDuration.TimeSpan.TotalSeconds > 0)
                {
                    mainMediaElement.Position = newPosition;
                    
                    // Синхронизируем позицию со вторым экраном
                    if (secondaryMediaElement != null && secondaryMediaElement.Source != null)
                    {
                        try
                        {
                            secondaryMediaElement.Position = newPosition;
                            System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ ПОЗИЦИИ ПРИ ПЕРЕМОТКЕ: {newPosition}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка синхронизации позиции со вторым экраном: {ex.Message}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Обработчик изменения значения слайдера видео (не делает ничего во время перетаскивания)
        /// </summary>
        public void OnVideoSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // НЕ ТРОГАЕМ ВИДЕО во время перетаскивания - это создает кашу в звуке и покадровую съемку!
            // Позиция будет установлена только при отпускании слайдера
        }
        
        /// <summary>
        /// Обработчик начала перетаскивания слайдера аудио
        /// </summary>
        public void OnAudioSliderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_timerService != null)
            {
                _timerService.IsAudioSliderDragging = true;
            }
        }
        
        /// <summary>
        /// Обработчик окончания перетаскивания слайдера аудио
        /// </summary>
        public void OnAudioSliderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_timerService != null)
            {
                _timerService.IsAudioSliderDragging = false;
            }
        }
        
        /// <summary>
        /// Обработчик изменения значения слайдера аудио
        /// </summary>
        public void OnAudioSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_timerService == null || _mediaStateService == null) return;
            
            if (_timerService.IsAudioSliderDragging && 
                _mediaStateService.CurrentAudioContent != null && 
                _mediaStateService.TryGetAudioSlot(_mediaStateService.CurrentAudioContent, out var audioElement))
            {
                var audioTotalDuration = GetAudioTotalDuration?.Invoke() ?? TimeSpan.Zero;
                var newPosition = TimeSpan.FromSeconds((e.NewValue / 100.0) * audioTotalDuration.TotalSeconds);
                
                // Проверяем, что аудио загружено и готово к воспроизведению
                if (audioElement != null && audioElement.NaturalDuration.HasTimeSpan && audioElement.NaturalDuration.TimeSpan.TotalSeconds > 0)
                {
                    // Устанавливаем позицию немедленно без задержки для мгновенной перемотки
                    audioElement.Position = newPosition;
                }
            }
        }
    }
}


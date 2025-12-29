using System;
using System.Windows.Threading;
using System.Windows.Controls;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления таймерами видео и аудио
    /// </summary>
    public class TimerService
    {
        private DispatcherTimer? _videoTimer;
        private DispatcherTimer? _audioTimer;
        
        // События для обновления UI
        public event Action<string>? VideoTimerUpdated;
        public event Action<double>? VideoSliderUpdated;
        public event Action<string>? AudioTimerUpdated;
        public event Action<double>? AudioSliderUpdated;
        
        // Состояние
        private bool _isVideoSliderDragging = false;
        private bool _isAudioSliderDragging = false;
        private TimeSpan _videoTotalDuration = TimeSpan.Zero;
        private TimeSpan _audioTotalDuration = TimeSpan.Zero;
        
        // Делегаты для получения данных
        private Func<TimeSpan>? _getVideoPosition;
        private Func<TimeSpan>? _getVideoDuration;
        private Func<bool>? _hasVideoSource;
        private Func<TimeSpan>? _getAudioPosition;
        private Func<TimeSpan>? _getAudioDuration;
        private Func<bool>? _hasAudioSource;
        
        public bool IsVideoSliderDragging
        {
            get => _isVideoSliderDragging;
            set => _isVideoSliderDragging = value;
        }
        
        public bool IsAudioSliderDragging
        {
            get => _isAudioSliderDragging;
            set => _isAudioSliderDragging = value;
        }
        
        public TimeSpan VideoTotalDuration => _videoTotalDuration;
        public TimeSpan AudioTotalDuration => _audioTotalDuration;
        
        public void SetVideoDataProviders(Func<TimeSpan>? getPosition, Func<TimeSpan>? getDuration, Func<bool>? hasSource)
        {
            _getVideoPosition = getPosition;
            _getVideoDuration = getDuration;
            _hasVideoSource = hasSource;
        }
        
        public void SetAudioDataProviders(Func<TimeSpan>? getPosition, Func<TimeSpan>? getDuration, Func<bool>? hasSource)
        {
            _getAudioPosition = getPosition;
            _getAudioDuration = getDuration;
            _hasAudioSource = hasSource;
        }
        
        public void StartVideoTimer()
        {
            if (_videoTimer == null)
            {
                _videoTimer = new DispatcherTimer();
                _videoTimer.Interval = TimeSpan.FromMilliseconds(100);
                _videoTimer.Tick += VideoTimer_Tick;
            }
            
            if (!_videoTimer.IsEnabled)
            {
                _videoTimer.Start();
            }
        }
        
        public void StopVideoTimer()
        {
            _videoTimer?.Stop();
        }
        
        public void StartAudioTimer()
        {
            if (_audioTimer == null)
            {
                _audioTimer = new DispatcherTimer();
                _audioTimer.Interval = TimeSpan.FromMilliseconds(100);
                _audioTimer.Tick += AudioTimer_Tick;
            }
            
            if (!_audioTimer.IsEnabled)
            {
                _audioTimer.Start();
            }
        }
        
        public void StopAudioTimer()
        {
            _audioTimer?.Stop();
        }
        
        private void VideoTimer_Tick(object? sender, EventArgs e)
        {
            if (_hasVideoSource?.Invoke() == true && _getVideoPosition != null && _getVideoDuration != null)
            {
                var total = _getVideoDuration();
                _videoTotalDuration = total;
                var current = _getVideoPosition();
                var remaining = total - current;
                
                VideoTimerUpdated?.Invoke($"Видео: {remaining.ToString(@"hh\:mm\:ss\.fff")}");
                
                // Обновляем слайдер только если пользователь его не перетаскивает
                if (!_isVideoSliderDragging && total.TotalSeconds > 0)
                {
                    var progress = (current.TotalSeconds / total.TotalSeconds) * 100;
                    VideoSliderUpdated?.Invoke(Math.Min(100, Math.Max(0, progress)));
                }
            }
            else
            {
                VideoTimerUpdated?.Invoke("Видео: 00:00:00.000");
                _videoTotalDuration = TimeSpan.Zero;
                VideoSliderUpdated?.Invoke(0);
            }
        }
        
        private void AudioTimer_Tick(object? sender, EventArgs e)
        {
            if (_hasAudioSource?.Invoke() == true && _getAudioPosition != null && _getAudioDuration != null)
            {
                var total = _getAudioDuration();
                _audioTotalDuration = total;
                var current = _getAudioPosition();
                var remaining = total - current;
                
                AudioTimerUpdated?.Invoke($"Аудио: {remaining.ToString(@"hh\:mm\:ss\.fff")}");
                
                // Обновляем слайдер только если пользователь его не перетаскивает
                if (!_isAudioSliderDragging && total.TotalSeconds > 0)
                {
                    var progress = (current.TotalSeconds / total.TotalSeconds) * 100;
                    AudioSliderUpdated?.Invoke(Math.Min(100, Math.Max(0, progress)));
                }
            }
            else
            {
                AudioTimerUpdated?.Invoke("Аудио: 00:00:00.000");
                _audioTotalDuration = TimeSpan.Zero;
                if (!_isAudioSliderDragging)
                {
                    AudioSliderUpdated?.Invoke(0);
                }
            }
        }
    }
}


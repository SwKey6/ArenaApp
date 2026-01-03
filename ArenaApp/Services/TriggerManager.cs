using System;
using System.Collections.Generic;
using System.Linq;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления триггерами
    /// </summary>
    public class TriggerManager
    {
        private readonly Dictionary<int, TriggerState> _triggerStates = new();
        private int? _activeTriggerColumn = null;
        private int? _lastUsedTriggerColumn = null;
        
        // Делегаты для работы с проектом
        public Func<int, int, MediaSlot?>? GetMediaSlot { get; set; }
        public Func<string, bool>? ShouldBlockMediaFile { get; set; }
        
        // Делегаты для проверки состояния медиа
        public Func<string, bool>? IsMediaFileAlreadyPlaying { get; set; }
        public Func<MediaType?>? GetCurrentMediaType { get; set; }
        
        // Делегаты для работы с UI
        public Action<int>? OnTriggerStarted { get; set; }
        public Action<int>? OnTriggerStopped { get; set; }
        public Action<int>? OnTriggerPaused { get; set; }
        public Action<int>? OnTriggerResumed { get; set; }
        
        public int? ActiveTriggerColumn
        {
            get => _activeTriggerColumn;
            set => _activeTriggerColumn = value;
        }
        
        public int? LastUsedTriggerColumn
        {
            get => _lastUsedTriggerColumn;
            set => _lastUsedTriggerColumn = value;
        }
        
        /// <summary>
        /// Получает состояние триггера
        /// </summary>
        public TriggerState GetTriggerState(int column)
        {
            return _triggerStates.GetValueOrDefault(column, TriggerState.Stopped);
        }
        
        /// <summary>
        /// Устанавливает состояние триггера
        /// </summary>
        public void SetTriggerState(int column, TriggerState state)
        {
            _triggerStates[column] = state;
        }
        
        /// <summary>
        /// Получает медиа слоты для триггера (строка 1 и 2)
        /// </summary>
        public (MediaSlot? slot1, MediaSlot? slot2) GetTriggerSlots(int column)
        {
            var slot1 = GetMediaSlot?.Invoke(column, 1);
            var slot2 = GetMediaSlot?.Invoke(column, 2);
            return (slot1, slot2);
        }
        
        /// <summary>
        /// Определяет типы медиа для триггера
        /// </summary>
        public (MediaSlot? videoSlot, MediaSlot? audioSlot, MediaSlot? imageSlot) DetermineMediaTypes(int column)
        {
            var (slot1, slot2) = GetTriggerSlots(column);
            
            MediaSlot? videoSlot = null;
            MediaSlot? audioSlot = null;
            MediaSlot? imageSlot = null;
            
            if (slot1 != null)
            {
                switch (slot1.Type)
                {
                    case MediaType.Video:
                        videoSlot = slot1;
                        break;
                    case MediaType.Audio:
                        audioSlot = slot1;
                        break;
                    case MediaType.Image:
                        imageSlot = slot1;
                        break;
                }
            }
            
            if (slot2 != null)
            {
                switch (slot2.Type)
                {
                    case MediaType.Video:
                        videoSlot = slot2;
                        break;
                    case MediaType.Audio:
                        audioSlot = slot2;
                        break;
                    case MediaType.Image:
                        imageSlot = slot2;
                        break;
                }
            }
            
            return (videoSlot, audioSlot, imageSlot);
        }
        
        /// <summary>
        /// Проверяет, активен ли триггер
        /// </summary>
        public bool IsTriggerActive(int column)
        {
            return _activeTriggerColumn == column && 
                   _triggerStates.GetValueOrDefault(column, TriggerState.Stopped) != TriggerState.Stopped;
        }
        
        /// <summary>
        /// Получает все активные триггеры
        /// </summary>
        public List<int> GetActiveTriggers()
        {
            return _triggerStates
                .Where(kvp => kvp.Value != TriggerState.Stopped)
                .Select(kvp => kvp.Key)
                .ToList();
        }
        
        /// <summary>
        /// Останавливает все активные триггеры кроме указанного
        /// </summary>
        public void StopAllTriggersExcept(int exceptColumn)
        {
            var activeColumns = _triggerStates
                .Where(kvp => kvp.Value != TriggerState.Stopped && kvp.Key != exceptColumn)
                .ToList();
            
            foreach (var activeColumn in activeColumns)
            {
                SetTriggerState(activeColumn.Key, TriggerState.Stopped);
                OnTriggerStopped?.Invoke(activeColumn.Key);
            }
        }
        
        /// <summary>
        /// Останавливает все активные триггеры
        /// </summary>
        public void StopAllTriggers()
        {
            var activeColumns = _triggerStates
                .Where(kvp => kvp.Value != TriggerState.Stopped)
                .ToList();
            
            foreach (var activeColumn in activeColumns)
            {
                SetTriggerState(activeColumn.Key, TriggerState.Stopped);
                OnTriggerStopped?.Invoke(activeColumn.Key);
            }
            
            _activeTriggerColumn = null;
        }
        
        /// <summary>
        /// Очищает состояние триггера
        /// </summary>
        public void ClearTrigger(int column)
        {
            _triggerStates.Remove(column);
            if (_activeTriggerColumn == column)
            {
                _activeTriggerColumn = null;
            }
        }
        
        /// <summary>
        /// Проверяет, нужно ли блокировать запуск триггера
        /// </summary>
        public bool ShouldBlockTrigger(MediaSlot? videoSlot, MediaSlot? audioSlot, MediaSlot? imageSlot)
        {
            // Для триггеров не блокируем если аудио уже играет - оно продолжит играть
            // Блокируем только если пытаемся запустить тот же аудио файл в отдельном слоте
            
            // Если есть аудио в триггере и оно уже играет - не блокируем (продолжит играть)
            if (audioSlot != null && IsMediaFileAlreadyPlaying != null && IsMediaFileAlreadyPlaying(audioSlot.MediaPath))
            {
                // Проверяем, играет ли это аудио в триггере или в отдельном слоте
                var currentAudioType = GetCurrentMediaType?.Invoke();
                if (currentAudioType == MediaType.Audio)
                {
                    // Если аудио играет в триггере - не блокируем
                    // Если аудио играет в отдельном слоте - блокируем
                    return false; // Для триггеров всегда разрешаем
                }
            }
            
            return false;
        }
    }
}


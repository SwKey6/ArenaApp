using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления состоянием медиа (активные слоты, позиции, пауза)
    /// </summary>
    public class MediaStateService
    {
        // Отслеживание активных медиа элементов для каждого слота
        private readonly Dictionary<string, MediaElement> _activeSlotMedia = new();
        private readonly Dictionary<string, Grid> _activeSlotContainers = new();
        private string? _currentMainMedia = null; // Что сейчас показывается в основном плеере
        
        // Отслеживание активных медиа по типам
        private readonly Dictionary<string, MediaElement> _activeAudioSlots = new(); // Ключ: "Slot_x_y" или "Trigger_x"
        private readonly Dictionary<string, Grid> _activeAudioContainers = new();
        private string? _currentVisualContent = null; // Видео или изображение в основном плеере
        private string? _currentAudioContent = null; // Активное аудио
        
        // Позиции возобновления воспроизведения по пути файла
        private readonly Dictionary<string, TimeSpan> _mediaResumePositions = new();
        
        // Позиции воспроизведения для каждого слота (в памяти, не сохраняется)
        private readonly Dictionary<string, TimeSpan> _slotPositions = new(); // Ключ: "Slot_x_y" или "Trigger_x"
        
        // Отслеживание активных медиафайлов по путям
        private readonly HashSet<string> _activeMediaFilePaths = new(); // Пути к активным медиафайлам
        
        // Отслеживание состояния паузы
        private bool _isVideoPaused = false;
        private readonly Dictionary<string, bool> _audioPausedStates = new(); // Ключ: slotKey, значение: true если на паузе
        
        // Свойства
        public string? CurrentMainMedia
        {
            get => _currentMainMedia;
            set => _currentMainMedia = value;
        }
        
        public string? CurrentVisualContent
        {
            get => _currentVisualContent;
            set => _currentVisualContent = value;
        }
        
        public string? CurrentAudioContent
        {
            get => _currentAudioContent;
            set => _currentAudioContent = value;
        }
        
        public bool IsVideoPaused
        {
            get => _isVideoPaused;
            set => _isVideoPaused = value;
        }
        
        // Методы для работы с позициями
        public void SaveSlotPosition(string slotKey, TimeSpan position)
        {
            _slotPositions[slotKey] = position;
        }
        
        public TimeSpan GetSlotPosition(string slotKey)
        {
            return _slotPositions.GetValueOrDefault(slotKey, TimeSpan.Zero);
        }
        
        public void ClearSlotPosition(string slotKey)
        {
            _slotPositions.Remove(slotKey);
        }
        
        public void SaveMediaResumePosition(string mediaPath, TimeSpan position)
        {
            _mediaResumePositions[mediaPath] = position;
        }
        
        public TimeSpan? GetMediaResumePosition(string mediaPath)
        {
            return _mediaResumePositions.TryGetValue(mediaPath, out var position) ? position : null;
        }
        
        // Методы для работы с активными медиафайлами
        public void RegisterActiveMediaFile(string mediaPath)
        {
            _activeMediaFilePaths.Add(mediaPath);
        }
        
        public void UnregisterActiveMediaFile(string mediaPath)
        {
            _activeMediaFilePaths.Remove(mediaPath);
        }
        
        public bool IsMediaFileAlreadyPlaying(string mediaPath)
        {
            return _activeMediaFilePaths.Contains(mediaPath);
        }
        
        // Методы для работы с аудио слотами
        public void AddAudioSlot(string slotKey, MediaElement element, Grid container)
        {
            _activeAudioSlots[slotKey] = element;
            _activeAudioContainers[slotKey] = container;
        }
        
        public bool TryGetAudioSlot(string slotKey, out MediaElement? element)
        {
            return _activeAudioSlots.TryGetValue(slotKey, out element);
        }
        
        public void RemoveAudioSlot(string slotKey)
        {
            _activeAudioSlots.Remove(slotKey);
            _activeAudioContainers.Remove(slotKey);
        }
        
        public void ClearAllAudioSlots()
        {
            _activeAudioSlots.Clear();
            _activeAudioContainers.Clear();
        }
        
        public Dictionary<string, MediaElement> GetAllAudioSlots()
        {
            return new Dictionary<string, MediaElement>(_activeAudioSlots);
        }
        
        public Dictionary<string, Grid> GetAllAudioContainers()
        {
            return new Dictionary<string, Grid>(_activeAudioContainers);
        }
        
        // Методы для работы с паузой
        public void SetAudioPaused(string slotKey, bool paused)
        {
            _audioPausedStates[slotKey] = paused;
        }
        
        public bool IsAudioPaused(string slotKey)
        {
            return _audioPausedStates.ContainsKey(slotKey) && _audioPausedStates[slotKey];
        }
        
        public void ClearAudioPausedState(string slotKey)
        {
            _audioPausedStates.Remove(slotKey);
        }
        
        public void ResetVideoPaused()
        {
            _isVideoPaused = false;
        }
        
        public void ResetAudioPaused(string slotKey)
        {
            _audioPausedStates[slotKey] = false;
        }
    }
}


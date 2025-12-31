using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using ArenaApp.Models;
using ArenaApp.Services;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using NAudio.CoreAudioApi;

namespace ArenaApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public ProjectManager _projectManager = null!;
    private PreviewGenerator _previewGenerator = null!;
    
    // Сервисы
    private readonly Services.TimerService _timerService = new();
    private readonly Services.MediaStateService _mediaStateService = new();
    private readonly Services.DeviceManager _deviceManager = new();
    private readonly Services.TransitionService _transitionService = new();
    private readonly Services.SettingsManager _settingsManager = new();
    private readonly Services.SlotManager _slotManager = new();
    private readonly Services.MediaPlayerService _mediaPlayerService = new();
    private readonly Services.TextBlockService _textBlockService = new();
    private readonly Services.TriggerManager _triggerManager = new();
    private readonly Services.NavigationService _navigationService = new();
    private readonly Services.DialogService _dialogService = new();
    private readonly Services.ContextMenuService _contextMenuService = new();
    private readonly Services.SlotUIService _slotUIService = new();
    private readonly Services.SecondaryScreenService _secondaryScreenService = new();
    private readonly Services.SlotCreationService _slotCreationService = new();
    private readonly Services.AutoPlayService _autoPlayService = new();
    private readonly Services.MediaControlService _mediaControlService = new();
    private readonly Services.TriggerPlaybackService _triggerPlaybackService = new();
    private readonly Services.VideoDisplayService _videoDisplayService = new();
    
    // Свойства для обратной совместимости с MediaStateService
    private string? _currentMainMedia
    {
        get => _mediaStateService.CurrentMainMedia;
        set => _mediaStateService.CurrentMainMedia = value;
    }
    
    private string? _currentVisualContent
    {
        get => _mediaStateService.CurrentVisualContent;
        set => _mediaStateService.CurrentVisualContent = value;
    }
    
    private string? _currentAudioContent
    {
        get => _mediaStateService.CurrentAudioContent;
        set => _mediaStateService.CurrentAudioContent = value;
    }
    
    // Временные обертки для обратной совместимости (будут постепенно заменены)
    private Dictionary<string, MediaElement> _activeAudioSlots => _mediaStateService.GetAllAudioSlots();
    private Dictionary<string, Grid> _activeAudioContainers => _mediaStateService.GetAllAudioContainers();
    
    private bool _isVideoPaused
    {
        get => _mediaStateService.IsVideoPaused;
        set => _mediaStateService.IsVideoPaused = value;
    }
    
    // Временные словари для обратной совместимости (будут постепенно заменены на методы MediaStateService)
    private readonly Dictionary<string, TimeSpan> _slotPositions = new(); // Используется через MediaStateService, но оставляем для обратной совместимости
    private readonly Dictionary<string, TimeSpan> _mediaResumePositions = new(); // Используется через MediaStateService, но оставляем для обратной совместимости
    private readonly HashSet<string> _activeMediaFilePaths = new(); // Используется через MediaStateService, но оставляем для обратной совместимости
    private readonly Dictionary<string, bool> _audioPausedStates = new(); // Используется через MediaStateService, но оставляем для обратной совместимости
    
    // Методы-обертки для синхронизации с MediaStateService
    private void RegisterActiveMediaFile(string mediaPath)
    {
        _activeMediaFilePaths.Add(mediaPath);
        _mediaStateService.RegisterActiveMediaFile(mediaPath);
    }
    
    private void UnregisterActiveMediaFile(string mediaPath)
    {
        _activeMediaFilePaths.Remove(mediaPath);
        _mediaStateService.UnregisterActiveMediaFile(mediaPath);
    }
    
    private bool IsMediaFileAlreadyPlaying(string mediaPath)
    {
        return _activeMediaFilePaths.Contains(mediaPath) || _mediaStateService.IsMediaFileAlreadyPlaying(mediaPath);
    }
    
    // Отслеживание состояния триггеров (используется через TriggerManager, но оставляем для обратной совместимости)
    private readonly Dictionary<int, MediaElement> _activeAudioElements = new();
    private readonly Dictionary<int, Grid> _tempContainers = new();
    
    // Свойства для обратной совместимости с TriggerManager
    // Временный словарь для обратной совместимости (синхронизируется с TriggerManager)
    private readonly Dictionary<int, TriggerState> _triggerStates = new();
    
    // Методы-обертки для синхронизации с TriggerManager
    private TriggerState GetTriggerState(int column)
    {
        return _triggerManager.GetTriggerState(column);
    }
    
    private void SetTriggerState(int column, TriggerState state)
    {
        _triggerStates[column] = state;
        _triggerManager.SetTriggerState(column, state);
    }
    
    private int? _activeTriggerColumn
    {
        get => _triggerManager.ActiveTriggerColumn;
        set => _triggerManager.ActiveTriggerColumn = value;
    }
    private int? _lastUsedTriggerColumn
    {
        get => _triggerManager.LastUsedTriggerColumn;
        set => _triggerManager.LastUsedTriggerColumn = value;
    }
    
    // Отслеживание активных медиа элементов для каждого слота
    private readonly Dictionary<string, MediaElement> _activeSlotMedia = new();
    private readonly Dictionary<string, Grid> _activeSlotContainers = new();
    
    // Отслеживание выбранных устройств (перенесено в DeviceManager, но оставляем для обратной совместимости)
    private bool _useUniformToFill
    {
        get => _deviceManager.UseUniformToFill;
        set => _deviceManager.UseUniformToFill = value;
    }
    
    private int _selectedScreenIndex
    {
        get => _deviceManager.SelectedScreenIndex;
        set => _deviceManager.SelectedScreenIndex = value;
    }
    
    private int _selectedAudioDeviceIndex
    {
        get => _deviceManager.SelectedAudioDeviceIndex;
        set => _deviceManager.SelectedAudioDeviceIndex = value;
    }
    
    private bool _useSelectedScreen
    {
        get => _deviceManager.UseSelectedScreen;
        set => _deviceManager.UseSelectedScreen = value;
    }
    
    private bool _useSelectedAudio
    {
        get => _deviceManager.UseSelectedAudio;
        set => _deviceManager.UseSelectedAudio = value;
    }
    
    // Переменные для таймеров (используются через TimerService, но оставляем для обратной совместимости)
    private TimeSpan _videoTotalDuration => _timerService.VideoTotalDuration;
    private TimeSpan _audioTotalDuration => _timerService.AudioTotalDuration;
    private bool _isAudioSliderDragging
    {
        get => _timerService.IsAudioSliderDragging;
        set => _timerService.IsAudioSliderDragging = value;
    }
    
    // Окна для вывода на дополнительные экраны
    private Window? _secondaryScreenWindow = null;
    private MediaElement? _secondaryMediaElement = null;
    
    // Перетаскивание панелей
    private bool _isDraggingElementSettings = false;
    private bool _isDraggingGlobalSettings = false;
    private bool _isDraggingMediaPlayer = false;
    private bool _isDraggingMediaCells = false;
    
    // Изменение размера панелей
    private bool _isResizingElementSettingsV = false;
    private bool _isResizingElementSettingsH = false;
    private bool _isResizingElementSettingsD = false;
    private bool _isResizingGlobalSettingsV = false;
    private bool _isResizingGlobalSettingsH = false;
    private bool _isResizingGlobalSettingsD = false;
    private bool _isResizingMediaPlayerV = false;
    private bool _isResizingMediaPlayerH = false;
    private bool _isResizingMediaPlayerD = false;
    private bool _isResizingMediaCellsV = false;
    private bool _isResizingMediaCellsH = false;
    private bool _isResizingMediaCellsD = false;
    
    // Флаг активного растягивания (блокирует перетаскивание)
    private bool _isAnyResizingActive = false;
    
    private Point _lastMousePosition;

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            _projectManager = new ProjectManager();
            _previewGenerator = new PreviewGenerator();
            
            // Инициализация сервисов
            InitializeServices();
            
            // Отладочная информация
            System.Diagnostics.Debug.WriteLine("MainWindow: InitializeComponent completed");
            
            CreateColumns(20); // Создаем 20 колонок для медиа элементов
            
            // Запускаем таймеры сразу - они всегда активны
            StartVideoTimer();
            StartAudioTimer();
            
            // Загружаем общие настройки
            LoadGlobalSettings();
            
            // Загружаем позиции панелей по умолчанию
            LoadPanelPositions();
            
            // Инициализируем меню экранов и звука
            InitializeScreensMenu();
            InitializeAudioMenu();
            
            // Подписываемся на событие открытия меню Edit для обновления устройств
            EditMenuItem.SubmenuOpened += EditMenuItem_SubmenuOpened;
            
            // Подписываемся на событие окончания воспроизведения для автоперехода
            mediaElement.MediaEnded += (s, e) => 
            {
                // Проверяем, включено ли зацикливание
                bool shouldLoop = _projectManager?.CurrentProject?.GlobalSettings?.LoopPlaylist == true;
                
                if (shouldLoop)
                {
                    // Если зацикливание включено, перезапускаем оба экрана с начала
                    if (_secondaryScreenWindow != null && _secondaryMediaElement != null && 
                        _secondaryMediaElement.Source != null &&
                        _secondaryScreenWindow.Content == _secondaryMediaElement)
                    {
                        try
                        {
                            _secondaryMediaElement.Position = TimeSpan.Zero;
                            _secondaryMediaElement.Play();
                            System.Diagnostics.Debug.WriteLine("ВИДЕО ЗАВЕРШЕНО: Перезапуск с начала на обоих экранах (зацикливание)");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка при перезапуске второго экрана: {ex.Message}");
                            // Не крашим приложение, просто логируем ошибку
                        }
                    }
                }
                else
                {
                    // Если зацикливание выключено, останавливаем второй экран
                    if (_secondaryScreenWindow != null && _secondaryMediaElement != null && 
                        _secondaryMediaElement.Source != null &&
                        _secondaryScreenWindow.Content == _secondaryMediaElement)
                    {
                        try
                        {
                            _secondaryMediaElement.Stop();
                            System.Diagnostics.Debug.WriteLine("ВИДЕО ЗАВЕРШЕНО НА ОСНОВНОМ ЭКРАНЕ: Остановлено на втором экране");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка при остановке второго экрана: {ex.Message}");
                            // Не крашим приложение, просто логируем ошибку
                        }
                    }
                }
                Dispatcher.Invoke(() => AutoPlayNextElement());
            };
            
            System.Diagnostics.Debug.WriteLine("MainWindow: Constructor completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow: Error in constructor: {ex.Message}");
            MessageBox.Show($"Ошибка при инициализации: {ex.Message}", "Ошибка");
        }
    }

    // Инициализация сервисов
    private void InitializeServices()
    {
        // Настройка TimerService
        _timerService.SetVideoDataProviders(
            () => mediaElement.Position,
            () => mediaElement.NaturalDuration.HasTimeSpan ? mediaElement.NaturalDuration.TimeSpan : TimeSpan.Zero,
            () => mediaElement.Source != null && mediaElement.NaturalDuration.HasTimeSpan
        );
        
        _timerService.SetAudioDataProviders(
            () => {
                if (_mediaStateService.CurrentAudioContent != null && 
                    _mediaStateService.TryGetAudioSlot(_mediaStateService.CurrentAudioContent, out var audioElement))
                {
                    return audioElement?.Position ?? TimeSpan.Zero;
                }
                return TimeSpan.Zero;
            },
            () => {
                if (_mediaStateService.CurrentAudioContent != null && 
                    _mediaStateService.TryGetAudioSlot(_mediaStateService.CurrentAudioContent, out var audioElement))
                {
                    return audioElement?.NaturalDuration.HasTimeSpan == true 
                        ? audioElement.NaturalDuration.TimeSpan 
                        : TimeSpan.Zero;
                }
                return TimeSpan.Zero;
            },
            () => _mediaStateService.CurrentAudioContent != null
        );
        
        _timerService.VideoTimerUpdated += (text) => 
        {
            videoTimerText.Text = text;
            
            // Синхронизируем позицию со вторым экраном в реальном времени
            if (_secondaryMediaElement != null && _secondaryMediaElement.Source != null && 
                mediaElement.Source != null && !_timerService.IsVideoSliderDragging)
            {
                try
                {
                    // Проверяем, что это тот же файл
                    if (mediaElement.Source.LocalPath == _secondaryMediaElement.Source.LocalPath)
                    {
                        var currentPos = mediaElement.Position;
                        var secondaryPos = _secondaryMediaElement.Position;
                        
                        // Синхронизируем только если разница больше 0.1 секунды (чтобы избежать постоянных обновлений)
                        if (Math.Abs((currentPos - secondaryPos).TotalSeconds) > 0.1)
                        {
                            _secondaryMediaElement.Position = currentPos;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Игнорируем ошибки синхронизации, чтобы не ломать основной таймер
                    System.Diagnostics.Debug.WriteLine($"Ошибка синхронизации позиции: {ex.Message}");
                }
            }
        };
        _timerService.VideoSliderUpdated += (value) => videoSlider.Value = value;
        _timerService.AudioTimerUpdated += (text) => audioTimerText.Text = text;
        _timerService.AudioSliderUpdated += (value) => audioSlider.Value = value;
        
        // Настройка SlotManager
        _slotManager.GetMediaSlot = (col, row) => _projectManager.GetMediaSlot(col, row);
        _slotManager.UpdateSlotButton = UpdateSlotButton;
        _slotManager.UpdateAllSlotButtonsHighlighting = UpdateAllSlotButtonsHighlighting;
        
        // Настройка SettingsManager
        _settingsManager.SetGlobalSettings(_projectManager.CurrentProject.GlobalSettings);
        
        // Настройка TransitionService
        _transitionService.SetGlobalSettings(_projectManager.CurrentProject.GlobalSettings);
        _transitionService.GetMediaBorder = () => mediaBorder;
        _transitionService.GetSecondaryScreenContent = () => 
        {
            var window = _secondaryScreenService.SecondaryScreenWindow ?? _secondaryScreenWindow;
            return window?.Content as FrameworkElement;
        };
        
        // Настройка MediaPlayerService
        _mediaPlayerService.SetMediaStateService(_mediaStateService);
        _mediaPlayerService.SetTransitionService(_transitionService);
        _mediaPlayerService.SetSettingsManager(_settingsManager);
        _mediaPlayerService.SetDeviceManager(_deviceManager);
        
        _mediaPlayerService.GetMainMediaElement = () => mediaElement;
        _mediaPlayerService.GetMediaBorder = () => mediaBorder;
        _mediaPlayerService.GetTextOverlayGrid = () => textOverlayGrid;
        _mediaPlayerService.GetSecondaryMediaElement = () => 
        {
            // Сначала проверяем сервис, потом локальную переменную
            return _secondaryScreenService.SecondaryMediaElement ?? _secondaryMediaElement;
        };
        _mediaPlayerService.GetSecondaryScreenWindow = () => 
        {
            // Сначала проверяем сервис, потом локальную переменную
            return _secondaryScreenService.SecondaryScreenWindow ?? _secondaryScreenWindow;
        };
        _mediaPlayerService.GetMainContentGrid = () => (Grid)Content;
        _mediaPlayerService.GetDispatcher = () => Dispatcher;
        _mediaPlayerService.GetCurrentMainMedia = () => _currentMainMedia;
        _mediaPlayerService.SetCurrentMainMedia = (value) => _currentMainMedia = value;
        _mediaPlayerService.GetCurrentVisualContent = () => _currentVisualContent;
        _mediaPlayerService.SetCurrentVisualContent = (value) => _currentVisualContent = value;
        _mediaPlayerService.GetCurrentAudioContent = () => _currentAudioContent;
        _mediaPlayerService.SetCurrentAudioContent = (value) => _currentAudioContent = value;
        _mediaPlayerService.GetUseUniformToFill = () => _useUniformToFill;
        _mediaPlayerService.GetIsVideoPaused = () => _isVideoPaused;
        _mediaPlayerService.SetIsVideoPaused = (value) => _isVideoPaused = value;
        _mediaPlayerService.SetIsVideoPlaying = (value) => isVideoPlaying = value;
        _mediaPlayerService.SetIsAudioPlaying = (value) => isAudioPlaying = value;
        
        _mediaPlayerService.GetSlotPosition = (slotKey) => GetSlotPosition(slotKey);
        _mediaPlayerService.SaveSlotPosition = (slotKey, position) => SaveSlotPosition(slotKey, position);
        _mediaPlayerService.RegisterActiveMediaFile = (path) => RegisterActiveMediaFile(path);
        _mediaPlayerService.IsMediaFileAlreadyPlaying = (path) => IsMediaFileAlreadyPlaying(path);
        _mediaPlayerService.ShouldBlockMediaFile = (path, type, slotKey) => ShouldBlockMediaFile(path, type, slotKey);
        
        _mediaPlayerService.TryGetAudioSlot = (slotKey) => _mediaStateService.TryGetAudioSlot(slotKey, out var element) ? element : null;
        _mediaPlayerService.TryGetAudioContainer = (slotKey) => _activeAudioContainers.TryGetValue(slotKey, out var container) ? container : null;
        _mediaPlayerService.AddAudioSlot = (slotKey, element, container) => _mediaStateService.AddAudioSlot(slotKey, element, container);
        _mediaPlayerService.StopActiveAudio = (slotKey) => StopActiveAudio();
        
        _mediaPlayerService.GetTextAlignment = (position) => GetTextAlignment(position);
        _mediaPlayerService.GetVerticalAlignment = (position) => GetVerticalAlignment(position);
        _mediaPlayerService.GetHorizontalAlignment = (position) => GetHorizontalAlignment(position);
        
        _mediaPlayerService.UpdateAllSlotButtonsHighlighting = () => UpdateAllSlotButtonsHighlighting();
        _mediaPlayerService.SelectElementForSettings = (slot, key) => SelectElementForSettings(slot, key);
        _mediaPlayerService.ApplyElementSettings = (slot, key) => ApplyElementSettings();
        _mediaPlayerService.ApplyGlobalSettings = () => ApplyGlobalSettings();
        _mediaPlayerService.GetGlobalSettings = () => _projectManager?.CurrentProject?.GlobalSettings;
        _mediaPlayerService.AutoPlayNextAudioElement = (slotKey) => AutoPlayNextAudioElement(slotKey);
        _mediaPlayerService.ConfigureAudioDevice = () => ConfigureAudioDevice();
        _mediaPlayerService.SetSecondaryMediaElement = (element) => _secondaryMediaElement = element;
        
        // Настройка TextBlockService
        _textBlockService.GetTextOverlayGrid = () => textOverlayGrid;
        _textBlockService.GetSecondaryScreenWindow = () => 
        {
            // Сначала проверяем сервис, потом локальную переменную
            return _secondaryScreenService.SecondaryScreenWindow ?? _secondaryScreenWindow;
        };
        
        // Настройка TriggerManager
        _triggerManager.GetMediaSlot = (col, row) => _projectManager.GetMediaSlot(col, row);
        _triggerManager.ShouldBlockMediaFile = (path) => false; // TODO: реализовать проверку
        
        // Настройка NavigationService
        _navigationService.SetProjectManager(_projectManager);
        _navigationService.GetSelectedElementSlot = () => _selectedElementSlot;
        _navigationService.SetSelectedElementSlot = (slot) => _selectedElementSlot = slot;
        _navigationService.LoadMediaFromSlotSelective = (slot) => LoadMediaFromSlotSelective(slot);
        _navigationService.SelectElementForSettings = (slot, key) => SelectElementForSettings(slot, key);
        
        // Настройка AutoPlayService
        _autoPlayService.SetProjectManager(_projectManager);
        _autoPlayService.GetCurrentMainMedia = () => _currentMainMedia;
        _autoPlayService.GetMediaSlot = (col, row) => _projectManager.GetMediaSlot(col, row);
        _autoPlayService.SelectElementForSettings = (slot, key) => 
        {
            _selectedElementSlot = slot;
            _selectedElementKey = key;
        };
        _autoPlayService.RestartCurrentElement = () => ElementRestart_Click(this, new RoutedEventArgs());
        _autoPlayService.LoadMediaFromSlot = async (slot) => 
        {
            LoadMediaFromSlotSelective(slot);
            await Task.CompletedTask;
        };
        
        // Настройка MediaControlService
        _mediaControlService.SetMediaStateService(_mediaStateService);
        _mediaControlService.GetMainMediaElement = () => mediaElement;
        _mediaControlService.SyncPauseWithSecondaryScreen = () => SyncPauseWithSecondaryScreen();
        _mediaControlService.GetCurrentMainMedia = () => _currentMainMedia;
        _mediaControlService.GetCurrentAudioContent = () => _currentAudioContent;
        _mediaControlService.TryGetAudioSlot = (slotKey) => _mediaStateService.TryGetAudioSlot(slotKey, out var element) ? element : null;
        _mediaControlService.GetSlotPosition = (slotKey) => GetSlotPosition(slotKey);
        _mediaControlService.SaveSlotPosition = (slotKey, position) => SaveSlotPosition(slotKey, position);
        _mediaControlService.GetMediaResumePosition = (path) => 
        {
            var position = _mediaStateService.GetMediaResumePosition(path);
            return position ?? TimeSpan.Zero;
        };
        _mediaControlService.SetIsVideoPlaying = (playing) => isVideoPlaying = playing;
        _mediaControlService.SetIsAudioPlaying = (playing) => isAudioPlaying = playing;
        
        // Настройка TriggerPlaybackService
        _triggerPlaybackService.SetMediaStateService(_mediaStateService);
        _triggerPlaybackService.SetTriggerManager(_triggerManager);
        _triggerPlaybackService.GetMainMediaElement = () => mediaElement;
        _triggerPlaybackService.GetMediaBorder = () => mediaBorder;
        _triggerPlaybackService.GetMainContentGrid = () => (Grid)Content;
        _triggerPlaybackService.GetDispatcher = () => Dispatcher;
        _triggerPlaybackService.GetCurrentMainMedia = () => _currentMainMedia;
        _triggerPlaybackService.SetCurrentMainMedia = (value) => _currentMainMedia = value;
        _triggerPlaybackService.GetCurrentVisualContent = () => _currentVisualContent;
        _triggerPlaybackService.SetCurrentVisualContent = (value) => _currentVisualContent = value;
        _triggerPlaybackService.GetCurrentAudioContent = () => _currentAudioContent;
        _triggerPlaybackService.SetCurrentAudioContent = (value) => _currentAudioContent = value;
        _triggerPlaybackService.IsMediaFileAlreadyPlaying = (path) => IsMediaFileAlreadyPlaying(path);
        _triggerPlaybackService.RegisterActiveMediaFile = (path) => RegisterActiveMediaFile(path);
        _triggerPlaybackService.GetSlotPosition = (slotKey) => GetSlotPosition(slotKey);
        _triggerPlaybackService.SaveSlotPosition = (slotKey, position) => SaveSlotPosition(slotKey, position);
        _triggerPlaybackService.TryGetAudioSlot = (slotKey) => _mediaStateService.TryGetAudioSlot(slotKey, out var element) ? element : null;
        _triggerPlaybackService.TryGetAudioContainer = (slotKey) => _activeAudioContainers.TryGetValue(slotKey, out var container) ? container : null;
        _triggerPlaybackService.AddAudioSlot = (slotKey, element, container) => _mediaStateService.AddAudioSlot(slotKey, element, container);
        _triggerPlaybackService.GetTriggerState = (column) => GetTriggerState(column);
        _triggerPlaybackService.SetTriggerState = (column, state) => SetTriggerState(column, state);
        _triggerPlaybackService.GetActiveTriggerColumn = () => _activeTriggerColumn;
        _triggerPlaybackService.SetActiveTriggerColumn = (column) => _activeTriggerColumn = column;
        _triggerPlaybackService.GetLastUsedTriggerColumn = () => _lastUsedTriggerColumn;
        _triggerPlaybackService.SetLastUsedTriggerColumn = (column) => _lastUsedTriggerColumn = column;
        _triggerPlaybackService.SetIsVideoPlaying = (playing) => isVideoPlaying = playing;
        _triggerPlaybackService.SetIsAudioPlaying = (playing) => isAudioPlaying = playing;
        _triggerPlaybackService.UpdateAllSlotButtonsHighlighting = () => UpdateAllSlotButtonsHighlighting();
        
        // Настройка DialogService
        _dialogService.SetDeviceManager(_deviceManager);
        _dialogService.GetAudioOutputDevices = () => GetAudioOutputDevices();
        _dialogService.SetSelectedScreenIndex = (index) => _selectedScreenIndex = index;
        _dialogService.SetUseSelectedScreen = (use) => _useSelectedScreen = use;
        _dialogService.SetUseUniformToFill = (use) => _useUniformToFill = use;
        _dialogService.SetSelectedAudioDeviceIndex = (index) => _selectedAudioDeviceIndex = index;
        _dialogService.SetUseSelectedAudio = (use) => _useSelectedAudio = use;
        _dialogService.GetUseSelectedScreen = () => _useSelectedScreen;
        _dialogService.GetSelectedScreenIndex = () => _selectedScreenIndex;
        _dialogService.GetUseUniformToFill = () => _useUniformToFill;
        _dialogService.GetUseSelectedAudio = () => _useSelectedAudio;
        _dialogService.GetSelectedAudioDeviceIndex = () => _selectedAudioDeviceIndex;
        _dialogService.CreateSecondaryScreenWindow = () => 
        {
            CreateSecondaryScreenWindow();
        };
        _dialogService.CloseSecondaryScreenWindow = () => 
        {
            CloseSecondaryScreenWindow();
        };
        _dialogService.LoadMediaToSlot = (col, row) => LoadMediaToSlot(col, row);
        _dialogService.CreateTextBlock = (col, row) => CreateTextBlock(col, row);
        
        // Настройка ContextMenuService
        _contextMenuService.GetContextMenuStyle = (obj) => (Style)FindResource("ContextMenuStyle");
        _contextMenuService.GetContextMenuItemStyle = (obj) => (Style)FindResource("ContextMenuItemStyle");
        _contextMenuService.GetDeleteMenuItemStyle = (obj) => (Style)FindResource("DeleteMenuItemStyle");
        _contextMenuService.IsSlotPaused = (slotKey) => 
        {
            if (_currentMainMedia == slotKey && _isVideoPaused) return true;
            if (_currentAudioContent == slotKey && _audioPausedStates.ContainsKey(slotKey) && _audioPausedStates[slotKey]) return true;
            return false;
        };
        _contextMenuService.GetTriggerState = (column) => GetTriggerState(column);
        _contextMenuService.GetActiveTriggerColumn = () => _activeTriggerColumn;
        _contextMenuService.OnRestartItemClick = (tag) => 
        {
            // Вызываем логику напрямую с тегом
            if (tag.StartsWith("Slot_"))
            {
                var parts = tag.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int column) && int.TryParse(parts[2], out int row))
                {
                    var slot = _projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => s.Column == column && s.Row == row);
                    if (slot != null)
                    {
                        var slotKey = $"Slot_{column}_{row}";
                        bool isMainMedia = _currentMainMedia == slotKey;
                        bool isAudioMedia = _currentAudioContent == slotKey && _activeAudioSlots.ContainsKey(slotKey);
                        
                        if (isMainMedia || isAudioMedia)
                        {
                            if (isMainMedia)
                            {
                                mediaElement.Stop();
                                mediaElement.Position = TimeSpan.Zero;
                                _isVideoPaused = false;
                                
                                if (_secondaryMediaElement != null && _secondaryMediaElement.Source != null &&
                                    mediaElement.Source != null &&
                                    mediaElement.Source.LocalPath == _secondaryMediaElement.Source.LocalPath)
                                {
                                    try
                                    {
                                        _secondaryMediaElement.Stop();
                                        _secondaryMediaElement.Position = TimeSpan.Zero;
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Ошибка при перезапуске на втором экране: {ex.Message}");
                                    }
                                }
                                
                                mediaElement.Play();
                                
                                if (_secondaryMediaElement != null && _secondaryMediaElement.Source != null)
                                {
                                    try
                                    {
                                        _secondaryMediaElement.Play();
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Ошибка при запуске на втором экране: {ex.Message}");
                                    }
                                }
                            }
                            
                            if (isAudioMedia && _activeAudioSlots.TryGetValue(slotKey, out var audioElement) && audioElement != null)
                            {
                                audioElement.Stop();
                                audioElement.Position = TimeSpan.Zero;
                                audioElement.Play();
                            }
                            
                            UpdateAllSlotButtonsHighlighting();
                        }
                        else
                        {
                            LoadMediaFromSlotSelective(slot);
                        }
                    }
                }
            }
        };
        _contextMenuService.OnPauseItemClick = (tag) => 
        {
            if (tag.StartsWith("Slot_"))
            {
                var parts = tag.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int column) && int.TryParse(parts[2], out int row))
                {
                    var slotKey = $"Slot_{column}_{row}";
                    
                    if (_currentMainMedia == slotKey)
                    {
                        if (_isVideoPaused)
                        {
                            mediaElement.Play();
                            _isVideoPaused = false;
                            SyncPlayWithSecondaryScreen();
                        }
                        else
                        {
                            SaveSlotPosition(slotKey, mediaElement.Position);
                            mediaElement.Pause();
                            _isVideoPaused = true;
                            SyncPauseWithSecondaryScreen();
                        }
                        UpdateAllSlotButtonsHighlighting();
                    }
                    else if (_currentAudioContent == slotKey && _activeAudioSlots.TryGetValue(slotKey, out var audioElement))
                    {
                        if (_audioPausedStates.ContainsKey(slotKey) && _audioPausedStates[slotKey])
                        {
                            audioElement.Play();
                            _audioPausedStates[slotKey] = false;
                        }
                        else
                        {
                            SaveSlotPosition(slotKey, audioElement.Position);
                            audioElement.Pause();
                            _audioPausedStates[slotKey] = true;
                        }
                        UpdateAllSlotButtonsHighlighting();
                    }
                }
            }
        };
        _contextMenuService.OnSettingsItemClick = (tag) => 
        {
            if (tag.StartsWith("Slot_"))
            {
                var parts = tag.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int column) && int.TryParse(parts[2], out int row))
                {
                    var slot = _projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => s.Column == column && s.Row == row);
                    if (slot != null)
                    {
                        SelectElementForSettings(slot, tag);
                    }
                }
            }
        };
        _contextMenuService.OnDeleteItemClick = (tag) => 
        {
            var result = MessageBox.Show("Вы уверены, что хотите удалить этот элемент?", "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                if (tag.StartsWith("Slot_"))
                {
                    var parts = tag.Split('_');
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int column) && int.TryParse(parts[2], out int row))
                    {
                        var slot = _projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => s.Column == column && s.Row == row);
                        if (slot != null)
                        {
                            _projectManager.CurrentProject.MediaSlots.Remove(slot);
                            UpdateSlotButton(column, row, "", MediaType.Video);
                        }
                    }
                }
            }
        };
        
        // Обновляем SlotManager для использования SlotUIService
        _slotManager.UpdateSlotButton = (col, row, path, type) => _slotUIService.UpdateSlotButton(col, row, path, type);
        _slotManager.UpdateAllSlotButtonsHighlighting = () => _slotUIService.UpdateAllSlotButtonsHighlighting();
        
        // Настройка SlotUIService
        _slotUIService.SetProjectManager(_projectManager);
        _slotUIService.GetBottomPanel = () => BottomPanel;
        _slotUIService.GetCurrentMainMedia = () => _currentMainMedia;
        _slotUIService.GetCurrentAudioContent = () => _currentAudioContent;
        _slotUIService.GetActiveTriggerColumn = () => _activeTriggerColumn;
        _slotUIService.GetTriggerState = (column) => GetTriggerState(column);
        
        // Настройка SlotCreationService
        _slotCreationService.GetBottomPanel = () => BottomPanel;
        _slotCreationService.GetTriggerButtonStyle = (obj) => (Style)FindResource("TriggerButtonStyle");
        _slotCreationService.OnSlotClick = (sender, e) => 
        {
            if (sender is Button button && e is RoutedEventArgs routedEventArgs)
            {
                Slot_Click(button, routedEventArgs);
            }
        };
        _slotCreationService.CreateContextMenu = (button) => CreateContextMenu(button);
        
        // Настройка SecondaryScreenService
        _secondaryScreenService.GetUseSelectedScreen = () => _useSelectedScreen;
        _secondaryScreenService.GetSelectedScreenIndex = () => _selectedScreenIndex;
        _secondaryScreenService.GetUseUniformToFill = () => _useUniformToFill;
        _secondaryScreenService.GetMainMediaElement = () => mediaElement;
        _secondaryScreenService.SetSecondaryMediaElement = (element) => 
        {
            _secondaryMediaElement = element;
            System.Diagnostics.Debug.WriteLine($"SetSecondaryMediaElement: Установлен MediaElement для второго экрана");
        };
        // GetSecondaryScreenWindow не используется в сервисе, так как сервис хранит окно сам
        
        // Настройка VideoDisplayService
        _videoDisplayService.GetMainMediaElement = () => mediaElement;
        _videoDisplayService.GetMediaBorder = () => mediaBorder;
        _videoDisplayService.GetTextOverlayGrid = () => textOverlayGrid;
        _videoDisplayService.GetSecondaryMediaElement = () => 
        {
            return _secondaryScreenService.SecondaryMediaElement ?? _secondaryMediaElement;
        };
        _videoDisplayService.GetCurrentMainMedia = () => _currentMainMedia;
        _videoDisplayService.SetCurrentMainMedia = (value) => _currentMainMedia = value;
        _videoDisplayService.SetIsVideoPlaying = (playing) => isVideoPlaying = playing;
        _videoDisplayService.GetIsVideoPaused = () => _isVideoPaused;
        _videoDisplayService.SetIsVideoPaused = (value) => _isVideoPaused = value;
        _videoDisplayService.SyncPlayWithSecondaryScreen = () => SyncPlayWithSecondaryScreen();
        _videoDisplayService.SyncPauseWithSecondaryScreen = () => SyncPauseWithSecondaryScreen();
        _videoDisplayService.GetMediaResumePosition = (path) => 
        {
            return _mediaStateService.GetMediaResumePosition(path) ?? TimeSpan.Zero;
        };
        _videoDisplayService.SaveMediaResumePosition = (path, position) => 
        {
            _mediaResumePositions[path] = position;
            _mediaStateService.SaveMediaResumePosition(path, position);
        };
        _videoDisplayService.ApplyElementSettings = (slot, key) => ApplyElementSettings(slot, key);
    }
    
    // Инициализация меню экранов
    private void InitializeScreensMenu()
    {
        try
        {
            ScreensMenuItem.Items.Clear();
            
            // Получаем реальные экраны
            var screens = System.Windows.Forms.Screen.AllScreens;
            
            for (int i = 0; i < screens.Length; i++)
            {
                var screen = screens[i];
                var menuItem = new System.Windows.Controls.MenuItem
                {
                    Header = $"Экран {i + 1}: {screen.Bounds.Width}x{screen.Bounds.Height} {(screen.Primary ? "(Основной)" : "")}",
                    Tag = i
                };
                menuItem.Click += ScreenMenuItem_Click;
                ScreensMenuItem.Items.Add(menuItem);
            }
            
            if (screens.Length == 0)
            {
                var noScreensItem = new System.Windows.Controls.MenuItem
                {
                    Header = "Экраны не найдены",
                    IsEnabled = false
                };
                ScreensMenuItem.Items.Add(noScreensItem);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка при инициализации меню экранов: {ex.Message}");
        }
    }
    
    // Инициализация меню звука
    private void InitializeAudioMenu()
    {
        try
        {
            AudioMenuItem.Items.Clear();
            
            // Получаем все устройства вывода звука
            var audioDevices = GetAudioOutputDevices();
            
            for (int i = 0; i < audioDevices.Count; i++)
            {
                var device = audioDevices[i];
                var menuItem = new System.Windows.Controls.MenuItem
                {
                    Header = device,
                    Tag = i
                };
                menuItem.Click += AudioMenuItem_Click;
                AudioMenuItem.Items.Add(menuItem);
            }
            
            if (audioDevices.Count == 0)
            {
                var noAudioItem = new System.Windows.Controls.MenuItem
                {
                    Header = "Устройства звука не найдены",
                    IsEnabled = false
                };
                AudioMenuItem.Items.Add(noAudioItem);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка при инициализации меню звука: {ex.Message}");
        }
    }
    
    // Получение списка устройств вывода звука
    private List<string> GetAudioOutputDevices()
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
    
    // Обработчик клика по экрану
    private void ScreenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is int screenIndex)
        {
            ShowScreenSelectionDialog(screenIndex);
        }
    }
    
    // Диалог выбора экрана
    private void ShowScreenSelectionDialog(int screenIndex)
    {
        _dialogService.ShowScreenSelectionDialog(screenIndex);
    }
    
    // Обработчик клика по устройству звука
    private void AudioMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is int deviceIndex)
        {
            ShowAudioSelectionDialog(deviceIndex);
        }
    }
    
    // Диалог выбора аудиоустройства
    private void ShowAudioSelectionDialog(int deviceIndex)
    {
        _dialogService.ShowAudioSelectionDialog(deviceIndex);
    }
    
    // Обработчик открытия меню Edit - обновляем устройства в реальном времени
    private void EditMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        // Обновляем список экранов и аудиоустройств
        InitializeScreensMenu();
        InitializeAudioMenu();
    }
    
    // Создание окна на дополнительном экране
    private void CreateSecondaryScreenWindow()
    {
        _secondaryScreenService.CreateSecondaryScreenWindow();
        // Обновляем локальные переменные для обратной совместимости
        _secondaryScreenWindow = _secondaryScreenService.SecondaryScreenWindow;
        _secondaryMediaElement = _secondaryScreenService.SecondaryMediaElement;
        
        System.Diagnostics.Debug.WriteLine($"CreateSecondaryScreenWindow: Окно создано. Window={_secondaryScreenWindow != null}, MediaElement={_secondaryMediaElement != null}");
    }
    
    // Закрытие окна на дополнительном экране
    private void CloseSecondaryScreenWindow()
    {
        _secondaryScreenService.CloseSecondaryScreenWindow();
        _secondaryScreenWindow = null;
        _secondaryMediaElement = null;
    }
    
    // Настройка аудиоустройства
    private void ConfigureAudioDevice()
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

    public bool isVideoPlaying = false;
    public bool isAudioPlaying = false;
    
    // Переменные для управления слайдерами (используются через TimerService)
    // Отслеживание состояния воспроизведения (используется через MediaStateService)
    
    // Таймеры теперь управляются через TimerService
    private void StartVideoTimer()
    {
        _timerService.StartVideoTimer();
    }
    
    private void StopVideoTimer()
    {
        _timerService.StopVideoTimer();
    }
    
    private void StartAudioTimer()
    {
        _timerService.StartAudioTimer();
    }
    
    private void StopAudioTimer()
    {
        _timerService.StopAudioTimer();
    }
    
    /// <summary>
    /// Сохраняет позицию воспроизведения для слота
    /// </summary>
    private void SaveSlotPosition(string slotKey, TimeSpan position)
    {
        _mediaStateService.SaveSlotPosition(slotKey, position);
    }
    
    /// <summary>
    /// Получает сохраненную позицию воспроизведения для слота
    /// </summary>
    private TimeSpan GetSlotPosition(string slotKey)
    {
        return _mediaStateService.GetSlotPosition(slotKey);
    }
    
    /// <summary>
    /// Очищает позицию слота (при остановке)
    /// </summary>
    private void ClearSlotPosition(string slotKey)
    {
        _mediaStateService.ClearSlotPosition(slotKey);
    }

    private void LoadMedia(object sender, RoutedEventArgs e){
        _mediaControlService.LoadMedia();
    }

    private void PlayMedia(object sender, RoutedEventArgs e){
        _mediaControlService.PlayMedia();
    }

    private void StopMedia(object sender, RoutedEventArgs e){
        _mediaControlService.StopMedia();
    }

    private void CloseMedia(object sender, RoutedEventArgs e){
        _mediaControlService.CloseMedia();
        _currentMainMedia = null;
        isVideoPlaying = false;
    }

    private void CreateColumns(int columns){
        _slotCreationService.CreateColumns(columns);
    }

    private void Slot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            string? tag = btn.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            // Парсим тег для получения координат слота
            string[] parts = tag.Replace("Slot_", "").Split('_');
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out int column) && 
                int.TryParse(parts[1], out int row))
            {
                // Проверяем, есть ли медиа в этом слоте
                var mediaSlot = _projectManager.GetMediaSlot(column, row);
                if (mediaSlot != null && (!string.IsNullOrEmpty(mediaSlot.MediaPath) || mediaSlot.Type == MediaType.Text))
                {
                    string slotKey = $"Slot_{column}_{row}";
                    
                    // Проверяем, воспроизводится ли этот слот сейчас
                    bool isMainMedia = false;
                    
                    if (mediaSlot.Type == MediaType.Text)
                    {
                        // Для текста проверяем наличие текста в textOverlayGrid и совпадение _currentMainMedia
                        isMainMedia = _currentMainMedia == slotKey && textOverlayGrid.Children.Count > 0;
                    }
                    else if (mediaSlot.Type == MediaType.Image)
                    {
                        // Для изображений проверяем наличие Image в mediaBorder
                        // Проверяем как по _currentMainMedia, так и по наличию изображения и пути
                        bool hasImage = false;
                        bool imagePathMatches = false;
                        
                        if (mediaBorder.Child is Grid mainGrid)
                        {
                            var images = mainGrid.Children.OfType<Image>().ToList();
                            hasImage = images.Any();
                            // Проверяем, совпадает ли путь изображения с путем слота
                            if (hasImage && !string.IsNullOrEmpty(mediaSlot.MediaPath))
                            {
                                imagePathMatches = images.Any(img => 
                                    img.Source is BitmapImage bitmap && 
                                    bitmap.UriSource != null && 
                                    bitmap.UriSource.LocalPath.Equals(mediaSlot.MediaPath, StringComparison.OrdinalIgnoreCase));
                            }
                        }
                        else if (mediaBorder.Child is Image currentImage)
                        {
                            hasImage = true;
                            // Проверяем путь изображения
                            if (!string.IsNullOrEmpty(mediaSlot.MediaPath) && currentImage.Source is BitmapImage bitmap)
                            {
                                imagePathMatches = bitmap.UriSource != null && 
                                    bitmap.UriSource.LocalPath.Equals(mediaSlot.MediaPath, StringComparison.OrdinalIgnoreCase);
                            }
                        }
                        
                        // Если изображение отображается И это тот же слот (по _currentMainMedia, _currentVisualContent или по пути)
                        isMainMedia = hasImage && (_currentMainMedia == slotKey || _currentVisualContent == slotKey || imagePathMatches);
                    }
                    else
                    {
                        // Для видео проверяем Source и _currentMainMedia
                        isMainMedia = _currentMainMedia == slotKey && mediaElement.Source != null;
                    }
                    
                    bool isAudioMedia = _currentAudioContent == slotKey && _activeAudioSlots.ContainsKey(slotKey);
                    
                    if (isMainMedia || isAudioMedia)
                    {
                        // Если этот слот уже воспроизводится - ставим на паузу/возобновляем или останавливаем
                        if (isMainMedia)
                        {
                            // Для изображений и текста - останавливаем (гасим) при повторном клике
                            if (mediaSlot.Type == MediaType.Image)
                            {
                                // Останавливаем изображение - очищаем визуальный контент
                                mediaElement.Stop();
                                mediaElement.Source = null;
                                _currentMainMedia = null;
                                
                                // Восстанавливаем MediaElement если был заменен на Image
                                if (mediaBorder.Child != mediaElement)
                                {
                                    RestoreMediaElement(mediaElement);
                                    mediaElement.Visibility = Visibility.Visible;
                                }
                                
                                // Очищаем изображение на втором экране
                                if (_secondaryScreenWindow?.Content is Image)
                                {
                                    _secondaryScreenWindow.Content = null;
                                }
                                else if (_secondaryScreenWindow?.Content is Grid secondaryGrid)
                                {
                                    var secondaryImages = secondaryGrid.Children.OfType<Image>().ToList();
                                    foreach (var img in secondaryImages)
                                    {
                                        secondaryGrid.Children.Remove(img);
                                    }
                                }
                                
                                // Обновляем подсветку кнопок
                                UpdateAllSlotButtonsHighlighting();
                                return;
                            }
                            
                            if (mediaSlot.Type == MediaType.Text)
                            {
                                // Останавливаем текст - очищаем textOverlayGrid
                                textOverlayGrid.Children.Clear();
                                textOverlayGrid.Visibility = Visibility.Hidden;
                                _currentMainMedia = null;
                                
                                // Очищаем текст на втором экране, если он там есть
                                if (_secondaryScreenWindow?.Content is Grid secondaryGrid)
                                {
                                    var secondaryTextBlocks = secondaryGrid.Children.OfType<TextBlock>().ToList();
                                    foreach (var textBlock in secondaryTextBlocks)
                                    {
                                        secondaryGrid.Children.Remove(textBlock);
                                    }
                                }
                                
                                // Обновляем подсветку кнопок
                                UpdateAllSlotButtonsHighlighting();
                                return;
                            }
                            
                            // Для видео - управляем паузой/возобновлением
                            if (_isVideoPaused)
                            {
                                // Видео на паузе - возобновляем
                                mediaElement.Play();
                                SyncPlayWithSecondaryScreen();
                                _isVideoPaused = false;
                                
                                // Скрываем кнопку триггера при возобновлении
                                // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                /*
                                var triggerButton = FindTriggerButton(column);
                                if (triggerButton != null)
                                {
                                    triggerButton.Visibility = Visibility.Hidden;
                                }
                                */
                            }
                            else
                            {
                                // Видео воспроизводится - ставим на паузу
                                mediaElement.Pause();
                                SyncPauseWithSecondaryScreen();
                                _isVideoPaused = true;
                                
                                // Возвращаем кнопку триггера при паузе
                                // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                /*
                                var triggerButton = FindTriggerButton(column);
                                if (triggerButton != null)
                                {
                                    triggerButton.Visibility = Visibility.Visible;
                                }
                                */
                            }
                        }
                        
                        if (isAudioMedia)
                        {
                            // Управляем аудио - безопасно получаем элемент
                            if (_mediaStateService.TryGetAudioSlot(slotKey, out var audioElement) && audioElement != null)
                            {
                                bool isAudioPaused = _mediaStateService.IsAudioPaused(slotKey);
                                
                                if (isAudioPaused)
                                {
                                    // Аудио на паузе - возобновляем
                                    audioElement.Play();
                                    _mediaStateService.SetAudioPaused(slotKey, false);
                                    
                                    // Скрываем кнопку триггера при возобновлении
                                    // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                    /*
                                    var triggerButton = FindTriggerButton(column);
                                    if (triggerButton != null)
                                    {
                                        triggerButton.Visibility = Visibility.Hidden;
                                    }
                                    */
                                }
                                else
                                {
                                    // Аудио воспроизводится - ставим на паузу
                                    audioElement.Pause();
                                    _mediaStateService.SetAudioPaused(slotKey, true);
                                    
                                    // Возвращаем кнопку триггера при паузе
                                    // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                    /*
                                    var triggerButton = FindTriggerButton(column);
                                    if (triggerButton != null)
                                    {
                                        triggerButton.Visibility = Visibility.Visible;
                                    }
                                    */
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"ОШИБКА: Аудио элемент не найден для слота {slotKey} в Slot_Click");
                            }
                        }
                    }
                    else
                    {
                        // Если этот слот не воспроизводится - запускаем его
                        LoadMediaFromSlotSelective(mediaSlot);
                    }
                }
                else
                {
                    // Предлагаем загрузить медиа или создать текстовый блок в этот слот
                    ShowSlotOptionsDialog(column, row);
                }
            }
        }
    }

    /// <summary>
    /// Синхронизирует паузу со вторым экраном
    /// </summary>
    private void SyncPauseWithSecondaryScreen()
    {
        _secondaryScreenService.SyncPauseWithSecondaryScreen();
    }
    
    /// <summary>
    /// Синхронизирует возобновление со вторым экраном
    /// </summary>
    private void SyncPlayWithSecondaryScreen()
    {
        _secondaryScreenService.SyncPlayWithSecondaryScreen();
    }
    
    /// <summary>
    /// Обновляет MediaElement, сохраняя текстовые блоки в textOverlayGrid
    /// </summary>
    private void UpdateMediaElement(MediaElement mediaElement)
    {
        _videoDisplayService.UpdateMediaElement(mediaElement);
    }
    
    /// <summary>
    /// Восстанавливает MediaElement в Border, сохраняя текстовые блоки
    /// </summary>
    private void RestoreMediaElement(MediaElement mediaElement)
    {
        _videoDisplayService.RestoreMediaElement(mediaElement);
    }

    private void LoadMediaFromSlot(MediaSlot mediaSlot)
    {
        try
        {
            // Сохраняем позицию текущего медиа перед переключением на новый слот
            if (_currentMainMedia != null && mediaElement.Source != null)
            {
                var currentPosition = mediaElement.Position;
                _mediaResumePositions[mediaElement.Source.LocalPath] = currentPosition;
                System.Diagnostics.Debug.WriteLine($"СОХРАНЕНИЕ ПОЗИЦИИ LoadMediaFromSlot: {mediaElement.Source.LocalPath} -> {currentPosition}");
            }
            
            // Устанавливаем что основной плеер теперь принадлежит этому слоту
            _currentMainMedia = $"Slot_{mediaSlot.Column}_{mediaSlot.Row}";
            
            // ВАЖНО: Устанавливаем LoadedBehavior ПЕРЕД установкой Source
            mediaElement.LoadedBehavior = MediaState.Manual;
            
            // Обновляем медиа, сохраняя текстовые блоки
            UpdateMediaElement(mediaElement);
            
            mediaElement.Source = new Uri(mediaSlot.MediaPath);
            
            // Восстанавливаем позицию после загрузки медиа
            RoutedEventHandler? mediaOpenedHandler = null;
            mediaOpenedHandler = (s, e) =>
            {
                // Отписываемся от события, чтобы избежать повторных вызовов
                mediaElement.MediaOpened -= mediaOpenedHandler;
                
                if (_mediaResumePositions.TryGetValue(mediaSlot.MediaPath, out var resumePosition))
                {
                    mediaElement.Position = resumePosition;
                    System.Diagnostics.Debug.WriteLine($"ВОССТАНОВЛЕНИЕ ПОЗИЦИИ LoadMediaFromSlot: {mediaSlot.MediaPath} -> {resumePosition}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"НЕТ СОХРАНЕННОЙ ПОЗИЦИИ LoadMediaFromSlot: {mediaSlot.MediaPath}");
                }
            mediaElement.Play();
            };
            mediaElement.MediaOpened += mediaOpenedHandler;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке медиа: {ex.Message}", "Ошибка");
        }
    }

    private async void LoadMediaFromSlotSelective(MediaSlot mediaSlot)
    {
        try
        {
            await _mediaPlayerService.LoadMediaFromSlotSelective(mediaSlot);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке медиа: {ex.Message}", "Ошибка");
        }
    }

    private void ShowSlotOptionsDialog(int column, int row)
    {
        _dialogService.ShowSlotOptionsDialog(column, row);
    }

    private void LoadMediaToSlot(int column, int row)
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Media files|*.mp4;*.avi;*.mov;*.wmv;*.flv;*.mkv;*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.mp3;*.wav;*.flac;*.aac";
        
        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            MediaType mediaType = GetMediaType(filePath);
            
            // Добавляем медиа в проект
            _projectManager.AddMediaToSlot(column, row, filePath, mediaType);
            
            // Обновляем кнопку с превью
            UpdateSlotButton(column, row, filePath, mediaType);
            
            MessageBox.Show($"Медиа добавлено в слот {column}-{row}", "Успех");
        }
    }

    private void CreateTextBlock(int column, int row)
    {
        try
        {
            // Проверяем, что ProjectManager инициализирован
            if (_projectManager?.CurrentProject == null)
            {
                MessageBox.Show("Проект не инициализирован. Создайте новый проект.", "Ошибка");
                return;
            }

            // Создаем простое диалоговое окно для ввода текста
            var textInputDialog = new TextInputDialog();
            textInputDialog.Title = "Создание текстового блока";
            textInputDialog.LabelText = "Введите текст:";
            textInputDialog.TextValue = "";
            
            if (textInputDialog.ShowDialog() == true)
            {
                string textContent = textInputDialog.TextValue;
                if (!string.IsNullOrWhiteSpace(textContent))
                {
                    // Создаем текстовый слот
                    var textSlot = new MediaSlot
                    {
                        Column = column,
                        Row = row,
                        MediaPath = "", // Для текстовых блоков путь пустой
                        Type = MediaType.Text,
                        PreviewPath = "",
                        DisplayName = textContent.Length > 10 ? textContent.Substring(0, 10) + "..." : textContent,
                        TextContent = textContent,
                        FontFamily = "Arial",
                        FontSize = 24,
                        FontColor = "White",
                        BackgroundColor = "Transparent",
                        TextPosition = "Center",
                        TextX = 0,
                        TextY = 0,
                        UseManualPosition = false,
                        IsTextVisible = true
                    };
                    
                    // Добавляем в проект
                    _projectManager.CurrentProject.MediaSlots.RemoveAll(slot => slot.Column == column && slot.Row == row);
                    _projectManager.CurrentProject.MediaSlots.Add(textSlot);
                    
                    // Обновляем кнопку
                    UpdateSlotButton(column, row, "", MediaType.Text);
                    
                    // Обновляем подсветку всех кнопок
                    UpdateAllSlotButtonsHighlighting();
                    
                    MessageBox.Show($"Текстовый блок создан в слоте {column}-{row}", "Успех");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании текстового блока: {ex.Message}", "Ошибка");
            System.Diagnostics.Debug.WriteLine($"CreateTextBlock Error: {ex}");
        }
    }

    private MediaType GetMediaType(string filePath)
    {
        string extension = System.IO.Path.GetExtension(filePath).ToLower();
        
        if (new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv" }.Contains(extension))
            return MediaType.Video;
        else if (new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" }.Contains(extension))
            return MediaType.Image;
        else if (new[] { ".mp3", ".wav", ".flac", ".aac" }.Contains(extension))
            return MediaType.Audio;
        
        return MediaType.Video; // По умолчанию
    }

    private void UpdateSlotButton(int column, int row, string mediaPath, MediaType mediaType)
    {
        _slotUIService.UpdateSlotButton(column, row, mediaPath, mediaType);
    }

    private string GetMediaIcon(MediaType mediaType)
    {
        return _slotUIService.GetMediaIcon(mediaType);
    }

    /// <summary>
    /// Обновляет подсветку всех кнопок слотов в зависимости от их активности
    /// </summary>
    private void UpdateAllSlotButtonsHighlighting()
    {
        _slotUIService.UpdateAllSlotButtonsHighlighting();
    }

    /// <summary>
    /// Проверяет совместимость типа медиа с текущим воспроизведением
    /// </summary>
    private bool IsMediaTypeCompatible(MediaType newType)
    {
        var currentType = GetCurrentMediaType();
        
        // Если ничего не воспроизводится - можно запустить любой тип
        if (currentType == null) return true;
        
        // Правила совместимости:
        // - Звук + картинка = OK (параллельно)
        // - Звук + видео = OK (параллельно)
        // - Звук + звук = OK (замена, не параллельно)
        // - Видео + видео = OK (замена, не параллельно)
        // - Картинка + картинка = OK (замена, не параллельно)
        // - Видео + изображение = OK (замена)
        // - Изображение + видео = OK (замена)
        
        // Все типы совместимы - они заменяют друг друга или воспроизводятся параллельно
        return true;
    }

    /// <summary>
    /// Получает текущий тип воспроизводимого медиа
    /// </summary>
    private MediaType? GetCurrentMediaType()
    {
        // Проверяем визуальный контент
        if (_currentVisualContent != null)
        {
            if (_currentVisualContent.StartsWith("Trigger_"))
            {
                // Для триггеров нужно проверить что именно воспроизводится
                var column = int.Parse(_currentVisualContent.Replace("Trigger_", ""));
                var slot1 = _projectManager.GetMediaSlot(column, 1);
                var slot2 = _projectManager.GetMediaSlot(column, 2);
                
                // Если есть видео - возвращаем Video
                if (slot1?.Type == MediaType.Video || slot2?.Type == MediaType.Video)
                    return MediaType.Video;
                // Если есть изображение - возвращаем Image
                if (slot1?.Type == MediaType.Image || slot2?.Type == MediaType.Image)
                    return MediaType.Image;
            }
            else if (_currentVisualContent.StartsWith("Slot_"))
            {
                // Для слотов получаем тип из проекта
                var parts = _currentVisualContent.Replace("Slot_", "").Split('_');
                if (parts.Length == 2 && 
                    int.TryParse(parts[0], out int column) && 
                    int.TryParse(parts[1], out int row))
                {
                    var slot = _projectManager.GetMediaSlot(column, row);
                    return slot?.Type;
                }
            }
        }
        
        // Проверяем аудио контент
        if (_currentAudioContent != null)
        {
            if (_currentAudioContent.StartsWith("Trigger_"))
            {
                // Для триггеров проверяем есть ли аудио
                var column = int.Parse(_currentAudioContent.Replace("Trigger_", ""));
                var slot1 = _projectManager.GetMediaSlot(column, 1);
                var slot2 = _projectManager.GetMediaSlot(column, 2);
                
                if (slot1?.Type == MediaType.Audio || slot2?.Type == MediaType.Audio)
                    return MediaType.Audio;
            }
            else if (_currentAudioContent.StartsWith("Slot_"))
            {
                return MediaType.Audio; // Если активен аудио слот
            }
        }
        
        return null; // Ничего не воспроизводится
    }

    /// <summary>
    /// Возвращает читаемое название типа медиа
    /// </summary>
    private string GetMediaTypeName(MediaType? mediaType)
    {
        return mediaType switch
        {
            MediaType.Video => "видео",
            MediaType.Image => "изображение", 
            MediaType.Audio => "аудио",
            MediaType.Text => "текст",
            _ => "неизвестный тип"
        };
    }

    private TextAlignment GetTextAlignment(string position)
    {
        return position switch
        {
            "TopLeft" or "CenterLeft" or "BottomLeft" => TextAlignment.Left,
            "TopRight" or "CenterRight" or "BottomRight" => TextAlignment.Right,
            _ => TextAlignment.Center
        };
    }

    private VerticalAlignment GetVerticalAlignment(string position)
    {
        return position switch
        {
            "TopLeft" or "TopCenter" or "TopRight" => VerticalAlignment.Top,
            "BottomLeft" or "BottomCenter" or "BottomRight" => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center
        };
    }

    private HorizontalAlignment GetHorizontalAlignment(string position)
    {
        return position switch
        {
            "TopLeft" or "CenterLeft" or "BottomLeft" => HorizontalAlignment.Left,
            "TopRight" or "CenterRight" or "BottomRight" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Center
        };
    }

    /// <summary>
    /// Проверяет, нужно ли блокировать запуск медиафайла (только для аудио)
    /// </summary>
    private bool ShouldBlockMediaFile(string mediaPath, MediaType mediaType, string? currentSlotKey = null)
    {
        // Блокируем только аудио файлы от дублирования
        // Изображения и видео должны заменяться
        if (mediaType == MediaType.Audio && IsMediaFileAlreadyPlaying(mediaPath))
        {
            // НЕ блокируем, если это тот же слот/триггер (для паузы/возобновления)
            if (!string.IsNullOrEmpty(currentSlotKey))
            {
                // Проверяем, воспроизводится ли этот файл в том же слоте/триггере
                if (_currentAudioContent == currentSlotKey || _currentMainMedia == currentSlotKey)
                {
                    return false; // Не блокируем, это тот же слот
                }
            }
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Проверяет, нужно ли блокировать запуск триггера
    /// </summary>
    private bool ShouldBlockTrigger(MediaSlot? videoSlot, MediaSlot? audioSlot, MediaSlot? imageSlot)
    {
        // Для триггеров не блокируем если аудио уже играет - оно продолжит играть
        // Блокируем только если пытаемся запустить тот же аудио файл в отдельном слоте
        
        // Если есть аудио в триггере и оно уже играет - не блокируем (продолжит играть)
        if (audioSlot != null && IsMediaFileAlreadyPlaying(audioSlot.MediaPath))
        {
            // Проверяем, играет ли это аудио в триггере или в отдельном слоте
            var currentAudioType = GetCurrentMediaType();
            if (currentAudioType == MediaType.Audio)
            {
                // Если аудио играет в триггере - не блокируем
                // Если аудио играет в отдельном слоте - блокируем
                return false; // Для триггеров всегда разрешаем
            }
        }
        
        return false;
    }

    /// <summary>
    /// Умная остановка триггеров - не останавливает аудио, которое должно продолжить играть
    /// </summary>
    // ЗАКОММЕНТИРОВАНО - триггеры отключены
    /*
    private void SmartStopTriggers(int newColumn, MediaSlot? audioSlot)
    {
        // Проверяем, играет ли аудио в отдельном слоте ИЛИ в триггере и будет ли оно использоваться в триггере
        bool audioWillBeReused = audioSlot != null && 
                                IsMediaFileAlreadyPlaying(audioSlot.MediaPath) && 
                                _currentAudioContent != null && 
                                (_currentAudioContent.StartsWith("Slot_") || _currentAudioContent.StartsWith("Trigger_"));
        
        if (!audioWillBeReused)
        {
            // Останавливаем все активное аудио только если оно не будет переиспользовано
            System.Diagnostics.Debug.WriteLine($"SmartStopTriggers: Останавливаем все активное аудио");
            StopActiveAudio();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"SmartStopTriggers: НЕ ТРОГАЕМ аудио - оно будет переиспользовано");
        }
        
        var activeColumns = _triggerManager.GetActiveTriggers().Where(col => col != newColumn).ToList();
        foreach (var activeColumn in activeColumns)
        {
            var otherTriggerButton = FindTriggerButton(activeColumn.Key);
            if (otherTriggerButton != null)
            {
                // Если новый триггер содержит то же аудио, что и текущий - не останавливаем аудио
                if (audioSlot != null && IsMediaFileAlreadyPlaying(audioSlot.MediaPath))
                {
                    // Останавливаем только визуальную часть триггера
                    SmartStopTriggerVisual(activeColumn.Key, otherTriggerButton);
                }
                else
                {
                    // Останавливаем весь триггер
                    // ЗАКОММЕНТИРОВАНО - триггеры отключены
                    // StopParallelMedia(activeColumn.Key, otherTriggerButton);
                }
            }
        }
    }

    /// <summary>
    /// Останавливает только визуальную часть триггера, оставляя аудио играть
    /// </summary>
    private void SmartStopTriggerVisual(int column, Button triggerButton)
    {
        string triggerKey = $"Trigger_{column}";
        
        // Останавливаем только визуальную часть
        if (_currentMainMedia == triggerKey)
        {
            // Сохраняем позицию и отменяем регистрацию файла
            if (mediaElement.Source != null)
            {
                _mediaResumePositions[mediaElement.Source.LocalPath] = mediaElement.Position;
                UnregisterActiveMediaFile(mediaElement.Source.LocalPath);
            }
            
            mediaElement.Stop();
            mediaElement.Source = null;
            _currentMainMedia = null;
            
            // Восстанавливаем MediaElement если был заменен на Image
            if (mediaBorder.Child != mediaElement)
            {
                RestoreMediaElement(mediaElement);
                mediaElement.Visibility = Visibility.Visible;
            }
        }
        
        // Очищаем визуальный контент
        if (_currentVisualContent == triggerKey)
        {
            _currentVisualContent = null;
        }
        
        // НЕ останавливаем аудио - оно продолжит играть
        
        // Сбрасываем состояние
        SetTriggerState(column, TriggerState.Stopped);
        if (_activeTriggerColumn == column)
        {
            _activeTriggerColumn = null;
        }
        triggerButton.Content = "▶";
        triggerButton.Background = Brushes.Orange;
        
        // Возвращаем кнопку
        triggerButton.Visibility = Visibility.Visible;
        
        // Останавливаем таймеры если нет активного медиа
        if (_currentMainMedia == null)
        {
            isVideoPlaying = false;
        }
        if (_currentAudioContent == null)
        {
            isAudioPlaying = false;
        }
    }


    // ЗАКОММЕНТИРОВАНО - триггеры отключены
    /*
    private void ClearAllSlots()
    {
        // Останавливаем все активные воспроизведения
        foreach (var columnState in _triggerManager.GetActiveTriggers())
        {
            var triggerButton = FindTriggerButton(columnState);
            if (triggerButton != null)
            {
                // ЗАКОММЕНТИРОВАНО - триггеры отключены
                // StopParallelMedia(columnState, triggerButton);
            }
        }
        
        foreach (var child in BottomPanel.Children.OfType<Grid>())
        {
            foreach (var button in child.Children.OfType<Button>())
            {
                // Проверяем, является ли кнопка триггером
                if (button.Tag?.ToString()?.StartsWith("Trigger_") == true)
                {
                    // Для триггеров сбрасываем состояние
                    button.Content = "▶";
                    button.Background = Brushes.Orange;
                }
                else
                {
                    // Для обычных слотов сбрасываем содержимое и цвет
                    button.Content = "Пусто";
                    button.Background = Brushes.LightGray;
                }
            }
        }
    }
    */

    private Button? FindTriggerButton(int column)
    {
        foreach (var child in BottomPanel.Children.OfType<Grid>())
        {
            int gridColumn = Grid.GetColumn(child);
            if (gridColumn == column - 1) // Индексы начинаются с 0
            {
                foreach (var button in child.Children.OfType<Button>())
                {
                    if (button.Tag?.ToString() == $"Trigger_{column}")
                    {
                        return button;
                    }
                }
            }
        }
        return null;
    }

    // ЗАКОММЕНТИРОВАНО - триггеры отключены
    /*
    private void StopActiveTriggersVisualOnly()
    {
        // Останавливаем только визуальную часть триггеров, сохраняя аудио
        var activeColumns = _triggerManager.GetActiveTriggers();
        foreach (var activeColumn in activeColumns)
        {
            var triggerButton = FindTriggerButton(activeColumn.Key);
            if (triggerButton != null)
            {
                // Останавливаем только визуальную часть, сохраняя аудио
                SmartStopTriggerVisual(activeColumn.Key, triggerButton);
            }
        }
    }
    */

    // ЗАКОММЕНТИРОВАНО - триггеры отключены
    /*
    private void StopActiveTriggersAndVisual()
    {
        // Останавливаем все триггеры
        var activeColumns = _triggerManager.GetActiveTriggers();
        foreach (var activeColumn in activeColumns)
        {
            var triggerButton = FindTriggerButton(activeColumn.Key);
            if (triggerButton != null)
            {
                StopParallelMedia(activeColumn.Key, triggerButton);
            }
        }
        
        // Очищаем визуальный контент только если он не аудио
        if (_currentVisualContent != null)
        {
            // Сохраняем позицию перед очисткой
            if (mediaElement.Source != null && _currentMainMedia != null)
            {
                SaveSlotPosition(_currentMainMedia, mediaElement.Position);
            }
            
            if (mediaBorder.Child != mediaElement)
            {
                RestoreMediaElement(mediaElement);
                mediaElement.Visibility = Visibility.Visible;
            }
            
            // Отменяем регистрацию файла
            if (mediaElement.Source != null)
            {
                UnregisterActiveMediaFile(mediaElement.Source.LocalPath);
            }
            
            // Сохраняем позицию перед остановкой
            if (mediaElement.Source != null)
            {
                _mediaResumePositions[mediaElement.Source.LocalPath] = mediaElement.Position;
            }
            mediaElement.Stop();
            mediaElement.Source = null;
            _currentVisualContent = null;
            _currentMainMedia = null;
            isVideoPlaying = false;
        }
    }
    */

    private void StopActiveAudio()
    {
        // Останавливаем все активные аудио слоты
        foreach (var audioSlot in _activeAudioSlots.ToList())
        {
            // Сохраняем позицию слота перед остановкой
            SaveSlotPosition(audioSlot.Key, audioSlot.Value.Position);
            
            // Отменяем регистрацию файла
            if (audioSlot.Value.Source != null)
            {
                UnregisterActiveMediaFile(audioSlot.Value.Source.LocalPath);
            }
            
            audioSlot.Value.Stop();
            audioSlot.Value.Source = null;
        }
        _activeAudioSlots.Clear();
        
        // Удаляем контейнеры
        foreach (var container in _activeAudioContainers.ToList())
        {
            ((Grid)Content).Children.Remove(container.Value);
        }
        _activeAudioContainers.Clear();
        
        _currentAudioContent = null;
        isAudioPlaying = false;
        
        // Обновляем подсветку кнопок
        UpdateAllSlotButtonsHighlighting();
    }

    private void LoadProjectSlots()
    {
        foreach (var slot in _projectManager.CurrentProject.MediaSlots)
        {
            UpdateSlotButton(slot.Column, slot.Row, slot.MediaPath, slot.Type);
        }
    }
    
    /// <summary>
    /// Очищает все слоты, удаляя иконки и сбрасывая их состояние
    /// </summary>
    private void ClearAllSlots()
    {
        var bottomPanel = BottomPanel;
        if (bottomPanel == null) return;
        
        // Проходим по всем колонкам
        foreach (var child in bottomPanel.Children)
        {
            if (child is Grid columnGrid)
            {
                int gridColumn = Grid.GetColumn(columnGrid);
                int column = gridColumn + 1; // Индексы начинаются с 1
                
                // Проходим по всем кнопкам в колонке
                foreach (var button in columnGrid.Children.OfType<Button>())
                {
                    int buttonRow = Grid.GetRow(button);
                    int row = buttonRow + 1; // Индексы начинаются с 1
                    
                    // Пропускаем триггеры (третья строка, индекс 2)
                    if (buttonRow == 2) continue;
                    
                    // Очищаем слот
                    UpdateSlotButton(column, row, "", MediaType.Video);
                }
            }
        }
    }


    // ЗАКОММЕНТИРОВАНО - триггеры отключены
    /*
    private void Trigger_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            string? tag = btn.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            // Парсим номер колонки из тега (например, "Trigger_3" -> 3)
            string columnStr = tag.Replace("Trigger_", "");
            if (int.TryParse(columnStr, out int column))
            {
                var currentState = GetTriggerState(column);
                
                // ПРОСТО: убираем кнопку, когда триггер играет
                if (_activeTriggerColumn == column && _currentMainMedia == $"Trigger_{column}")
                {
                    // Играет - кнопка уже скрыта, пошел нахуй
                    return;
                }
                
                // Не играет - велком, запускаем (НЕ скрываем кнопку триггера)
                StartParallelMedia(column, btn);
            }
        }
    }
    */

    // ЗАКОММЕНТИРОВАНО - триггеры отключены
    /*
    private void StartParallelMedia(int column, Button triggerButton)
    {
        // Сразу устанавливаем активный триггер
        _activeTriggerColumn = column;
        _currentMainMedia = $"Trigger_{column}";
        
        // Получаем медиа из первой и второй строки этой колонки
        var slot1 = _projectManager.GetMediaSlot(column, 1);
        var slot2 = _projectManager.GetMediaSlot(column, 2);

        if (slot1 == null && slot2 == null)
        {
            return;
        }

        // Определяем, что воспроизводить
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


        // Умная остановка триггеров - не останавливаем аудио, которое должно продолжить играть
        SmartStopTriggers(column, audioSlot);
        
        // Также останавливаем основной медиаплеер если он используется не этим триггером
        if (_currentMainMedia != null && !_currentMainMedia.StartsWith($"Trigger_{column}"))
        {
            mediaElement.Stop();
            mediaElement.Source = null;
            _currentMainMedia = null;
        }

        // Воспроизводим комбинацию
        if (videoSlot != null && audioSlot != null)
        {
            StartVideoWithAudio(column, videoSlot, audioSlot, triggerButton);
        }
        else if (imageSlot != null && audioSlot != null)
        {
            StartImageWithAudio(column, imageSlot, audioSlot, triggerButton);
        }
        else if (videoSlot != null)
        {
            StartSingleMedia(column, videoSlot, triggerButton);
        }
        else if (imageSlot != null)
        {
            StartSingleMedia(column, imageSlot, triggerButton);
        }
        else if (audioSlot != null)
        {
            StartSingleMedia(column, audioSlot, triggerButton);
        }
    }

    private void PauseParallelMedia(int column, Button triggerButton)
    {
        string triggerKey = $"Trigger_{column}";
        
        // Приостанавливаем основной плеер и сохраняем позицию слота
        if (_currentMainMedia == triggerKey)
        {
            SaveSlotPosition(triggerKey, mediaElement.Position);
            mediaElement.Pause();
        }
        
        // Приостанавливаем аудио, если есть
        if (_activeAudioElements.ContainsKey(column))
        {
            SaveSlotPosition(triggerKey, _activeAudioElements[column].Position);
            _activeAudioElements[column].Pause();
        }
        
        // Обновляем состояние
        SetTriggerState(column, TriggerState.Paused);
        triggerButton.Content = "⏸";
        triggerButton.Background = Brushes.Yellow;
        
        // Останавливаем таймеры при паузе
        isVideoPlaying = false;
        isAudioPlaying = false;
    }

    private void ResumeParallelMedia(int column, Button triggerButton)
    {
        string triggerKey = $"Trigger_{column}";
        
        // Возобновляем основной плеер с позиции слота
        if (_currentMainMedia == triggerKey)
        {
            var slotPosition = GetSlotPosition(triggerKey);
            if (slotPosition > TimeSpan.Zero)
            {
                mediaElement.Position = slotPosition;
            }
            mediaElement.Play();
        }
        
        // Возобновляем аудио, если есть
        if (_activeAudioElements.ContainsKey(column))
        {
            var ae = _activeAudioElements[column];
            var audioSlotPosition = GetSlotPosition(triggerKey);
            if (audioSlotPosition > TimeSpan.Zero)
            {
                ae.Position = audioSlotPosition;
            }
            ae.Play();
        }
        
        // Обновляем состояние
        SetTriggerState(column, TriggerState.Playing);
        _activeTriggerColumn = column;
        _lastUsedTriggerColumn = column;
        triggerButton.Content = "⏹";
        triggerButton.Background = Brushes.Red;
        
        // Обновляем подсветку всех кнопок
        UpdateAllSlotButtonsHighlighting();
        
        // Запускаем таймеры при возобновлении
        isVideoPlaying = true;
        isAudioPlaying = true;
    }

    private void StopParallelMedia(int column, Button triggerButton)
    {
        string triggerKey = $"Trigger_{column}";
        
        // Останавливаем основной плеер только если он принадлежит этому триггеру
        if (_currentMainMedia == triggerKey)
        {
            // Сохраняем позицию слота перед остановкой
            SaveSlotPosition(triggerKey, mediaElement.Position);
            
            // Отменяем регистрацию файла
            if (mediaElement.Source != null)
            {
                UnregisterActiveMediaFile(mediaElement.Source.LocalPath);
            }
            
            mediaElement.Stop();
            mediaElement.Source = null;
            _currentMainMedia = null;
            
            // Восстанавливаем MediaElement если был заменен на Image
            if (mediaBorder.Child != mediaElement)
            {
                RestoreMediaElement(mediaElement);
                mediaElement.Visibility = Visibility.Visible;
            }
        }
        
        // Очищаем визуальный контент если он принадлежит этому триггеру
        if (_currentVisualContent == triggerKey)
        {
            _currentVisualContent = null;
        }
        
        // Останавливаем аудио этого триггера
        if (_activeAudioSlots.TryGetValue(triggerKey, out var triggerAudioElement) && triggerAudioElement != null)
        {
            // Сохраняем позицию и отменяем регистрацию файла
            if (triggerAudioElement.Source != null)
            {
                _mediaResumePositions[triggerAudioElement.Source.LocalPath] = triggerAudioElement.Position;
                UnregisterActiveMediaFile(triggerAudioElement.Source.LocalPath);
            }
            
            triggerAudioElement.Stop();
            triggerAudioElement.Source = null;
            _mediaStateService.RemoveAudioSlot(triggerKey);
        }
        
        if (_activeAudioContainers.ContainsKey(triggerKey))
        {
            ((Grid)Content).Children.Remove(_activeAudioContainers[triggerKey]);
            _activeAudioContainers.Remove(triggerKey);
        }
        
        if (_currentAudioContent == triggerKey)
        {
            _currentAudioContent = null;
        }
        
        // Останавливаем и удаляем старые элементы (для совместимости)
        if (_activeAudioElements.ContainsKey(column))
        {
            // Сохраняем позицию и отменяем регистрацию файла
            if (_activeAudioElements[column].Source != null)
            {
                _mediaResumePositions[_activeAudioElements[column].Source.LocalPath] = _activeAudioElements[column].Position;
                UnregisterActiveMediaFile(_activeAudioElements[column].Source.LocalPath);
            }
            
            _activeAudioElements[column].Stop();
            _activeAudioElements[column].Source = null;
            _activeAudioElements.Remove(column);
        }
        
        if (_tempContainers.ContainsKey(column))
        {
            ((Grid)Content).Children.Remove(_tempContainers[column]);
            _tempContainers.Remove(column);
        }
        
        // Сбрасываем состояние
        SetTriggerState(column, TriggerState.Stopped);
        if (_activeTriggerColumn == column)
        {
            _activeTriggerColumn = null;
        }
        triggerButton.Content = "▶";
        triggerButton.Background = Brushes.Orange;
        
        // Возвращаем кнопку
        triggerButton.Visibility = Visibility.Visible;
        
        // Останавливаем таймеры если нет активного медиа
        if (_currentMainMedia == null)
        {
            isVideoPlaying = false;
        }
        if (_currentAudioContent == null)
        {
            isAudioPlaying = false;
        }
    }
    */

    private Border? FindMediaBorder()
    {
        // Прямое обращение к Border по имени
        return mediaBorder;
    }



    private void StartVideoWithAudio(int column, MediaSlot videoSlot, MediaSlot audioSlot, Button triggerButton)
    {
        _triggerPlaybackService.StartVideoWithAudio(column, videoSlot, audioSlot);
    }

    private void StartImageWithAudio(int column, MediaSlot imageSlot, MediaSlot audioSlot, Button triggerButton)
    {
        _triggerPlaybackService.StartImageWithAudio(column, imageSlot, audioSlot);
    }

    private void StartSingleMedia(int column, MediaSlot mediaSlot, Button triggerButton)
    {
        _triggerPlaybackService.StartSingleMedia(column, mediaSlot);
    }

    /// <summary>
    /// Создает контекстное меню для кнопок слотов и триггеров
    /// </summary>
    private ContextMenu CreateContextMenu(Button button)
    {
        return _contextMenuService.CreateContextMenu(button);
    }

    /// <summary>
    /// Определяет текст для пункта "Пауза/Продолжить" в зависимости от текущего состояния
    /// </summary>
    private string GetPauseMenuItemText(string? tag)
    {
        // Метод оставлен для обратной совместимости, но логика теперь в ContextMenuService
        if (string.IsNullOrEmpty(tag)) return "Пауза";

        if (tag.StartsWith("Slot_"))
        {
            var parts = tag.Split('_');
            if (parts.Length >= 3 && int.TryParse(parts[1], out int column) && int.TryParse(parts[2], out int row))
            {
                var slotKey = $"Slot_{column}_{row}";
                
                // Проверяем состояние основного медиа
                if (_currentMainMedia == slotKey && _isVideoPaused)
                {
                    return "Продолжить";
                }
                
                // Проверяем состояние аудио
                if (_currentAudioContent == slotKey && _audioPausedStates.ContainsKey(slotKey) && _audioPausedStates[slotKey])
                {
                    return "Продолжить";
                }
            }
        }
        else if (tag.StartsWith("Trigger_"))
        {
            var parts = tag.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int column))
            {
                if (_activeTriggerColumn == column && GetTriggerState(column) == TriggerState.Paused)
                {
                    return "Продолжить";
                }
            }
        }

        return "Пауза";
    }

    /// <summary>
    /// Обработчик для пункта "Заново" в контекстном меню
    /// </summary>
    private void RestartItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string tag)
        {
            if (tag.StartsWith("Slot_"))
            {
                // Для слотов - перезапуск воспроизведения
                var parts = tag.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int column) && int.TryParse(parts[2], out int row))
                {
                    var slot = _projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => s.Column == column && s.Row == row);
                    if (slot != null)
                    {
                        // Проверяем, воспроизводится ли этот слот сейчас
                        var slotKey = $"Slot_{column}_{row}";
                        bool isMainMedia = _currentMainMedia == slotKey;
                        bool isAudioMedia = _currentAudioContent == slotKey && _activeAudioSlots.ContainsKey(slotKey);
                        
                        if (isMainMedia || isAudioMedia)
                        {
                            // Если это текущий слот - перезапускаем с самого начала
                            if (isMainMedia)
                            {
                                mediaElement.Stop(); // Полная остановка
                                mediaElement.Position = TimeSpan.Zero; // Сброс позиции
                                _isVideoPaused = false;
                                
                                // Синхронизируем перезапуск со вторым экраном
                                if (_secondaryMediaElement != null && _secondaryMediaElement.Source != null &&
                                    mediaElement.Source != null &&
                                    mediaElement.Source.LocalPath == _secondaryMediaElement.Source.LocalPath)
                                {
                                    try
                                    {
                                        _secondaryMediaElement.Stop();
                                        _secondaryMediaElement.Position = TimeSpan.Zero;
                                        System.Diagnostics.Debug.WriteLine("ПЕРЕЗАПУСК: Синхронизирован со вторым экраном");
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Ошибка при перезапуске на втором экране: {ex.Message}");
                                    }
                                }
                                
                                // Скрываем кнопку триггера при возобновлении
                                // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                /*
                                var triggerButton = FindTriggerButton(column);
                                if (triggerButton != null)
                                {
                                    triggerButton.Visibility = Visibility.Hidden;
                                }
                                */ // Сбрасываем состояние паузы
                                mediaElement.Play(); // Запуск с начала
                                
                                // Синхронизируем запуск со вторым экраном
                                if (_secondaryMediaElement != null && _secondaryMediaElement.Source != null)
                                {
                                    try
                                    {
                                        _secondaryMediaElement.Play();
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Ошибка при запуске на втором экране: {ex.Message}");
                                    }
                                }
                            }
                            
                            if (isAudioMedia)
                            {
                                var audioElement = _activeAudioSlots[slotKey];
                                audioElement.Stop(); // Полная остановка
                                audioElement.Position = TimeSpan.Zero; // Сброс позиции
                                _audioPausedStates[slotKey] = false;
                                
                                // Скрываем кнопку триггера при возобновлении
                                // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                /*
                                var triggerButton = FindTriggerButton(column);
                                if (triggerButton != null)
                                {
                                    triggerButton.Visibility = Visibility.Hidden;
                                }
                                */ // Сбрасываем состояние паузы
                                audioElement.Play(); // Запуск с начала
                            }
                        }
                        else
                        {
                            // Если это не текущий слот - просто запускаем
                            var button = FindButtonByTag(tag);
                            if (button != null)
                            {
                                Slot_Click(button, new RoutedEventArgs());
                            }
                        }
                    }
                }
            }
            else if (tag.StartsWith("Trigger_"))
            {
                // Для триггеров - перезапуск параллельного воспроизведения
                var parts = tag.Split('_');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int column))
                {
                    var triggerButton = FindButtonByTag(tag);
                    if (triggerButton != null)
                    {
                        // Проверяем, активен ли этот триггер сейчас
                        bool isCurrentlyActive = _activeTriggerColumn == column;
                        
                        if (isCurrentlyActive)
                        {
                            // Если это активный триггер - перезапускаем с самого начала
                            // ЗАКОММЕНТИРОВАНО - триггеры отключены
                // StopParallelMedia(column, triggerButton);
                            
                            // Сбрасываем позиции для этого триггера
                            var triggerKey = $"Trigger_{column}";
                            _slotPositions[triggerKey] = TimeSpan.Zero;
                            
                            // ЗАКОММЕНТИРОВАНО - триггеры отключены
                            // Trigger_Click(triggerButton, new RoutedEventArgs());
                        }
                        else
                        {
                            // Если это не активный триггер - просто запускаем
                            // ЗАКОММЕНТИРОВАНО - триггеры отключены
                            // Trigger_Click(triggerButton, new RoutedEventArgs());
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Обработчик для пункта "Пауза" в контекстном меню
    /// </summary>
    private void PauseItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string tag)
        {
            if (tag.StartsWith("Slot_"))
            {
                // Для слотов - пауза/возобновление
                var parts = tag.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int column) && int.TryParse(parts[2], out int row))
                {
                    var slotKey = $"Slot_{column}_{row}";
                    
                    // Проверяем, воспроизводится ли этот слот сейчас
                    bool isMainMedia = _currentMainMedia == slotKey && mediaElement.Source != null;
                    bool isAudioMedia = _currentAudioContent == slotKey && _activeAudioSlots.ContainsKey(slotKey);
                    
                    if (isMainMedia || isAudioMedia)
                    {
                        // Проверяем состояние воспроизведения
                        if (isMainMedia)
                        {
                            // Управляем основным медиа (видео/изображение)
                            if (_isVideoPaused)
                            {
                                // Видео на паузе - возобновляем
                                mediaElement.Play();
                                SyncPlayWithSecondaryScreen();
                                _isVideoPaused = false;
                                
                                // Скрываем кнопку триггера при возобновлении
                                // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                /*
                                var triggerButton = FindTriggerButton(column);
                                if (triggerButton != null)
                                {
                                    triggerButton.Visibility = Visibility.Hidden;
                                }
                                */
                            }
                            else
                            {
                                // Видео воспроизводится - ставим на паузу
                                mediaElement.Pause();
                                SyncPauseWithSecondaryScreen();
                                _isVideoPaused = true;
                                
                                // Возвращаем кнопку триггера при паузе
                                // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                /*
                                var triggerButton = FindTriggerButton(column);
                                if (triggerButton != null)
                                {
                                    triggerButton.Visibility = Visibility.Visible;
                                }
                                */
                            }
                        }
                        
                        if (isAudioMedia)
                        {
                            // Управляем аудио - безопасно получаем элемент
                            if (_mediaStateService.TryGetAudioSlot(slotKey, out var audioElement) && audioElement != null)
                            {
                                bool isAudioPaused = _mediaStateService.IsAudioPaused(slotKey);
                                
                                if (isAudioPaused)
                                {
                                    // Аудио на паузе - возобновляем
                                    audioElement.Play();
                                    _mediaStateService.SetAudioPaused(slotKey, false);
                                    
                                    // Скрываем кнопку триггера при возобновлении
                                    // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                    /*
                                    var triggerButton = FindTriggerButton(column);
                                    if (triggerButton != null)
                                    {
                                        triggerButton.Visibility = Visibility.Hidden;
                                    }
                                    */
                                }
                                else
                                {
                                    // Аудио воспроизводится - ставим на паузу
                                    audioElement.Pause();
                                    _mediaStateService.SetAudioPaused(slotKey, true);
                                    
                                    // Возвращаем кнопку триггера при паузе
                                    // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                    /*
                                    var triggerButton = FindTriggerButton(column);
                                    if (triggerButton != null)
                                    {
                                        triggerButton.Visibility = Visibility.Visible;
                                    }
                                    */
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"ОШИБКА: Аудио элемент не найден для слота {slotKey} в PauseItem_Click");
                            }
                        }
                    }
                    else
                    {
                        // Если этот слот не воспроизводится сейчас, запускаем его
                        var button = FindButtonByTag(tag);
                        if (button != null)
                        {
                            Slot_Click(button, new RoutedEventArgs());
                        }
                    }
                }
            }
            else if (tag.StartsWith("Trigger_"))
            {
                // Для триггеров - пауза/возобновление параллельного воспроизведения
                var parts = tag.Split('_');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int column))
                {
                    var triggerButton = FindButtonByTag(tag);
                    if (triggerButton != null)
                    {
                        // Проверяем, активен ли этот триггер сейчас
                        if (_activeTriggerColumn == column)
                        {
                            var state = GetTriggerState(column);
                            if (state == TriggerState.Playing)
                            {
                                // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                // PauseParallelMedia(column, triggerButton);
                            }
                            else if (state == TriggerState.Paused)
                            {
                                // ЗАКОММЕНТИРОВАНО - триггеры отключены
                                // ResumeParallelMedia(column, triggerButton);
                            }
                        }
                        else
                        {
                            // Если этот триггер не активен, запускаем его
                            // ЗАКОММЕНТИРОВАНО - триггеры отключены
                            // Trigger_Click(triggerButton, new RoutedEventArgs());
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Обработчик клика по пункту "Настройки" в контекстном меню
    /// </summary>
    private void SettingsItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string slotKey)
        {
            // Находим слот в проекте
            var parts = slotKey.Split('_');
            if (parts.Length >= 3 && int.TryParse(parts[1], out int column) && int.TryParse(parts[2], out int row))
            {
                var mediaSlot = _projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => s.Column == column && s.Row == row);
                if (mediaSlot != null)
                {
                    // Выбираем элемент для настройки
                    SelectElementForSettings(mediaSlot, slotKey);
                }
            }
        }
    }

    /// <summary>
    /// Обработчик для пункта "Удалить" в контекстном меню
    /// </summary>
    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string tag)
        {
            System.Diagnostics.Debug.WriteLine($"ПОПЫТКА УДАЛЕНИЯ: Тег = {tag}");
            
            var result = MessageBox.Show("Вы уверены, что хотите удалить этот элемент?", "Подтверждение удаления", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                if (tag.StartsWith("Slot_"))
                {
                    // Удаляем слот из проекта
                    var parts = tag.Split('_');
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int column) && int.TryParse(parts[2], out int row))
                    {
                        System.Diagnostics.Debug.WriteLine($"УДАЛЕНИЕ СЛОТА: Колонка = {column}, Строка = {row}");
                        
                        var slot = _projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => s.Column == column && s.Row == row);
                        if (slot != null)
                        {
                            _projectManager.CurrentProject.MediaSlots.Remove(slot);
                            UpdateSlotButton(column, row, "", MediaType.Video); // Очищаем слот
                            System.Diagnostics.Debug.WriteLine($"СЛОТ УДАЛЕН: {column}_{row}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"СЛОТ НЕ НАЙДЕН: {column}_{row}");
                        }
                    }
                }
                else if (tag.StartsWith("Trigger_"))
                {
                    // Останавливаем триггер и очищаем его
                    var parts = tag.Split('_');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int column))
                    {
                        var triggerButton = FindButtonByTag(tag);
                        if (triggerButton != null)
                        {
                            // ЗАКОММЕНТИРОВАНО - триггеры отключены
                // StopParallelMedia(column, triggerButton);
                            // Сбрасываем состояние триггера
                            SetTriggerState(column, TriggerState.Stopped);
                            triggerButton.Content = "▶";
                            triggerButton.Background = Brushes.Orange;
        
        // Возвращаем кнопку
        triggerButton.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Находит кнопку по тегу
    /// </summary>
    private Button? FindButtonByTag(string tag)
    {
        return FindVisualChild<Button>(BottomPanel, b => b.Tag?.ToString() == tag);
    }

    /// <summary>
    /// Рекурсивно ищет визуальный элемент по условию
    /// </summary>
    private T? FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t && predicate(t))
                return t;
            
            var childOfChild = FindVisualChild<T>(child, predicate);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }
    
    #region Slider Event Handlers
    
    
    /// <summary>
    /// Обработчик изменения значения слайдера видео
    /// </summary>
    private void VideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // НЕ ТРОГАЕМ ВИДЕО во время перетаскивания - это создает кашу в звуке и покадровую съемку!
        // Позиция будет установлена только при отпускании слайдера
    }

    private void VideoSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _timerService.IsVideoSliderDragging = true;
    }
    
    private void VideoSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _timerService.IsVideoSliderDragging = false;
        
        // Устанавливаем позицию при отпускании слайдера
        if (_videoTotalDuration.TotalSeconds > 0)
        {
            var newPosition = TimeSpan.FromSeconds((videoSlider.Value / 100.0) * _videoTotalDuration.TotalSeconds);
            
            if (mediaElement.NaturalDuration.HasTimeSpan && mediaElement.NaturalDuration.TimeSpan.TotalSeconds > 0)
            {
                mediaElement.Position = newPosition;
                
                // Синхронизируем позицию со вторым экраном
                if (_secondaryMediaElement != null && _secondaryMediaElement.Source != null)
                {
                    try
                    {
                        _secondaryMediaElement.Position = newPosition;
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
    /// Обработчик начала перетаскивания слайдера аудио
    /// </summary>
    private void AudioSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isAudioSliderDragging = true;
    }
    
    /// <summary>
    /// Обработчик окончания перетаскивания слайдера аудио
    /// </summary>
    private void AudioSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isAudioSliderDragging = false;
    }
    
    /// <summary>
    /// Обработчик изменения значения слайдера аудио
    /// </summary>
    private void AudioSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_timerService.IsAudioSliderDragging && 
            _mediaStateService.CurrentAudioContent != null && 
            _mediaStateService.TryGetAudioSlot(_mediaStateService.CurrentAudioContent, out var audioElement))
        {
            var newPosition = TimeSpan.FromSeconds((e.NewValue / 100.0) * _audioTotalDuration.TotalSeconds);
            
            // Проверяем, что аудио загружено и готово к воспроизведению
            if (audioElement != null && audioElement.NaturalDuration.HasTimeSpan && audioElement.NaturalDuration.TimeSpan.TotalSeconds > 0)
            {
                // Устанавливаем позицию немедленно без задержки для мгновенной перемотки
                audioElement.Position = newPosition;
            }
        }
    }
    
    #endregion
}


// Управление панелью настроек элемента
public partial class MainWindow
{
    private MediaSlot? _selectedElementSlot = null;
    private string? _selectedElementKey = null; // Ключ выбранного элемента ("Slot_x_y" или "Trigger_x")
    
    // Обработчики меню File
    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        // Останавливаем текущее воспроизведение медиа
        _mediaControlService.StopMedia();
        _mediaControlService.CloseMedia();
        StopActiveAudio();
        
        // Очищаем состояние медиа
        _currentMainMedia = null;
        _currentAudioContent = null;
        _currentVisualContent = null;
        isVideoPlaying = false;
        isAudioPlaying = false;
        
        // Очищаем медиа элементы
        _videoDisplayService.ClearMediaElements();
        
        // Создаем новый проект
        _projectManager.NewProject();
        
        // Очищаем все слоты
        ClearAllSlots();
        
        // Обновляем подсветку кнопок
        UpdateAllSlotButtonsHighlighting();
        
        // Загружаем позиции панелей по умолчанию
        LoadPanelPositions();
        
        MessageBox.Show("Новый проект создан", "Информация");
    }

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        if (_projectManager.OpenProject())
        {
            LoadProjectSlots();
            LoadGlobalSettings();
            LoadPanelPositions(); // Загружаем сохраненные позиции панелей
            MessageBox.Show("Проект загружен", "Информация");
        }
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        SavePanelPositions(); // Сохраняем текущие позиции панелей
        if (_projectManager.SaveProject())
        {
            MessageBox.Show("Проект сохранен", "Информация");
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Сохраняем позиции панелей перед закрытием
        SavePanelPositions();
        
        // Закрываем окно вывода на второй монитор при закрытии приложения
        CloseSecondaryScreenWindow();
    }
    
    
    // Событие handlers для панели настроек элемента
    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_selectedElementSlot == null) return;
        
        _selectedElementSlot.PlaybackSpeed = SpeedSlider.Value;
        SpeedValueText.Text = $"Скорость: {SpeedSlider.Value:F1}x";
        
        // Применяем настройки только если слайдер не перетаскивается
        if (!SpeedSlider.IsMouseCaptured)
        {
            ApplyElementSettings();
        }
    }
    
    private void SpeedSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Применяем настройки после окончания перетаскивания
        if (_selectedElementSlot != null)
        {
            ApplyElementSettings();
        }
    }

    private void SpeedPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && double.TryParse(button.Tag.ToString(), out double speed))
        {
            SpeedSlider.Value = speed;
        }
    }
    
    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_selectedElementSlot == null) return;
        
        _selectedElementSlot.Opacity = OpacitySlider.Value;
        OpacityValueText.Text = $"Прозрачность: {(OpacitySlider.Value * 100):F0}%";
        ApplyElementSettings();
    }
    
    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_selectedElementSlot == null) return;
        
        _selectedElementSlot.Volume = VolumeSlider.Value;
        VolumeValueText.Text = $"Звук: {(VolumeSlider.Value * 100):F0}%";
        ApplyElementSettings();
    }
    
    private void VolumePreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && double.TryParse(button.Tag.ToString(), out double volume))
        {
            VolumeSlider.Value = volume;
        }
    }
    
    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_selectedElementSlot == null) return;
        
        _selectedElementSlot.Scale = ScaleSlider.Value;
        ScaleValueText.Text = $"Масштаб: {(ScaleSlider.Value * 100):F0}%";
        ApplyElementSettings();
    }
    
    private void ScalePreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && double.TryParse(button.Tag.ToString(), out double scale))
        {
            ScaleSlider.Value = scale;
        }
    }
    
    private void RotationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_selectedElementSlot == null) return;
        
        _selectedElementSlot.Rotation = RotationSlider.Value;
        RotationValueText.Text = $"Поворот: {RotationSlider.Value:F0}°";
        ApplyElementSettings();
    }
    
    private void RotationPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && double.TryParse(button.Tag.ToString(), out double rotation))
        {
            RotationSlider.Value = rotation;
        }
    }
    
    // Обработчики для настроек текста
    private void HideTextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        _selectedElementSlot.IsTextVisible = !_selectedElementSlot.IsTextVisible;
        
        // Обновляем кнопку
        if (_selectedElementSlot.IsTextVisible)
        {
            HideTextButton.Content = "👁️ Скрыть текст";
            HideTextButton.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Красный
        }
        else
        {
            HideTextButton.Content = "👁️ Показать текст";
            HideTextButton.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зеленый
        }
        
        // Применяем изменения к отображаемому тексту
        ApplyTextSettings();
    }
    
    private void TextColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        if (TextColorComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            _selectedElementSlot.FontColor = selectedItem.Tag?.ToString() ?? "White";
            ApplyTextSettings();
        }
    }
    
    private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        if (FontFamilyComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            _selectedElementSlot.FontFamily = selectedItem.Tag?.ToString() ?? "Arial";
            ApplyTextSettings();
        }
    }
    
    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        _selectedElementSlot.FontSize = FontSizeSlider.Value;
        FontSizeValueText.Text = $"{FontSizeSlider.Value:F0}px";
        ApplyTextSettings();
    }
    
    private void TextContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        _selectedElementSlot.TextContent = TextContentTextBox.Text;
        ApplyTextSettings();
    }
    
    private void UseManualPositionCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        _selectedElementSlot.UseManualPosition = true;
        ManualPositionPanel.Visibility = Visibility.Visible;
        ApplyTextSettings();
    }
    
    private void UseManualPositionCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        _selectedElementSlot.UseManualPosition = false;
        ManualPositionPanel.Visibility = Visibility.Collapsed;
        ApplyTextSettings();
    }
    
    private void TextXTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        if (double.TryParse(TextXTextBox.Text, out double x))
        {
            _selectedElementSlot.TextX = x;
            ApplyTextSettings();
        }
    }
    
    private void TextYTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        if (double.TryParse(TextYTextBox.Text, out double y))
        {
            _selectedElementSlot.TextY = y;
            ApplyTextSettings();
        }
    }
    
    // Применить настройки текста к отображаемому элементу
    private void ApplyTextSettings()
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        // Находим текстовый элемент в textOverlayGrid и обновляем его
        System.Diagnostics.Debug.WriteLine($"ApplyTextSettings: Ищем текстовый элемент в textOverlayGrid. Детей в Grid: {textOverlayGrid.Children.Count}");
        var textElement = textOverlayGrid.Children.OfType<TextBlock>().FirstOrDefault();
        if (textElement != null)
        {
            System.Diagnostics.Debug.WriteLine($"ApplyTextSettings: Найден текстовый элемент с текстом: '{textElement.Text}'");
            // Обновляем свойства текста
            textElement.Text = _selectedElementSlot.TextContent ?? "";
            textElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_selectedElementSlot.FontColor));
            textElement.FontFamily = new FontFamily(_selectedElementSlot.FontFamily);
            textElement.FontSize = _selectedElementSlot.FontSize;
            textElement.Opacity = 1.0; // Убираем прозрачность, всегда 100%
            
            // Применяем ручную настройку положения
            if (_selectedElementSlot.UseManualPosition)
            {
                textElement.Margin = new Thickness(_selectedElementSlot.TextX, _selectedElementSlot.TextY, 0, 0);
                textElement.HorizontalAlignment = HorizontalAlignment.Left;
                textElement.VerticalAlignment = VerticalAlignment.Top;
            }
            else
            {
                textElement.Margin = new Thickness(0);
                textElement.HorizontalAlignment = HorizontalAlignment.Center;
                textElement.VerticalAlignment = VerticalAlignment.Center;
            }
            
            // Управляем видимостью
            textElement.Visibility = _selectedElementSlot.IsTextVisible ? Visibility.Visible : Visibility.Hidden;
            
            // Обновляем видимость textOverlayGrid в зависимости от видимости текста
            textOverlayGrid.Visibility = _selectedElementSlot.IsTextVisible ? Visibility.Visible : Visibility.Hidden;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("ApplyTextSettings: Текстовый элемент НЕ найден в textOverlayGrid!");
            
            // Если текстовый элемент не найден, скрываем textOverlayGrid
            textOverlayGrid.Visibility = Visibility.Hidden;
        }
        
        // Также обновляем на втором экране если он активен
        if (_secondaryScreenWindow != null && _secondaryScreenWindow.Content is Grid secondaryGrid)
        {
            var secondaryTextElement = secondaryGrid.Children.OfType<TextBlock>().FirstOrDefault();
            if (secondaryTextElement != null)
            {
                secondaryTextElement.Text = _selectedElementSlot.TextContent ?? "";
                secondaryTextElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_selectedElementSlot.FontColor));
                secondaryTextElement.FontFamily = new FontFamily(_selectedElementSlot.FontFamily);
                secondaryTextElement.FontSize = _selectedElementSlot.FontSize;
                secondaryTextElement.Opacity = 1.0; // Убираем прозрачность, всегда 100%
                
                if (_selectedElementSlot.UseManualPosition)
                {
                    secondaryTextElement.Margin = new Thickness(_selectedElementSlot.TextX, _selectedElementSlot.TextY, 0, 0);
                    secondaryTextElement.HorizontalAlignment = HorizontalAlignment.Left;
                    secondaryTextElement.VerticalAlignment = VerticalAlignment.Top;
                }
                else
                {
                    secondaryTextElement.Margin = new Thickness(0);
                    secondaryTextElement.HorizontalAlignment = HorizontalAlignment.Center;
                    secondaryTextElement.VerticalAlignment = VerticalAlignment.Center;
                }
                
                secondaryTextElement.Visibility = _selectedElementSlot.IsTextVisible ? Visibility.Visible : Visibility.Hidden;
            }
        }
    }
    
    public void ElementPlay_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedElementSlot == null || string.IsNullOrEmpty(_selectedElementKey)) return;
        
        // Проверяем, играет ли уже этот элемент
        bool isCurrentlyPlaying = false;
        bool isCurrentlyPaused = false;
        
        if (_selectedElementSlot.Type == MediaType.Video || _selectedElementSlot.Type == MediaType.Image)
        {
            // Для видео/изображения проверяем основной плеер
            isCurrentlyPlaying = _currentMainMedia == _selectedElementKey && mediaElement.Source != null;
            isCurrentlyPaused = isCurrentlyPlaying && !isVideoPlaying;
        }
        else if (_selectedElementSlot.Type == MediaType.Audio)
        {
            // Для аудио проверяем активные аудио слоты
            if (_activeAudioSlots.TryGetValue(_selectedElementKey, out MediaElement? audioElement))
            {
                isCurrentlyPlaying = _currentAudioContent == _selectedElementKey;
                isCurrentlyPaused = isCurrentlyPlaying && !isAudioPlaying;
            }
        }
        else if (_selectedElementSlot.Type == MediaType.Text)
        {
            // Для текстовых элементов проверяем основной плеер
            isCurrentlyPlaying = _currentMainMedia == _selectedElementKey;
            isCurrentlyPaused = false; // Текстовые элементы не имеют состояния паузы
        }
        
        // Если элемент уже играет - ставим на паузу/возобновляем
        if (isCurrentlyPlaying)
        {
            if (isCurrentlyPaused)
            {
                // Возобновляем воспроизведение с сохраненной позиции
                if (_selectedElementSlot.Type == MediaType.Video || _selectedElementSlot.Type == MediaType.Image)
                {
                    // Восстанавливаем позицию из сохраненных позиций
                    if (mediaElement.Source != null && _mediaResumePositions.TryGetValue(mediaElement.Source.LocalPath, out var resume))
                    {
                        mediaElement.Position = resume;
                        // Синхронизируем позицию со вторым экраном
                        if (_secondaryMediaElement != null && _secondaryMediaElement.Source != null)
                        {
                            try
                            {
                                _secondaryMediaElement.Position = resume;
                            }
                            catch { }
                        }
                    }
                    mediaElement.Play();
                    SyncPlayWithSecondaryScreen();
                    isVideoPlaying = true;
                }
                else if (_selectedElementSlot.Type == MediaType.Audio && _activeAudioSlots.TryGetValue(_selectedElementKey, out MediaElement? audioElement))
                {
                    // Восстанавливаем позицию из сохраненных позиций
                    if (audioElement.Source != null && _mediaResumePositions.TryGetValue(audioElement.Source.LocalPath, out var audioResume))
                    {
                        audioElement.Position = audioResume;
                    }
                    audioElement.Play();
                    isAudioPlaying = true;
                }
                else if (_selectedElementSlot.Type == MediaType.Text)
                {
                    // Для текстовых элементов просто применяем настройки
                    ApplyTextSettings();
                }
                ElementPlayButton.Content = "⏸️";
                ElementPlayButton.ToolTip = "Пауза";
                UpdateAllSlotButtonsHighlighting();
            }
            else
            {
                // Ставим на паузу и сохраняем позицию
                if (_selectedElementSlot.Type == MediaType.Video || _selectedElementSlot.Type == MediaType.Image)
                {
                    // Сохраняем позицию перед паузой
                    if (mediaElement.Source != null)
                    {
                        _mediaResumePositions[mediaElement.Source.LocalPath] = mediaElement.Position;
                    }
                    mediaElement.Pause();
                    SyncPauseWithSecondaryScreen();
                    isVideoPlaying = false;
                }
                else if (_selectedElementSlot.Type == MediaType.Audio && _activeAudioSlots.TryGetValue(_selectedElementKey, out MediaElement? audioElement))
                {
                    // Сохраняем позицию перед паузой
                    if (audioElement.Source != null)
                    {
                        _mediaResumePositions[audioElement.Source.LocalPath] = audioElement.Position;
                    }
                    audioElement.Pause();
                    isAudioPlaying = false;
                }
                else if (_selectedElementSlot.Type == MediaType.Text)
                {
                    // Для текстовых элементов ничего не делаем при "паузе"
                    // Текст остается видимым
                }
                ElementPlayButton.Content = "▶️";
                ElementPlayButton.ToolTip = "Продолжить";
                UpdateAllSlotButtonsHighlighting();
            }
            return;
        }
        
        // Если элемент не играет - запускаем заново
        if (_selectedElementSlot.Type == MediaType.Video)
        {
            _videoDisplayService.LoadAndPlayVideo(_selectedElementSlot, _selectedElementKey);
        }
        else if (_selectedElementSlot.Type == MediaType.Image)
        {
            // Для изображения заменяем MediaElement на Image
            mediaElement.Stop();
            mediaElement.Source = null;
            
            // Создаем Image элемент
            var imageElement = new Image
            {
                Source = new BitmapImage(new Uri(_selectedElementSlot.MediaPath)),
                Stretch = Stretch.Uniform, // Изменено с UniformToFill на Uniform чтобы не обрезать
                Width = 600,
                Height = 400
            };
            
            // Обновляем содержимое Grid, сохраняя textOverlayGrid
            if (mediaBorder.Child is Grid mainGrid)
            {
                // Удаляем старые элементы
                var oldMediaElement = mainGrid.Children.OfType<MediaElement>().FirstOrDefault();
                if (oldMediaElement != null)
                {
                    mainGrid.Children.Remove(oldMediaElement);
                }
                var oldImages = mainGrid.Children.OfType<Image>().ToList();
                foreach (var oldImage in oldImages)
                {
                    mainGrid.Children.Remove(oldImage);
                }
                
                // Добавляем новое изображение
                mainGrid.Children.Insert(0, imageElement);
                
                // Убеждаемся, что textOverlayGrid остается
                if (!mainGrid.Children.Contains(textOverlayGrid))
                {
                    mainGrid.Children.Add(textOverlayGrid);
                }
                
                // Делаем textOverlayGrid невидимым если в нем нет текста
                if (textOverlayGrid.Children.Count == 0)
                {
                    textOverlayGrid.Visibility = Visibility.Hidden;
                }
                else
                {
                    textOverlayGrid.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Если нет Grid, создаем новый
                var newGrid = new Grid();
                newGrid.Children.Add(imageElement);
                newGrid.Children.Add(textOverlayGrid);
                mediaBorder.Child = newGrid;
                
                // Делаем textOverlayGrid невидимым если в нем нет текста
                if (textOverlayGrid.Children.Count == 0)
                {
                    textOverlayGrid.Visibility = Visibility.Hidden;
                }
                else
                {
                    textOverlayGrid.Visibility = Visibility.Visible;
                }
            }
            
            _currentMainMedia = _selectedElementKey;
            isVideoPlaying = false; // Изображения не "играют"
        }
        else if (_selectedElementSlot.Type == MediaType.Audio)
        {
            // Для аудио создаем отдельный MediaElement
            if (!_activeAudioSlots.ContainsKey(_selectedElementKey))
            {
                var audioElement = new MediaElement
                {
                    LoadedBehavior = MediaState.Manual,
                    Source = new Uri(_selectedElementSlot.MediaPath),
                    Volume = _selectedElementSlot.Volume,
                    SpeedRatio = _selectedElementSlot.PlaybackSpeed
                };
                
                // Создаем контейнер для аудио элемента
                var audioContainer = new Grid
                {
                    Width = 1,
                    Height = 1,
                    Visibility = Visibility.Hidden
                };
                audioContainer.Children.Add(audioElement);
                
                BottomPanel.Children.Add(audioContainer);
                
                _activeAudioSlots[_selectedElementKey] = audioElement;
                _activeAudioContainers[_selectedElementKey] = audioContainer;
            }
            
            var element = _activeAudioSlots[_selectedElementKey];
            element.Play();
            _currentAudioContent = _selectedElementKey;
            isAudioPlaying = true;
        }
        else if (_selectedElementSlot.Type == MediaType.Text)
        {
            // Для текстовых элементов просто устанавливаем как активное медиа
            _currentMainMedia = _selectedElementKey;
            
            // Применяем настройки текста
            ApplyTextSettings();
        }
        
        // Обновляем кнопку
        ElementPlayButton.Content = "⏸️";
        ElementPlayButton.ToolTip = "Пауза";
        
        // Обновляем подсветку кнопок слотов
        UpdateAllSlotButtonsHighlighting();
    }
    
    private void ElementStop_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedElementSlot == null || string.IsNullOrEmpty(_selectedElementKey)) return;
        
        // Останавливаем выбранный элемент
        if (_selectedElementSlot.Type == MediaType.Video || _selectedElementSlot.Type == MediaType.Image)
        {
            StopCurrentMainMedia();
        }
        else if (_selectedElementSlot.Type == MediaType.Audio)
        {
            StopAudioInSlot(_selectedElementKey);
        }
        
        // Сбрасываем состояние кнопки "Продолжить"
        ElementPlayButton.Content = "▶️";
        ElementPlayButton.ToolTip = "Воспроизвести";
        UpdateAllSlotButtonsHighlighting();
    }
    
    public void ElementRestart_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedElementSlot == null || string.IsNullOrEmpty(_selectedElementKey)) return;
        
        // Перезапускаем выбранный элемент
        ElementStop_Click(sender, e);
        
        // Сбрасываем состояние кнопки "Продолжить"
        ElementPlayButton.Content = "▶️";
        ElementPlayButton.ToolTip = "Воспроизвести";
        UpdateAllSlotButtonsHighlighting();
        
        Task.Delay(100).ContinueWith(_ => Dispatcher.Invoke(() => ElementPlay_Click(sender, e)));
    }
    
    private void PreviousMediaButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToMediaAndPlay(-1);
    }
    
    private void NextMediaButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToMediaAndPlay(1);
    }
    
    private void RenameElementButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedElementSlot == null) return;
        
        // Показываем диалог переименования
        string currentName = _selectedElementSlot.DisplayName;
        string? newName = Microsoft.VisualBasic.Interaction.InputBox(
            "Введите новое имя элемента:", 
            "Переименование элемента", 
            currentName);
            
        if (!string.IsNullOrWhiteSpace(newName) && newName != currentName)
        {
            _selectedElementSlot.DisplayName = newName;
            UpdateElementTitle();
        }
    }
    
    private void PreviousElementButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToElement(-1);
    }
    
    private void NextElementButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToElement(1);
    }
    
    public void NavigateToMediaAndPlay(int direction)
    {
        _navigationService.NavigateToMediaAndPlay(direction);
    }
    
    private void NavigateToElement(int direction)
    {
        _navigationService.NavigateToElement(direction);
    }
    
    // Обработчики событий для общих настроек проекта
    private void UseGlobalVolumeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.UseGlobalVolume = UseGlobalVolumeCheckBox.IsChecked == true;
        ApplyGlobalSettings();
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    private void GlobalVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.GlobalVolume = GlobalVolumeSlider.Value;
        GlobalVolumeValueText.Text = $"Общая громкость: {(GlobalVolumeSlider.Value * 100):F0}%";
        ApplyGlobalSettings();
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    private void GlobalVolumePreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && double.TryParse(button.Tag.ToString(), out double volume))
        {
            GlobalVolumeSlider.Value = volume;
        }
    }
    
    private void UseGlobalScaleCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.UseGlobalScale = UseGlobalScaleCheckBox.IsChecked == true;
        ApplyGlobalSettings();
        
        // Также применяем настройки к выбранному элементу если он есть
        if (_selectedElementSlot != null && !string.IsNullOrEmpty(_selectedElementKey))
        {
            ApplyElementSettings();
        }
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    private void GlobalScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.GlobalScale = GlobalScaleSlider.Value;
        GlobalScaleValueText.Text = $"Общий масштаб: {(GlobalScaleSlider.Value * 100):F0}%";
        ApplyGlobalSettings();
        
        // Также применяем настройки к выбранному элементу если он есть
        if (_selectedElementSlot != null && !string.IsNullOrEmpty(_selectedElementKey))
        {
            ApplyElementSettings();
        }
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    private void GlobalScalePreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && double.TryParse(button.Tag.ToString(), out double scale))
        {
            GlobalScaleSlider.Value = scale;
        }
    }
    
    private void UseGlobalRotationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.UseGlobalRotation = UseGlobalRotationCheckBox.IsChecked == true;
        ApplyGlobalSettings();
        
        // Также применяем настройки к выбранному элементу если он есть
        if (_selectedElementSlot != null && !string.IsNullOrEmpty(_selectedElementKey))
        {
            ApplyElementSettings();
        }
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    private void GlobalRotationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.GlobalRotation = GlobalRotationSlider.Value;
        GlobalRotationValueText.Text = $"Общий поворот: {GlobalRotationSlider.Value:F0}°";
        ApplyGlobalSettings();
        
        // Также применяем настройки к выбранному элементу если он есть
        if (_selectedElementSlot != null && !string.IsNullOrEmpty(_selectedElementKey))
        {
            ApplyElementSettings();
        }
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    private void GlobalRotationPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && double.TryParse(button.Tag.ToString(), out double rotation))
        {
            GlobalRotationSlider.Value = rotation;
        }
    }
    
    private void UseGlobalOpacityCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.UseGlobalOpacity = UseGlobalOpacityCheckBox.IsChecked == true;
        ApplyGlobalSettings();
        
        // Также применяем настройки к выбранному элементу если он есть
        if (_selectedElementSlot != null && !string.IsNullOrEmpty(_selectedElementKey))
        {
            ApplyElementSettings();
        }
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    private void GlobalOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.GlobalOpacity = GlobalOpacitySlider.Value;
        GlobalOpacityValueText.Text = $"Общая прозрачность: {(GlobalOpacitySlider.Value * 100):F0}%";
        ApplyGlobalSettings();
        
        // Также применяем настройки к выбранному элементу если он есть
        if (_selectedElementSlot != null && !string.IsNullOrEmpty(_selectedElementKey))
        {
            ApplyElementSettings();
        }
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    private void TransitionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.TransitionType = (TransitionType)TransitionTypeComboBox.SelectedIndex;
        
        // Обновляем TransitionService с новыми настройками
        _transitionService.SetGlobalSettings(_projectManager.CurrentProject.GlobalSettings);
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    private void TransitionDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.TransitionDuration = TransitionDurationSlider.Value;
        TransitionDurationValueText.Text = $"Длительность: {TransitionDurationSlider.Value:F1}с";
        
        // Обновляем TransitionService с новыми настройками
        _transitionService.SetGlobalSettings(_projectManager.CurrentProject.GlobalSettings);
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    private void AutoPlayNextCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.AutoPlayNext = AutoPlayNextCheckBox.IsChecked == true;
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    private void LoopPlaylistCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        _projectManager.CurrentProject.GlobalSettings.LoopPlaylist = LoopPlaylistCheckBox.IsChecked == true;
        
        // Автоматически сохраняем проект при изменении общих настроек
        _projectManager.SaveProject();
    }
    
    // Выбрать элемент для настройки
    public void SelectElementForSettings(MediaSlot slot, string slotKey)
    {
        _selectedElementSlot = slot;
        _selectedElementKey = slotKey;
        
        // Устанавливаем DisplayName по умолчанию если пустое
        if (string.IsNullOrEmpty(slot.DisplayName))
        {
            slot.DisplayName = System.IO.Path.GetFileNameWithoutExtension(slot.MediaPath);
        }
        
        // Показываем панель настроек
        NoElementSelectedText.Visibility = Visibility.Collapsed;
        SettingsContentPanel.Visibility = Visibility.Visible;
        RenameElementButton.Visibility = Visibility.Visible;
        
        // Показываем кнопки навигации если есть больше одного элемента
        bool hasMultipleElements = _projectManager?.CurrentProject?.MediaSlots?.Count() > 1;
        PreviousElementButton.Visibility = hasMultipleElements ? Visibility.Visible : Visibility.Collapsed;
        NextElementButton.Visibility = hasMultipleElements ? Visibility.Visible : Visibility.Collapsed;
        
        // Загружаем текущие настройки элемента
        LoadElementSettings();
    }
    
    // Снять выбор элемента
    public void UnselectElement()
    {
        _selectedElementSlot = null;
        _selectedElementKey = null;
        
        // Скрываем панель настроек
        NoElementSelectedText.Visibility = Visibility.Visible;
        SettingsContentPanel.Visibility = Visibility.Collapsed;
        RenameElementButton.Visibility = Visibility.Collapsed;
        PreviousElementButton.Visibility = Visibility.Collapsed;
        NextElementButton.Visibility = Visibility.Collapsed;
        
        ElementTitleText.Text = "Настройки элемента";
    }
    
    // Загрузить настройки элемента в UI
    private void LoadElementSettings()
    {
        if (_selectedElementSlot == null) return;
        
        // Устанавливаем значения слайдеров без вызова событий
        SpeedSlider.ValueChanged -= SpeedSlider_ValueChanged;
        OpacitySlider.ValueChanged -= OpacitySlider_ValueChanged;
        VolumeSlider.ValueChanged -= VolumeSlider_ValueChanged;
        ScaleSlider.ValueChanged -= ScaleSlider_ValueChanged;
        RotationSlider.ValueChanged -= RotationSlider_ValueChanged;
        
        SpeedSlider.Value = _selectedElementSlot.PlaybackSpeed;
        OpacitySlider.Value = _selectedElementSlot.Opacity;
        VolumeSlider.Value = _selectedElementSlot.Volume;
        ScaleSlider.Value = _selectedElementSlot.Scale;
        RotationSlider.Value = _selectedElementSlot.Rotation;
        
        SpeedSlider.ValueChanged += SpeedSlider_ValueChanged;
        OpacitySlider.ValueChanged += OpacitySlider_ValueChanged;
        VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
        ScaleSlider.ValueChanged += ScaleSlider_ValueChanged;
        RotationSlider.ValueChanged += RotationSlider_ValueChanged;
        
        // Обновляем текстовые метки
        SpeedValueText.Text = $"Скорость: {_selectedElementSlot.PlaybackSpeed:F1}x";
        OpacityValueText.Text = $"Прозрачность: {(_selectedElementSlot.Opacity * 100):F0}%";
        VolumeValueText.Text = $"Звук: {(_selectedElementSlot.Volume * 100):F0}%";
        ScaleValueText.Text = $"Масштаб: {(_selectedElementSlot.Scale * 100):F0}%";
        RotationValueText.Text = $"Поворот: {_selectedElementSlot.Rotation:F0}°";
        
        // Показываем или скрываем секции настроек в зависимости от типа элемента
        if (_selectedElementSlot.Type == MediaType.Text)
        {
            // Для текстовых элементов скрываем ненужные настройки
            SpeedGroupBox.Visibility = Visibility.Collapsed;
            OpacityGroupBox.Visibility = Visibility.Collapsed;
            VolumeGroupBox.Visibility = Visibility.Collapsed;
            
            // Показываем настройки текста
            TextSettingsGroupBox.Visibility = Visibility.Visible;
            LoadTextSettings();
        }
        else if (_selectedElementSlot.Type == MediaType.Image)
        {
            // Для изображений скрываем скорость и громкость (они не применимы)
            SpeedGroupBox.Visibility = Visibility.Collapsed;
            VolumeGroupBox.Visibility = Visibility.Collapsed;
            
            // Показываем прозрачность и другие настройки
            OpacityGroupBox.Visibility = Visibility.Visible;
            
            // Скрываем настройки текста
            TextSettingsGroupBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Для видео и аудио показываем все настройки
            SpeedGroupBox.Visibility = Visibility.Visible;
            OpacityGroupBox.Visibility = Visibility.Visible;
            VolumeGroupBox.Visibility = Visibility.Visible;
            
            // Скрываем настройки текста
            TextSettingsGroupBox.Visibility = Visibility.Collapsed;
        }
        
        // Применяем настройки к активным медиа элементам
        ApplyElementSettings();
        
        UpdateElementTitle();
    }
    
    // Загрузить настройки текста в UI
    private void LoadTextSettings()
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        // Отключаем события чтобы избежать лишних вызовов
        TextColorComboBox.SelectionChanged -= TextColorComboBox_SelectionChanged;
        FontFamilyComboBox.SelectionChanged -= FontFamilyComboBox_SelectionChanged;
        FontSizeSlider.ValueChanged -= FontSizeSlider_ValueChanged;
        TextContentTextBox.TextChanged -= TextContentTextBox_TextChanged;
        UseManualPositionCheckBox.Checked -= UseManualPositionCheckBox_Checked;
        UseManualPositionCheckBox.Unchecked -= UseManualPositionCheckBox_Unchecked;
        TextXTextBox.TextChanged -= TextXTextBox_TextChanged;
        TextYTextBox.TextChanged -= TextYTextBox_TextChanged;
        
        // Загружаем настройки цвета
        for (int i = 0; i < TextColorComboBox.Items.Count; i++)
        {
            if (TextColorComboBox.Items[i] is ComboBoxItem item && item.Tag.ToString() == _selectedElementSlot.FontColor)
            {
                TextColorComboBox.SelectedIndex = i;
                break;
            }
        }
        
        // Загружаем шрифт
        for (int i = 0; i < FontFamilyComboBox.Items.Count; i++)
        {
            if (FontFamilyComboBox.Items[i] is ComboBoxItem item && item.Tag.ToString() == _selectedElementSlot.FontFamily)
            {
                FontFamilyComboBox.SelectedIndex = i;
                break;
            }
        }
        
        // Загружаем размер шрифта
        FontSizeSlider.Value = _selectedElementSlot.FontSize;
        FontSizeValueText.Text = $"{_selectedElementSlot.FontSize:F0}px";
        
        // Загружаем содержимое текста
        TextContentTextBox.Text = _selectedElementSlot.TextContent ?? "";
        
        // Загружаем ручную настройку положения
        UseManualPositionCheckBox.IsChecked = _selectedElementSlot.UseManualPosition;
        ManualPositionPanel.Visibility = _selectedElementSlot.UseManualPosition ? Visibility.Visible : Visibility.Collapsed;
        TextXTextBox.Text = _selectedElementSlot.TextX.ToString();
        TextYTextBox.Text = _selectedElementSlot.TextY.ToString();
        
        // Загружаем состояние видимости
        if (_selectedElementSlot.IsTextVisible)
        {
            HideTextButton.Content = "👁️ Скрыть текст";
            HideTextButton.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Красный
        }
        else
        {
            HideTextButton.Content = "👁️ Показать текст";
            HideTextButton.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зеленый
        }
        
        // Включаем события обратно
        TextColorComboBox.SelectionChanged += TextColorComboBox_SelectionChanged;
        FontFamilyComboBox.SelectionChanged += FontFamilyComboBox_SelectionChanged;
        FontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;
        TextContentTextBox.TextChanged += TextContentTextBox_TextChanged;
        UseManualPositionCheckBox.Checked += UseManualPositionCheckBox_Checked;
        UseManualPositionCheckBox.Unchecked += UseManualPositionCheckBox_Unchecked;
        TextXTextBox.TextChanged += TextXTextBox_TextChanged;
        TextYTextBox.TextChanged += TextYTextBox_TextChanged;
    }
    
    // Обновить заголовок элемента
    private void UpdateElementTitle()
    {
        if (_selectedElementSlot != null)
        {
            ElementTitleText.Text = $"Настройки: {_selectedElementSlot.DisplayName}";
        }
    }
    
    // Применить настройки элемента к активным медиа
    public void ApplyElementSettings(MediaSlot slot, string slotKey)
    {
        if (slot == null || string.IsNullOrEmpty(slotKey)) return;
        
        // Получаем финальные значения с учетом общих настроек
        var finalVolume = GetFinalVolume(slot.Volume);
        var finalOpacity = GetFinalOpacity(slot.Opacity);
        var finalScale = GetFinalScale(slot.Scale);
        var finalRotation = GetFinalRotation(slot.Rotation);
        
        System.Diagnostics.Debug.WriteLine($"ApplyElementSettings: Slot={slotKey}, Type={slot.Type}, FinalOpacity={finalOpacity}, _currentMainMedia={_currentMainMedia}");
        
        // Применяем настройки к активному медиа элементу
        if (_activeSlotMedia.TryGetValue(slotKey, out MediaElement? mediaElement))
        {
            mediaElement.SpeedRatio = slot.PlaybackSpeed;
            mediaElement.Opacity = finalOpacity;
            mediaElement.Volume = finalVolume;
            
            // Применяем масштаб и поворот
            ApplyScaleAndRotation(mediaElement, finalScale, finalRotation);
        }
        
        // Если это главный плеер
        if (_currentMainMedia == slotKey)
        {
            this.mediaElement.SpeedRatio = slot.PlaybackSpeed;
            this.mediaElement.Volume = finalVolume;
            
            // Синхронизируем настройки с вторым экраном
            if (_secondaryMediaElement != null)
            {
                _secondaryMediaElement.SpeedRatio = slot.PlaybackSpeed;
                _secondaryMediaElement.Volume = 0; // Отключаем звук на втором экране чтобы избежать дублирования
                
                // Применяем масштаб и поворот к видео на втором экране
                ApplyScaleAndRotation(_secondaryMediaElement, finalScale, finalRotation);
                
                System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ НАСТРОЕК: Скорость={slot.PlaybackSpeed:F1}x, Звук отключен на втором экране");
            }
            
            // Для изображений применяем прозрачность к Border контейнеру
            if (slot.Type == MediaType.Image)
            {
                mediaBorder.Opacity = finalOpacity;
                System.Diagnostics.Debug.WriteLine($"ApplyElementSettings: Применена прозрачность {finalOpacity} к mediaBorder для изображения");
                
                // Применяем масштаб и поворот к mediaBorder для правильного центра поворота
                ApplyScaleAndRotation(mediaBorder, finalScale, finalRotation);
                
                // Синхронизируем прозрачность изображения на втором экране
                if (_secondaryScreenWindow?.Content is FrameworkElement secondaryElement)
                {
                    secondaryElement.Opacity = finalOpacity;
                    
                    // Применяем масштаб и поворот к изображению на втором экране
                    ApplyScaleAndRotation(secondaryElement, finalScale, finalRotation);
                }
            }
            else if (slot.Type == MediaType.Video)
            {
                // Для видео применяем прозрачность к MediaElement
                this.mediaElement.Visibility = Visibility.Visible;
                
                // Проверяем, что прозрачность не равна 0
                if (finalOpacity <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: Прозрачность равна {finalOpacity}, устанавливаем 1.0");
                    finalOpacity = 1.0;
                }
                
                this.mediaElement.Opacity = finalOpacity;
                
                // Убеждаемся, что mediaBorder видим
                mediaBorder.Visibility = Visibility.Visible;
                mediaBorder.Opacity = 1.0; // Border всегда непрозрачен
                
                // Применяем масштаб и поворот к mediaBorder для правильного центра поворота
                ApplyScaleAndRotation(mediaBorder, finalScale, finalRotation);
                
                // Синхронизируем прозрачность, масштаб и поворот видео на втором экране
                if (_secondaryMediaElement != null)
                {
                    _secondaryMediaElement.Visibility = Visibility.Visible;
                    _secondaryMediaElement.Opacity = finalOpacity;
                    ApplyScaleAndRotation(_secondaryMediaElement, finalScale, finalRotation);
                }
                
                System.Diagnostics.Debug.WriteLine($"ApplyElementSettings (Video): Opacity={finalOpacity}, Visibility=Visible, SlotKey={slotKey}, Source={this.mediaElement.Source?.LocalPath}");
            }
            
            // Для текстовых блоков применяем прозрачность, масштаб и поворот
            if (slot.Type == MediaType.Text)
            {
                textOverlayGrid.Opacity = finalOpacity;
                var textElement = textOverlayGrid.Children.OfType<TextBlock>().FirstOrDefault();
                if (textElement != null)
                {
                    // Применяем масштаб и поворот к текстовому блоку
                    ApplyScaleAndRotation(textElement, finalScale, finalRotation);
                }
                
                // Синхронизируем настройки текста на втором экране
                if (_secondaryScreenWindow?.Content is Grid secondaryGrid)
                {
                    var secondaryTextElement = secondaryGrid.Children.OfType<TextBlock>().FirstOrDefault();
                    if (secondaryTextElement != null)
                    {
                        secondaryTextElement.Opacity = finalOpacity;
                        
                        // Применяем масштаб и поворот к тексту на втором экране
                        ApplyScaleAndRotation(secondaryTextElement, finalScale, finalRotation);
                    }
                }
            }
        }
    }
    
    private void ApplyElementSettings()
    {
        if (_selectedElementSlot == null || string.IsNullOrEmpty(_selectedElementKey)) return;
        
        // Получаем финальные значения с учетом общих настроек
        var finalVolume = GetFinalVolume(_selectedElementSlot.Volume);
        var finalOpacity = GetFinalOpacity(_selectedElementSlot.Opacity);
        var finalScale = GetFinalScale(_selectedElementSlot.Scale);
        var finalRotation = GetFinalRotation(_selectedElementSlot.Rotation);
        
        // Применяем настройки к активному медиа элементу
        if (_activeSlotMedia.TryGetValue(_selectedElementKey, out MediaElement? mediaElement))
        {
            mediaElement.SpeedRatio = _selectedElementSlot.PlaybackSpeed;
            mediaElement.Opacity = finalOpacity;
            mediaElement.Volume = finalVolume;
            
            // Применяем масштаб и поворот
            ApplyScaleAndRotation(mediaElement, finalScale, finalRotation);
        }
        
        // Если это главный плеер
        if (_currentMainMedia == _selectedElementKey)
        {
            this.mediaElement.SpeedRatio = _selectedElementSlot.PlaybackSpeed;
            this.mediaElement.Volume = finalVolume;
            
            // Синхронизируем настройки с вторым экраном
            if (_secondaryMediaElement != null)
            {
                _secondaryMediaElement.SpeedRatio = _selectedElementSlot.PlaybackSpeed;
                _secondaryMediaElement.Volume = 0; // Отключаем звук на втором экране чтобы избежать дублирования
                
                // Применяем масштаб и поворот к видео на втором экране
                ApplyScaleAndRotation(_secondaryMediaElement, finalScale, finalRotation);
                
                System.Diagnostics.Debug.WriteLine($"СИНХРОНИЗАЦИЯ НАСТРОЕК: Скорость={_selectedElementSlot.PlaybackSpeed:F1}x, Звук отключен на втором экране");
            }
            
            // Для изображений применяем прозрачность к Border контейнеру
            if (_selectedElementSlot.Type == MediaType.Image)
            {
                mediaBorder.Opacity = finalOpacity;
                
                // Применяем масштаб и поворот к mediaBorder для правильного центра поворота
                ApplyScaleAndRotation(mediaBorder, finalScale, finalRotation);
                
                // Синхронизируем прозрачность изображения на втором экране
                if (_secondaryScreenWindow?.Content is FrameworkElement secondaryElement)
                {
                    secondaryElement.Opacity = finalOpacity;
                    
                    // Применяем масштаб и поворот к изображению на втором экране
                    ApplyScaleAndRotation(secondaryElement, finalScale, finalRotation);
                }
            }
            else
            {
                // Для видео применяем прозрачность к MediaElement
                this.mediaElement.Opacity = finalOpacity;
                
                // Применяем масштаб и поворот к mediaBorder для правильного центра поворота
                ApplyScaleAndRotation(mediaBorder, finalScale, finalRotation);
                
                // Синхронизируем прозрачность видео на втором экране
                if (_secondaryMediaElement != null)
                {
                    _secondaryMediaElement.Opacity = finalOpacity;
                }
            }
            
            // Для текстовых блоков применяем прозрачность, масштаб и поворот
            if (_selectedElementSlot.Type == MediaType.Text)
            {
                textOverlayGrid.Opacity = finalOpacity;
                var textElement = textOverlayGrid.Children.OfType<TextBlock>().FirstOrDefault();
                if (textElement != null)
                {
                    // Применяем масштаб и поворот к текстовому блоку
                    ApplyScaleAndRotation(textElement, finalScale, finalRotation);
                }
                
                // Синхронизируем настройки текста на втором экране
                if (_secondaryScreenWindow?.Content is Grid secondaryGrid)
                {
                    var secondaryTextElement = secondaryGrid.Children.OfType<TextBlock>().FirstOrDefault();
                    if (secondaryTextElement != null)
                    {
                        secondaryTextElement.Opacity = finalOpacity;
                        
                        // Применяем масштаб и поворот к тексту на втором экране
                        ApplyScaleAndRotation(secondaryTextElement, finalScale, finalRotation);
                    }
                }
            }
        }
        
        // Применяем к аудио элементам
        if (_activeAudioSlots.TryGetValue(_selectedElementKey, out MediaElement? audioElement))
        {
            audioElement.SpeedRatio = _selectedElementSlot.PlaybackSpeed;
            audioElement.Volume = finalVolume;
        }
    }
    
    // Применить общие настройки ко всем активным медиа элементам
    public void ApplyGlobalSettings()
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) 
        {
            System.Diagnostics.Debug.WriteLine("ApplyGlobalSettings: GlobalSettings is null, returning");
            return;
        }
        
        var globalSettings = _projectManager.CurrentProject.GlobalSettings;
        System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: UseGlobalOpacity={globalSettings.UseGlobalOpacity}, GlobalOpacity={globalSettings.GlobalOpacity}");
        System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: _currentMainMedia={_currentMainMedia}");
        
        // Применяем к главному плееру (основной медиа элемент)
        if (_currentMainMedia != null)
        {
            var slot = _projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => 
                (s.IsTrigger ? $"Trigger_{s.Column}" : $"Slot_{s.Column}_{s.Row}") == _currentMainMedia);
            
            if (slot != null)
            {
                var finalVolume = GetFinalVolume(slot.Volume);
                var finalOpacity = GetFinalOpacity(slot.Opacity);
                var finalScale = GetFinalScale(slot.Scale);
                var finalRotation = GetFinalRotation(slot.Rotation);
                
                System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Main media - Slot={_currentMainMedia}, FinalOpacity={finalOpacity}, FinalScale={finalScale}, FinalRotation={finalRotation}");
                
                this.mediaElement.Volume = finalVolume;
                
                if (slot.Type == MediaType.Image)
                {
                    // Для изображений применяем прозрачность к mediaBorder
                    mediaBorder.Opacity = finalOpacity;
                    
                    // Применяем масштаб и поворот к mediaBorder для правильного центра поворота
                    ApplyScaleAndRotation(mediaBorder, finalScale, finalRotation);
                    
                    System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied opacity {finalOpacity} to mediaBorder for image");
                }
                else if (slot.Type == MediaType.Video)
                {
                    // Для видео применяем прозрачность к mediaElement
                    this.mediaElement.Opacity = finalOpacity;
                    
                    // Применяем масштаб и поворот к mediaBorder для правильного центра поворота
                    ApplyScaleAndRotation(mediaBorder, finalScale, finalRotation);
                    
                    // Синхронизируем прозрачность, масштаб и поворот видео на втором экране
                    if (_secondaryMediaElement != null)
                    {
                        _secondaryMediaElement.Opacity = finalOpacity;
                        ApplyScaleAndRotation(_secondaryMediaElement, finalScale, finalRotation);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied opacity {finalOpacity} to mediaElement for video");
                }
                else if (slot.Type == MediaType.Text)
                {
                    // Для текстовых блоков применяем прозрачность к textOverlayGrid
                    textOverlayGrid.Opacity = finalOpacity;
                    
                    // Применяем масштаб и поворот к текстовому блоку
                    var textElement = textOverlayGrid.Children.OfType<TextBlock>().FirstOrDefault();
                    if (textElement != null)
                    {
                        ApplyScaleAndRotation(textElement, finalScale, finalRotation);
                    }
                    System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied opacity {finalOpacity} to textOverlayGrid for text");
                }
            }
        }
        
        // Применяем к аудио элементам
        foreach (var kvp in _activeAudioSlots)
        {
            var slotKey = kvp.Key;
            var audioElement = kvp.Value;
            
            var slot = _projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => 
                (s.IsTrigger ? $"Trigger_{s.Column}" : $"Slot_{s.Column}_{s.Row}") == slotKey);
            
            if (slot != null)
            {
                var finalVolume = GetFinalVolume(slot.Volume);
                audioElement.Volume = finalVolume;
                System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied volume {finalVolume} to audio element {slotKey}");
            }
        }
        
        // Применяем ко всем активным медиа элементам в _activeSlotMedia
        foreach (var kvp in _activeSlotMedia)
        {
            var slotKey = kvp.Key;
            var mediaElement = kvp.Value;
            
            // Находим соответствующий слот
            var slot = _projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => 
                (s.IsTrigger ? $"Trigger_{s.Column}" : $"Slot_{s.Column}_{s.Row}") == slotKey);
            
            if (slot != null)
            {
                var finalVolume = GetFinalVolume(slot.Volume);
                var finalOpacity = GetFinalOpacity(slot.Opacity);
                
                mediaElement.Volume = finalVolume;
                mediaElement.Opacity = finalOpacity;
                System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied volume {finalVolume} and opacity {finalOpacity} to slot media {slotKey}");
            }
        }
        
        // Применяем общие настройки ко всем активным медиа элементам
        // Перебираем все активные слоты и применяем к ним финальные настройки
        foreach (var kvp in _activeSlotMedia)
        {
            var slotKey = kvp.Key;
            var mediaElement = kvp.Value;
            
            var slot = _projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => 
                (s.IsTrigger ? $"Trigger_{s.Column}" : $"Slot_{s.Column}_{s.Row}") == slotKey);
            
            if (slot != null)
            {
                var finalOpacity = GetFinalOpacity(slot.Opacity);
                var finalScale = GetFinalScale(slot.Scale);
                var finalRotation = GetFinalRotation(slot.Rotation);
                
                mediaElement.Opacity = finalOpacity;
                ApplyScaleAndRotation(mediaElement, finalScale, finalRotation);
                System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied final settings to slot media {slotKey} - Opacity={finalOpacity}, Scale={finalScale}, Rotation={finalRotation}");
            }
        }
        
        // Применяем ко второму экрану если он активен
        if (_secondaryScreenWindow != null)
        {
            var secondaryFinalOpacity = globalSettings.UseGlobalOpacity ? globalSettings.GlobalOpacity : 1.0;
            _secondaryScreenWindow.Opacity = secondaryFinalOpacity;
            System.Diagnostics.Debug.WriteLine($"ApplyGlobalSettings: Applied opacity {secondaryFinalOpacity} to secondary screen");
        }
    }
    
    // Получить финальную громкость с учетом общих настроек
    private double GetFinalVolume(double personalVolume)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return personalVolume;
        
        var globalSettings = _projectManager.CurrentProject.GlobalSettings;
        return globalSettings.UseGlobalVolume ? globalSettings.GlobalVolume : personalVolume;
    }
    
    // Получить финальную прозрачность с учетом общих настроек
    private double GetFinalOpacity(double personalOpacity)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) 
        {
            System.Diagnostics.Debug.WriteLine($"GetFinalOpacity: GlobalSettings is null, returning personalOpacity={personalOpacity}");
            return personalOpacity;
        }
        
        var globalSettings = _projectManager.CurrentProject.GlobalSettings;
        var finalOpacity = globalSettings.UseGlobalOpacity ? globalSettings.GlobalOpacity : personalOpacity;
        System.Diagnostics.Debug.WriteLine($"GetFinalOpacity: UseGlobalOpacity={globalSettings.UseGlobalOpacity}, GlobalOpacity={globalSettings.GlobalOpacity}, PersonalOpacity={personalOpacity}, FinalOpacity={finalOpacity}");
        return finalOpacity;
    }
    
    // Получить финальный масштаб с учетом общих настроек
    private double GetFinalScale(double personalScale)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return personalScale;
        
        var globalSettings = _projectManager.CurrentProject.GlobalSettings;
        return globalSettings.UseGlobalScale ? globalSettings.GlobalScale : personalScale;
    }
    
    // Получить финальный поворот с учетом общих настроек
    private double GetFinalRotation(double personalRotation)
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return personalRotation;
        
        var globalSettings = _projectManager.CurrentProject.GlobalSettings;
        return globalSettings.UseGlobalRotation ? globalSettings.GlobalRotation : personalRotation;
    }
    
    // Применить масштаб и поворот к элементу с правильным центром
    private void ApplyScaleAndRotation(FrameworkElement element, double scale, double rotation)
    {
        if (element == null) return;
        
        var transform = new TransformGroup();
        transform.Children.Add(new ScaleTransform(scale, scale));
        transform.Children.Add(new RotateTransform(rotation));
        element.RenderTransform = transform;
        
        // Для медиа элементов (MediaElement, Image) устанавливаем точку поворота в центр медиаплеера
        if (element is MediaElement || element is Image)
        {
            element.RenderTransformOrigin = new Point(0.5, 0.5); // Центр медиаплеера
        }
        else
        {
            element.RenderTransformOrigin = new Point(0.5, 0.5); // Центр элемента для других типов
        }
    }
    
    // Загрузить общие настройки в UI
    private void LoadGlobalSettings()
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        var globalSettings = _projectManager.CurrentProject.GlobalSettings;
        
        // Отключаем события для предотвращения вызова ApplyGlobalSettings
        UseGlobalVolumeCheckBox.Checked -= UseGlobalVolumeCheckBox_Changed;
        UseGlobalVolumeCheckBox.Unchecked -= UseGlobalVolumeCheckBox_Changed;
        GlobalVolumeSlider.ValueChanged -= GlobalVolumeSlider_ValueChanged;
        UseGlobalOpacityCheckBox.Checked -= UseGlobalOpacityCheckBox_Changed;
        UseGlobalOpacityCheckBox.Unchecked -= UseGlobalOpacityCheckBox_Changed;
        GlobalOpacitySlider.ValueChanged -= GlobalOpacitySlider_ValueChanged;
        UseGlobalScaleCheckBox.Checked -= UseGlobalScaleCheckBox_Changed;
        UseGlobalScaleCheckBox.Unchecked -= UseGlobalScaleCheckBox_Changed;
        GlobalScaleSlider.ValueChanged -= GlobalScaleSlider_ValueChanged;
        UseGlobalRotationCheckBox.Checked -= UseGlobalRotationCheckBox_Changed;
        UseGlobalRotationCheckBox.Unchecked -= UseGlobalRotationCheckBox_Changed;
        GlobalRotationSlider.ValueChanged -= GlobalRotationSlider_ValueChanged;
        TransitionTypeComboBox.SelectionChanged -= TransitionTypeComboBox_SelectionChanged;
        TransitionDurationSlider.ValueChanged -= TransitionDurationSlider_ValueChanged;
        AutoPlayNextCheckBox.Checked -= AutoPlayNextCheckBox_Changed;
        AutoPlayNextCheckBox.Unchecked -= AutoPlayNextCheckBox_Changed;
        LoopPlaylistCheckBox.Checked -= LoopPlaylistCheckBox_Changed;
        LoopPlaylistCheckBox.Unchecked -= LoopPlaylistCheckBox_Changed;
        
        // Загружаем значения
        UseGlobalVolumeCheckBox.IsChecked = globalSettings.UseGlobalVolume;
        GlobalVolumeSlider.Value = globalSettings.GlobalVolume;
        GlobalVolumeValueText.Text = $"Общая громкость: {(globalSettings.GlobalVolume * 100):F0}%";
        
        UseGlobalOpacityCheckBox.IsChecked = globalSettings.UseGlobalOpacity;
        GlobalOpacitySlider.Value = globalSettings.GlobalOpacity;
        GlobalOpacityValueText.Text = $"Общая прозрачность: {(globalSettings.GlobalOpacity * 100):F0}%";
        
        UseGlobalScaleCheckBox.IsChecked = globalSettings.UseGlobalScale;
        GlobalScaleSlider.Value = globalSettings.GlobalScale;
        GlobalScaleValueText.Text = $"Общий масштаб: {(globalSettings.GlobalScale * 100):F0}%";
        
        UseGlobalRotationCheckBox.IsChecked = globalSettings.UseGlobalRotation;
        GlobalRotationSlider.Value = globalSettings.GlobalRotation;
        GlobalRotationValueText.Text = $"Общий поворот: {globalSettings.GlobalRotation:F0}°";
        
        TransitionTypeComboBox.SelectedIndex = (int)globalSettings.TransitionType;
        TransitionDurationSlider.Value = globalSettings.TransitionDuration;
        TransitionDurationValueText.Text = $"Длительность: {globalSettings.TransitionDuration:F1}с";
        
        AutoPlayNextCheckBox.IsChecked = globalSettings.AutoPlayNext;
        LoopPlaylistCheckBox.IsChecked = globalSettings.LoopPlaylist;
        
        // Обновляем TransitionService с загруженными настройками
        _transitionService.SetGlobalSettings(globalSettings);
        
        // Включаем события обратно
        UseGlobalVolumeCheckBox.Checked += UseGlobalVolumeCheckBox_Changed;
        UseGlobalVolumeCheckBox.Unchecked += UseGlobalVolumeCheckBox_Changed;
        GlobalVolumeSlider.ValueChanged += GlobalVolumeSlider_ValueChanged;
        UseGlobalOpacityCheckBox.Checked += UseGlobalOpacityCheckBox_Changed;
        UseGlobalOpacityCheckBox.Unchecked += UseGlobalOpacityCheckBox_Changed;
        GlobalOpacitySlider.ValueChanged += GlobalOpacitySlider_ValueChanged;
        UseGlobalScaleCheckBox.Checked += UseGlobalScaleCheckBox_Changed;
        UseGlobalScaleCheckBox.Unchecked += UseGlobalScaleCheckBox_Changed;
        GlobalScaleSlider.ValueChanged += GlobalScaleSlider_ValueChanged;
        UseGlobalRotationCheckBox.Checked += UseGlobalRotationCheckBox_Changed;
        UseGlobalRotationCheckBox.Unchecked += UseGlobalRotationCheckBox_Changed;
        GlobalRotationSlider.ValueChanged += GlobalRotationSlider_ValueChanged;
        TransitionTypeComboBox.SelectionChanged += TransitionTypeComboBox_SelectionChanged;
        TransitionDurationSlider.ValueChanged += TransitionDurationSlider_ValueChanged;
        AutoPlayNextCheckBox.Checked += AutoPlayNextCheckBox_Changed;
        AutoPlayNextCheckBox.Unchecked += AutoPlayNextCheckBox_Changed;
        LoopPlaylistCheckBox.Checked += LoopPlaylistCheckBox_Changed;
        LoopPlaylistCheckBox.Unchecked += LoopPlaylistCheckBox_Changed;
        
        // Применяем общие настройки к активным медиа элементам после загрузки
        System.Diagnostics.Debug.WriteLine($"LoadGlobalSettings: Вызываем ApplyGlobalSettings() после загрузки настроек");
        ApplyGlobalSettings();
    }
    
    // Применить переход между медиа элементами
    private async Task ApplyTransition(Action transitionAction)
    {
        await _transitionService.ApplyTransition(transitionAction, null);
    }
    
    // Применить переход между медиа элементами с поддержкой второго экрана
    private async Task ApplyTransitionWithSecondaryScreen(Action transitionAction, Action? secondaryTransitionAction = null)
    {
        await _transitionService.ApplyTransition(transitionAction, secondaryTransitionAction);
    }
    
    // Найти следующий элемент в той же строке для автоперехода
    private MediaSlot? FindNextElementInRow(int currentColumn, int currentRow)
    {
        return _autoPlayService.FindNextElementInRow(currentColumn, currentRow);
    }
    
    // Автопереход на следующий элемент
    private async void AutoPlayNextElement()
    {
        await _autoPlayService.AutoPlayNextElement();
    }
    
    // Автопереход для аудио элементов
    private async void AutoPlayNextAudioElement(string currentSlotKey)
    {
        await _autoPlayService.AutoPlayNextAudioElement(currentSlotKey);
    }
    
    // Остановить текущее медиа в главном плеере
    private void StopCurrentMainMedia()
    {
        _videoDisplayService.StopCurrentMainMedia();
        _currentVisualContent = null;
        
        // Закрываем дополнительное окно
        CloseSecondaryScreenWindow();
    }
    
    // Остановить аудио в слоте
    private void StopAudioInSlot(string slotKey)
    {
        if (_activeAudioSlots.TryGetValue(slotKey, out MediaElement? audioElement))
        {
            audioElement.Stop();
            _activeAudioSlots.Remove(slotKey);
            
            if (_activeAudioContainers.TryGetValue(slotKey, out Grid? container))
            {
                BottomPanel.Children.Remove(container);
                _activeAudioContainers.Remove(slotKey);
            }
        }
    }
    
    // Обработчики для перетаскивания панели настроек элемента
    private void ElementSettingsBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isAnyResizingActive)
        {
            _isDraggingElementSettings = true;
            _lastMousePosition = e.GetPosition(this);
            ElementSettingsBorder.CaptureMouse();
        }
    }
    
    private void ElementSettingsBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingElementSettings && !_isAnyResizingActive)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            // Обновляем позицию панели
            Canvas.SetLeft(ElementSettingsBorder, Canvas.GetLeft(ElementSettingsBorder) + deltaX);
            Canvas.SetTop(ElementSettingsBorder, Canvas.GetTop(ElementSettingsBorder) + deltaY);
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void ElementSettingsBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingElementSettings = false;
        ElementSettingsBorder.ReleaseMouseCapture();
    }
    
    // Обработчики для изменения размера панели настроек элемента
    // Вертикальное растягивание
    private void ElementSettingsResizeHandleV_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingElementSettingsV = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        ElementSettingsResizeHandleV.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingElementSettings = false;
        ElementSettingsBorder.ReleaseMouseCapture();
    }
    
    private void ElementSettingsResizeHandleV_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingElementSettingsV)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            if (ElementSettingsBorder.Height + deltaY >= 200) // Минимальная высота
            {
                ElementSettingsBorder.Height += deltaY;
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void ElementSettingsResizeHandleV_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingElementSettingsV = false;
        _isAnyResizingActive = false;
        ElementSettingsResizeHandleV.ReleaseMouseCapture();
    }
    
    // Горизонтальное растягивание
    private void ElementSettingsResizeHandleH_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingElementSettingsH = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        ElementSettingsResizeHandleH.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingElementSettings = false;
        ElementSettingsBorder.ReleaseMouseCapture();
    }
    
    private void ElementSettingsResizeHandleH_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingElementSettingsH)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            
            if (ElementSettingsBorder.Width + deltaX >= 300) // Минимальная ширина
            {
                ElementSettingsBorder.Width += deltaX;
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void ElementSettingsResizeHandleH_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingElementSettingsH = false;
        _isAnyResizingActive = false;
        ElementSettingsResizeHandleH.ReleaseMouseCapture();
    }
    
    // Диагональное растягивание
    private void ElementSettingsResizeHandleD_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingElementSettingsD = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        ElementSettingsResizeHandleD.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingElementSettings = false;
        ElementSettingsBorder.ReleaseMouseCapture();
    }
    
    private void ElementSettingsResizeHandleD_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingElementSettingsD)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            if (ElementSettingsBorder.Width + deltaX >= 300 && ElementSettingsBorder.Height + deltaY >= 200)
            {
                ElementSettingsBorder.Width += deltaX;
                ElementSettingsBorder.Height += deltaY;
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void ElementSettingsResizeHandleD_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingElementSettingsD = false;
        _isAnyResizingActive = false;
        ElementSettingsResizeHandleD.ReleaseMouseCapture();
    }
    
    // Обработчики для перетаскивания панели общих настроек
    private void GlobalSettingsBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isAnyResizingActive)
        {
            _isDraggingGlobalSettings = true;
            _lastMousePosition = e.GetPosition(this);
            GlobalSettingsBorder.CaptureMouse();
        }
    }
    
    private void GlobalSettingsBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingGlobalSettings && !_isAnyResizingActive)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            // Обновляем позицию панели
            Canvas.SetLeft(GlobalSettingsBorder, Canvas.GetLeft(GlobalSettingsBorder) + deltaX);
            Canvas.SetTop(GlobalSettingsBorder, Canvas.GetTop(GlobalSettingsBorder) + deltaY);
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void GlobalSettingsBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingGlobalSettings = false;
        GlobalSettingsBorder.ReleaseMouseCapture();
    }
    
    // Обработчики для изменения размера панели общих настроек
    // Вертикальное растягивание
    private void GlobalSettingsResizeHandleV_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingGlobalSettingsV = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        GlobalSettingsResizeHandleV.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingGlobalSettings = false;
        GlobalSettingsBorder.ReleaseMouseCapture();
    }
    
    private void GlobalSettingsResizeHandleV_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingGlobalSettingsV)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            if (GlobalSettingsBorder.Height + deltaY >= 200) // Минимальная высота
            {
                GlobalSettingsBorder.Height += deltaY;
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void GlobalSettingsResizeHandleV_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingGlobalSettingsV = false;
        _isAnyResizingActive = false;
        GlobalSettingsResizeHandleV.ReleaseMouseCapture();
    }
    
    // Горизонтальное растягивание
    private void GlobalSettingsResizeHandleH_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingGlobalSettingsH = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        GlobalSettingsResizeHandleH.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingGlobalSettings = false;
        GlobalSettingsBorder.ReleaseMouseCapture();
    }
    
    private void GlobalSettingsResizeHandleH_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingGlobalSettingsH)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            
            if (GlobalSettingsBorder.Width + deltaX >= 300) // Минимальная ширина
            {
                GlobalSettingsBorder.Width += deltaX;
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void GlobalSettingsResizeHandleH_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingGlobalSettingsH = false;
        _isAnyResizingActive = false;
        GlobalSettingsResizeHandleH.ReleaseMouseCapture();
    }
    
    // Диагональное растягивание
    private void GlobalSettingsResizeHandleD_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingGlobalSettingsD = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        GlobalSettingsResizeHandleD.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingGlobalSettings = false;
        GlobalSettingsBorder.ReleaseMouseCapture();
    }
    
    private void GlobalSettingsResizeHandleD_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingGlobalSettingsD)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            if (GlobalSettingsBorder.Width + deltaX >= 300 && GlobalSettingsBorder.Height + deltaY >= 200)
            {
                GlobalSettingsBorder.Width += deltaX;
                GlobalSettingsBorder.Height += deltaY;
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void GlobalSettingsResizeHandleD_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingGlobalSettingsD = false;
        _isAnyResizingActive = false;
        GlobalSettingsResizeHandleD.ReleaseMouseCapture();
    }
    
    // Обработчики для перетаскивания медиаплеера
    private void MediaPlayerBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isAnyResizingActive)
        {
            _isDraggingMediaPlayer = true;
            _lastMousePosition = e.GetPosition(this);
            MediaPlayerBorder.CaptureMouse();
        }
    }
    
    private void MediaPlayerBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingMediaPlayer && !_isAnyResizingActive)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            // Обновляем позицию панели
            Canvas.SetLeft(MediaPlayerBorder, Canvas.GetLeft(MediaPlayerBorder) + deltaX);
            Canvas.SetTop(MediaPlayerBorder, Canvas.GetTop(MediaPlayerBorder) + deltaY);
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void MediaPlayerBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingMediaPlayer = false;
        MediaPlayerBorder.ReleaseMouseCapture();
    }
    
    // Обработчики для изменения размера медиаплеера
    // Вертикальное растягивание
    private void MediaPlayerResizeHandleV_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaPlayerV = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        MediaPlayerResizeHandleV.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingMediaPlayer = false;
        MediaPlayerBorder.ReleaseMouseCapture();
    }
    
    private void MediaPlayerResizeHandleV_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingMediaPlayerV)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            if (MediaPlayerBorder.Height + deltaY >= 300) // Минимальная высота
            {
                MediaPlayerBorder.Height += deltaY;
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void MediaPlayerResizeHandleV_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaPlayerV = false;
        _isAnyResizingActive = false;
        MediaPlayerResizeHandleV.ReleaseMouseCapture();
    }
    
    // Горизонтальное растягивание
    private void MediaPlayerResizeHandleH_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaPlayerH = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        MediaPlayerResizeHandleH.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingMediaPlayer = false;
        MediaPlayerBorder.ReleaseMouseCapture();
    }
    
    private void MediaPlayerResizeHandleH_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingMediaPlayerH)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            
            if (MediaPlayerBorder.Width + deltaX >= 400) // Минимальная ширина
            {
                MediaPlayerBorder.Width += deltaX;
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void MediaPlayerResizeHandleH_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaPlayerH = false;
        _isAnyResizingActive = false;
        MediaPlayerResizeHandleH.ReleaseMouseCapture();
    }
    
    // Диагональное растягивание
    private void MediaPlayerResizeHandleD_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaPlayerD = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        MediaPlayerResizeHandleD.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingMediaPlayer = false;
        MediaPlayerBorder.ReleaseMouseCapture();
    }
    
    private void MediaPlayerResizeHandleD_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingMediaPlayerD)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            if (MediaPlayerBorder.Width + deltaX >= 400 && MediaPlayerBorder.Height + deltaY >= 300)
            {
                MediaPlayerBorder.Width += deltaX;
                MediaPlayerBorder.Height += deltaY;
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void MediaPlayerResizeHandleD_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaPlayerD = false;
        _isAnyResizingActive = false;
        MediaPlayerResizeHandleD.ReleaseMouseCapture();
    }
    
    // Обработчики для перетаскивания панели медиа-клеток
    private void MediaCellsBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isAnyResizingActive)
        {
            _isDraggingMediaCells = true;
            _lastMousePosition = e.GetPosition(this);
            MediaCellsBorder.CaptureMouse();
        }
    }
    
    private void MediaCellsBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingMediaCells && !_isAnyResizingActive)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            // Обновляем позицию панели
            Canvas.SetLeft(MediaCellsBorder, Canvas.GetLeft(MediaCellsBorder) + deltaX);
            Canvas.SetBottom(MediaCellsBorder, Canvas.GetBottom(MediaCellsBorder) - deltaY);
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void MediaCellsBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingMediaCells = false;
        MediaCellsBorder.ReleaseMouseCapture();
    }
    
    // Обработчики для изменения размера панели медиа-клеток
    // Вертикальное растягивание
    private void MediaCellsResizeHandleV_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaCellsV = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        MediaCellsResizeHandleV.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingMediaCells = false;
        MediaCellsBorder.ReleaseMouseCapture();
    }
    
    private void MediaCellsResizeHandleV_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingMediaCellsV)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            if (MediaCellsBorder.Height - deltaY >= 200) // Минимальная высота
            {
                MediaCellsBorder.Height -= deltaY;
                Canvas.SetBottom(MediaCellsBorder, Canvas.GetBottom(MediaCellsBorder) + deltaY);
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void MediaCellsResizeHandleV_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaCellsV = false;
        _isAnyResizingActive = false;
        MediaCellsResizeHandleV.ReleaseMouseCapture();
    }
    
    // Горизонтальное растягивание
    private void MediaCellsResizeHandleH_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaCellsH = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        MediaCellsResizeHandleH.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingMediaCells = false;
        MediaCellsBorder.ReleaseMouseCapture();
    }
    
    private void MediaCellsResizeHandleH_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingMediaCellsH)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            
            if (MediaCellsBorder.Width + deltaX >= 400) // Минимальная ширина
            {
                MediaCellsBorder.Width += deltaX;
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void MediaCellsResizeHandleH_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaCellsH = false;
        _isAnyResizingActive = false;
        MediaCellsResizeHandleH.ReleaseMouseCapture();
    }
    
    // Диагональное растягивание
    private void MediaCellsResizeHandleD_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaCellsD = true;
        _isAnyResizingActive = true;
        _lastMousePosition = e.GetPosition(this);
        MediaCellsResizeHandleD.CaptureMouse();
        
        // Останавливаем перетаскивание панели
        _isDraggingMediaCells = false;
        MediaCellsBorder.ReleaseMouseCapture();
    }
    
    private void MediaCellsResizeHandleD_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizingMediaCellsD)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;
            
            if (MediaCellsBorder.Width + deltaX >= 400 && MediaCellsBorder.Height - deltaY >= 200)
            {
                MediaCellsBorder.Width += deltaX;
                MediaCellsBorder.Height -= deltaY;
                Canvas.SetBottom(MediaCellsBorder, Canvas.GetBottom(MediaCellsBorder) + deltaY);
            }
            
            _lastMousePosition = currentPosition;
        }
    }
    
    private void MediaCellsResizeHandleD_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isResizingMediaCellsD = false;
        _isAnyResizingActive = false;
        MediaCellsResizeHandleD.ReleaseMouseCapture();
    }
    
    // Обработчики MouseEnter/MouseLeave для визуальной обратной связи
    // Панель настроек элемента
    private void ElementSettingsResizeHandleV_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void ElementSettingsResizeHandleV_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    private void ElementSettingsResizeHandleH_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void ElementSettingsResizeHandleH_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    private void ElementSettingsResizeHandleD_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void ElementSettingsResizeHandleD_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    
    // Панель общих настроек
    private void GlobalSettingsResizeHandleV_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void GlobalSettingsResizeHandleV_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    private void GlobalSettingsResizeHandleH_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void GlobalSettingsResizeHandleH_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    private void GlobalSettingsResizeHandleD_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void GlobalSettingsResizeHandleD_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    
    // Медиаплеер
    private void MediaPlayerResizeHandleV_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void MediaPlayerResizeHandleV_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    private void MediaPlayerResizeHandleH_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void MediaPlayerResizeHandleH_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    private void MediaPlayerResizeHandleD_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void MediaPlayerResizeHandleD_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    
    // Панель медиа-клеток
    private void MediaCellsResizeHandleV_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void MediaCellsResizeHandleV_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    private void MediaCellsResizeHandleH_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void MediaCellsResizeHandleH_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    private void MediaCellsResizeHandleD_MouseEnter(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 1.0;
    private void MediaCellsResizeHandleD_MouseLeave(object sender, MouseEventArgs e) => ((Border)sender).Opacity = 0.7;
    
    // Методы для сохранения и загрузки позиций панелей
    private void SavePanelPositions()
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        var settings = _projectManager.CurrentProject.GlobalSettings;
        
        // Сохраняем позиции и размеры панелей
        settings.ElementSettingsPanel.Left = Canvas.GetLeft(ElementSettingsBorder);
        settings.ElementSettingsPanel.Top = Canvas.GetTop(ElementSettingsBorder);
        settings.ElementSettingsPanel.Width = ElementSettingsBorder.Width;
        settings.ElementSettingsPanel.Height = ElementSettingsBorder.Height;
        
        settings.GlobalSettingsPanel.Left = Canvas.GetLeft(GlobalSettingsBorder);
        settings.GlobalSettingsPanel.Top = Canvas.GetTop(GlobalSettingsBorder);
        settings.GlobalSettingsPanel.Width = GlobalSettingsBorder.Width;
        settings.GlobalSettingsPanel.Height = GlobalSettingsBorder.Height;
        
        settings.MediaPlayerPanel.Left = Canvas.GetLeft(MediaPlayerBorder);
        settings.MediaPlayerPanel.Top = Canvas.GetTop(MediaPlayerBorder);
        settings.MediaPlayerPanel.Width = MediaPlayerBorder.Width;
        settings.MediaPlayerPanel.Height = MediaPlayerBorder.Height;
        
        settings.MediaCellsPanel.Left = Canvas.GetLeft(MediaCellsBorder);
        settings.MediaCellsPanel.Top = Canvas.GetBottom(MediaCellsBorder);
        settings.MediaCellsPanel.Width = MediaCellsBorder.Width;
        settings.MediaCellsPanel.Height = MediaCellsBorder.Height;
    }
    
    private void LoadPanelPositions()
    {
        if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
        
        var settings = _projectManager.CurrentProject.GlobalSettings;
        
        // Загружаем позиции и размеры панелей
        Canvas.SetLeft(ElementSettingsBorder, settings.ElementSettingsPanel.Left);
        Canvas.SetTop(ElementSettingsBorder, settings.ElementSettingsPanel.Top);
        ElementSettingsBorder.Width = settings.ElementSettingsPanel.Width;
        ElementSettingsBorder.Height = settings.ElementSettingsPanel.Height;
        
        Canvas.SetLeft(GlobalSettingsBorder, settings.GlobalSettingsPanel.Left);
        Canvas.SetTop(GlobalSettingsBorder, settings.GlobalSettingsPanel.Top);
        GlobalSettingsBorder.Width = settings.GlobalSettingsPanel.Width;
        GlobalSettingsBorder.Height = settings.GlobalSettingsPanel.Height;
        
        Canvas.SetLeft(MediaPlayerBorder, settings.MediaPlayerPanel.Left);
        Canvas.SetTop(MediaPlayerBorder, settings.MediaPlayerPanel.Top);
        MediaPlayerBorder.Width = settings.MediaPlayerPanel.Width;
        MediaPlayerBorder.Height = settings.MediaPlayerPanel.Height;
        
        Canvas.SetLeft(MediaCellsBorder, settings.MediaCellsPanel.Left);
        Canvas.SetBottom(MediaCellsBorder, settings.MediaCellsPanel.Top);
        MediaCellsBorder.Width = settings.MediaCellsPanel.Width;
        MediaCellsBorder.Height = settings.MediaCellsPanel.Height;
    }
}
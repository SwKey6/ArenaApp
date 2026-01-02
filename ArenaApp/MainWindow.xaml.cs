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
    private readonly Services.PanelPositionService _panelPositionService = new();
    private readonly Services.MediaTypeService _mediaTypeService = new();
    private readonly Services.ElementControlService _elementControlService = new();
    private readonly Services.ElementSettingsService _elementSettingsService = new();
    private readonly Services.ElementSettingsUIService _elementSettingsUIService = new();
    private readonly Services.GlobalSettingsUIService _globalSettingsUIService = new();
    private readonly Services.ProjectManagementService _projectManagementService = new();
    private readonly Services.SliderService _sliderService = new();
    private readonly Services.MenuService _menuService = new();
    private readonly Services.ElementSettingsEventHandlerService _elementSettingsEventHandlerService = new();
    private readonly Services.GlobalSettingsEventHandlerService _globalSettingsEventHandlerService = new();
    
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
    
    // Сервисы для перетаскивания панелей
    private readonly Services.PanelDragService _elementSettingsDragService = new();
    private readonly Services.PanelDragService _globalSettingsDragService = new();
    private readonly Services.PanelDragService _mediaPlayerDragService = new();
    private readonly Services.PanelDragService _mediaCellsDragService = new();

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
        
        // Настройка SliderService
        _sliderService.SetTimerService(_timerService);
        _sliderService.SetMediaStateService(_mediaStateService);
        _sliderService.GetVideoSlider = () => videoSlider;
        _sliderService.GetMainMediaElement = () => mediaElement;
        _sliderService.GetSecondaryMediaElement = () => _secondaryScreenService.SecondaryMediaElement ?? _secondaryMediaElement;
        _sliderService.GetAudioSlider = () => audioSlider;
        _sliderService.GetVideoTotalDuration = () => _videoTotalDuration;
        _sliderService.GetAudioTotalDuration = () => _audioTotalDuration;
        
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
        _mediaPlayerService.StopActiveAudio = (slotKey) => _mediaControlService.StopActiveAudio();
        
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
        _mediaControlService.GetAllAudioSlots = () => _activeAudioSlots;
        _mediaControlService.GetAllAudioContainers = () => _activeAudioContainers;
        _mediaControlService.GetMainContentGrid = () => (Grid)Content;
        _mediaControlService.UnregisterActiveMediaFile = (path) => UnregisterActiveMediaFile(path);
        _mediaControlService.SetCurrentAudioContent = (value) => _currentAudioContent = value;
        _mediaControlService.UpdateAllSlotButtonsHighlighting = () => UpdateAllSlotButtonsHighlighting();
        
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
        
        // Настройка PanelPositionService
        _panelPositionService.GetElementSettingsPanel = () => ElementSettingsBorder;
        _panelPositionService.GetGlobalSettingsPanel = () => GlobalSettingsBorder;
        _panelPositionService.GetMediaPlayerPanel = () => MediaPlayerBorder;
        _panelPositionService.GetMediaCellsPanel = () => MediaCellsBorder;
        _panelPositionService.GetGlobalSettings = () => _projectManager?.CurrentProject?.GlobalSettings;
        
        // Настройка MediaTypeService
        _mediaTypeService.GetMediaSlot = (col, row) => _projectManager.GetMediaSlot(col, row);
        _mediaTypeService.GetCurrentVisualContent = () => _currentVisualContent;
        _mediaTypeService.GetCurrentAudioContent = () => _currentAudioContent;
        _mediaTypeService.GetCurrentMainMedia = () => _currentMainMedia;
        _mediaTypeService.IsMediaFileAlreadyPlaying = (path) => IsMediaFileAlreadyPlaying(path);
        
        // Настройка ElementControlService
        _elementControlService.SetMediaStateService(_mediaStateService);
        _elementControlService.SetVideoDisplayService(_videoDisplayService);
        _elementControlService.GetMainMediaElement = () => mediaElement;
        _elementControlService.GetMediaBorder = () => mediaBorder;
        _elementControlService.GetTextOverlayGrid = () => textOverlayGrid;
        _elementControlService.GetMainContentGrid = () => (Grid)Content;
        _elementControlService.GetDispatcher = () => Dispatcher;
        _elementControlService.GetSecondaryMediaElement = () => _secondaryScreenService.SecondaryMediaElement ?? _secondaryMediaElement;
        _elementControlService.GetSecondaryScreenWindow = () => _secondaryScreenService.SecondaryScreenWindow ?? _secondaryScreenWindow;
        _elementControlService.GetCurrentMainMedia = () => _currentMainMedia;
        _elementControlService.SetCurrentMainMedia = (value) => _currentMainMedia = value;
        _elementControlService.GetCurrentAudioContent = () => _currentAudioContent;
        _elementControlService.GetCurrentVisualContent = () => _currentVisualContent;
        _elementControlService.GetIsVideoPaused = () => _isVideoPaused;
        _elementControlService.SetIsVideoPaused = (value) => _isVideoPaused = value;
        _elementControlService.SetIsVideoPlaying = (value) => isVideoPlaying = value;
        _elementControlService.SetIsAudioPlaying = (value) => isAudioPlaying = value;
        _elementControlService.TryGetAudioSlot = (slotKey) => _mediaStateService.TryGetAudioSlot(slotKey, out var element) ? element : null;
        _elementControlService.GetAllAudioSlots = () => _activeAudioSlots;
        _elementControlService.GetAllAudioContainers = () => _activeAudioContainers;
        _elementControlService.GetBottomPanel = () => BottomPanel;
        _elementControlService.GetMediaResumePosition = (path) => _mediaStateService.GetMediaResumePosition(path) ?? TimeSpan.Zero;
        _elementControlService.SaveMediaResumePosition = (path, position) => 
        {
            _mediaResumePositions[path] = position;
            _mediaStateService.SaveMediaResumePosition(path, position);
        };
        _elementControlService.UpdateAllSlotButtonsHighlighting = () => UpdateAllSlotButtonsHighlighting();
        _elementControlService.ApplyTextSettings = () => ApplyTextSettings();
        _elementControlService.SyncPlayWithSecondaryScreen = () => SyncPlayWithSecondaryScreen();
        _elementControlService.SyncPauseWithSecondaryScreen = () => SyncPauseWithSecondaryScreen();
        
        // Настройка ElementSettingsService
        _elementSettingsService.SetSettingsManager(_settingsManager);
        _elementSettingsService.GetMainMediaElement = () => mediaElement;
        _elementSettingsService.GetMediaBorder = () => mediaBorder;
        _elementSettingsService.GetTextOverlayGrid = () => textOverlayGrid;
        _elementSettingsService.GetSecondaryMediaElement = () => _secondaryScreenService.SecondaryMediaElement ?? _secondaryMediaElement;
        _elementSettingsService.GetSecondaryScreenWindow = () => _secondaryScreenService.SecondaryScreenWindow ?? _secondaryScreenWindow;
        _elementSettingsService.GetProjectManager = () => _projectManager;
        _elementSettingsService.GetCurrentMainMedia = () => _currentMainMedia;
        _elementSettingsService.GetAllAudioSlots = () => _activeAudioSlots;
        _elementSettingsService.GetActiveSlotMedia = () => _activeSlotMedia;
        
        // Настройка PanelDragService для ElementSettings
        _elementSettingsDragService.GetPanel = () => ElementSettingsBorder;
        _elementSettingsDragService.GetWindow = () => this;
        _elementSettingsDragService.MinWidth = 300;
        _elementSettingsDragService.MinHeight = 200;
        _elementSettingsDragService.UseBottomAnchor = false;
        
        // Настройка PanelDragService для GlobalSettings
        _globalSettingsDragService.GetPanel = () => GlobalSettingsBorder;
        _globalSettingsDragService.GetWindow = () => this;
        _globalSettingsDragService.MinWidth = 300;
        _globalSettingsDragService.MinHeight = 200;
        _globalSettingsDragService.UseBottomAnchor = false;
        
        // Настройка PanelDragService для MediaPlayer
        _mediaPlayerDragService.GetPanel = () => MediaPlayerBorder;
        _mediaPlayerDragService.GetWindow = () => this;
        _mediaPlayerDragService.MinWidth = 400;
        _mediaPlayerDragService.MinHeight = 300;
        _mediaPlayerDragService.UseBottomAnchor = false;
        
        // Настройка PanelDragService для MediaCells (использует Bottom anchor)
        _mediaCellsDragService.GetPanel = () => MediaCellsBorder;
        _mediaCellsDragService.GetWindow = () => this;
        _mediaCellsDragService.MinWidth = 400;
        _mediaCellsDragService.MinHeight = 200;
        _mediaCellsDragService.UseBottomAnchor = true;
        
        // Настройка ElementSettingsUIService
        _elementSettingsUIService.SetProjectManager(_projectManager);
        _elementSettingsUIService.GetNoElementSelectedText = () => NoElementSelectedText;
        _elementSettingsUIService.GetSettingsContentPanel = () => SettingsContentPanel;
        _elementSettingsUIService.GetRenameElementButton = () => RenameElementButton;
        _elementSettingsUIService.GetPreviousElementButton = () => PreviousElementButton;
        _elementSettingsUIService.GetNextElementButton = () => NextElementButton;
        _elementSettingsUIService.GetElementTitleText = () => ElementTitleText;
        _elementSettingsUIService.GetSpeedSlider = () => SpeedSlider;
        _elementSettingsUIService.GetOpacitySlider = () => OpacitySlider;
        _elementSettingsUIService.GetVolumeSlider = () => VolumeSlider;
        _elementSettingsUIService.GetScaleSlider = () => ScaleSlider;
        _elementSettingsUIService.GetRotationSlider = () => RotationSlider;
        _elementSettingsUIService.GetSpeedValueText = () => SpeedValueText;
        _elementSettingsUIService.GetOpacityValueText = () => OpacityValueText;
        _elementSettingsUIService.GetVolumeValueText = () => VolumeValueText;
        _elementSettingsUIService.GetScaleValueText = () => ScaleValueText;
        _elementSettingsUIService.GetRotationValueText = () => RotationValueText;
        _elementSettingsUIService.GetSpeedGroupBox = () => SpeedGroupBox;
        _elementSettingsUIService.GetOpacityGroupBox = () => OpacityGroupBox;
        _elementSettingsUIService.GetVolumeGroupBox = () => VolumeGroupBox;
        _elementSettingsUIService.GetTextSettingsGroupBox = () => TextSettingsGroupBox;
        _elementSettingsUIService.GetTextColorComboBox = () => TextColorComboBox;
        _elementSettingsUIService.GetFontFamilyComboBox = () => FontFamilyComboBox;
        _elementSettingsUIService.GetFontSizeSlider = () => FontSizeSlider;
        _elementSettingsUIService.GetFontSizeValueText = () => FontSizeValueText;
        _elementSettingsUIService.GetTextContentTextBox = () => TextContentTextBox;
        _elementSettingsUIService.GetUseManualPositionCheckBox = () => UseManualPositionCheckBox;
        _elementSettingsUIService.GetManualPositionPanel = () => ManualPositionPanel;
        _elementSettingsUIService.GetTextXTextBox = () => TextXTextBox;
        _elementSettingsUIService.GetTextYTextBox = () => TextYTextBox;
        _elementSettingsUIService.GetHideTextButton = () => HideTextButton;
        _elementSettingsUIService.SpeedSlider_ValueChanged = SpeedSlider_ValueChanged;
        _elementSettingsUIService.OpacitySlider_ValueChanged = OpacitySlider_ValueChanged;
        _elementSettingsUIService.VolumeSlider_ValueChanged = VolumeSlider_ValueChanged;
        _elementSettingsUIService.ScaleSlider_ValueChanged = ScaleSlider_ValueChanged;
        _elementSettingsUIService.RotationSlider_ValueChanged = RotationSlider_ValueChanged;
        _elementSettingsUIService.TextColorComboBox_SelectionChanged = TextColorComboBox_SelectionChanged;
        _elementSettingsUIService.FontFamilyComboBox_SelectionChanged = FontFamilyComboBox_SelectionChanged;
        _elementSettingsUIService.FontSizeSlider_ValueChanged = FontSizeSlider_ValueChanged;
        _elementSettingsUIService.TextContentTextBox_TextChanged = TextContentTextBox_TextChanged;
        _elementSettingsUIService.UseManualPositionCheckBox_Checked = UseManualPositionCheckBox_Checked;
        _elementSettingsUIService.UseManualPositionCheckBox_Unchecked = UseManualPositionCheckBox_Unchecked;
        _elementSettingsUIService.TextXTextBox_TextChanged = TextXTextBox_TextChanged;
        _elementSettingsUIService.TextYTextBox_TextChanged = TextYTextBox_TextChanged;
        _elementSettingsUIService.ApplyElementSettings = () => ApplyElementSettings();
        
        // Обновляем делегаты событий в ElementSettingsUIService для использования ElementSettingsEventHandlerService
        _elementSettingsUIService.SpeedSlider_ValueChanged = SpeedSlider_ValueChanged;
        _elementSettingsUIService.OpacitySlider_ValueChanged = OpacitySlider_ValueChanged;
        _elementSettingsUIService.VolumeSlider_ValueChanged = VolumeSlider_ValueChanged;
        _elementSettingsUIService.ScaleSlider_ValueChanged = ScaleSlider_ValueChanged;
        _elementSettingsUIService.RotationSlider_ValueChanged = RotationSlider_ValueChanged;
        _elementSettingsUIService.TextColorComboBox_SelectionChanged = TextColorComboBox_SelectionChanged;
        _elementSettingsUIService.FontFamilyComboBox_SelectionChanged = FontFamilyComboBox_SelectionChanged;
        _elementSettingsUIService.FontSizeSlider_ValueChanged = FontSizeSlider_ValueChanged;
        _elementSettingsUIService.TextContentTextBox_TextChanged = TextContentTextBox_TextChanged;
        _elementSettingsUIService.UseManualPositionCheckBox_Checked = UseManualPositionCheckBox_Checked;
        _elementSettingsUIService.UseManualPositionCheckBox_Unchecked = UseManualPositionCheckBox_Unchecked;
        
        // Настройка GlobalSettingsUIService
        _globalSettingsUIService.SetProjectManager(_projectManager);
        _globalSettingsUIService.SetSettingsManager(_settingsManager);
        _globalSettingsUIService.SetTransitionService(_transitionService);
        _globalSettingsUIService.GetUseGlobalVolumeCheckBox = () => UseGlobalVolumeCheckBox;
        _globalSettingsUIService.GetGlobalVolumeSlider = () => GlobalVolumeSlider;
        _globalSettingsUIService.GetGlobalVolumeValueText = () => GlobalVolumeValueText;
        _globalSettingsUIService.GetUseGlobalOpacityCheckBox = () => UseGlobalOpacityCheckBox;
        _globalSettingsUIService.GetGlobalOpacitySlider = () => GlobalOpacitySlider;
        _globalSettingsUIService.GetGlobalOpacityValueText = () => GlobalOpacityValueText;
        _globalSettingsUIService.GetUseGlobalScaleCheckBox = () => UseGlobalScaleCheckBox;
        _globalSettingsUIService.GetGlobalScaleSlider = () => GlobalScaleSlider;
        _globalSettingsUIService.GetGlobalScaleValueText = () => GlobalScaleValueText;
        _globalSettingsUIService.GetUseGlobalRotationCheckBox = () => UseGlobalRotationCheckBox;
        _globalSettingsUIService.GetGlobalRotationSlider = () => GlobalRotationSlider;
        _globalSettingsUIService.GetGlobalRotationValueText = () => GlobalRotationValueText;
        _globalSettingsUIService.GetTransitionTypeComboBox = () => TransitionTypeComboBox;
        _globalSettingsUIService.GetTransitionDurationSlider = () => TransitionDurationSlider;
        _globalSettingsUIService.GetTransitionDurationValueText = () => TransitionDurationValueText;
        _globalSettingsUIService.GetAutoPlayNextCheckBox = () => AutoPlayNextCheckBox;
        _globalSettingsUIService.GetLoopPlaylistCheckBox = () => LoopPlaylistCheckBox;
        _globalSettingsUIService.UseGlobalVolumeCheckBox_Changed = UseGlobalVolumeCheckBox_Changed;
        _globalSettingsUIService.GlobalVolumeSlider_ValueChanged = GlobalVolumeSlider_ValueChanged;
        _globalSettingsUIService.UseGlobalOpacityCheckBox_Changed = UseGlobalOpacityCheckBox_Changed;
        _globalSettingsUIService.GlobalOpacitySlider_ValueChanged = GlobalOpacitySlider_ValueChanged;
        _globalSettingsUIService.UseGlobalScaleCheckBox_Changed = UseGlobalScaleCheckBox_Changed;
        _globalSettingsUIService.GlobalScaleSlider_ValueChanged = GlobalScaleSlider_ValueChanged;
        _globalSettingsUIService.UseGlobalRotationCheckBox_Changed = UseGlobalRotationCheckBox_Changed;
        _globalSettingsUIService.GlobalRotationSlider_ValueChanged = GlobalRotationSlider_ValueChanged;
        _globalSettingsUIService.TransitionTypeComboBox_SelectionChanged = TransitionTypeComboBox_SelectionChanged;
        _globalSettingsUIService.TransitionDurationSlider_ValueChanged = TransitionDurationSlider_ValueChanged;
        _globalSettingsUIService.AutoPlayNextCheckBox_Changed = AutoPlayNextCheckBox_Changed;
        _globalSettingsUIService.LoopPlaylistCheckBox_Changed = LoopPlaylistCheckBox_Changed;
        _globalSettingsUIService.SaveProject = () => _projectManager.SaveProject();
        _globalSettingsUIService.ApplyGlobalSettings = () => ApplyGlobalSettings();
        _globalSettingsUIService.ApplyElementSettings = () => ApplyElementSettings();
        
        // Настройка ProjectManagementService
        _projectManagementService.SetProjectManager(_projectManager);
        _projectManagementService.SetSettingsManager(_settingsManager);
        _projectManagementService.SetVideoDisplayService(_videoDisplayService);
        _projectManagementService.SetMediaControlService(_mediaControlService);
        _projectManagementService.SetPanelPositionService(_panelPositionService);
        _projectManagementService.SetCurrentMainMedia = (value) => _currentMainMedia = value;
        _projectManagementService.SetCurrentAudioContent = (value) => _currentAudioContent = value;
        _projectManagementService.SetCurrentVisualContent = (value) => _currentVisualContent = value;
        _projectManagementService.SetIsVideoPlaying = (playing) => isVideoPlaying = playing;
        _projectManagementService.SetIsAudioPlaying = (playing) => isAudioPlaying = playing;
        _projectManagementService.StopActiveAudio = () => StopActiveAudio();
        _projectManagementService.ClearAllSlots = () => ClearAllSlots();
        _projectManagementService.UpdateAllSlotButtonsHighlighting = () => UpdateAllSlotButtonsHighlighting();
        _projectManagementService.LoadProjectSlots = () => LoadProjectSlots();
        _projectManagementService.LoadGlobalSettings = () => LoadGlobalSettings();
        _projectManagementService.LoadPanelPositions = () => LoadPanelPositions();
        _projectManagementService.SavePanelPositions = () => SavePanelPositions();
        _projectManagementService.CloseSecondaryScreenWindow = () => CloseSecondaryScreenWindow();
        _projectManagementService.ShowMessage = (message, title) => MessageBox.Show(message, title);
        
        // Настройка MenuService
        _menuService.SetDeviceManager(_deviceManager);
        _menuService.GetScreensMenuItem = () => ScreensMenuItem;
        _menuService.GetAudioMenuItem = () => AudioMenuItem;
        _menuService.GetAudioOutputDevices = () => GetAudioOutputDevices();
        _menuService.OnScreenMenuItemClick = (screenIndex) => ShowScreenSelectionDialog(screenIndex);
        _menuService.OnAudioMenuItemClick = (deviceIndex) => ShowAudioSelectionDialog(deviceIndex);
        
        // Настройка GlobalSettingsEventHandlerService
        _globalSettingsEventHandlerService.SetProjectManager(_projectManager);
        _globalSettingsEventHandlerService.SetTransitionService(_transitionService);
        _globalSettingsEventHandlerService.GetUseGlobalVolumeCheckBox = () => UseGlobalVolumeCheckBox;
        _globalSettingsEventHandlerService.GetGlobalVolumeSlider = () => GlobalVolumeSlider;
        _globalSettingsEventHandlerService.GetGlobalVolumeValueText = () => GlobalVolumeValueText;
        _globalSettingsEventHandlerService.GetUseGlobalOpacityCheckBox = () => UseGlobalOpacityCheckBox;
        _globalSettingsEventHandlerService.GetGlobalOpacitySlider = () => GlobalOpacitySlider;
        _globalSettingsEventHandlerService.GetGlobalOpacityValueText = () => GlobalOpacityValueText;
        _globalSettingsEventHandlerService.GetUseGlobalScaleCheckBox = () => UseGlobalScaleCheckBox;
        _globalSettingsEventHandlerService.GetGlobalScaleSlider = () => GlobalScaleSlider;
        _globalSettingsEventHandlerService.GetGlobalScaleValueText = () => GlobalScaleValueText;
        _globalSettingsEventHandlerService.GetUseGlobalRotationCheckBox = () => UseGlobalRotationCheckBox;
        _globalSettingsEventHandlerService.GetGlobalRotationSlider = () => GlobalRotationSlider;
        _globalSettingsEventHandlerService.GetGlobalRotationValueText = () => GlobalRotationValueText;
        _globalSettingsEventHandlerService.GetTransitionTypeComboBox = () => TransitionTypeComboBox;
        _globalSettingsEventHandlerService.GetTransitionDurationSlider = () => TransitionDurationSlider;
        _globalSettingsEventHandlerService.GetTransitionDurationValueText = () => TransitionDurationValueText;
        _globalSettingsEventHandlerService.GetAutoPlayNextCheckBox = () => AutoPlayNextCheckBox;
        _globalSettingsEventHandlerService.GetLoopPlaylistCheckBox = () => LoopPlaylistCheckBox;
        _globalSettingsEventHandlerService.GetSelectedElementSlot = () => _selectedElementSlot;
        _globalSettingsEventHandlerService.GetSelectedElementKey = () => _selectedElementKey;
        _globalSettingsEventHandlerService.ApplyGlobalSettings = () => ApplyGlobalSettings();
        _globalSettingsEventHandlerService.ApplyElementSettings = () => ApplyElementSettings();
        _globalSettingsEventHandlerService.SaveProject = () => _projectManager.SaveProject();
        
        // Настройка ElementSettingsEventHandlerService
        _elementSettingsEventHandlerService.GetSelectedElementSlot = () => _selectedElementSlot;
        _elementSettingsEventHandlerService.GetSelectedElementKey = () => _selectedElementKey;
        _elementSettingsEventHandlerService.GetSpeedSlider = () => SpeedSlider;
        _elementSettingsEventHandlerService.GetSpeedValueText = () => SpeedValueText;
        _elementSettingsEventHandlerService.GetOpacitySlider = () => OpacitySlider;
        _elementSettingsEventHandlerService.GetOpacityValueText = () => OpacityValueText;
        _elementSettingsEventHandlerService.GetVolumeSlider = () => VolumeSlider;
        _elementSettingsEventHandlerService.GetVolumeValueText = () => VolumeValueText;
        _elementSettingsEventHandlerService.GetScaleSlider = () => ScaleSlider;
        _elementSettingsEventHandlerService.GetScaleValueText = () => ScaleValueText;
        _elementSettingsEventHandlerService.GetRotationSlider = () => RotationSlider;
        _elementSettingsEventHandlerService.GetRotationValueText = () => RotationValueText;
        _elementSettingsEventHandlerService.GetHideTextButton = () => HideTextButton;
        _elementSettingsEventHandlerService.GetTextColorComboBox = () => TextColorComboBox;
        _elementSettingsEventHandlerService.GetFontFamilyComboBox = () => FontFamilyComboBox;
        _elementSettingsEventHandlerService.GetFontSizeSlider = () => FontSizeSlider;
        _elementSettingsEventHandlerService.GetFontSizeValueText = () => FontSizeValueText;
        _elementSettingsEventHandlerService.GetTextContentTextBox = () => TextContentTextBox;
        _elementSettingsEventHandlerService.GetUseManualPositionCheckBox = () => UseManualPositionCheckBox;
        _elementSettingsEventHandlerService.GetManualPositionPanel = () => ManualPositionPanel;
        _elementSettingsEventHandlerService.GetTextXTextBox = () => TextXTextBox;
        _elementSettingsEventHandlerService.GetTextYTextBox = () => TextYTextBox;
        _elementSettingsEventHandlerService.GetElementPlayButton = () => ElementPlayButton;
        _elementSettingsEventHandlerService.ApplyElementSettings = () => ApplyElementSettings();
        _elementSettingsEventHandlerService.ApplyTextSettings = () => ApplyTextSettings();
        _elementSettingsEventHandlerService.UpdateElementTitle = () => UpdateElementTitle();
        _elementSettingsEventHandlerService.PlayElement = (slot, key) => _elementControlService.PlayElement(slot, key);
        _elementSettingsEventHandlerService.StopElement = (slot, key) => _elementControlService.StopElement(slot, key);
        _elementSettingsEventHandlerService.RestartElement = async (slot, key) => await _elementControlService.RestartElement(slot, key);
    }
    
    // Инициализация меню экранов
    private void InitializeScreensMenu()
    {
        _menuService.InitializeScreensMenu();
    }
    
    // Инициализация меню звука
    private void InitializeAudioMenu()
    {
        _menuService.InitializeAudioMenu();
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
    
    // Диалог выбора экрана
    private void ShowScreenSelectionDialog(int screenIndex)
    {
        _dialogService.ShowScreenSelectionDialog(screenIndex);
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
        _menuService.InitializeScreensMenu();
        _menuService.InitializeAudioMenu();
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
                                
                            }
                            else
                            {
                                // Видео воспроизводится - ставим на паузу
                                mediaElement.Pause();
                                SyncPauseWithSecondaryScreen();
                                _isVideoPaused = true;
                                
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
                                }
                                else
                                {
                                    // Аудио воспроизводится - ставим на паузу
                                    audioElement.Pause();
                                    _mediaStateService.SetAudioPaused(slotKey, true);
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
                        System.Diagnostics.Debug.WriteLine($"Slot_Click: Запускаем слот {column}-{row}, Type={mediaSlot.Type}, Path={mediaSlot.MediaPath}");
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

    private async void LoadMediaFromSlotSelective(MediaSlot mediaSlot)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow.LoadMediaFromSlotSelective: НАЧАЛО, Type={mediaSlot.Type}, Path={mediaSlot.MediaPath}");
            await _mediaPlayerService.LoadMediaFromSlotSelective(mediaSlot);
            System.Diagnostics.Debug.WriteLine($"MainWindow.LoadMediaFromSlotSelective: ЗАВЕРШЕНО");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow.LoadMediaFromSlotSelective: ОШИБКА - {ex.Message}");
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
        return _mediaTypeService.GetMediaType(filePath);
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
        return _mediaTypeService.IsMediaTypeCompatible(newType);
    }

    /// <summary>
    /// Получает текущий тип воспроизводимого медиа
    /// </summary>
    private MediaType? GetCurrentMediaType()
    {
        return _mediaTypeService.GetCurrentMediaType();
    }

    /// <summary>
    /// Возвращает читаемое название типа медиа
    /// </summary>
    private string GetMediaTypeName(MediaType? mediaType)
    {
        return _mediaTypeService.GetMediaTypeName(mediaType);
    }

    private TextAlignment GetTextAlignment(string position)
    {
        return _textBlockService.GetTextAlignment(position);
    }

    private VerticalAlignment GetVerticalAlignment(string position)
    {
        return _textBlockService.GetVerticalAlignment(position);
    }

    private HorizontalAlignment GetHorizontalAlignment(string position)
    {
        return _textBlockService.GetHorizontalAlignment(position);
    }

    /// <summary>
    /// Проверяет, нужно ли блокировать запуск медиафайла (только для аудио)
    /// </summary>
    private bool ShouldBlockMediaFile(string mediaPath, MediaType mediaType, string? currentSlotKey = null)
    {
        return _mediaTypeService.ShouldBlockMediaFile(mediaPath, mediaType, currentSlotKey);
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


    // Остановить все активные аудио (используется через MediaControlService)
    private void StopActiveAudio()
    {
        _mediaControlService.StopActiveAudio();
    }
    
    // Остановить аудио в слоте (используется через ElementControlService, но оставляем для обратной совместимости)
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

    private void LoadProjectSlots()
    {
        _slotUIService.LoadProjectSlots();
    }
    
    /// <summary>
    /// Очищает все слоты, удаляя иконки и сбрасывая их состояние
    /// </summary>
    private void ClearAllSlots()
    {
        _slotUIService.ClearAllSlots();
    }


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
                                
                            }
                            else
                            {
                                // Видео воспроизводится - ставим на паузу
                                mediaElement.Pause();
                                SyncPauseWithSecondaryScreen();
                                _isVideoPaused = true;
                                
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
                                }
                                else
                                {
                                    // Аудио воспроизводится - ставим на паузу
                                    audioElement.Pause();
                                    _mediaStateService.SetAudioPaused(slotKey, true);
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
        _sliderService.OnVideoSliderValueChanged(sender, e);
    }

    private void VideoSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _sliderService.OnVideoSliderMouseLeftButtonDown(sender, e);
    }
    
    private void VideoSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _sliderService.OnVideoSliderMouseLeftButtonUp(sender, e);
    }
    
    /// <summary>
    /// Обработчик начала перетаскивания слайдера аудио
    /// </summary>
    private void AudioSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _sliderService.OnAudioSliderMouseLeftButtonDown(sender, e);
    }
    
    /// <summary>
    /// Обработчик окончания перетаскивания слайдера аудио
    /// </summary>
    private void AudioSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _sliderService.OnAudioSliderMouseLeftButtonUp(sender, e);
    }
    
    /// <summary>
    /// Обработчик изменения значения слайдера аудио
    /// </summary>
    private void AudioSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _sliderService.OnAudioSliderValueChanged(sender, e);
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
        _projectManagementService.NewProject();
    }

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        _projectManagementService.OpenProject();
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        _projectManagementService.SaveProject();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _projectManagementService.OnWindowClosing();
    }
    
    
    // Событие handlers для панели настроек элемента
    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _elementSettingsEventHandlerService.OnSpeedSliderValueChanged(sender, e);
    }
    
    private void SpeedSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _elementSettingsEventHandlerService.OnSpeedSliderMouseLeftButtonUp(sender, e);
    }

    private void SpeedPreset_Click(object sender, RoutedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnSpeedPresetClick(sender, e);
    }
    
    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _elementSettingsEventHandlerService.OnOpacitySliderValueChanged(sender, e);
    }
    
    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _elementSettingsEventHandlerService.OnVolumeSliderValueChanged(sender, e);
    }
    
    private void VolumePreset_Click(object sender, RoutedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnVolumePresetClick(sender, e);
    }
    
    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _elementSettingsEventHandlerService.OnScaleSliderValueChanged(sender, e);
    }
    
    private void ScalePreset_Click(object sender, RoutedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnScalePresetClick(sender, e);
    }
    
    private void RotationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _elementSettingsEventHandlerService.OnRotationSliderValueChanged(sender, e);
    }
    
    private void RotationPreset_Click(object sender, RoutedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnRotationPresetClick(sender, e);
    }
    
    // Обработчики для настроек текста
    private void HideTextButton_Click(object sender, RoutedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnHideTextButtonClick(sender, e);
    }
    
    private void TextColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnTextColorComboBoxSelectionChanged(sender, e);
    }
    
    private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnFontFamilyComboBoxSelectionChanged(sender, e);
    }
    
    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _elementSettingsEventHandlerService.OnFontSizeSliderValueChanged(sender, e);
    }
    
    private void TextContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnTextContentTextBoxTextChanged(sender, e);
    }
    
    private void UseManualPositionCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnUseManualPositionCheckBoxChecked(sender, e);
    }
    
    private void UseManualPositionCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnUseManualPositionCheckBoxUnchecked(sender, e);
    }
    
    private void TextXTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnTextXTextBoxTextChanged(sender, e);
    }
    
    private void TextYTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnTextYTextBoxTextChanged(sender, e);
    }
    
    // Применить настройки текста к отображаемому элементу
    private void ApplyTextSettings()
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        
        _textBlockService.ApplyTextSettingsFromSlot(_selectedElementSlot, textOverlayGrid);
    }
    
    public void ElementPlay_Click(object sender, RoutedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnElementPlayClick(sender, e);
    }
    
    private void ElementStop_Click(object sender, RoutedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnElementStopClick(sender, e);
    }
    
    public void ElementRestart_Click(object sender, RoutedEventArgs e)
    {
        _elementSettingsEventHandlerService.OnElementRestartClick(sender, e);
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
        _elementSettingsEventHandlerService.OnRenameElementButtonClick(sender, e);
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
        _globalSettingsEventHandlerService.OnUseGlobalVolumeCheckBoxChanged(sender, e);
    }
    
    private void GlobalVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _globalSettingsEventHandlerService.OnGlobalVolumeSliderValueChanged(sender, e);
    }
    
    private void GlobalVolumePreset_Click(object sender, RoutedEventArgs e)
    {
        _globalSettingsEventHandlerService.OnGlobalVolumePresetClick(sender, e);
    }
    
    private void UseGlobalScaleCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _globalSettingsEventHandlerService.OnUseGlobalScaleCheckBoxChanged(sender, e);
    }
    
    private void GlobalScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _globalSettingsEventHandlerService.OnGlobalScaleSliderValueChanged(sender, e);
    }
    
    private void GlobalScalePreset_Click(object sender, RoutedEventArgs e)
    {
        _globalSettingsEventHandlerService.OnGlobalScalePresetClick(sender, e);
    }
    
    private void UseGlobalRotationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _globalSettingsEventHandlerService.OnUseGlobalRotationCheckBoxChanged(sender, e);
    }
    
    private void GlobalRotationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _globalSettingsEventHandlerService.OnGlobalRotationSliderValueChanged(sender, e);
    }
    
    private void GlobalRotationPreset_Click(object sender, RoutedEventArgs e)
    {
        _globalSettingsEventHandlerService.OnGlobalRotationPresetClick(sender, e);
    }
    
    private void UseGlobalOpacityCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _globalSettingsEventHandlerService.OnUseGlobalOpacityCheckBoxChanged(sender, e);
    }
    
    private void GlobalOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _globalSettingsEventHandlerService.OnGlobalOpacitySliderValueChanged(sender, e);
    }
    
    private void TransitionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _globalSettingsEventHandlerService.OnTransitionTypeComboBoxSelectionChanged(sender, e);
    }
    
    private void TransitionDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _globalSettingsEventHandlerService.OnTransitionDurationSliderValueChanged(sender, e);
    }
    
    private void AutoPlayNextCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _globalSettingsEventHandlerService.OnAutoPlayNextCheckBoxChanged(sender, e);
    }
    
    private void LoopPlaylistCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _globalSettingsEventHandlerService.OnLoopPlaylistCheckBoxChanged(sender, e);
    }
    
    // Выбрать элемент для настройки
    public void SelectElementForSettings(MediaSlot slot, string slotKey)
    {
        _selectedElementSlot = slot;
        _selectedElementKey = slotKey;
        _elementSettingsUIService.SelectElementForSettings(slot, slotKey);
    }
    
    // Снять выбор элемента
    public void UnselectElement()
    {
        _selectedElementSlot = null;
        _selectedElementKey = null;
        _elementSettingsUIService.UnselectElement();
    }
    
    // Загрузить настройки элемента в UI
    private void LoadElementSettings()
    {
        if (_selectedElementSlot == null) return;
        _elementSettingsUIService.LoadElementSettings(_selectedElementSlot);
    }
    
    // Загрузить настройки текста в UI
    private void LoadTextSettings()
    {
        if (_selectedElementSlot == null || _selectedElementSlot.Type != MediaType.Text) return;
        _elementSettingsUIService.LoadTextSettings(_selectedElementSlot);
    }
    
    // Обновить заголовок элемента
    private void UpdateElementTitle()
    {
        if (_selectedElementSlot != null)
        {
            _elementSettingsUIService.UpdateElementTitle(_selectedElementSlot);
        }
    }
    
    // Применить настройки элемента к активным медиа
    public void ApplyElementSettings(MediaSlot slot, string slotKey)
    {
        _elementSettingsService.ApplyElementSettings(slot, slotKey);
    }
    
    private void ApplyElementSettings()
    {
        if (_selectedElementSlot == null || string.IsNullOrEmpty(_selectedElementKey)) return;
        _elementSettingsService.ApplyElementSettings(_selectedElementSlot, _selectedElementKey);
    }
    
    // Применить общие настройки ко всем активным медиа элементам
    public void ApplyGlobalSettings()
    {
        _elementSettingsService.ApplyGlobalSettings();
    }
    
    // Получить финальную громкость с учетом общих настроек
    private double GetFinalVolume(double personalVolume)
    {
        return _settingsManager.GetFinalVolume(personalVolume);
    }
    
    // Получить финальную прозрачность с учетом общих настроек
    private double GetFinalOpacity(double personalOpacity)
    {
        return _settingsManager.GetFinalOpacity(personalOpacity);
    }
    
    // Получить финальный масштаб с учетом общих настроек
    private double GetFinalScale(double personalScale)
    {
        return _settingsManager.GetFinalScale(personalScale);
    }
    
    // Получить финальный поворот с учетом общих настроек
    private double GetFinalRotation(double personalRotation)
    {
        return _settingsManager.GetFinalRotation(personalRotation);
    }
    
    // Применить масштаб и поворот к элементу с правильным центром
    private void ApplyScaleAndRotation(FrameworkElement element, double scale, double rotation)
    {
        _settingsManager.ApplyScaleAndRotation(element, scale, rotation);
    }
    
    // Загрузить общие настройки в UI
    private void LoadGlobalSettings()
    {
        _globalSettingsUIService.LoadGlobalSettings();
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
    
    // Обработчики для перетаскивания панели настроек элемента
    private void ElementSettingsBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _elementSettingsDragService.HandleMouseLeftButtonDown(sender, e, false, Services.ResizeType.None);
    }
    
    private void ElementSettingsBorder_MouseMove(object sender, MouseEventArgs e)
    {
        _elementSettingsDragService.HandleMouseMove(sender, e);
    }
    
    private void ElementSettingsBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _elementSettingsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    // Обработчики для изменения размера панели настроек элемента
    private void ElementSettingsResizeHandleV_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _elementSettingsDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Vertical);
    }
    
    private void ElementSettingsResizeHandleV_MouseMove(object sender, MouseEventArgs e)
    {
        _elementSettingsDragService.HandleMouseMove(sender, e);
    }
    
    private void ElementSettingsResizeHandleV_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _elementSettingsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    private void ElementSettingsResizeHandleH_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _elementSettingsDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Horizontal);
    }
    
    private void ElementSettingsResizeHandleH_MouseMove(object sender, MouseEventArgs e)
    {
        _elementSettingsDragService.HandleMouseMove(sender, e);
    }
    
    private void ElementSettingsResizeHandleH_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _elementSettingsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    private void ElementSettingsResizeHandleD_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _elementSettingsDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Diagonal);
    }
    
    private void ElementSettingsResizeHandleD_MouseMove(object sender, MouseEventArgs e)
    {
        _elementSettingsDragService.HandleMouseMove(sender, e);
    }
    
    private void ElementSettingsResizeHandleD_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _elementSettingsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    // Обработчики для перетаскивания панели общих настроек
    private void GlobalSettingsBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _globalSettingsDragService.HandleMouseLeftButtonDown(sender, e, false, Services.ResizeType.None);
    }
    
    private void GlobalSettingsBorder_MouseMove(object sender, MouseEventArgs e)
    {
        _globalSettingsDragService.HandleMouseMove(sender, e);
    }
    
    private void GlobalSettingsBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _globalSettingsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    // Обработчики для изменения размера панели общих настроек
    private void GlobalSettingsResizeHandleV_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _globalSettingsDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Vertical);
    }
    
    private void GlobalSettingsResizeHandleV_MouseMove(object sender, MouseEventArgs e)
    {
        _globalSettingsDragService.HandleMouseMove(sender, e);
    }
    
    private void GlobalSettingsResizeHandleV_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _globalSettingsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    private void GlobalSettingsResizeHandleH_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _globalSettingsDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Horizontal);
    }
    
    private void GlobalSettingsResizeHandleH_MouseMove(object sender, MouseEventArgs e)
    {
        _globalSettingsDragService.HandleMouseMove(sender, e);
    }
    
    private void GlobalSettingsResizeHandleH_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _globalSettingsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    private void GlobalSettingsResizeHandleD_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _globalSettingsDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Diagonal);
    }
    
    private void GlobalSettingsResizeHandleD_MouseMove(object sender, MouseEventArgs e)
    {
        _globalSettingsDragService.HandleMouseMove(sender, e);
    }
    
    private void GlobalSettingsResizeHandleD_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _globalSettingsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    // Обработчики для перетаскивания медиаплеера
    private void MediaPlayerBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseLeftButtonDown(sender, e, false, Services.ResizeType.None);
    }
    
    private void MediaPlayerBorder_MouseMove(object sender, MouseEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseMove(sender, e);
    }
    
    private void MediaPlayerBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    // Обработчики для изменения размера медиаплеера
    private void MediaPlayerResizeHandleV_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Vertical);
    }
    
    private void MediaPlayerResizeHandleV_MouseMove(object sender, MouseEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseMove(sender, e);
    }
    
    private void MediaPlayerResizeHandleV_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    private void MediaPlayerResizeHandleH_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Horizontal);
    }
    
    private void MediaPlayerResizeHandleH_MouseMove(object sender, MouseEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseMove(sender, e);
    }
    
    private void MediaPlayerResizeHandleH_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    private void MediaPlayerResizeHandleD_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Diagonal);
    }
    
    private void MediaPlayerResizeHandleD_MouseMove(object sender, MouseEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseMove(sender, e);
    }
    
    private void MediaPlayerResizeHandleD_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _mediaPlayerDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    // Обработчики для перетаскивания панели медиа-клеток
    private void MediaCellsBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _mediaCellsDragService.HandleMouseLeftButtonDown(sender, e, false, Services.ResizeType.None);
    }
    
    private void MediaCellsBorder_MouseMove(object sender, MouseEventArgs e)
    {
        _mediaCellsDragService.HandleMouseMove(sender, e);
    }
    
    private void MediaCellsBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _mediaCellsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    // Обработчики для изменения размера панели медиа-клеток
    private void MediaCellsResizeHandleV_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _mediaCellsDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Vertical);
    }
    
    private void MediaCellsResizeHandleV_MouseMove(object sender, MouseEventArgs e)
    {
        _mediaCellsDragService.HandleMouseMove(sender, e);
    }
    
    private void MediaCellsResizeHandleV_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _mediaCellsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    private void MediaCellsResizeHandleH_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _mediaCellsDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Horizontal);
    }
    
    private void MediaCellsResizeHandleH_MouseMove(object sender, MouseEventArgs e)
    {
        _mediaCellsDragService.HandleMouseMove(sender, e);
    }
    
    private void MediaCellsResizeHandleH_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _mediaCellsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    private void MediaCellsResizeHandleD_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _mediaCellsDragService.HandleMouseLeftButtonDown(sender, e, true, Services.ResizeType.Diagonal);
    }
    
    private void MediaCellsResizeHandleD_MouseMove(object sender, MouseEventArgs e)
    {
        _mediaCellsDragService.HandleMouseMove(sender, e);
    }
    
    private void MediaCellsResizeHandleD_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _mediaCellsDragService.HandleMouseLeftButtonUp(sender, e);
    }
    
    // Обработчики MouseEnter/MouseLeave для визуальной обратной связи (используются напрямую из PanelDragService)
    private void ResizeHandle_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.Opacity = 1.0;
        }
    }
    
    private void ResizeHandle_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.Opacity = 0.7;
        }
    }
    
    // Обработчики для ElementSettings ResizeHandle
    private void ElementSettingsResizeHandleV_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void ElementSettingsResizeHandleV_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    private void ElementSettingsResizeHandleH_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void ElementSettingsResizeHandleH_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    private void ElementSettingsResizeHandleD_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void ElementSettingsResizeHandleD_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    
    // Обработчики для GlobalSettings ResizeHandle
    private void GlobalSettingsResizeHandleV_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void GlobalSettingsResizeHandleV_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    private void GlobalSettingsResizeHandleH_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void GlobalSettingsResizeHandleH_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    private void GlobalSettingsResizeHandleD_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void GlobalSettingsResizeHandleD_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    
    // Обработчики для MediaPlayer ResizeHandle
    private void MediaPlayerResizeHandleV_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void MediaPlayerResizeHandleV_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    private void MediaPlayerResizeHandleH_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void MediaPlayerResizeHandleH_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    private void MediaPlayerResizeHandleD_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void MediaPlayerResizeHandleD_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    
    // Обработчики для MediaCells ResizeHandle
    private void MediaCellsResizeHandleV_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void MediaCellsResizeHandleV_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    private void MediaCellsResizeHandleH_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void MediaCellsResizeHandleH_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    private void MediaCellsResizeHandleD_MouseEnter(object sender, MouseEventArgs e) => ResizeHandle_MouseEnter(sender, e);
    private void MediaCellsResizeHandleD_MouseLeave(object sender, MouseEventArgs e) => ResizeHandle_MouseLeave(sender, e);
    
    // Методы для сохранения и загрузки позиций панелей
    private void SavePanelPositions()
    {
        _panelPositionService.SavePanelPositions();
    }
    
    private void LoadPanelPositions()
    {
        _panelPositionService.LoadPanelPositions();
    }
}
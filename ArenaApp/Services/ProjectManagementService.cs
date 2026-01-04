using System;
using System.Windows;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления проектами (создание, открытие, сохранение)
    /// </summary>
    public class ProjectManagementService
    {
        private ProjectManager? _projectManager;
        private SettingsManager? _settingsManager;
        private VideoDisplayService? _videoDisplayService;
        private MediaControlService? _mediaControlService;
        private PanelPositionService? _panelPositionService;
        
        // Делегаты для работы с состоянием
        public Action<string?>? SetCurrentMainMedia { get; set; }
        public Action<string?>? SetCurrentAudioContent { get; set; }
        public Action<string?>? SetCurrentVisualContent { get; set; }
        public Action<bool>? SetIsVideoPlaying { get; set; }
        public Action<bool>? SetIsAudioPlaying { get; set; }
        
        // Делегаты для работы с UI
        public Action? StopActiveAudio { get; set; }
        public Action? ClearAllSlots { get; set; }
        public Action? UpdateAllSlotButtonsHighlighting { get; set; }
        public Action? LoadProjectSlots { get; set; }
        public Action? LoadGlobalSettings { get; set; }
        public Action? LoadPanelPositions { get; set; }
        public Action? SavePanelPositions { get; set; }
        public Action? CloseSecondaryScreenWindow { get; set; }
        public Action? UpdateStorageModeControlsAvailability { get; set; }
        public Action? SaveSelectedStorageModeToProject { get; set; }
        public Action? ApplyStorageModeFromProject { get; set; }
        public Action? LoadOutputSettings { get; set; }
        public Action? SaveOutputSettings { get; set; }
        
        // Делегаты для показа сообщений
        public Action<string, string>? ShowMessage { get; set; }
        
        public void SetProjectManager(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        
        public void SetSettingsManager(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }
        
        public void SetVideoDisplayService(VideoDisplayService videoDisplayService)
        {
            _videoDisplayService = videoDisplayService;
        }
        
        public void SetMediaControlService(MediaControlService mediaControlService)
        {
            _mediaControlService = mediaControlService;
        }
        
        public void SetPanelPositionService(PanelPositionService panelPositionService)
        {
            _panelPositionService = panelPositionService;
        }
        
        /// <summary>
        /// Создает новый проект
        /// </summary>
        public void NewProject()
        {
            if (_projectManager == null) return;
            
            // Останавливаем текущее воспроизведение медиа
            _mediaControlService?.StopMedia();
            _mediaControlService?.CloseMedia();
            StopActiveAudio?.Invoke();
            
            // Очищаем состояние медиа
            SetCurrentMainMedia?.Invoke(null);
            SetCurrentAudioContent?.Invoke(null);
            SetCurrentVisualContent?.Invoke(null);
            SetIsVideoPlaying?.Invoke(false);
            SetIsAudioPlaying?.Invoke(false);
            
            // Очищаем медиа элементы
            _videoDisplayService?.ClearMediaElements();
            
            // Создаем новый проект
            _projectManager.NewProject();
            
            // Очищаем все слоты
            ClearAllSlots?.Invoke();
            
            // Обновляем подсветку кнопок
            UpdateAllSlotButtonsHighlighting?.Invoke();
            
            // Загружаем позиции панелей по умолчанию
            LoadPanelPositions?.Invoke();
            
            // Разблокируем переключатели режима хранения для нового проекта
            UpdateStorageModeControlsAvailability?.Invoke();
            
            ShowMessage?.Invoke("Новый проект создан", "Информация");
        }
        
        /// <summary>
        /// Открывает существующий проект
        /// </summary>
        public void OpenProject()
        {
            if (_projectManager == null || _settingsManager == null) return;
            
            if (_projectManager.OpenProject())
            {
                // Обновляем SettingsManager с новыми настройками
                _settingsManager.SetGlobalSettings(_projectManager.CurrentProject.GlobalSettings);
                LoadProjectSlots?.Invoke();
                LoadGlobalSettings?.Invoke();
                LoadPanelPositions?.Invoke(); // Загружаем сохраненные позиции панелей
                LoadOutputSettings?.Invoke(); // Загружаем настройки вывода
                // Блокируем переключатели режима хранения, если проект уже сохранен
                UpdateStorageModeControlsAvailability?.Invoke();
                ShowMessage?.Invoke("Проект загружен", "Информация");
            }
        }
        
        /// <summary>
        /// Сохраняет текущий проект
        /// </summary>
        public void SaveProject()
        {
            if (_projectManager == null) return;
            
            // Сохраняем выбранный режим хранения из UI в проект перед сохранением
            SaveSelectedStorageModeToProject?.Invoke();
            
            SavePanelPositions?.Invoke(); // Сохраняем текущие позиции панелей
            SaveOutputSettings?.Invoke(); // Сохраняем настройки вывода
            if (_projectManager.SaveProject())
            {
                ShowMessage?.Invoke("Проект сохранен", "Информация");
                
                // Применяем режим из проекта в UI после сохранения
                ApplyStorageModeFromProject?.Invoke();
                
                // Блокируем переключатели режима хранения после сохранения
                UpdateStorageModeControlsAvailability?.Invoke();
            }
        }
        
        /// <summary>
        /// Выполняет действия при закрытии окна
        /// </summary>
        public void OnWindowClosing()
        {
            // Сохраняем позиции панелей перед закрытием
            SavePanelPositions?.Invoke();
            
            // Закрываем окно вывода на второй монитор при закрытии приложения
            CloseSecondaryScreenWindow?.Invoke();
        }
    }
}


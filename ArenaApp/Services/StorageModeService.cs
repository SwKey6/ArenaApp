using System;
using System.Windows;
using System.Windows.Controls;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления режимом хранения медиафайлов
    /// </summary>
    public class StorageModeService
    {
        private ProjectManager? _projectManager;
        
        // Делегаты для доступа к UI элементам
        public Func<RadioButton>? GetStorageModePathsRadio { get; set; }
        public Func<RadioButton>? GetStorageModeEmbeddedRadio { get; set; }
        public Func<TextBlock>? GetStorageModeInfoText { get; set; }
        
        // Делегат для проверки инициализации UI
        public Func<bool>? GetIsStorageModeUiInitializing { get; set; }
        public Action<bool>? SetIsStorageModeUiInitializing { get; set; }
        
        public void SetProjectManager(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        
        /// <summary>
        /// Сохраняет текущий выбранный режим из UI в проект перед сохранением
        /// </summary>
        public void SaveSelectedStorageModeToProject()
        {
            if (_projectManager == null) return;
            
            var pathsRadio = GetStorageModePathsRadio?.Invoke();
            var embeddedRadio = GetStorageModeEmbeddedRadio?.Invoke();
            
            if (pathsRadio == null || embeddedRadio == null) return;
            
            // Сохраняем выбранный режим из UI в проект
            if (pathsRadio.IsChecked == true)
            {
                _projectManager.CurrentProject.GlobalSettings.StorageMode = StorageMode.Paths;
                System.Diagnostics.Debug.WriteLine("SaveSelectedStorageModeToProject: Сохранен режим Paths");
            }
            else if (embeddedRadio.IsChecked == true)
            {
                _projectManager.CurrentProject.GlobalSettings.StorageMode = StorageMode.Embedded;
                System.Diagnostics.Debug.WriteLine("SaveSelectedStorageModeToProject: Сохранен режим Embedded");
            }
        }
        
        /// <summary>
        /// Применяет режим из проекта в UI после сохранения
        /// </summary>
        public void ApplyStorageModeFromProject()
        {
            if (_projectManager == null) return;
            
            var pathsRadio = GetStorageModePathsRadio?.Invoke();
            var embeddedRadio = GetStorageModeEmbeddedRadio?.Invoke();
            var infoText = GetStorageModeInfoText?.Invoke();
            
            if (pathsRadio == null || embeddedRadio == null) return;
            
            // Применяем режим из проекта в UI
            var storageMode = _projectManager.CurrentProject.GlobalSettings.StorageMode;
            
            // Устанавливаем флаг инициализации, чтобы не вызывать обработчики событий
            if (GetIsStorageModeUiInitializing != null && SetIsStorageModeUiInitializing != null)
            {
                SetIsStorageModeUiInitializing(true);
            }
            
            try
            {
                if (storageMode == StorageMode.Paths)
                {
                    pathsRadio.IsChecked = true;
                    embeddedRadio.IsChecked = false;
                    if (infoText != null)
                    {
                        infoText.Text = "Текущий режим: Пути к файлам (экономно)";
                    }
                }
                else
                {
                    pathsRadio.IsChecked = false;
                    embeddedRadio.IsChecked = true;
                    if (infoText != null)
                    {
                        infoText.Text = "Текущий режим: Копировать в папку проекта (автономно)";
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"ApplyStorageModeFromProject: Применен режим {storageMode}");
            }
            finally
            {
                if (SetIsStorageModeUiInitializing != null)
                {
                    SetIsStorageModeUiInitializing(false);
                }
            }
        }
        
        /// <summary>
        /// Блокирует или разблокирует переключатели режима хранения в зависимости от того, сохранен ли проект
        /// </summary>
        public void UpdateStorageModeControlsAvailability()
        {
            var pathsRadio = GetStorageModePathsRadio?.Invoke();
            var embeddedRadio = GetStorageModeEmbeddedRadio?.Invoke();
            
            if (pathsRadio == null || embeddedRadio == null) return;
            
            // Блокируем переключатели, если проект уже сохранен
            bool isProjectSaved = _projectManager?.HasProjectFilePath ?? false;
            pathsRadio.IsEnabled = !isProjectSaved;
            embeddedRadio.IsEnabled = !isProjectSaved;
            
            if (isProjectSaved)
            {
                var infoText = GetStorageModeInfoText?.Invoke();
                if (infoText != null && _projectManager != null)
                {
                    var storageMode = _projectManager.CurrentProject.GlobalSettings.StorageMode;
                    string modeText = storageMode == StorageMode.Paths 
                        ? "Пути к файлам (экономно)" 
                        : "Копировать в папку проекта (автономно)";
                    infoText.Text = $"Текущий режим: {modeText} (заблокировано после сохранения)";
                }
            }
        }
        
        /// <summary>
        /// Обрабатывает выбор режима хранения медиафайлов
        /// </summary>
        public void HandleStorageModeRadioChecked(object sender, RoutedEventArgs e)
        {
            // Не реагируем на события, которые были вызваны программной установкой IsChecked при старте
            if (GetIsStorageModeUiInitializing?.Invoke() == true)
            {
                return;
            }

            // Если проект уже сохранен, блокируем изменение режима
            if (_projectManager?.HasProjectFilePath == true)
            {
                // Возвращаем переключатель в исходное состояние
                var currentMode = _projectManager.CurrentProject.GlobalSettings.StorageMode;
                var pathsRadio = GetStorageModePathsRadio?.Invoke();
                var embeddedRadio = GetStorageModeEmbeddedRadio?.Invoke();
                
                if (pathsRadio != null && embeddedRadio != null)
                {
                    if (currentMode == StorageMode.Paths)
                    {
                        pathsRadio.IsChecked = true;
                        embeddedRadio.IsChecked = false;
                    }
                    else
                    {
                        pathsRadio.IsChecked = false;
                        embeddedRadio.IsChecked = true;
                    }
                }
                
                MessageBox.Show(
                    "Режим хранения нельзя изменить после сохранения проекта.\n" +
                    "Создайте новый проект, чтобы выбрать другой режим.",
                    "Режим заблокирован",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (sender is not RadioButton radioButton || radioButton.Tag is not string modeString)
            {
                return;
            }
            
            if (!Enum.TryParse<StorageMode>(modeString, out var storageMode))
            {
                return;
            }
            
            if (_projectManager == null) return;
            
            // Сохраняем выбранный режим в настройках проекта
            _projectManager.CurrentProject.GlobalSettings.StorageMode = storageMode;
            
            // Если пользователь выбрал Embedded, но проект еще не сохранен — показываем предупреждение
            // но разрешаем выбор (не сбрасываем обратно)
            if (storageMode == StorageMode.Embedded && !_projectManager.HasProjectFilePath)
            {
                var infoText = GetStorageModeInfoText?.Invoke();
                if (infoText != null)
                {
                    infoText.Text = "Текущий режим: Копировать в папку проекта (автономно) - сохраните проект для применения";
                }
                
                MessageBox.Show(
                    "Режим \"Копировать в папку проекта\" будет применен после сохранения проекта.\n" +
                    "Сохраните проект (Файл → Сохранить проект как), чтобы активировать этот режим.",
                    "Режим выбран",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return; // Выходим, чтобы не обновлять текст еще раз
            }
            
            // Обновляем текст информации
            var infoTextBlock = GetStorageModeInfoText?.Invoke();
            if (infoTextBlock != null)
            {
                string modeText = storageMode == StorageMode.Paths 
                    ? "Пути к файлам (экономно)" 
                    : "Копировать в папку проекта (автономно)";
                infoTextBlock.Text = $"Текущий режим: {modeText}";
            }
            
            System.Diagnostics.Debug.WriteLine($"StorageMode изменен на: {storageMode}");
            // ВАЖНО: не сохраняем автоматически, чтобы не всплывал диалог Save/Open при старте.
            // Проект будет сохранён пользователем через меню.
        }
    }
}


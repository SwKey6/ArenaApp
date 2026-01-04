using System;
using System.IO;
using System.Text.Json;
using ArenaApp.Models;
using Microsoft.Win32;

namespace ArenaApp.Services
{
    public class ProjectManager
    {
        private ProjectModel _currentProject;
        private string? _currentProjectPath;

        public ProjectModel CurrentProject => _currentProject;
        public bool HasProjectFilePath => !string.IsNullOrEmpty(_currentProjectPath);
        public string? CurrentProjectFilePath => _currentProjectPath;

        public ProjectManager()
        {
            _currentProject = new ProjectModel();
        }

        public void NewProject()
        {
            _currentProject = new ProjectModel();
            _currentProject.GlobalSettings = new GlobalSettings(); // Убеждаемся, что GlobalSettings инициализированы
            _currentProjectPath = null;
        }

        public bool OpenProject()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Project Files|*.json",
                Title = "Открыть проект"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string jsonContent = File.ReadAllText(openFileDialog.FileName);
                    _currentProject = JsonSerializer.Deserialize<ProjectModel>(jsonContent) ?? new ProjectModel();
                    
                    // Убеждаемся, что GlobalSettings инициализированы
                    if (_currentProject.GlobalSettings == null)
                    {
                        _currentProject.GlobalSettings = new GlobalSettings();
                    }
                    
                    // Убеждаемся, что OutputSettings инициализированы (для обратной совместимости)
                    if (_currentProject.GlobalSettings.OutputSettings == null)
                    {
                        _currentProject.GlobalSettings.OutputSettings = new OutputSettings();
                    }
                    
                    // Если StorageMode не установлен (старые проекты) - устанавливаем Paths по умолчанию
                    // Это обратная совместимость
                    
                    _currentProjectPath = openFileDialog.FileName;
                    
                    // Проверяем доступность медиафайлов в режиме Embedded
                    if (_currentProject.GlobalSettings.StorageMode == StorageMode.Embedded)
                    {
                        int missingFiles = 0;
                        foreach (var slot in _currentProject.MediaSlots)
                        {
                            if (slot.Type != MediaType.Text && !MediaStorageService.MediaFileExists(slot.MediaPath, _currentProjectPath))
                            {
                                missingFiles++;
                                System.Diagnostics.Debug.WriteLine($"OpenProject: Медиафайл не найден: {slot.MediaPath}");
                            }
                        }
                        
                        if (missingFiles > 0)
                        {
                            System.Windows.MessageBox.Show(
                                $"Внимание: {missingFiles} медиафайл(ов) не найдено в папке проекта.\n" +
                                "Возможно, файлы были перемещены или удалены.",
                                "Предупреждение",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Warning);
                        }
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при открытии проекта: {ex.Message}", "Ошибка");
                    return false;
                }
            }
            return false;
        }

        public bool SaveProject()
        {
            if (string.IsNullOrEmpty(_currentProjectPath))
            {
                return SaveProjectAs();
            }

            try
            {
                // Если режим Embedded - убеждаемся, что все медиафайлы скопированы
                if (_currentProject.GlobalSettings.StorageMode == StorageMode.Embedded)
                {
                    System.Diagnostics.Debug.WriteLine("SaveProject: Режим Embedded - проверяем и копируем медиафайлы");
                    
                    // Мигрируем все медиафайлы, которые еще не скопированы (если они были добавлены до сохранения проекта)
                    foreach (var slot in _currentProject.MediaSlots)
                    {
                        if (slot.Type != MediaType.Text && !string.IsNullOrEmpty(slot.MediaPath))
                        {
                            // Если путь абсолютный - значит файл еще не скопирован
                            if (Path.IsPathRooted(slot.MediaPath) && File.Exists(slot.MediaPath))
                            {
                                try
                                {
                                    string relativePath = MediaStorageService.CopyMediaToProject(slot.MediaPath, _currentProjectPath, slot.Type);
                                    slot.MediaPath = relativePath;
                                    System.Diagnostics.Debug.WriteLine($"SaveProject: Мигрирован файл {slot.MediaPath} -> {relativePath}");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"SaveProject: Ошибка миграции файла {slot.MediaPath}: {ex.Message}");
                                    // Оставляем абсолютный путь
                                }
                            }
                        }
                    }
                }
                
                string jsonContent = JsonSerializer.Serialize(_currentProject, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_currentProjectPath, jsonContent);
                System.Diagnostics.Debug.WriteLine($"SaveProject: Проект сохранен в {_currentProjectPath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при сохранении проекта: {ex.Message}", "Ошибка");
                return false;
            }
        }

        public bool SaveProjectAs()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Project Files|*.json",
                Title = "Сохранить проект как"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                _currentProjectPath = saveFileDialog.FileName;
                return SaveProject();
            }
            return false;
        }

        public string AddMediaToSlot(int column, int row, string mediaPath, MediaType type)
        {
            // Удаляем существующий слот, если есть
            _currentProject.MediaSlots.RemoveAll(slot => slot.Column == column && slot.Row == row);
            
            // Определяем путь для сохранения в зависимости от режима
            string savedPath = mediaPath;
            
            System.Diagnostics.Debug.WriteLine($"AddMediaToSlot: Начало, mediaPath={mediaPath}, StorageMode={_currentProject.GlobalSettings.StorageMode}, ProjectPath={_currentProjectPath}");
            
            if (_currentProject.GlobalSettings.StorageMode == StorageMode.Embedded)
            {
                if (string.IsNullOrEmpty(_currentProjectPath))
                {
                    // Проект еще не сохранен - используем абсолютный путь, но предупреждаем пользователя
                    System.Diagnostics.Debug.WriteLine($"AddMediaToSlot: Проект не сохранен, используем абсолютный путь. Сохраните проект для копирования файлов.");
                    savedPath = mediaPath; // Используем абсолютный путь до сохранения проекта
                }
                else
                {
                    try
                    {
                        // Копируем файл в папку проекта и получаем относительный путь
                        savedPath = MediaStorageService.CopyMediaToProject(mediaPath, _currentProjectPath, type);
                        System.Diagnostics.Debug.WriteLine($"AddMediaToSlot: Файл скопирован в проект. Исходный: {mediaPath}, Сохраненный: {savedPath}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ОШИБКА при копировании медиафайла: {ex.Message}");
                        System.Windows.MessageBox.Show($"Ошибка при копировании медиафайла в проект:\n{ex.Message}\n\nБудет использован исходный путь.", 
                            "Предупреждение", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        // В случае ошибки используем исходный путь
                        savedPath = mediaPath;
                    }
                }
            }
            else
            {
                // Режим Paths - сохраняем абсолютный путь
                System.Diagnostics.Debug.WriteLine($"AddMediaToSlot: Режим Paths, сохраняем абсолютный путь: {savedPath}");
            }
            
            // Добавляем новый слот
            var newSlot = new MediaSlot
            {
                Column = column,
                Row = row,
                MediaPath = savedPath,
                Type = type,
                PreviewPath = GeneratePreviewPath(savedPath, type),
                DisplayName = type == MediaType.Text ? "" : Path.GetFileNameWithoutExtension(mediaPath) // Для текста имя будет установлено позже
            };
            
            _currentProject.MediaSlots.Add(newSlot);
            return savedPath;
        }
        
        /// <summary>
        /// Получает абсолютный путь к медиафайлу с учетом режима хранения
        /// </summary>
        public string GetAbsoluteMediaPath(string relativePath)
        {
            System.Diagnostics.Debug.WriteLine($"GetAbsoluteMediaPath: Начало, relativePath={relativePath}");
            System.Diagnostics.Debug.WriteLine($"GetAbsoluteMediaPath: StorageMode={_currentProject.GlobalSettings.StorageMode}, ProjectPath={_currentProjectPath}");
            
            if (string.IsNullOrEmpty(relativePath))
            {
                System.Diagnostics.Debug.WriteLine($"GetAbsoluteMediaPath: Путь пустой, возвращаем пустую строку");
                return string.Empty;
            }
            
            // Если путь уже абсолютный - возвращаем как есть (нормализованный)
            if (Path.IsPathRooted(relativePath))
            {
                try
                {
                    string normalized = Path.GetFullPath(relativePath);
                    System.Diagnostics.Debug.WriteLine($"GetAbsoluteMediaPath: Путь абсолютный, нормализован: {normalized}");
                    return normalized;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GetAbsoluteMediaPath: Ошибка нормализации абсолютного пути: {ex.Message}");
                    return relativePath;
                }
            }
            
            // Если режим Embedded - получаем путь относительно папки проекта
            if (_currentProject.GlobalSettings.StorageMode == StorageMode.Embedded && !string.IsNullOrEmpty(_currentProjectPath))
            {
                string absolutePath = MediaStorageService.GetAbsoluteMediaPath(relativePath, _currentProjectPath);
                System.Diagnostics.Debug.WriteLine($"GetAbsoluteMediaPath: Режим Embedded, абсолютный путь: {absolutePath}");
                return absolutePath;
            }
            
            // В режиме Paths возвращаем как есть (ожидается абсолютный путь)
            System.Diagnostics.Debug.WriteLine($"GetAbsoluteMediaPath: Режим Paths, возвращаем как есть: {relativePath}");
            return relativePath;
        }

        public MediaSlot? GetMediaSlot(int column, int row)
        {
            return _currentProject.MediaSlots.Find(slot => slot.Column == column && slot.Row == row);
        }

        private string GeneratePreviewPath(string mediaPath, MediaType type)
        {
            if (type == MediaType.Text)
            {
                return ""; // Для текстовых блоков превью не нужно
            }
            
            string? directory = Path.GetDirectoryName(mediaPath);
            string fileName = Path.GetFileNameWithoutExtension(mediaPath);
            string extension = type == MediaType.Video ? ".jpg" : ".jpg";
            
            return Path.Combine(directory ?? "", $"{fileName}_preview{extension}");
        }
    }
}

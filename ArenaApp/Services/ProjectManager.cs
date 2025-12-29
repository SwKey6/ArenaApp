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
                    
                    _currentProjectPath = openFileDialog.FileName;
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
                string jsonContent = JsonSerializer.Serialize(_currentProject, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_currentProjectPath, jsonContent);
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

        public void AddMediaToSlot(int column, int row, string mediaPath, MediaType type)
        {
            // Удаляем существующий слот, если есть
            _currentProject.MediaSlots.RemoveAll(slot => slot.Column == column && slot.Row == row);
            
            // Добавляем новый слот
            var newSlot = new MediaSlot
            {
                Column = column,
                Row = row,
                MediaPath = mediaPath,
                Type = type,
                PreviewPath = GeneratePreviewPath(mediaPath, type),
                DisplayName = type == MediaType.Text ? "" : Path.GetFileNameWithoutExtension(mediaPath) // Для текста имя будет установлено позже
            };
            
            _currentProject.MediaSlots.Add(newSlot);
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

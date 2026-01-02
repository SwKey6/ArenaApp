using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для загрузки медиа в слоты и создания текстовых блоков
    /// </summary>
    public class SlotMediaService
    {
        private ProjectManager? _projectManager;
        
        // Делегаты для работы с UI
        public Action<int, int, string, MediaType>? UpdateSlotButton { get; set; }
        public Action? UpdateAllSlotButtonsHighlighting { get; set; }
        public Func<string, MediaType>? GetMediaType { get; set; }
        public Func<Window>? GetMainWindow { get; set; }
        
        // Делегаты для создания диалогов
        public Func<TextInputDialog>? CreateTextInputDialog { get; set; }
        public Func<string, string, MessageBoxResult>? ShowMessageBox { get; set; }
        
        public void SetProjectManager(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        
        /// <summary>
        /// Загружает медиа файл в указанный слот
        /// </summary>
        public void LoadMediaToSlot(int column, int row)
        {
            if (_projectManager?.CurrentProject == null)
            {
                ShowMessageBox?.Invoke("Проект не инициализирован. Создайте новый проект.", "Ошибка");
                return;
            }
            
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Media files|*.mp4;*.avi;*.mov;*.wmv;*.flv;*.mkv;*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.mp3;*.wav;*.flac;*.aac";
            
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                MediaType mediaType = GetMediaType?.Invoke(filePath) ?? MediaType.Video;
                
                // Добавляем медиа в проект (получаем фактически сохраненный путь: относительный в Embedded или абсолютный в Paths)
                var savedPath = _projectManager.AddMediaToSlot(column, row, filePath, mediaType);
                
                // Обновляем кнопку с превью
                UpdateSlotButton?.Invoke(column, row, savedPath, mediaType);
                
                ShowMessageBox?.Invoke($"Медиа добавлено в слот {column}-{row}", "Успех");
            }
        }
        
        /// <summary>
        /// Создает текстовый блок в указанном слоте
        /// </summary>
        public void CreateTextBlock(int column, int row)
        {
            try
            {
                // Проверяем, что ProjectManager инициализирован
                if (_projectManager?.CurrentProject == null)
                {
                    ShowMessageBox?.Invoke("Проект не инициализирован. Создайте новый проект.", "Ошибка");
                    return;
                }

                // Создаем простое диалоговое окно для ввода текста
                var textInputDialog = CreateTextInputDialog?.Invoke();
                if (textInputDialog == null)
                {
                    ShowMessageBox?.Invoke("Не удалось создать диалог ввода текста.", "Ошибка");
                    return;
                }
                
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
                        UpdateSlotButton?.Invoke(column, row, "", MediaType.Text);
                        
                        // Обновляем подсветку всех кнопок
                        UpdateAllSlotButtonsHighlighting?.Invoke();
                        
                        ShowMessageBox?.Invoke($"Текстовый блок создан в слоте {column}-{row}", "Успех");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox?.Invoke($"Ошибка при создании текстового блока: {ex.Message}", "Ошибка");
                System.Diagnostics.Debug.WriteLine($"CreateTextBlock Error: {ex}");
            }
        }
    }
}


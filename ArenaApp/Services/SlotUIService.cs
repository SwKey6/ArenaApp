using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для обновления UI элементов слотов
    /// </summary>
    public class SlotUIService
    {
        private ProjectManager? _projectManager;
        
        // Делегаты для работы с UI
        public Func<Panel>? GetBottomPanel { get; set; }
        public Func<string?>? GetCurrentMainMedia { get; set; }
        public Func<string?>? GetCurrentAudioContent { get; set; }
        public Func<int?>? GetActiveTriggerColumn { get; set; }
        public Func<int, TriggerState>? GetTriggerState { get; set; }
        
        public void SetProjectManager(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        
        /// <summary>
        /// Обновляет кнопку слота с иконкой и цветом
        /// </summary>
        public void UpdateSlotButton(int column, int row, string mediaPath, MediaType mediaType)
        {
            var bottomPanel = GetBottomPanel?.Invoke();
            if (bottomPanel == null) return;
            
            // Находим кнопку по координатам
            foreach (var child in bottomPanel.Children)
            {
                if (child is Grid columnGrid)
                {
                    int gridColumn = Grid.GetColumn(columnGrid);
                    if (gridColumn == column - 1) // Индексы начинаются с 0
                    {
                        foreach (var button in columnGrid.Children.OfType<Button>())
                        {
                            int buttonRow = Grid.GetRow(button);
                            if (buttonRow == row - 1) // Индексы начинаются с 0
                            {
                                // Обновляем кнопку
                                // Если mediaPath пустой и это не текстовый блок, очищаем иконку
                                if (string.IsNullOrEmpty(mediaPath) && mediaType != MediaType.Text)
                                {
                                    button.Content = "";
                                }
                                else
                                {
                                    button.Content = GetMediaIcon(mediaType);
                                }
                                
                                // Определяем, активна ли эта кнопка
                                string slotKey = $"Slot_{column}_{row}";
                                bool isActive = (GetCurrentMainMedia?.Invoke() == slotKey) || 
                                              (GetCurrentAudioContent?.Invoke() == slotKey);
                                
                                // Если есть медиа файл или это текстовый блок, проверяем настройки по умолчанию
                                if ((!string.IsNullOrEmpty(mediaPath) || mediaType == MediaType.Text) && _projectManager != null)
                                {
                                    var mediaSlot = _projectManager.GetMediaSlot(column, row);
                                    if (mediaSlot != null && string.IsNullOrEmpty(mediaSlot.DisplayName))
                                    {
                                        if (mediaType == MediaType.Text)
                                        {
                                            // Для текстовых блоков используем содержимое как имя
                                            mediaSlot.DisplayName = mediaSlot.TextContent.Length > 10 ? 
                                                mediaSlot.TextContent.Substring(0, 10) + "..." : 
                                                mediaSlot.TextContent;
                                        }
                                        else
                                        {
                                            mediaSlot.DisplayName = Path.GetFileNameWithoutExtension(mediaPath);
                                        }
                                    }
                                }
                                
                                // Устанавливаем цвет в зависимости от активности
                                button.Background = isActive ? Brushes.LightGreen : Brushes.LightBlue;
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Возвращает иконку для типа медиа
        /// </summary>
        public string GetMediaIcon(MediaType mediaType)
        {
            return mediaType switch
            {
                MediaType.Video => "🎥",
                MediaType.Image => "🖼️",
                MediaType.Audio => "🎵",
                MediaType.Text => "T",
                _ => "📁"
            };
        }
        
        /// <summary>
        /// Обновляет подсветку всех кнопок слотов в зависимости от их активности
        /// </summary>
        public void UpdateAllSlotButtonsHighlighting()
        {
            var bottomPanel = GetBottomPanel?.Invoke();
            if (bottomPanel == null) return;
            
            var currentMainMedia = GetCurrentMainMedia?.Invoke();
            var currentAudioContent = GetCurrentAudioContent?.Invoke();
            var activeTriggerColumn = GetActiveTriggerColumn?.Invoke();
            
            foreach (var child in bottomPanel.Children)
            {
                if (child is Grid columnGrid)
                {
                    int gridColumn = Grid.GetColumn(columnGrid);
                    foreach (var button in columnGrid.Children.OfType<Button>())
                    {
                        int buttonRow = Grid.GetRow(button);
                        int column = gridColumn + 1; // Индексы начинаются с 1
                        int row = buttonRow + 1; // Индексы начинаются с 1
                        
                        // Проверяем, является ли это кнопкой-триггером (третья строка)
                        if (buttonRow == 2) // Триггеры находятся в третьей строке (индекс 2)
                        {
                            // Обновляем состояние триггера
                            var triggerState = GetTriggerState?.Invoke(column) ?? TriggerState.Stopped;
                            
                            // Отладочная информация
                            System.Diagnostics.Debug.WriteLine($"Триггер колонка {column}: состояние {triggerState}");
                            
                            switch (triggerState)
                            {
                                case TriggerState.Playing:
                                    button.Content = "⏹";
                                    button.Background = Brushes.Green; // Зеленый цвет для воспроизведения!
                                    System.Diagnostics.Debug.WriteLine($"Установлен зеленый цвет для триггера {column}");
                                    break;
                                case TriggerState.Paused:
                                    button.Content = "⏸";
                                    button.Background = Brushes.Yellow;
                                    break;
                                case TriggerState.Stopped:
                                default:
                                    button.Content = "▶";
                                    button.Background = Brushes.Orange;
                                    break;
                            }
                        }
                        else
                        {
                            // Обычные слоты (первая и вторая строки)
                            if (_projectManager?.CurrentProject?.MediaSlots != null)
                            {
                                var slot = _projectManager.CurrentProject.MediaSlots.FirstOrDefault(s => s.Column == column && s.Row == row);
                                if (slot != null)
                                {
                                    // Определяем, активна ли эта кнопка
                                    string slotKey = $"Slot_{column}_{row}";
                                    bool isActive = (currentMainMedia == slotKey) || (currentAudioContent == slotKey);
                                    
                                    // Если в этой колонке активен триггер, то слоты тоже должны быть активными
                                    if (activeTriggerColumn == column)
                                    {
                                        isActive = true;
                                    }
                                    
                                    // Устанавливаем цвет в зависимости от активности
                                    button.Background = isActive ? Brushes.LightGreen : Brushes.LightBlue;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}


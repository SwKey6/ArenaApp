using System;
using System.Windows;
using System.Windows.Controls;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для создания и управления контекстными меню
    /// </summary>
    public class ContextMenuService
    {
        // Делегаты для получения ресурсов стилей
        public Func<object, Style>? GetContextMenuStyle { get; set; }
        public Func<object, Style>? GetContextMenuItemStyle { get; set; }
        public Func<object, Style>? GetDeleteMenuItemStyle { get; set; }
        
        // Делегаты для определения состояния паузы
        public Func<string, bool>? IsSlotPaused { get; set; }
        public Func<int, TriggerState>? GetTriggerState { get; set; }
        public Func<int?>? GetActiveTriggerColumn { get; set; }
        
        // Делегаты для обработки действий контекстного меню
        public Action<string>? OnRestartItemClick { get; set; }
        public Action<string>? OnPauseItemClick { get; set; }
        public Action<string>? OnSettingsItemClick { get; set; }
        public Action<string>? OnDeleteItemClick { get; set; }
        
        /// <summary>
        /// Создает контекстное меню для кнопки
        /// </summary>
        public ContextMenu CreateContextMenu(Button button)
        {
            var contextMenu = new ContextMenu();
            
            // Применяем стиль если доступен
            if (GetContextMenuStyle != null)
            {
                try
                {
                    contextMenu.Style = GetContextMenuStyle(button);
                }
                catch
                {
                    // Игнорируем ошибки стиля
                }
            }

            // Пункт "Заново"
            var restartItem = new MenuItem
            {
                Header = "Заново",
                Tag = button.Tag
            };
            
            if (GetContextMenuItemStyle != null)
            {
                try
                {
                    restartItem.Style = GetContextMenuItemStyle(button);
                }
                catch { }
            }
            
            restartItem.Click += (s, e) =>
            {
                if (button.Tag is string tag)
                {
                    OnRestartItemClick?.Invoke(tag);
                }
            };
            contextMenu.Items.Add(restartItem);

            // Пункт "Пауза/Продолжить" - динамический текст
            var pauseItem = new MenuItem
            {
                Header = GetPauseMenuItemText(button.Tag?.ToString()),
                Tag = button.Tag
            };
            
            if (GetContextMenuItemStyle != null)
            {
                try
                {
                    pauseItem.Style = GetContextMenuItemStyle(button);
                }
                catch { }
            }
            
            pauseItem.Click += (s, e) =>
            {
                if (button.Tag is string tag)
                {
                    OnPauseItemClick?.Invoke(tag);
                }
            };
            contextMenu.Items.Add(pauseItem);

            // Пункт "Настройки"
            var settingsItem = new MenuItem
            {
                Header = "Настройки",
                Tag = button.Tag
            };
            
            if (GetContextMenuItemStyle != null)
            {
                try
                {
                    settingsItem.Style = GetContextMenuItemStyle(button);
                }
                catch { }
            }
            
            settingsItem.Click += (s, e) =>
            {
                if (button.Tag is string tag)
                {
                    OnSettingsItemClick?.Invoke(tag);
                }
            };
            contextMenu.Items.Add(settingsItem);

            // Пункт "Удалить" (красный)
            var deleteItem = new MenuItem
            {
                Header = "Удалить",
                Tag = button.Tag
            };
            
            if (GetDeleteMenuItemStyle != null)
            {
                try
                {
                    deleteItem.Style = GetDeleteMenuItemStyle(button);
                }
                catch { }
            }
            
            deleteItem.Click += (s, e) =>
            {
                if (button.Tag is string tag)
                {
                    OnDeleteItemClick?.Invoke(tag);
                }
            };
            contextMenu.Items.Add(deleteItem);

            return contextMenu;
        }

        /// <summary>
        /// Определяет текст для пункта "Пауза/Продолжить" в зависимости от текущего состояния
        /// </summary>
        public string GetPauseMenuItemText(string? tag)
        {
            if (string.IsNullOrEmpty(tag)) return "Пауза";

            if (tag.StartsWith("Slot_"))
            {
                var parts = tag.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int column) && int.TryParse(parts[2], out int row))
                {
                    var slotKey = $"Slot_{column}_{row}";
                    
                    // Проверяем состояние через делегат
                    if (IsSlotPaused != null && IsSlotPaused(slotKey))
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
                    var activeTriggerColumn = GetActiveTriggerColumn?.Invoke();
                    var triggerState = GetTriggerState?.Invoke(column);
                    
                    if (activeTriggerColumn == column && triggerState == TriggerState.Paused)
                    {
                        return "Продолжить";
                    }
                }
            }

            return "Пауза";
        }
    }
}


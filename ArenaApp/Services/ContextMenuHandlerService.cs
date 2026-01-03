using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для обработки действий контекстного меню слотов
    /// </summary>
    public class ContextMenuHandlerService
    {
        private ProjectManager? _projectManager;
        private MediaStateService? _mediaStateService;
        
        // Делегаты для доступа к UI элементам
        public Func<MediaElement>? GetMainMediaElement { get; set; }
        public Func<MediaElement?>? GetSecondaryMediaElement { get; set; }
        public Func<Dictionary<string, MediaElement>>? GetAllAudioSlots { get; set; }
        
        // Делегаты для работы с состоянием
        public Func<string?>? GetCurrentMainMedia { get; set; }
        public Func<string?>? GetCurrentAudioContent { get; set; }
        public Func<bool>? GetIsVideoPaused { get; set; }
        public Action<bool>? SetIsVideoPaused { get; set; }
        
        // Делегаты для действий
        public Action<object, RoutedEventArgs>? HandleSlotClick { get; set; }
        public Action<MediaSlot, string>? SelectElementForSettings { get; set; }
        public Action<int, int, string, MediaType>? UpdateSlotButton { get; set; }
        public Action? UpdateAllSlotButtonsHighlighting { get; set; }
        public Func<string, Button?>? FindButtonByTag { get; set; }
        
        public void SetProjectManager(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        
        public void SetMediaStateService(MediaStateService mediaStateService)
        {
            _mediaStateService = mediaStateService;
        }
        
        /// <summary>
        /// Обрабатывает клик по пункту "Заново" в контекстном меню
        /// </summary>
        public void HandleRestartItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not string tag) return;
            
            if (!tag.StartsWith("Slot_")) return;
            
            var parts = tag.Split('_');
            if (parts.Length < 3 || 
                !int.TryParse(parts[1], out int column) || 
                !int.TryParse(parts[2], out int row))
            {
                return;
            }
            
            var slot = _projectManager?.CurrentProject.MediaSlots.FirstOrDefault(s => s.Column == column && s.Row == row);
            if (slot == null) return;
            
            var slotKey = $"Slot_{column}_{row}";
            bool isMainMedia = GetCurrentMainMedia?.Invoke() == slotKey;
            bool isAudioMedia = GetCurrentAudioContent?.Invoke() == slotKey && 
                               GetAllAudioSlots?.Invoke()?.ContainsKey(slotKey) == true;
            
            if (isMainMedia || isAudioMedia)
            {
                // Если это текущий слот - перезапускаем с самого начала
                if (isMainMedia)
                {
                    RestartMainMedia(slotKey);
                }
                
                if (isAudioMedia)
                {
                    RestartAudioMedia(slotKey);
                }
            }
            else
            {
                // Если это не текущий слот - просто запускаем
                var button = FindButtonByTag?.Invoke(tag);
                if (button != null)
                {
                    HandleSlotClick?.Invoke(button, new RoutedEventArgs());
                }
            }
        }
        
        private void RestartMainMedia(string slotKey)
        {
            var mediaElement = GetMainMediaElement?.Invoke();
            var secondaryMediaElement = GetSecondaryMediaElement?.Invoke();
            
            if (mediaElement == null) return;
            
            // Полная остановка
            mediaElement.Stop();
            mediaElement.Position = TimeSpan.Zero;
            SetIsVideoPaused?.Invoke(false);
            
            // Синхронизируем перезапуск со вторым экраном
            if (secondaryMediaElement != null && secondaryMediaElement.Source != null &&
                mediaElement.Source != null &&
                mediaElement.Source.LocalPath == secondaryMediaElement.Source.LocalPath)
            {
                try
                {
                    secondaryMediaElement.Stop();
                    secondaryMediaElement.Position = TimeSpan.Zero;
                    System.Diagnostics.Debug.WriteLine("ПЕРЕЗАПУСК: Синхронизирован со вторым экраном");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при перезапуске на втором экране: {ex.Message}");
                }
            }
            
            // Запуск с начала
            mediaElement.Play();
            
            // Синхронизируем запуск со вторым экраном
            if (secondaryMediaElement != null && secondaryMediaElement.Source != null)
            {
                try
                {
                    secondaryMediaElement.Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при запуске на втором экране: {ex.Message}");
                }
            }
            
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
        
        private void RestartAudioMedia(string slotKey)
        {
            if (_mediaStateService?.TryGetAudioSlot(slotKey, out var audioElement) != true || audioElement == null)
            {
                return;
            }
            
            // Полная остановка
            audioElement.Stop();
            audioElement.Position = TimeSpan.Zero;
            _mediaStateService.SetAudioPaused(slotKey, false);
            
            // Запуск с начала
            audioElement.Play();
            
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
        
        /// <summary>
        /// Обрабатывает клик по пункту "Пауза" в контекстном меню
        /// </summary>
        public void HandlePauseItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not string tag) return;
            
            if (!tag.StartsWith("Slot_")) return;
            
            var parts = tag.Split('_');
            if (parts.Length < 3 || 
                !int.TryParse(parts[1], out int column) || 
                !int.TryParse(parts[2], out int row))
            {
                return;
            }
            
            var slotKey = $"Slot_{column}_{row}";
            var mediaElement = GetMainMediaElement?.Invoke();
            
            bool isMainMedia = GetCurrentMainMedia?.Invoke() == slotKey && mediaElement?.Source != null;
            bool isAudioMedia = GetCurrentAudioContent?.Invoke() == slotKey && 
                               GetAllAudioSlots?.Invoke()?.ContainsKey(slotKey) == true;
            
            if (isMainMedia || isAudioMedia)
            {
                // Проверяем состояние воспроизведения
                if (isMainMedia && mediaElement != null)
                {
                    ToggleMainMediaPause(mediaElement);
                }
                
                if (isAudioMedia)
                {
                    ToggleAudioPause(slotKey);
                }
            }
            else
            {
                // Если этот слот не воспроизводится сейчас, запускаем его
                var button = FindButtonByTag?.Invoke(tag);
                if (button != null)
                {
                    HandleSlotClick?.Invoke(button, new RoutedEventArgs());
                }
            }
        }
        
        private void ToggleMainMediaPause(MediaElement mediaElement)
        {
            bool isPaused = GetIsVideoPaused?.Invoke() ?? false;
            
            if (isPaused)
            {
                // Видео на паузе - возобновляем
                mediaElement.Play();
                SetIsVideoPaused?.Invoke(false);
            }
            else
            {
                // Видео воспроизводится - ставим на паузу
                mediaElement.Pause();
                SetIsVideoPaused?.Invoke(true);
            }
            
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
        
        private void ToggleAudioPause(string slotKey)
        {
            if (_mediaStateService?.TryGetAudioSlot(slotKey, out var audioElement) != true || audioElement == null)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА: Аудио элемент не найден для слота {slotKey} в PauseItem_Click");
                return;
            }
            
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
            
            UpdateAllSlotButtonsHighlighting?.Invoke();
        }
        
        /// <summary>
        /// Обрабатывает клик по пункту "Настройки" в контекстном меню
        /// </summary>
        public void HandleSettingsItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not string slotKey) return;
            
            // Находим слот в проекте
            var parts = slotKey.Split('_');
            if (parts.Length < 3 || 
                !int.TryParse(parts[1], out int column) || 
                !int.TryParse(parts[2], out int row))
            {
                return;
            }
            
            var mediaSlot = _projectManager?.CurrentProject.MediaSlots.FirstOrDefault(s => s.Column == column && s.Row == row);
            if (mediaSlot != null)
            {
                // Выбираем элемент для настройки
                SelectElementForSettings?.Invoke(mediaSlot, slotKey);
            }
        }
        
        /// <summary>
        /// Обрабатывает клик по пункту "Удалить" в контекстном меню
        /// </summary>
        public void HandleDeleteItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not string tag) return;
            
            System.Diagnostics.Debug.WriteLine($"ПОПЫТКА УДАЛЕНИЯ: Тег = {tag}");
            
            var result = MessageBox.Show("Вы уверены, что хотите удалить этот элемент?", "Подтверждение удаления", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes) return;
            
            if (!tag.StartsWith("Slot_")) return;
            
            // Удаляем слот из проекта
            var parts = tag.Split('_');
            if (parts.Length < 3 || 
                !int.TryParse(parts[1], out int column) || 
                !int.TryParse(parts[2], out int row))
            {
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"УДАЛЕНИЕ СЛОТА: Колонка = {column}, Строка = {row}");
            
            var slot = _projectManager?.CurrentProject.MediaSlots.FirstOrDefault(s => s.Column == column && s.Row == row);
            if (slot != null && _projectManager != null)
            {
                _projectManager.CurrentProject.MediaSlots.Remove(slot);
                UpdateSlotButton?.Invoke(column, row, "", MediaType.Video); // Очищаем слот
                System.Diagnostics.Debug.WriteLine($"СЛОТ УДАЛЕН: {column}_{row}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"СЛОТ НЕ НАЙДЕН: {column}_{row}");
            }
        }
    }
}


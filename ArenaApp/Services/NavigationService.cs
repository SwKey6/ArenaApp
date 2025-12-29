using System;
using System.Collections.Generic;
using System.Linq;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для навигации между элементами проекта
    /// </summary>
    public class NavigationService
    {
        private ProjectManager? _projectManager;
        
        // Делегаты для работы с UI и медиа
        public Func<MediaSlot?>? GetSelectedElementSlot { get; set; }
        public Action<MediaSlot>? SetSelectedElementSlot { get; set; }
        public Action<MediaSlot>? LoadMediaFromSlotSelective { get; set; }
        public Action<MediaSlot, string>? SelectElementForSettings { get; set; }
        
        public void SetProjectManager(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        
        /// <summary>
        /// Навигация к следующему/предыдущему медиа в строке с автоматическим воспроизведением
        /// </summary>
        /// <param name="direction">1 для следующего, -1 для предыдущего</param>
        public void NavigateToMediaAndPlay(int direction)
        {
            if (_projectManager?.CurrentProject?.MediaSlots == null) return;
            
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null) return;
            
            // Получаем текущую строку
            int currentRow = selectedSlot.Row;
            int currentColumn = selectedSlot.Column;
            
            // Ищем все слоты в той же строке
            var rowSlots = _projectManager.CurrentProject.MediaSlots
                .Where(s => s.Row == currentRow && !string.IsNullOrEmpty(s.MediaPath))
                .OrderBy(s => s.Column)
                .ToList();
            
            if (rowSlots.Count == 0) return;
            
            // Находим текущий индекс в строке
            int currentIndex = rowSlots.FindIndex(s => s.Column == currentColumn);
            if (currentIndex == -1) return;
            
            // Вычисляем новый индекс с учетом направления
            int newIndex = currentIndex + direction;
            
            // Обрабатываем переходы через границы строки
            if (newIndex < 0)
                newIndex = rowSlots.Count - 1;
            else if (newIndex >= rowSlots.Count)
                newIndex = 0;
            
            // Выбираем новый элемент
            var newSlot = rowSlots[newIndex];
            string newSlotKey = $"Slot_{newSlot.Column}_{newSlot.Row}";
            
            // Запускаем медиа из нового слота
            LoadMediaFromSlotSelective?.Invoke(newSlot);
            
            // Открываем настройки этого элемента
            SelectElementForSettings?.Invoke(newSlot, newSlotKey);
        }
        
        /// <summary>
        /// Навигация к следующему/предыдущему элементу в проекте
        /// </summary>
        /// <param name="direction">1 для следующего, -1 для предыдущего</param>
        public void NavigateToElement(int direction)
        {
            if (_projectManager?.CurrentProject?.MediaSlots == null) return;
            
            var selectedSlot = GetSelectedElementSlot?.Invoke();
            if (selectedSlot == null) return;
            
            var allSlots = _projectManager.CurrentProject.MediaSlots.ToList();
            if (allSlots.Count <= 1) return;
            
            // Находим текущий индекс
            int currentIndex = allSlots.FindIndex(s => s == selectedSlot);
            if (currentIndex == -1) return;
            
            // Вычисляем новый индекс с учетом направления
            int newIndex = currentIndex + direction;
            
            // Обрабатываем переходы через границы
            if (newIndex < 0)
                newIndex = allSlots.Count - 1;
            else if (newIndex >= allSlots.Count)
                newIndex = 0;
            
            // Выбираем новый элемент
            var newSlot = allSlots[newIndex];
            string newSlotKey = $"Slot_{newSlot.Column}_{newSlot.Row}";
            SelectElementForSettings?.Invoke(newSlot, newSlotKey);
        }
        
        /// <summary>
        /// Находит следующий элемент в строке
        /// </summary>
        public MediaSlot? FindNextElementInRow(int currentColumn, int currentRow)
        {
            if (_projectManager?.CurrentProject?.MediaSlots == null) return null;
            
            // Ищем все слоты в той же строке, отсортированные по колонкам
            var rowSlots = _projectManager.CurrentProject.MediaSlots
                .Where(s => s.Row == currentRow && !string.IsNullOrEmpty(s.MediaPath))
                .OrderBy(s => s.Column)
                .ToList();
            
            if (rowSlots.Count == 0) return null;
            
            // Находим текущий индекс
            int currentIndex = rowSlots.FindIndex(s => s.Column == currentColumn);
            if (currentIndex == -1) return null;
            
            // Возвращаем следующий элемент (с зацикливанием)
            int nextIndex = (currentIndex + 1) % rowSlots.Count;
            return rowSlots[nextIndex];
        }
    }
}


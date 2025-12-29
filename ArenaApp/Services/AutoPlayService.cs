using System;
using System.Linq;
using System.Threading.Tasks;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления автопереходом между элементами
    /// </summary>
    public class AutoPlayService
    {
        private ProjectManager? _projectManager;
        
        // Делегаты для работы с состоянием и UI
        public Func<string?>? GetCurrentMainMedia { get; set; }
        public Func<int, int, MediaSlot?>? GetMediaSlot { get; set; }
        public Func<string, MediaSlot?>? GetMediaSlotByKey { get; set; }
        public Action<MediaSlot, string>? SelectElementForSettings { get; set; }
        public Action? RestartCurrentElement { get; set; }
        public Func<MediaSlot, Task>? LoadMediaFromSlot { get; set; }
        
        public void SetProjectManager(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        
        /// <summary>
        /// Находит следующий элемент в той же строке
        /// </summary>
        public MediaSlot? FindNextElementInRow(int currentColumn, int currentRow)
        {
            if (_projectManager?.CurrentProject?.MediaSlots == null) return null;
            
            // Ищем следующий элемент в той же строке
            var nextSlot = _projectManager.CurrentProject.MediaSlots
                .Where(s => s.Row == currentRow && s.Column > currentColumn && !string.IsNullOrEmpty(s.MediaPath))
                .OrderBy(s => s.Column)
                .FirstOrDefault();
            
            // Если не найден следующий элемент и включено автопереключение, ищем первый элемент в строке
            if (nextSlot == null && _projectManager.CurrentProject.GlobalSettings.AutoPlayNext)
            {
                nextSlot = _projectManager.CurrentProject.MediaSlots
                    .Where(s => s.Row == currentRow && !string.IsNullOrEmpty(s.MediaPath))
                    .OrderBy(s => s.Column)
                    .FirstOrDefault();
            }
            
            return nextSlot;
        }
        
        /// <summary>
        /// Автопереход на следующий элемент
        /// </summary>
        public async Task AutoPlayNextElement()
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var settings = _projectManager.CurrentProject.GlobalSettings;
            
            // Если включено зацикливание плейлиста, перезапускаем текущий элемент
            if (settings.LoopPlaylist)
            {
                // Небольшая задержка перед повторением
                await Task.Delay(500);
                
                // Перезапускаем текущий элемент
                var currentMainMedia = GetCurrentMainMedia?.Invoke();
                if (currentMainMedia != null)
                {
                    string[] parts = currentMainMedia.Split('_');
                    if (parts.Length >= 2)
                    {
                        int currentColumn = int.Parse(parts[1]);
                        int currentRow = parts.Length > 2 ? int.Parse(parts[2]) : 0;
                        
                        var currentSlot = GetMediaSlot?.Invoke(currentColumn, currentRow);
                        if (currentSlot != null)
                        {
                            string slotKey = $"Slot_{currentColumn}_{currentRow}";
                            
                            // Устанавливаем выбранный элемент для restart
                            SelectElementForSettings?.Invoke(currentSlot, slotKey);
                            
                            // Вызываем метод перезапуска
                            RestartCurrentElement?.Invoke();
                            return;
                        }
                    }
                }
            }
            
            // Если включено автопереключение, переходим к следующему элементу
            if (settings.AutoPlayNext)
            {
                // Определяем текущий активный элемент
                var currentMainMedia = GetCurrentMainMedia?.Invoke();
                if (currentMainMedia == null) return;
                
                // Парсим ключ текущего медиа
                string[] parts = currentMainMedia.Split('_');
                if (parts.Length < 2) return;
                
                int currentColumn = int.Parse(parts[1]);
                int currentRow = parts.Length > 2 ? int.Parse(parts[2]) : 0; // Для триггеров row = 0
                
                // Ищем следующий элемент
                var nextSlot = FindNextElementInRow(currentColumn, currentRow);
                
                if (nextSlot != null)
                {
                    // Небольшая задержка перед автопереходом
                    await Task.Delay(500);
                    
                    // Запускаем следующий элемент
                    if (nextSlot.IsTrigger)
                    {
                        // Для триггеров - пока не реализовано, так как триггеры отключены
                        // TODO: Реализовать автопереход для триггеров когда они будут включены
                    }
                    else
                    {
                        // Для обычных слотов
                        if (LoadMediaFromSlot != null)
                        {
                            await LoadMediaFromSlot(nextSlot);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Автопереход для аудио элементов
        /// </summary>
        public async Task AutoPlayNextAudioElement(string currentSlotKey)
        {
            if (_projectManager?.CurrentProject?.GlobalSettings == null) return;
            
            var settings = _projectManager.CurrentProject.GlobalSettings;
            
            // Если включено зацикливание плейлиста, перезапускаем текущий аудио элемент
            if (settings.LoopPlaylist)
            {
                // Небольшая задержка перед повторением
                await Task.Delay(500);
                
                // Перезапускаем текущий аудио элемент
                string[] parts = currentSlotKey.Split('_');
                if (parts.Length >= 3)
                {
                    int currentColumn = int.Parse(parts[1]);
                    int currentRow = int.Parse(parts[2]);
                    
                    var currentSlot = GetMediaSlot?.Invoke(currentColumn, currentRow);
                    if (currentSlot != null && currentSlot.Type == MediaType.Audio)
                    {
                        // Устанавливаем выбранный элемент для restart
                        SelectElementForSettings?.Invoke(currentSlot, currentSlotKey);
                        
                        // Вызываем метод перезапуска
                        RestartCurrentElement?.Invoke();
                        return;
                    }
                }
            }
            
            // Если включено автопереключение, переходим к следующему аудио элементу
            if (settings.AutoPlayNext)
            {
                // Парсим ключ текущего аудио слота
                string[] parts = currentSlotKey.Split('_');
                if (parts.Length < 3) return;
                
                int currentColumn = int.Parse(parts[1]);
                int currentRow = int.Parse(parts[2]);
                
                // Ищем следующий элемент в той же строке
                var nextSlot = FindNextElementInRow(currentColumn, currentRow);
                
                if (nextSlot != null && nextSlot.Type == MediaType.Audio)
                {
                    // Небольшая задержка перед автопереходом
                    await Task.Delay(500);
                    
                    // Запускаем следующий аудио элемент
                    if (LoadMediaFromSlot != null)
                    {
                        await LoadMediaFromSlot(nextSlot);
                    }
                }
            }
        }
    }
}


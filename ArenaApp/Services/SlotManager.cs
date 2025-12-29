using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления слотами медиа
    /// </summary>
    public class SlotManager
    {
        // Делегаты для работы с UI
        public Func<int, int, MediaSlot?>? GetMediaSlot { get; set; }
        public Action<int, int, string, MediaType>? UpdateSlotButton { get; set; }
        public Action? UpdateAllSlotButtonsHighlighting { get; set; }
        
        // Методы для работы со слотами
        public string GetSlotKey(int column, int row)
        {
            return $"Slot_{column}_{row}";
        }
        
        public (int column, int row)? ParseSlotKey(string slotKey)
        {
            if (string.IsNullOrEmpty(slotKey) || !slotKey.StartsWith("Slot_"))
                return null;
            
            string[] parts = slotKey.Replace("Slot_", "").Split('_');
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out int column) && 
                int.TryParse(parts[1], out int row))
            {
                return (column, row);
            }
            
            return null;
        }
        
        public MediaSlot? GetSlotByKey(string slotKey)
        {
            var parsed = ParseSlotKey(slotKey);
            if (parsed.HasValue)
            {
                return GetMediaSlot?.Invoke(parsed.Value.column, parsed.Value.row);
            }
            return null;
        }
        
        public void UpdateSlotButtonByKey(string slotKey, string mediaPath, MediaType mediaType)
        {
            var parsed = ParseSlotKey(slotKey);
            if (parsed.HasValue)
            {
                UpdateSlotButton?.Invoke(parsed.Value.column, parsed.Value.row, mediaPath, mediaType);
            }
        }
        
        public string GetMediaIcon(MediaType mediaType)
        {
            return mediaType switch
            {
                MediaType.Video => "🎥",
                MediaType.Image => "🖼️",
                MediaType.Audio => "🎵",
                MediaType.Text => "T",
                _ => "?"
            };
        }
        
        public string GetMediaTypeName(MediaType? mediaType)
        {
            return mediaType switch
            {
                MediaType.Video => "видео",
                MediaType.Image => "изображение",
                MediaType.Audio => "аудио",
                MediaType.Text => "текст",
                _ => "неизвестный тип"
            };
        }
        
        public bool IsMediaTypeCompatible(MediaType newType, MediaType? currentType)
        {
            // Все типы совместимы - они заменяют друг друга или воспроизводятся параллельно
            return true;
        }
        
        public MediaSlot? FindNextElementInRow(int currentColumn, int currentRow, List<MediaSlot> allSlots)
        {
            // Ищем следующий элемент в той же строке
            var rowSlots = allSlots
                .Where(s => s.Row == currentRow && !string.IsNullOrEmpty(s.MediaPath))
                .OrderBy(s => s.Column)
                .ToList();
            
            if (rowSlots.Count == 0) return null;
            
            int currentIndex = rowSlots.FindIndex(s => s.Column == currentColumn);
            if (currentIndex == -1) return null;
            
            int nextIndex = (currentIndex + 1) % rowSlots.Count;
            return rowSlots[nextIndex];
        }
        
        public MediaSlot? FindPreviousElementInRow(int currentColumn, int currentRow, List<MediaSlot> allSlots)
        {
            // Ищем предыдущий элемент в той же строке
            var rowSlots = allSlots
                .Where(s => s.Row == currentRow && !string.IsNullOrEmpty(s.MediaPath))
                .OrderBy(s => s.Column)
                .ToList();
            
            if (rowSlots.Count == 0) return null;
            
            int currentIndex = rowSlots.FindIndex(s => s.Column == currentColumn);
            if (currentIndex == -1) return null;
            
            int previousIndex = (currentIndex - 1 + rowSlots.Count) % rowSlots.Count;
            return rowSlots[previousIndex];
        }
    }
}


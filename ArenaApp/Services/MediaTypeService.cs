using System;
using System.IO;
using System.Linq;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для работы с типами медиа
    /// </summary>
    public class MediaTypeService
    {
        // Делегаты для доступа к состоянию проекта
        public Func<int, int, MediaSlot?>? GetMediaSlot { get; set; }
        public Func<string?>? GetCurrentVisualContent { get; set; }
        public Func<string?>? GetCurrentAudioContent { get; set; }
        public Func<string?>? GetCurrentMainMedia { get; set; }
        public Func<string, bool>? IsMediaFileAlreadyPlaying { get; set; }
        
        /// <summary>
        /// Определяет тип медиа по расширению файла
        /// </summary>
        public MediaType GetMediaType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return MediaType.Video; // По умолчанию
            
            string extension = Path.GetExtension(filePath).ToLower();
            
            var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv" };
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var audioExtensions = new[] { ".mp3", ".wav", ".flac", ".aac" };
            
            if (videoExtensions.Contains(extension))
                return MediaType.Video;
            else if (imageExtensions.Contains(extension))
                return MediaType.Image;
            else if (audioExtensions.Contains(extension))
                return MediaType.Audio;
            
            return MediaType.Video; // По умолчанию
        }
        
        /// <summary>
        /// Проверяет совместимость типа медиа с текущим воспроизведением
        /// </summary>
        public bool IsMediaTypeCompatible(MediaType newType)
        {
            var currentType = GetCurrentMediaType();
            
            // Если ничего не воспроизводится - можно запустить любой тип
            if (currentType == null) return true;
            
            // Правила совместимости:
            // - Звук + картинка = OK (параллельно)
            // - Звук + видео = OK (параллельно)
            // - Звук + звук = OK (замена, не параллельно)
            // - Видео + видео = OK (замена, не параллельно)
            // - Картинка + картинка = OK (замена, не параллельно)
            // - Видео + изображение = OK (замена)
            // - Изображение + видео = OK (замена)
            
            // Все типы совместимы - они заменяют друг друга или воспроизводятся параллельно
            return true;
        }
        
        /// <summary>
        /// Получает текущий тип воспроизводимого медиа
        /// </summary>
        public MediaType? GetCurrentMediaType()
        {
            // Проверяем визуальный контент
            var currentVisualContent = GetCurrentVisualContent?.Invoke();
            if (currentVisualContent != null)
            {
                if (currentVisualContent.StartsWith("Trigger_"))
                {
                    // Для триггеров нужно проверить что именно воспроизводится
                    var columnStr = currentVisualContent.Replace("Trigger_", "");
                    if (int.TryParse(columnStr, out int column))
                    {
                        var slot1 = GetMediaSlot?.Invoke(column, 1);
                        var slot2 = GetMediaSlot?.Invoke(column, 2);
                        
                        // Если есть видео - возвращаем Video
                        if (slot1?.Type == MediaType.Video || slot2?.Type == MediaType.Video)
                            return MediaType.Video;
                        // Если есть изображение - возвращаем Image
                        if (slot1?.Type == MediaType.Image || slot2?.Type == MediaType.Image)
                            return MediaType.Image;
                    }
                }
                else if (currentVisualContent.StartsWith("Slot_"))
                {
                    // Для слотов получаем тип из проекта
                    var parts = currentVisualContent.Replace("Slot_", "").Split('_');
                    if (parts.Length == 2 && 
                        int.TryParse(parts[0], out int column) && 
                        int.TryParse(parts[1], out int row))
                    {
                        var slot = GetMediaSlot?.Invoke(column, row);
                        return slot?.Type;
                    }
                }
            }
            
            // Проверяем аудио контент
            var currentAudioContent = GetCurrentAudioContent?.Invoke();
            if (currentAudioContent != null)
            {
                if (currentAudioContent.StartsWith("Trigger_"))
                {
                    // Для триггеров проверяем есть ли аудио
                    var columnStr = currentAudioContent.Replace("Trigger_", "");
                    if (int.TryParse(columnStr, out int column))
                    {
                        var slot1 = GetMediaSlot?.Invoke(column, 1);
                        var slot2 = GetMediaSlot?.Invoke(column, 2);
                        
                        if (slot1?.Type == MediaType.Audio || slot2?.Type == MediaType.Audio)
                            return MediaType.Audio;
                    }
                }
                else if (currentAudioContent.StartsWith("Slot_"))
                {
                    return MediaType.Audio; // Если активен аудио слот
                }
            }
            
            return null; // Ничего не воспроизводится
        }
        
        /// <summary>
        /// Возвращает читаемое название типа медиа
        /// </summary>
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
        
        /// <summary>
        /// Проверяет, нужно ли блокировать запуск медиафайла (только для аудио)
        /// </summary>
        public bool ShouldBlockMediaFile(string mediaPath, MediaType mediaType, string? currentSlotKey = null)
        {
            // Блокируем только аудио файлы от дублирования
            // Изображения и видео должны заменяться
            if (mediaType == MediaType.Audio && IsMediaFileAlreadyPlaying != null && IsMediaFileAlreadyPlaying(mediaPath))
            {
                // НЕ блокируем, если это тот же слот/триггер (для паузы/возобновления)
                if (!string.IsNullOrEmpty(currentSlotKey))
                {
                    // Проверяем, воспроизводится ли этот файл в том же слоте/триггере
                    var currentAudioContent = GetCurrentAudioContent?.Invoke();
                    var currentMainMedia = GetCurrentMainMedia?.Invoke();
                    
                    if (currentAudioContent == currentSlotKey || currentMainMedia == currentSlotKey)
                    {
                        return false; // Не блокируем, это тот же слот
                    }
                }
                return true;
            }
            
            return false;
        }
    }
}


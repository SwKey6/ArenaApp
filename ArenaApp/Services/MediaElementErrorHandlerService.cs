using System;
using System.Windows;
using System.Windows.Controls;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для обработки ошибок MediaElement
    /// </summary>
    public class MediaElementErrorHandlerService
    {
        /// <summary>
        /// Обрабатывает ошибку MediaElement
        /// </summary>
        public void HandleMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            var errorMessage = e.ErrorException?.Message ?? "Неизвестная ошибка";
            var errorCode = e.ErrorException?.HResult.ToString("X") ?? "N/A";
            
            System.Diagnostics.Debug.WriteLine($"ОШИБКА MediaElement: {errorMessage}");
            System.Diagnostics.Debug.WriteLine($"ОШИБКА MediaElement Code: {errorCode}");
            System.Diagnostics.Debug.WriteLine($"ОШИБКА MediaElement StackTrace: {e.ErrorException?.StackTrace}");
            
            // Расшифровка кода ошибки
            string errorDescription = errorCode switch
            {
                "0xC00D109B" => "Формат файла не поддерживается Windows Media Foundation.\n\nВозможные решения:\n1. Установите K-Lite Codec Pack\n2. Конвертируйте видео в MP4 (H.264)\n3. Используйте другой формат файла",
                "0x80070002" => "Файл не найден",
                "0x80070005" => "Нет доступа к файлу",
                _ => $"Код ошибки: {errorCode}"
            };
            
            MessageBox.Show($"Ошибка при загрузке медиа:\n{errorMessage}\n\n{errorDescription}", 
                "Ошибка MediaElement", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}


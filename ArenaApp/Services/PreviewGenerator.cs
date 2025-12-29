using System;
using System.IO;
using System.Windows.Media.Imaging;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    public class PreviewGenerator
    {
        public string? GeneratePreview(string mediaPath, MediaType type)
        {
            try
            {
                string previewPath = GetPreviewPath(mediaPath, type);
                
                switch (type)
                {
                    case MediaType.Video:
                        return GenerateVideoPreview(mediaPath, previewPath);
                    case MediaType.Image:
                        return GenerateImagePreview(mediaPath, previewPath);
                    case MediaType.Audio:
                        return GenerateAudioPreview(mediaPath, previewPath);
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при создании превью: {ex.Message}", "Ошибка");
                return null;
            }
        }

        private string GetPreviewPath(string mediaPath, MediaType type)
        {
            string? directory = Path.GetDirectoryName(mediaPath);
            string fileName = Path.GetFileNameWithoutExtension(mediaPath);
            string extension = type == MediaType.Audio ? ".png" : ".jpg";
            
            return Path.Combine(directory ?? "", $"{fileName}_preview{extension}");
        }

        private string? GenerateVideoPreview(string videoPath, string previewPath)
        {
            // Для видео создаем простую иконку (в реальном проекте можно использовать FFmpeg)
            return CreateDefaultPreview(previewPath, "🎥");
        }

        private string? GenerateImagePreview(string imagePath, string previewPath)
        {
            try
            {
                // Создаем миниатюру изображения
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.DecodePixelWidth = 64; // Размер превью
                bitmap.DecodePixelHeight = 64;
                bitmap.EndInit();
                bitmap.Freeze();

                // Сохраняем как JPEG
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                
                using (var fileStream = new FileStream(previewPath, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }

                return previewPath;
            }
            catch
            {
                return CreateDefaultPreview(previewPath, "🖼️");
            }
        }

        private string? GenerateAudioPreview(string audioPath, string previewPath)
        {
            // Для аудио создаем простую иконку
            return CreateDefaultPreview(previewPath, "🎵");
        }

        private string? CreateDefaultPreview(string previewPath, string icon)
        {
            // Создаем простую иконку (в реальном проекте можно использовать WPF для создания изображения)
            // Пока возвращаем null, чтобы использовать стандартные иконки
            return null;
        }
    }
}

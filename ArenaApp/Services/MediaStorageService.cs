using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления хранением медиафайлов
    /// </summary>
    public class MediaStorageService
    {
        private const string MediaFolderName = "Media";
        
        /// <summary>
        /// Получает путь к папке медиа для проекта
        /// </summary>
        public static string GetMediaFolderPath(string projectFilePath)
        {
            if (string.IsNullOrEmpty(projectFilePath))
                return string.Empty;
            
            string projectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
            string projectName = Path.GetFileNameWithoutExtension(projectFilePath);
            return Path.Combine(projectDirectory, $"{projectName}_{MediaFolderName}");
        }
        
        /// <summary>
        /// Копирует медиафайл в папку проекта и возвращает относительный путь
        /// </summary>
        public static string CopyMediaToProject(string sourcePath, string projectFilePath, MediaType mediaType)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                throw new FileNotFoundException($"Файл не найден: {sourcePath}");
            
            string mediaFolder = GetMediaFolderPath(projectFilePath);
            if (string.IsNullOrEmpty(mediaFolder))
                throw new InvalidOperationException("Не удалось определить папку проекта");
            
            // Создаем папку если не существует
            Directory.CreateDirectory(mediaFolder);
            
            // Создаем подпапки по типу медиа
            string typeFolder = mediaType switch
            {
                MediaType.Video => "Videos",
                MediaType.Image => "Images",
                MediaType.Audio => "Audio",
                MediaType.Text => "Text",
                _ => "Other"
            };
            
            string targetFolder = Path.Combine(mediaFolder, typeFolder);
            Directory.CreateDirectory(targetFolder);
            
            // Генерируем уникальное имя файла (на основе хеша исходного пути + имя файла)
            string originalFileName = Path.GetFileName(sourcePath);
            string extension = Path.GetExtension(sourcePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
            
            // Создаем хеш из исходного пути для уникальности
            string hash = ComputeHash(sourcePath);
            string uniqueFileName = $"{SanitizeFileName(fileNameWithoutExt)}_{hash.Substring(0, 8)}{extension}";
            
            string targetPath = Path.Combine(targetFolder, uniqueFileName);
            
            // Если файл уже существует и идентичен - возвращаем существующий путь
            if (File.Exists(targetPath))
            {
                if (AreFilesIdentical(sourcePath, targetPath))
                {
                    return GetRelativePath(targetPath, Path.GetDirectoryName(projectFilePath) ?? string.Empty);
                }
                // Если файл существует но отличается - добавляем суффикс
                int counter = 1;
                string baseName = Path.GetFileNameWithoutExtension(uniqueFileName);
                while (File.Exists(targetPath))
                {
                    uniqueFileName = $"{baseName}_{counter}{extension}";
                    targetPath = Path.Combine(targetFolder, uniqueFileName);
                    counter++;
                }
            }
            
            // Копируем файл
            File.Copy(sourcePath, targetPath, false);
            
            // Возвращаем относительный путь от папки проекта
            return GetRelativePath(targetPath, Path.GetDirectoryName(projectFilePath) ?? string.Empty);
        }
        
        /// <summary>
        /// Получает абсолютный путь к медиафайлу на основе относительного пути и пути проекта
        /// </summary>
        public static string GetAbsoluteMediaPath(string relativePath, string projectFilePath)
        {
            System.Diagnostics.Debug.WriteLine($"MediaStorageService.GetAbsoluteMediaPath: Начало, relativePath={relativePath}, projectFilePath={projectFilePath}");
            
            if (string.IsNullOrEmpty(relativePath))
            {
                System.Diagnostics.Debug.WriteLine($"MediaStorageService.GetAbsoluteMediaPath: relativePath пустой");
                return string.Empty;
            }
            
            if (string.IsNullOrEmpty(projectFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"MediaStorageService.GetAbsoluteMediaPath: projectFilePath пустой, возвращаем relativePath");
                return relativePath; // Если нет пути проекта, возвращаем как есть (старый режим)
            }
            
            // Если путь уже абсолютный - возвращаем как есть (нормализованный)
            if (Path.IsPathRooted(relativePath))
            {
                // Нормализуем путь (убираем .. и .)
                try
                {
                    string normalized = Path.GetFullPath(relativePath);
                    System.Diagnostics.Debug.WriteLine($"MediaStorageService.GetAbsoluteMediaPath: Путь абсолютный, нормализован: {normalized}");
                    return normalized;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MediaStorageService.GetAbsoluteMediaPath: Ошибка нормализации: {ex.Message}");
                    return relativePath;
                }
            }
            
            string projectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
            System.Diagnostics.Debug.WriteLine($"MediaStorageService.GetAbsoluteMediaPath: projectDirectory={projectDirectory}");
            
            if (string.IsNullOrEmpty(projectDirectory))
            {
                System.Diagnostics.Debug.WriteLine($"MediaStorageService.GetAbsoluteMediaPath: projectDirectory пустой, возвращаем relativePath");
                return relativePath;
            }
            
            // Объединяем пути и нормализуем
            try
            {
                string fullPath = Path.Combine(projectDirectory, relativePath);
                string normalized = Path.GetFullPath(fullPath);
                System.Diagnostics.Debug.WriteLine($"MediaStorageService.GetAbsoluteMediaPath: Объединенный путь: {fullPath}, нормализован: {normalized}");
                return normalized;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА GetAbsoluteMediaPath: {ex.Message}, relativePath={relativePath}, projectFilePath={projectFilePath}");
                return relativePath;
            }
        }
        
        /// <summary>
        /// Проверяет, существует ли медиафайл
        /// </summary>
        public static bool MediaFileExists(string mediaPath, string projectFilePath)
        {
            if (string.IsNullOrEmpty(mediaPath))
                return false;
            
            string absolutePath = GetAbsoluteMediaPath(mediaPath, projectFilePath);
            return File.Exists(absolutePath);
        }
        
        /// <summary>
        /// Очищает папку медиа проекта (удаляет все файлы)
        /// </summary>
        public static void CleanMediaFolder(string projectFilePath)
        {
            string mediaFolder = GetMediaFolderPath(projectFilePath);
            if (Directory.Exists(mediaFolder))
            {
                try
                {
                    Directory.Delete(mediaFolder, true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при очистке папки медиа: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Вычисляет хеш файла для уникальности
        /// </summary>
        private static string ComputeHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                string content = filePath + File.GetLastWriteTime(filePath).Ticks;
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
        
        /// <summary>
        /// Проверяет, идентичны ли два файла
        /// </summary>
        private static bool AreFilesIdentical(string file1, string file2)
        {
            try
            {
                var info1 = new FileInfo(file1);
                var info2 = new FileInfo(file2);
                
                if (info1.Length != info2.Length)
                    return false;
                
                // Для больших файлов можно добавить проверку хеша содержимого
                // Пока проверяем только размер
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Очищает имя файла от недопустимых символов
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = fileName;
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            return sanitized;
        }
        
        /// <summary>
        /// Получает относительный путь от базовой директории
        /// </summary>
        private static string GetRelativePath(string fullPath, string baseDirectory)
        {
            if (string.IsNullOrEmpty(baseDirectory))
                return fullPath;
            
            try
            {
                // Нормализуем пути
                string normalizedFullPath = Path.GetFullPath(fullPath);
                string normalizedBaseDir = Path.GetFullPath(baseDirectory);
                
                // Убеждаемся, что baseDirectory заканчивается на разделитель
                if (!normalizedBaseDir.EndsWith(Path.DirectorySeparatorChar.ToString()) && 
                    !normalizedBaseDir.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    normalizedBaseDir += Path.DirectorySeparatorChar;
                }
                
                // Создаем URI с правильным форматом
                Uri fullUri = new Uri(normalizedFullPath, UriKind.Absolute);
                Uri baseUri = new Uri(normalizedBaseDir, UriKind.Absolute);
                
                // Получаем относительный путь
                Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
                string relativePath = Uri.UnescapeDataString(relativeUri.ToString());
                
                // Заменяем прямые слеши на обратные для Windows
                relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
                
                System.Diagnostics.Debug.WriteLine($"GetRelativePath: fullPath={fullPath}, baseDirectory={baseDirectory}, relativePath={relativePath}");
                return relativePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА GetRelativePath: {ex.Message}, fullPath={fullPath}, baseDirectory={baseDirectory}");
                // В случае ошибки возвращаем только имя файла
                return Path.GetFileName(fullPath);
            }
        }
    }
}


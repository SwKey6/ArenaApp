using System;
using System.Windows;
using System.Windows.Controls;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления позициями и размерами панелей
    /// </summary>
    public class PanelPositionService
    {
        // Делегаты для получения панелей
        public Func<FrameworkElement>? GetElementSettingsPanel { get; set; }
        public Func<FrameworkElement>? GetGlobalSettingsPanel { get; set; }
        public Func<FrameworkElement>? GetMediaPlayerPanel { get; set; }
        public Func<FrameworkElement>? GetMediaCellsPanel { get; set; }
        
        // Делегат для получения GlobalSettings
        public Func<GlobalSettings?>? GetGlobalSettings { get; set; }
        
        /// <summary>
        /// Сохраняет позиции и размеры всех панелей в GlobalSettings
        /// </summary>
        public void SavePanelPositions()
        {
            var settings = GetGlobalSettings?.Invoke();
            if (settings == null) return;
            
            var elementPanel = GetElementSettingsPanel?.Invoke();
            if (elementPanel != null)
            {
                settings.ElementSettingsPanel.Left = Canvas.GetLeft(elementPanel);
                settings.ElementSettingsPanel.Top = Canvas.GetTop(elementPanel);
                settings.ElementSettingsPanel.Width = elementPanel.Width;
                settings.ElementSettingsPanel.Height = elementPanel.Height;
            }
            
            var globalPanel = GetGlobalSettingsPanel?.Invoke();
            if (globalPanel != null)
            {
                settings.GlobalSettingsPanel.Left = Canvas.GetLeft(globalPanel);
                settings.GlobalSettingsPanel.Top = Canvas.GetTop(globalPanel);
                settings.GlobalSettingsPanel.Width = globalPanel.Width;
                settings.GlobalSettingsPanel.Height = globalPanel.Height;
            }
            
            var mediaPlayerPanel = GetMediaPlayerPanel?.Invoke();
            if (mediaPlayerPanel != null)
            {
                settings.MediaPlayerPanel.Left = Canvas.GetLeft(mediaPlayerPanel);
                settings.MediaPlayerPanel.Top = Canvas.GetTop(mediaPlayerPanel);
                settings.MediaPlayerPanel.Width = mediaPlayerPanel.Width;
                settings.MediaPlayerPanel.Height = mediaPlayerPanel.Height;
            }
            
            var mediaCellsPanel = GetMediaCellsPanel?.Invoke();
            if (mediaCellsPanel != null)
            {
                settings.MediaCellsPanel.Left = Canvas.GetLeft(mediaCellsPanel);
                settings.MediaCellsPanel.Top = Canvas.GetBottom(mediaCellsPanel);
                settings.MediaCellsPanel.Width = mediaCellsPanel.Width;
                settings.MediaCellsPanel.Height = mediaCellsPanel.Height;
            }
        }
        
        /// <summary>
        /// Загружает позиции и размеры всех панелей из GlobalSettings
        /// </summary>
        public void LoadPanelPositions()
        {
            var settings = GetGlobalSettings?.Invoke();
            if (settings == null) return;
            
            var elementPanel = GetElementSettingsPanel?.Invoke();
            if (elementPanel != null)
            {
                Canvas.SetLeft(elementPanel, settings.ElementSettingsPanel.Left);
                Canvas.SetTop(elementPanel, settings.ElementSettingsPanel.Top);
                elementPanel.Width = settings.ElementSettingsPanel.Width;
                elementPanel.Height = settings.ElementSettingsPanel.Height;
            }
            
            var globalPanel = GetGlobalSettingsPanel?.Invoke();
            if (globalPanel != null)
            {
                Canvas.SetLeft(globalPanel, settings.GlobalSettingsPanel.Left);
                Canvas.SetTop(globalPanel, settings.GlobalSettingsPanel.Top);
                globalPanel.Width = settings.GlobalSettingsPanel.Width;
                globalPanel.Height = settings.GlobalSettingsPanel.Height;
            }
            
            var mediaPlayerPanel = GetMediaPlayerPanel?.Invoke();
            if (mediaPlayerPanel != null)
            {
                Canvas.SetLeft(mediaPlayerPanel, settings.MediaPlayerPanel.Left);
                Canvas.SetTop(mediaPlayerPanel, settings.MediaPlayerPanel.Top);
                mediaPlayerPanel.Width = settings.MediaPlayerPanel.Width;
                mediaPlayerPanel.Height = settings.MediaPlayerPanel.Height;
            }
            
            var mediaCellsPanel = GetMediaCellsPanel?.Invoke();
            if (mediaCellsPanel != null)
            {
                Canvas.SetLeft(mediaCellsPanel, settings.MediaCellsPanel.Left);
                Canvas.SetBottom(mediaCellsPanel, settings.MediaCellsPanel.Top);
                mediaCellsPanel.Width = settings.MediaCellsPanel.Width;
                mediaCellsPanel.Height = settings.MediaCellsPanel.Height;
            }
        }
    }
}


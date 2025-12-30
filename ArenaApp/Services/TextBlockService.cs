using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления текстовыми блоками
    /// </summary>
    public class TextBlockService
    {
        // Делегаты для доступа к UI элементам
        public Func<Grid>? GetTextOverlayGrid { get; set; }
        public Func<Window?>? GetSecondaryScreenWindow { get; set; }
        
        /// <summary>
        /// Создает или обновляет текстовый блок
        /// </summary>
        public void CreateOrUpdateTextBlock(string slotKey, MediaSlot slot, Grid? textOverlayGrid = null)
        {
            var grid = textOverlayGrid ?? GetTextOverlayGrid?.Invoke();
            if (grid == null) return;
            
            // Удаляем старые текстовые блоки
            var oldTextBlocks = grid.Children.OfType<TextBlock>().ToList();
            foreach (var oldBlock in oldTextBlocks)
            {
                grid.Children.Remove(oldBlock);
            }
            
            // Создаем новый текстовый блок
            var textBlock = new TextBlock
            {
                Text = slot.TextContent ?? "",
                FontSize = slot.FontSize,
                FontFamily = new FontFamily(slot.FontFamily ?? "Arial"),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(slot.FontColor ?? "White")),
                HorizontalAlignment = GetHorizontalAlignmentFromPosition(slot.TextPosition),
                VerticalAlignment = GetVerticalAlignmentFromPosition(slot.TextPosition),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = GetTextAlignmentFromPosition(slot.TextPosition)
            };
            
            // Устанавливаем позицию
            if (slot.UseManualPosition)
            {
                Canvas.SetLeft(textBlock, slot.TextX);
                Canvas.SetTop(textBlock, slot.TextY);
            }
            
            grid.Children.Add(textBlock);
            grid.Visibility = Visibility.Visible;
        }
        
        /// <summary>
        /// Удаляет текстовый блок
        /// </summary>
        public void RemoveTextBlock(Grid? textOverlayGrid = null)
        {
            var grid = textOverlayGrid ?? GetTextOverlayGrid?.Invoke();
            if (grid == null) return;
            
            var textBlocks = grid.Children.OfType<TextBlock>().ToList();
            foreach (var textBlock in textBlocks)
            {
                grid.Children.Remove(textBlock);
            }
            
            if (grid.Children.Count == 0)
            {
                grid.Visibility = Visibility.Hidden;
            }
        }
        
        /// <summary>
        /// Синхронизирует текстовый блок со вторым экраном
        /// </summary>
        public void SyncTextBlockToSecondaryScreen(string slotKey, MediaSlot slot)
        {
            var secondaryWindow = GetSecondaryScreenWindow?.Invoke();
            if (secondaryWindow?.Content is Grid secondaryGrid)
            {
                // Удаляем старые текстовые блоки
                var oldTextBlocks = secondaryGrid.Children.OfType<TextBlock>().ToList();
                foreach (var oldBlock in oldTextBlocks)
                {
                    secondaryGrid.Children.Remove(oldBlock);
                }
                
                // Создаем новый текстовый блок
                var textBlock = new TextBlock
                {
                    Text = slot.TextContent ?? "",
                    FontSize = slot.FontSize,
                    FontFamily = new FontFamily(slot.FontFamily ?? "Arial"),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(slot.FontColor ?? "White")),
                    HorizontalAlignment = GetHorizontalAlignmentFromPosition(slot.TextPosition),
                    VerticalAlignment = GetVerticalAlignmentFromPosition(slot.TextPosition),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = GetTextAlignmentFromPosition(slot.TextPosition)
                };
                
                // Устанавливаем позицию
                if (slot.UseManualPosition)
                {
                    Canvas.SetLeft(textBlock, slot.TextX);
                    Canvas.SetTop(textBlock, slot.TextY);
                }
                
                secondaryGrid.Children.Add(textBlock);
            }
        }
        
        /// <summary>
        /// Применяет настройки к текстовому блоку
        /// </summary>
        public void ApplyTextSettings(Grid? textOverlayGrid, double opacity, double scale, double rotation)
        {
            var grid = textOverlayGrid ?? GetTextOverlayGrid?.Invoke();
            if (grid == null) return;
            
            grid.Opacity = opacity;
            
            var textElement = grid.Children.OfType<TextBlock>().FirstOrDefault();
            if (textElement != null)
            {
                ApplyScaleAndRotation(textElement, scale, rotation);
            }
        }
        
        private void ApplyScaleAndRotation(FrameworkElement element, double scale, double rotation)
        {
            if (element == null) return;
            
            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform(scale, scale));
            transform.Children.Add(new RotateTransform(rotation));
            element.RenderTransform = transform;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        
        private HorizontalAlignment GetHorizontalAlignmentFromPosition(string? position)
        {
            return position switch
            {
                "TopLeft" or "CenterLeft" or "BottomLeft" => HorizontalAlignment.Left,
                "TopRight" or "CenterRight" or "BottomRight" => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Center
            };
        }
        
        private VerticalAlignment GetVerticalAlignmentFromPosition(string? position)
        {
            return position switch
            {
                "TopLeft" or "TopCenter" or "TopRight" => VerticalAlignment.Top,
                "BottomLeft" or "BottomCenter" or "BottomRight" => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center
            };
        }
        
        private TextAlignment GetTextAlignmentFromPosition(string? position)
        {
            return position switch
            {
                "TopLeft" or "CenterLeft" or "BottomLeft" => TextAlignment.Left,
                "TopRight" or "CenterRight" or "BottomRight" => TextAlignment.Right,
                _ => TextAlignment.Center
            };
        }
    }
}


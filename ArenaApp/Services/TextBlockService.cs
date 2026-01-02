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
        
        /// <summary>
        /// Применяет настройки текста к отображаемому элементу на основе MediaSlot
        /// </summary>
        public void ApplyTextSettingsFromSlot(MediaSlot slot, Grid? textOverlayGrid = null)
        {
            if (slot == null || slot.Type != MediaType.Text) return;
            
            var grid = textOverlayGrid ?? GetTextOverlayGrid?.Invoke();
            if (grid == null) return;
            
            // Находим текстовый элемент в textOverlayGrid и обновляем его
            var textElement = grid.Children.OfType<TextBlock>().FirstOrDefault();
            if (textElement != null)
            {
                // Обновляем свойства текста
                textElement.Text = slot.TextContent ?? "";
                textElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(slot.FontColor ?? "White"));
                textElement.FontFamily = new FontFamily(slot.FontFamily ?? "Arial");
                textElement.FontSize = slot.FontSize;
                textElement.Opacity = 1.0; // Убираем прозрачность, всегда 100%
                
                // Применяем ручную настройку положения
                if (slot.UseManualPosition)
                {
                    textElement.Margin = new Thickness(slot.TextX, slot.TextY, 0, 0);
                    textElement.HorizontalAlignment = HorizontalAlignment.Left;
                    textElement.VerticalAlignment = VerticalAlignment.Top;
                }
                else
                {
                    textElement.Margin = new Thickness(0);
                    textElement.HorizontalAlignment = HorizontalAlignment.Center;
                    textElement.VerticalAlignment = VerticalAlignment.Center;
                }
                
                // Управляем видимостью
                textElement.Visibility = slot.IsTextVisible ? Visibility.Visible : Visibility.Hidden;
                
                // Обновляем видимость textOverlayGrid в зависимости от видимости текста
                grid.Visibility = slot.IsTextVisible ? Visibility.Visible : Visibility.Hidden;
            }
            else
            {
                // Если текстовый элемент не найден, скрываем textOverlayGrid
                grid.Visibility = Visibility.Hidden;
            }
            
            // Также обновляем на втором экране если он активен
            var secondaryWindow = GetSecondaryScreenWindow?.Invoke();
            if (secondaryWindow?.Content is Grid secondaryGrid)
            {
                var secondaryTextElement = secondaryGrid.Children.OfType<TextBlock>().FirstOrDefault();
                if (secondaryTextElement != null)
                {
                    secondaryTextElement.Text = slot.TextContent ?? "";
                    secondaryTextElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(slot.FontColor ?? "White"));
                    secondaryTextElement.FontFamily = new FontFamily(slot.FontFamily ?? "Arial");
                    secondaryTextElement.FontSize = slot.FontSize;
                    secondaryTextElement.Opacity = 1.0; // Убираем прозрачность, всегда 100%
                    
                    if (slot.UseManualPosition)
                    {
                        secondaryTextElement.Margin = new Thickness(slot.TextX, slot.TextY, 0, 0);
                        secondaryTextElement.HorizontalAlignment = HorizontalAlignment.Left;
                        secondaryTextElement.VerticalAlignment = VerticalAlignment.Top;
                    }
                    else
                    {
                        secondaryTextElement.Margin = new Thickness(0);
                        secondaryTextElement.HorizontalAlignment = HorizontalAlignment.Center;
                        secondaryTextElement.VerticalAlignment = VerticalAlignment.Center;
                    }
                    
                    secondaryTextElement.Visibility = slot.IsTextVisible ? Visibility.Visible : Visibility.Hidden;
                }
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
        
        /// <summary>
        /// Получает горизонтальное выравнивание из строки позиции
        /// </summary>
        public HorizontalAlignment GetHorizontalAlignment(string position)
        {
            return position switch
            {
                "TopLeft" or "CenterLeft" or "BottomLeft" => HorizontalAlignment.Left,
                "TopRight" or "CenterRight" or "BottomRight" => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Center
            };
        }
        
        /// <summary>
        /// Получает вертикальное выравнивание из строки позиции
        /// </summary>
        public VerticalAlignment GetVerticalAlignment(string position)
        {
            return position switch
            {
                "TopLeft" or "TopCenter" or "TopRight" => VerticalAlignment.Top,
                "BottomLeft" or "BottomCenter" or "BottomRight" => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center
            };
        }
        
        /// <summary>
        /// Получает выравнивание текста из строки позиции
        /// </summary>
        public TextAlignment GetTextAlignment(string position)
        {
            return position switch
            {
                "TopLeft" or "CenterLeft" or "BottomLeft" => TextAlignment.Left,
                "TopRight" or "CenterRight" or "BottomRight" => TextAlignment.Right,
                _ => TextAlignment.Center
            };
        }
        
        private HorizontalAlignment GetHorizontalAlignmentFromPosition(string? position)
        {
            return GetHorizontalAlignment(position ?? "Center");
        }
        
        private VerticalAlignment GetVerticalAlignmentFromPosition(string? position)
        {
            return GetVerticalAlignment(position ?? "Center");
        }
        
        private TextAlignment GetTextAlignmentFromPosition(string? position)
        {
            return GetTextAlignment(position ?? "Center");
        }
    }
}


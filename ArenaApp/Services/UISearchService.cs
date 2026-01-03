using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для поиска UI элементов в визуальном дереве
    /// </summary>
    public class UISearchService
    {
        // Делегаты для получения UI элементов
        public Func<Border>? GetMediaBorder { get; set; }
        public Func<Panel>? GetBottomPanel { get; set; }
        
        /// <summary>
        /// Находит MediaBorder
        /// </summary>
        public Border? FindMediaBorder()
        {
            return GetMediaBorder?.Invoke();
        }
        
        /// <summary>
        /// Находит кнопку по тегу в BottomPanel
        /// </summary>
        public Button? FindButtonByTag(string tag)
        {
            var bottomPanel = GetBottomPanel?.Invoke();
            if (bottomPanel == null) return null;
            
            return FindVisualChild<Button>(bottomPanel, b => b.Tag?.ToString() == tag);
        }
        
        /// <summary>
        /// Рекурсивно ищет визуальный элемент по условию
        /// </summary>
        public T? FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : DependencyObject
        {
            if (parent == null) return null;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && predicate(t))
                    return t;
                
                var childOfChild = FindVisualChild<T>(child, predicate);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
        
        /// <summary>
        /// Находит первый визуальный элемент указанного типа
        /// </summary>
        public T? FindFirstVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;
                
                var childOfChild = FindFirstVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}


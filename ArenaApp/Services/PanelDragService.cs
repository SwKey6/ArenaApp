using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления перетаскиванием и изменением размера панелей
    /// </summary>
    public class PanelDragService
    {
        private bool _isDragging = false;
        private bool _isResizing = false;
        private bool _isAnyResizingActive = false;
        private Point _lastMousePosition;
        private ResizeDirection _resizeDirection = ResizeDirection.None;
        
        private enum ResizeDirection
        {
            None,
            Vertical,
            Horizontal,
            Diagonal
        }
        
        // Делегаты для доступа к UI элементам
        public Func<FrameworkElement>? GetPanel { get; set; }
        public Func<Point>? GetMousePosition { get; set; }
        public Action<double, double>? SetPanelPosition { get; set; }
        public Action<double, double>? SetPanelSize { get; set; }
        public Func<double, double>? GetPanelWidth { get; set; }
        public Func<double, double>? GetPanelHeight { get; set; }
        public double MinWidth { get; set; } = 300;
        public double MinHeight { get; set; } = 200;
        
        public bool IsAnyResizingActive => _isAnyResizingActive;
        
        public void HandleMouseLeftButtonDown(MouseButtonEventArgs e, bool isResizeHandle = false, ResizeType resizeType = ResizeType.None)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (isResizeHandle && resizeType != ResizeType.None)
                {
                    _isResizing = true;
                    _isAnyResizingActive = true;
                    _lastMousePosition = e.GetPosition(null);
                    
                    _resizeDirection = resizeType switch
                    {
                        ResizeType.Vertical => ResizeDirection.Vertical,
                        ResizeType.Horizontal => ResizeDirection.Horizontal,
                        ResizeType.Diagonal => ResizeDirection.Diagonal,
                        _ => ResizeDirection.None
                    };
                    
                    // Останавливаем перетаскивание панели
                    _isDragging = false;
                }
                else if (!_isAnyResizingActive)
                {
                    _isDragging = true;
                    _lastMousePosition = e.GetPosition(null);
                }
            }
        }
        
        public void HandleMouseMove(MouseEventArgs e)
        {
            if (_isResizing)
            {
                Point currentPosition = e.GetPosition(null);
                double deltaX = currentPosition.X - _lastMousePosition.X;
                double deltaY = currentPosition.Y - _lastMousePosition.Y;
                
                var panel = GetPanel?.Invoke();
                if (panel == null) return;
                
                double newWidth = GetPanelWidth?.Invoke(panel.ActualWidth) ?? panel.ActualWidth;
                double newHeight = GetPanelHeight?.Invoke(panel.ActualHeight) ?? panel.ActualHeight;
                
                switch (_resizeDirection)
                {
                    case ResizeDirection.Vertical:
                        if (newHeight + deltaY >= MinHeight)
                        {
                            newHeight += deltaY;
                            SetPanelSize?.Invoke(newWidth, newHeight);
                        }
                        break;
                    case ResizeDirection.Horizontal:
                        if (newWidth + deltaX >= MinWidth)
                        {
                            newWidth += deltaX;
                            SetPanelSize?.Invoke(newWidth, newHeight);
                        }
                        break;
                    case ResizeDirection.Diagonal:
                        if (newWidth + deltaX >= MinWidth && newHeight + deltaY >= MinHeight)
                        {
                            newWidth += deltaX;
                            newHeight += deltaY;
                            SetPanelSize?.Invoke(newWidth, newHeight);
                        }
                        break;
                }
                
                _lastMousePosition = currentPosition;
            }
            else if (_isDragging && !_isAnyResizingActive)
            {
                Point currentPosition = e.GetPosition(null);
                double deltaX = currentPosition.X - _lastMousePosition.X;
                double deltaY = currentPosition.Y - _lastMousePosition.Y;
                
                SetPanelPosition?.Invoke(deltaX, deltaY);
                _lastMousePosition = currentPosition;
            }
        }
        
        public void HandleMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                _isAnyResizingActive = false;
                _resizeDirection = ResizeDirection.None;
            }
            else
            {
                _isDragging = false;
            }
        }
        
        public void HandleMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.Opacity = 1.0;
            }
        }
        
        public void HandleMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.Opacity = 0.7;
            }
        }
    }
    
    public enum ResizeType
    {
        None,
        Vertical,
        Horizontal,
        Diagonal
    }
}


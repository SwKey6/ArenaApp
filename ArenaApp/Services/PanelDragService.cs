using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        private FrameworkElement? _panel = null;
        private FrameworkElement? _resizeHandle = null;
        private Window? _window = null;
        
        private enum ResizeDirection
        {
            None,
            Vertical,
            Horizontal,
            Diagonal
        }
        
        // Делегаты для доступа к UI элементам
        public Func<FrameworkElement>? GetPanel { get; set; }
        public Func<Window>? GetWindow { get; set; }
        public double MinWidth { get; set; } = 300;
        public double MinHeight { get; set; } = 200;
        public bool UseBottomAnchor { get; set; } = false; // Для панелей с привязкой к низу
        
        public bool IsAnyResizingActive => _isAnyResizingActive;
        
        public void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e, bool isResizeHandle = false, ResizeType resizeType = ResizeType.None)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            
            _panel = GetPanel?.Invoke();
            _window = GetWindow?.Invoke();
            if (_panel == null || _window == null) return;
            
            if (isResizeHandle && resizeType != ResizeType.None)
            {
                _resizeHandle = sender as FrameworkElement;
                if (_resizeHandle == null) return;
                
                _isResizing = true;
                _isAnyResizingActive = true;
                _lastMousePosition = e.GetPosition(_window);
                _resizeHandle.CaptureMouse();
                
                _resizeDirection = resizeType switch
                {
                    ResizeType.Vertical => ResizeDirection.Vertical,
                    ResizeType.Horizontal => ResizeDirection.Horizontal,
                    ResizeType.Diagonal => ResizeDirection.Diagonal,
                    _ => ResizeDirection.None
                };
                
                // Останавливаем перетаскивание панели
                _isDragging = false;
                _panel.ReleaseMouseCapture();
            }
            else if (!_isAnyResizingActive)
            {
                _isDragging = true;
                _lastMousePosition = e.GetPosition(_window);
                _panel.CaptureMouse();
            }
        }
        
        public void HandleMouseMove(object sender, MouseEventArgs e)
        {
            if (_panel == null || _window == null) return;
            
            if (_isResizing)
            {
                Point currentPosition = e.GetPosition(_window);
                double deltaX = currentPosition.X - _lastMousePosition.X;
                double deltaY = currentPosition.Y - _lastMousePosition.Y;
                
                double currentWidth = _panel.Width;
                double currentHeight = _panel.Height;
                
                switch (_resizeDirection)
                {
                    case ResizeDirection.Vertical:
                        double newHeight = currentHeight + (UseBottomAnchor ? -deltaY : deltaY);
                        if (newHeight >= MinHeight)
                        {
                            _panel.Height = newHeight;
                            if (UseBottomAnchor)
                            {
                                // Для панелей с Bottom anchor нужно корректировать позицию
                                Canvas.SetBottom(_panel, Canvas.GetBottom(_panel) + deltaY);
                            }
                        }
                        break;
                    case ResizeDirection.Horizontal:
                        double newWidth = currentWidth + deltaX;
                        if (newWidth >= MinWidth)
                        {
                            _panel.Width = newWidth;
                        }
                        break;
                    case ResizeDirection.Diagonal:
                        double diagonalHeight = currentHeight + (UseBottomAnchor ? -deltaY : deltaY);
                        double diagonalWidth = currentWidth + deltaX;
                        if (diagonalWidth >= MinWidth && diagonalHeight >= MinHeight)
                        {
                            _panel.Width = diagonalWidth;
                            _panel.Height = diagonalHeight;
                            if (UseBottomAnchor)
                            {
                                Canvas.SetBottom(_panel, Canvas.GetBottom(_panel) + deltaY);
                            }
                        }
                        break;
                }
                
                _lastMousePosition = currentPosition;
            }
            else if (_isDragging && !_isAnyResizingActive)
            {
                Point currentPosition = e.GetPosition(_window);
                double deltaX = currentPosition.X - _lastMousePosition.X;
                double deltaY = currentPosition.Y - _lastMousePosition.Y;
                
                // Обновляем позицию панели в Canvas
                if (UseBottomAnchor)
                {
                    Canvas.SetLeft(_panel, Canvas.GetLeft(_panel) + deltaX);
                    Canvas.SetBottom(_panel, Canvas.GetBottom(_panel) - deltaY);
                }
                else
                {
                    Canvas.SetLeft(_panel, Canvas.GetLeft(_panel) + deltaX);
                    Canvas.SetTop(_panel, Canvas.GetTop(_panel) + deltaY);
                }
                
                _lastMousePosition = currentPosition;
            }
        }
        
        public void HandleMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing && _resizeHandle != null)
            {
                _isResizing = false;
                _isAnyResizingActive = false;
                _resizeDirection = ResizeDirection.None;
                _resizeHandle.ReleaseMouseCapture();
                _resizeHandle = null;
            }
            else if (_isDragging && _panel != null)
            {
                _isDragging = false;
                _panel.ReleaseMouseCapture();
            }
            _panel = null;
            _window = null;
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


using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingPoint = System.Drawing.Point;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления вторичным экраном
    /// </summary>
    public class SecondaryScreenService
    {
        // Win32 API для позиционирования окна
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(DrawingPoint pt, uint dwFlags);
        
        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
        
        private const int MONITOR_DEFAULTTONEAREST = 2;
        private const int MDT_EFFECTIVE_DPI = 0;
        
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        
        private Window? _secondaryScreenWindow = null;
        private MediaElement? _secondaryMediaElement = null;
        
        // Делегаты для работы с устройствами
        public Func<bool>? GetUseSelectedScreen { get; set; }
        public Func<int>? GetSelectedScreenIndex { get; set; }
        public Func<bool>? GetUseUniformToFill { get; set; }
        
        // Делегаты для синхронизации с основным экраном
        public Func<MediaElement>? GetMainMediaElement { get; set; }
        public Func<System.Windows.Controls.Image>? GetMainImageElement { get; set; }
        public Action<MediaElement>? SetSecondaryMediaElement { get; set; }
        public Func<Window?>? GetSecondaryScreenWindow { get; set; }
        
        public Window? SecondaryScreenWindow => _secondaryScreenWindow;
        public MediaElement? SecondaryMediaElement => _secondaryMediaElement;
        
        /// <summary>
        /// Создает окно на дополнительном экране
        /// </summary>
        public void CreateSecondaryScreenWindow()
        {
            try
            {
                if (GetUseSelectedScreen?.Invoke() != true) return;
                
                var selectedScreenIndex = GetSelectedScreenIndex?.Invoke() ?? -1;
                var screens = System.Windows.Forms.Screen.AllScreens;
                
                if (selectedScreenIndex >= 0 && selectedScreenIndex < screens.Length)
                {
                    var screen = screens[selectedScreenIndex];
                    
                    // Закрываем предыдущее окно если есть
                    CloseSecondaryScreenWindow();
                    
                    // Отладочная информация
                    System.Diagnostics.Debug.WriteLine($"СОЗДАНИЕ ОКНА НА ЭКРАНЕ: Индекс={selectedScreenIndex}, Разрешение={screen.Bounds.Width}x{screen.Bounds.Height}, Позиция=({screen.Bounds.X}, {screen.Bounds.Y}), Основной={screen.Primary}");
                    
                    // Создаем новое окно
                    // ВАЖНО: Не устанавливаем Left/Top при создании, так как WPF может неправильно интерпретировать координаты
                    // Установим их через Win32 API после создания
                    _secondaryScreenWindow = new Window
                    {
                        Title = "ArenaApp - Дополнительный экран",
                        WindowStyle = WindowStyle.None,
                        AllowsTransparency = false,
                        ResizeMode = ResizeMode.NoResize,
                        Topmost = true,
                        Background = Brushes.Black,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        // Устанавливаем размер, но НЕ позицию - она будет установлена через Win32 API
                        Width = screen.Bounds.Width,
                        Height = screen.Bounds.Height,
                        WindowState = WindowState.Normal,
                        // Временно устанавливаем позицию на основном экране, чтобы окно создалось
                        Left = 0,
                        Top = 0
                    };
                    
                    // Создаем MediaElement для дополнительного экрана
                    var useUniformToFill = GetUseUniformToFill?.Invoke() ?? false;
                    _secondaryMediaElement = new MediaElement
                    {
                        LoadedBehavior = MediaState.Manual,
                        Stretch = useUniformToFill ? Stretch.UniformToFill : Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Margin = new Thickness(0),
                        Volume = 0 // Отключаем звук на втором экране чтобы избежать дублирования
                    };
                    
                    _secondaryScreenWindow.Content = _secondaryMediaElement;
                    
                    // Уведомляем о создании MediaElement
                    SetSecondaryMediaElement?.Invoke(_secondaryMediaElement);
                    
                    // Показываем окно сначала (на основном экране)
                    _secondaryScreenWindow.Show();
                    
                    // Сразу после показа используем Win32 API для точного позиционирования
                    var windowHelper = new WindowInteropHelper(_secondaryScreenWindow);
                    windowHelper.EnsureHandle();
                    var handle = windowHelper.Handle;
                    
                    if (handle != IntPtr.Zero)
                    {
                        // Получаем DPI для экрана (может влиять на координаты)
                        var screenPoint = new DrawingPoint(screen.Bounds.X + screen.Bounds.Width / 2, screen.Bounds.Y + screen.Bounds.Height / 2);
                        var monitorHandle = MonitorFromPoint(screenPoint, MONITOR_DEFAULTTONEAREST);
                        uint dpiX = 96, dpiY = 96;
                        if (monitorHandle != IntPtr.Zero)
                        {
                            GetDpiForMonitor(monitorHandle, MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                        }
                        System.Diagnostics.Debug.WriteLine($"DPI для экрана {selectedScreenIndex + 1}: DPI_X={dpiX}, DPI_Y={dpiY}");
                        
                        // Используем точные координаты из screen.Bounds
                        // Эти координаты уже в физических пикселях экрана
                        int targetX = screen.Bounds.X;
                        int targetY = screen.Bounds.Y;
                        int targetWidth = screen.Bounds.Width;
                        int targetHeight = screen.Bounds.Height;
                        
                        System.Diagnostics.Debug.WriteLine($"Целевые координаты (screen.Bounds): X={targetX}, Y={targetY}, W={targetWidth}, H={targetHeight}");
                        
                        // Используем Win32 API для установки позиции и размера
                        // Это работает корректно независимо от расположения мониторов (включая отрицательные координаты)
                        // ВАЖНО: Используем только Win32 API для позиции, не устанавливаем через WPF Left/Top
                        bool result = SetWindowPos(handle, IntPtr.Zero, 
                            targetX, 
                            targetY, 
                            targetWidth, 
                            targetHeight, 
                            SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                        
                        System.Diagnostics.Debug.WriteLine($"Win32 SetWindowPos вызван: X={targetX}, Y={targetY}, W={targetWidth}, H={targetHeight}, Result={result}");
                        
                        // Небольшая задержка перед проверкой, чтобы окно успело переместиться
                        System.Threading.Thread.Sleep(100);
                        
                        // Проверяем фактическую позицию через Win32 API
                        if (GetWindowRect(handle, out RECT rect))
                        {
                            int actualX = rect.Left;
                            int actualY = rect.Top;
                            int actualWidth = rect.Right - rect.Left;
                            int actualHeight = rect.Bottom - rect.Top;
                            
                            System.Diagnostics.Debug.WriteLine($"Win32 GetWindowRect (после SetWindowPos): X={actualX}, Y={actualY}, W={actualWidth}, H={actualHeight}");
                            System.Diagnostics.Debug.WriteLine($"Разница координат: dX={actualX - targetX}, dY={actualY - targetY}");
                            
                            // Если позиция не совпадает, пытаемся исправить
                            if (Math.Abs(actualX - targetX) > 5 || Math.Abs(actualY - targetY) > 5)
                            {
                                System.Diagnostics.Debug.WriteLine($"ПОЗИЦИЯ НЕ СОВПАДАЕТ! Ожидалось: X={targetX}, Y={targetY}, Получено: X={actualX}, Y={actualY}");
                                
                                // Вычисляем корректировку
                                int correctedX = targetX - (actualX - targetX);
                                int correctedY = targetY - (actualY - targetY);
                                
                                System.Diagnostics.Debug.WriteLine($"Попытка корректировки: X={correctedX}, Y={correctedY}");
                                
                                // Повторная попытка с корректировкой
                                SetWindowPos(handle, IntPtr.Zero, 
                                    correctedX, 
                                    correctedY, 
                                    targetWidth, 
                                    targetHeight, 
                                    SWP_NOZORDER | SWP_NOACTIVATE);
                                
                                // Проверяем еще раз
                                System.Threading.Thread.Sleep(100);
                                if (GetWindowRect(handle, out RECT rect2))
                                {
                                    System.Diagnostics.Debug.WriteLine($"Win32 GetWindowRect (после корректировки): X={rect2.Left}, Y={rect2.Top}, W={rect2.Right - rect2.Left}, H={rect2.Bottom - rect2.Top}");
                                }
                            }
                        }
                    }
                    
                    // НЕ устанавливаем Left/Top через WPF, так как это может конфликтовать с Win32 API
                    // Устанавливаем только размер через WPF для совместимости
                    _secondaryScreenWindow.Width = screen.Bounds.Width;
                    _secondaryScreenWindow.Height = screen.Bounds.Height;
                    
                    // Добавляем обработчик для проверки и корректировки после полной загрузки
                    _secondaryScreenWindow.Loaded += (s, e) =>
                    {
                        _secondaryScreenWindow.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // Повторно устанавливаем через Win32 API после загрузки
                            var hwnd = new WindowInteropHelper(_secondaryScreenWindow).Handle;
                            if (hwnd != IntPtr.Zero)
                            {
                                bool result = SetWindowPos(hwnd, IntPtr.Zero, 
                                    screen.Bounds.X, 
                                    screen.Bounds.Y, 
                                    screen.Bounds.Width, 
                                    screen.Bounds.Height, 
                                    SWP_NOZORDER | SWP_NOACTIVATE);
                                
                                System.Diagnostics.Debug.WriteLine($"Win32 SetWindowPos (Loaded): Result={result}");
                                
                                // Проверяем фактическую позицию
                                if (GetWindowRect(hwnd, out RECT rect))
                                {
                                    int actualWidth = rect.Right - rect.Left;
                                    int actualHeight = rect.Bottom - rect.Top;
                                    System.Diagnostics.Debug.WriteLine($"Win32 GetWindowRect (Loaded): X={rect.Left}, Y={rect.Top}, W={actualWidth}, H={actualHeight}");
                                    
                                    // Если размеры не совпадают, пытаемся исправить
                                    if (actualWidth != screen.Bounds.Width || actualHeight != screen.Bounds.Height)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"РАЗМЕРЫ НЕ СОВПАДАЮТ! Повторная попытка установки...");
                                        SetWindowPos(hwnd, IntPtr.Zero, 
                                            screen.Bounds.X, 
                                            screen.Bounds.Y, 
                                            screen.Bounds.Width, 
                                            screen.Bounds.Height, 
                                            SWP_NOZORDER | SWP_NOACTIVATE);
                                    }
                                }
                            }
                            
                            // НЕ устанавливаем Left/Top через WPF - используем только Win32 API
                            // Устанавливаем только размер через WPF для совместимости
                            _secondaryScreenWindow.Width = screen.Bounds.Width;
                            _secondaryScreenWindow.Height = screen.Bounds.Height;
                            _secondaryScreenWindow.UpdateLayout();
                            
                            System.Diagnostics.Debug.WriteLine($"ОКНО СОЗДАНО НА ЭКРАНЕ {selectedScreenIndex + 1}:");
                            System.Diagnostics.Debug.WriteLine($"  Целевые координаты: X={screen.Bounds.X}, Y={screen.Bounds.Y}");
                            System.Diagnostics.Debug.WriteLine($"  Целевой размер: {screen.Bounds.Width}x{screen.Bounds.Height}");
                            System.Diagnostics.Debug.WriteLine($"  Фактическая позиция (WPF): X={_secondaryScreenWindow.Left}, Y={_secondaryScreenWindow.Top}");
                            System.Diagnostics.Debug.WriteLine($"  Фактический размер (WPF): {_secondaryScreenWindow.Width}x{_secondaryScreenWindow.Height}");
                            System.Diagnostics.Debug.WriteLine($"  Фактический размер (Actual): {_secondaryScreenWindow.ActualWidth}x{_secondaryScreenWindow.ActualHeight}");
                            System.Diagnostics.Debug.WriteLine($"  WindowState: {_secondaryScreenWindow.WindowState}");
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    };
                    
                    // Отладочная информация в консоль вместо MessageBox
                    System.Diagnostics.Debug.WriteLine($"Окно создано на экране {selectedScreenIndex + 1}! Позиция: ({screen.Bounds.X}, {screen.Bounds.Y}), Размер: {screen.Bounds.Width}x{screen.Bounds.Height}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна на дополнительном экране: {ex.Message}", "Ошибка");
            }
        }
        
        /// <summary>
        /// Закрывает окно на дополнительном экране
        /// </summary>
        public void CloseSecondaryScreenWindow()
        {
            try
            {
                if (_secondaryScreenWindow != null)
                {
                    _secondaryScreenWindow.Close();
                    _secondaryScreenWindow = null;
                    _secondaryMediaElement = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при закрытии окна на дополнительном экране: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Синхронизирует паузу со вторичным экраном
        /// </summary>
        public void SyncPauseWithSecondaryScreen()
        {
            try
            {
                if (_secondaryMediaElement != null && _secondaryMediaElement.Source != null)
                {
                    var mainMediaElement = GetMainMediaElement?.Invoke();
                    if (mainMediaElement != null && mainMediaElement.Source != null)
                    {
                        // Проверяем, что это тот же файл
                        if (mainMediaElement.Source.LocalPath == _secondaryMediaElement.Source.LocalPath)
                        {
                            _secondaryMediaElement.Pause();
                            System.Diagnostics.Debug.WriteLine("ПАУЗА: Синхронизировано со вторым экраном");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при синхронизации паузы со вторым экраном: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Синхронизирует воспроизведение со вторичным экраном
        /// </summary>
        public void SyncPlayWithSecondaryScreen()
        {
            try
            {
                if (_secondaryMediaElement != null && _secondaryMediaElement.Source != null)
                {
                    var mainMediaElement = GetMainMediaElement?.Invoke();
                    if (mainMediaElement != null && mainMediaElement.Source != null)
                    {
                        // Проверяем, что это тот же файл
                        if (mainMediaElement.Source.LocalPath == _secondaryMediaElement.Source.LocalPath)
                        {
                            // Синхронизируем позицию перед воспроизведением
                            _secondaryMediaElement.Position = mainMediaElement.Position;
                            _secondaryMediaElement.Play();
                            System.Diagnostics.Debug.WriteLine("ВОСПРОИЗВЕДЕНИЕ: Синхронизировано со вторым экраном");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при синхронизации воспроизведения со вторым экраном: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Обновляет контент вторичного экрана для изображения
        /// </summary>
        public void UpdateSecondaryScreenForImage(string imagePath)
        {
            try
            {
                if (_secondaryScreenWindow != null)
                {
                    var useUniformToFill = GetUseUniformToFill?.Invoke() ?? false;
                    var secondaryImageElement = new Image
                    {
                        Source = new BitmapImage(new Uri(imagePath)),
                        Stretch = useUniformToFill ? Stretch.UniformToFill : Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Margin = new Thickness(0)
                    };
                    _secondaryScreenWindow.Content = secondaryImageElement;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении вторичного экрана для изображения: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Обновляет контент вторичного экрана для видео
        /// </summary>
        public void UpdateSecondaryScreenForVideo()
        {
            try
            {
                if (_secondaryScreenWindow != null && _secondaryMediaElement != null)
                {
                    _secondaryScreenWindow.Content = _secondaryMediaElement;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении вторичного экрана для видео: {ex.Message}");
            }
        }
    }
}


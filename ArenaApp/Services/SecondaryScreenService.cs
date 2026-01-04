using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
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
        
        // Делегаты для VLC VideoView
        public Func<LibVLCSharp.Shared.MediaPlayer>? GetVlcSecondaryPlayer { get; set; }
        public Action<VideoView>? SetSecondaryVlcVideoView { get; set; }
        
        // Делегаты для настроек вывода
        public Func<(int x, int y)>? GetOutputPosition { get; set; }
        public Func<(int width, int height)>? GetOutputSize { get; set; }
        public Func<(double scaleX, double scaleY)>? GetOutputScale { get; set; }
        
        // Делегат для синхронизации медиа после создания окна
        public Action? SyncMediaAfterWindowCreated { get; set; }
        
        public Window? SecondaryScreenWindow => _secondaryScreenWindow;
        public MediaElement? SecondaryMediaElement => _secondaryMediaElement;
        public VideoView? SecondaryVlcVideoView { get; private set; }
        
        /// <summary>
        /// Применяет настройки вывода к окну второго экрана
        /// </summary>
        public void ApplyOutputSettings()
        {
            if (_secondaryScreenWindow == null) return;
            
            try
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                var selectedScreenIndex = GetSelectedScreenIndex?.Invoke() ?? -1;
                
                if (selectedScreenIndex >= 0 && selectedScreenIndex < screens.Length)
                {
                    var screen = screens[selectedScreenIndex];
                    
                    // Получаем настройки
                    var position = GetOutputPosition?.Invoke() ?? (0, 0);
                    var size = GetOutputSize?.Invoke() ?? (1920, 1080);
                    var scale = GetOutputScale?.Invoke() ?? (100.0, 100.0);

                    // Базовые значения
                    var baseWidth = size.width;
                    var baseHeight = size.height;

                    // ВАЖНО: LibVLCSharp.WPF.VideoView обычно рендерит через нативную поверхность (HwndHost),
                    // и WPF-трансформации (RenderTransform/LayoutTransform) могут не работать визуально.
                    // Поэтому, если на втором экране активен VideoView, применяем Scale через реальный размер окна.
                    bool hasVlcVideoView = false;
                    if (_secondaryScreenWindow.Content is VideoView)
                    {
                        hasVlcVideoView = true;
                    }
                    else if (_secondaryScreenWindow.Content is Grid grid)
                    {
                        hasVlcVideoView = grid.Children.OfType<VideoView>().Any();
                    }

                    int finalWidth;
                    int finalHeight;
                    int finalX;
                    int finalY;

                    if (hasVlcVideoView)
                    {
                        // Масштабируем окно относительно "базового" размера и центрируем внутри базовой области
                        finalWidth = (int)Math.Max(1, Math.Round(baseWidth * (scale.scaleX / 100.0)));
                        finalHeight = (int)Math.Max(1, Math.Round(baseHeight * (scale.scaleY / 100.0)));

                        var centerOffsetX = (baseWidth - finalWidth) / 2;
                        var centerOffsetY = (baseHeight - finalHeight) / 2;

                        finalX = screen.Bounds.X + position.x + centerOffsetX;
                        finalY = screen.Bounds.Y + position.y + centerOffsetY;

                        // На всякий случай сбрасываем трансформации контента (чтобы не было двойного масштаба)
                        ApplyScaleToContent(1.0, 1.0);
                    }
                    else
                    {
                        // Для обычных WPF-элементов масштабируем содержимое, окно оставляем базового размера
                        finalWidth = baseWidth;
                        finalHeight = baseHeight;
                        finalX = screen.Bounds.X + position.x;
                        finalY = screen.Bounds.Y + position.y;

                        // Применяем масштаб к содержимому СРАЗУ, до установки позиции
                        ApplyScaleToContent(scale.scaleX / 100.0, scale.scaleY / 100.0);
                    }
                    
                    // Применяем размер окна
                    _secondaryScreenWindow.Width = finalWidth;
                    _secondaryScreenWindow.Height = finalHeight;
                    
                    // Применяем позицию через Win32 API
                    var windowHelper = new WindowInteropHelper(_secondaryScreenWindow);
                    var handle = windowHelper.Handle;
                    
                    if (handle != IntPtr.Zero)
                    {
                        const uint SWP_FRAMECHANGED = 0x0020;
                        SetWindowPos(handle, IntPtr.Zero, 
                            finalX, 
                            finalY, 
                            finalWidth, 
                            finalHeight, 
                            SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_FRAMECHANGED);
                        
                        System.Diagnostics.Debug.WriteLine($"Применены настройки вывода: Позиция=({finalX}, {finalY}), Размер={finalWidth}x{finalHeight}, Скейл=({scale.scaleX}%, {scale.scaleY}%), VLC={hasVlcVideoView}");
                    }
                    else
                    {
                        _secondaryScreenWindow.Left = finalX;
                        _secondaryScreenWindow.Top = finalY;
                    }
                    
                    // Принудительно обновляем layout после применения всех настроек
                    _secondaryScreenWindow.UpdateLayout();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при применении настроек вывода: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Применяет масштаб к содержимому окна второго экрана
        /// </summary>
        private void ApplyScaleToContent(double scaleX, double scaleY)
        {
            if (_secondaryScreenWindow == null) return;
            
            try
            {
                // Отключаем обрезку на окне, чтобы масштабированное содержимое не обрезалось
                _secondaryScreenWindow.ClipToBounds = false;
                
                System.Diagnostics.Debug.WriteLine($"ApplyScaleToContent: Начинаем применение масштаба ScaleX={scaleX}, ScaleY={scaleY}");
                
                // ВАЖНО: Применяем RenderTransform напрямую к MediaElement/Image, а не к Grid
                // Это позволяет масштабировать содержимое независимо от размера контейнера
                
                // Применяем масштаб к MediaElement, если он есть
                if (_secondaryMediaElement != null)
                {
                    _secondaryMediaElement.ClipToBounds = false;
                    
                    // ВАЖНО: НЕ меняем Stretch во время применения масштаба, чтобы избежать глитчей
                    // Stretch должен быть установлен при создании MediaElement
                    
                    // Применяем RenderTransform для масштабирования
                    var transform = new ScaleTransform(scaleX, scaleY);
                    _secondaryMediaElement.RenderTransform = transform;
                    _secondaryMediaElement.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                    
                    // Принудительно обновляем MediaElement
                    _secondaryMediaElement.InvalidateVisual();
                    
                    System.Diagnostics.Debug.WriteLine($"Применен RenderTransform к MediaElement: ScaleX={scaleX}, ScaleY={scaleY}, Stretch={_secondaryMediaElement.Stretch}, ActualWidth={_secondaryMediaElement.ActualWidth}, ActualHeight={_secondaryMediaElement.ActualHeight}");
                }
                
                // Применяем масштаб к VLC VideoView, если он есть
                if (SecondaryVlcVideoView != null)
                {
                    SecondaryVlcVideoView.ClipToBounds = false;
                    var transform = new ScaleTransform(scaleX, scaleY);
                    SecondaryVlcVideoView.RenderTransform = transform;
                    SecondaryVlcVideoView.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                    SecondaryVlcVideoView.InvalidateVisual();
                    System.Diagnostics.Debug.WriteLine($"Применен RenderTransform к VLC VideoView: ScaleX={scaleX}, ScaleY={scaleY}, ActualWidth={SecondaryVlcVideoView.ActualWidth}, ActualHeight={SecondaryVlcVideoView.ActualHeight}");
                }

                // Применяем масштаб к Image, если он есть в Grid
                if (_secondaryScreenWindow.Content is Grid gridContainer)
                {
                    gridContainer.ClipToBounds = false;
                    
                    var imageElement = gridContainer.Children.OfType<Image>().FirstOrDefault();
                    if (imageElement != null)
                    {
                        imageElement.ClipToBounds = false;
                        imageElement.Stretch = Stretch.Fill;
                        
                        var transform = new ScaleTransform(scaleX, scaleY);
                        imageElement.RenderTransform = transform;
                        imageElement.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                        
                        System.Diagnostics.Debug.WriteLine($"Применен RenderTransform к Image: ScaleX={scaleX}, ScaleY={scaleY}, ActualWidth={imageElement.ActualWidth}, ActualHeight={imageElement.ActualHeight}");
                    }

                    // Если в Grid лежит VLC VideoView (например, для VLC-видео), тоже масштабируем его
                    var videoViewElement = gridContainer.Children.OfType<VideoView>().FirstOrDefault();
                    if (videoViewElement != null)
                    {
                        videoViewElement.ClipToBounds = false;
                        var transform = new ScaleTransform(scaleX, scaleY);
                        videoViewElement.RenderTransform = transform;
                        videoViewElement.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                        videoViewElement.InvalidateVisual();
                        System.Diagnostics.Debug.WriteLine($"Применен RenderTransform к Grid.VideoView: ScaleX={scaleX}, ScaleY={scaleY}, ActualWidth={videoViewElement.ActualWidth}, ActualHeight={videoViewElement.ActualHeight}");
                    }
                    
                    // НЕ применяем трансформацию к Grid, чтобы не конфликтовать с трансформацией элементов
                    gridContainer.LayoutTransform = null;
                    gridContainer.RenderTransform = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при применении масштаба к содержимому: {ex.Message}");
            }
        }
        
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
                    
                    // Получаем настройки размера и скейла
                    var size = GetOutputSize?.Invoke() ?? (screen.Bounds.Width, screen.Bounds.Height);
                    var scale = GetOutputScale?.Invoke() ?? (100.0, 100.0);
                    // ВАЖНО: Размер окна устанавливаем БЕЗ масштаба, масштаб будет применен к содержимому через RenderTransform
                    var finalWidth = size.width;
                    var finalHeight = size.height;
                    
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
                        // Устанавливаем базовый размер БЕЗ масштаба - масштаб будет применен к содержимому
                        Width = finalWidth,
                        Height = finalHeight,
                        ClipToBounds = false, // ВАЖНО: Отключаем обрезку, чтобы масштабированное содержимое не обрезалось
                        WindowState = WindowState.Normal,
                        // Временно устанавливаем позицию на основном экране, чтобы окно создалось
                        Left = 0,
                        Top = 0,
                        // Убеждаемся, что содержимое окна правильно растягивается
                        SizeToContent = SizeToContent.Manual,
                        // Убираем все отступы и границы
                        Padding = new Thickness(0),
                        BorderThickness = new Thickness(0)
                    };
                    
                    // Создаем Grid как контейнер для правильного растягивания
                    var contentGrid = new Grid
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Margin = new Thickness(0),
                        Background = Brushes.Black,
                        ClipToBounds = false // ВАЖНО: Отключаем обрезку, чтобы масштаб мог увеличивать содержимое
                    };
                    
                    // Создаем MediaElement для дополнительного экрана
                    var useUniformToFill = GetUseUniformToFill?.Invoke() ?? false;
                    _secondaryMediaElement = new MediaElement
                    {
                        LoadedBehavior = MediaState.Manual,
                        Stretch = useUniformToFill ? Stretch.UniformToFill : Stretch.Uniform, // Возвращаем нормальный Stretch для качества
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Margin = new Thickness(0),
                        Volume = 0, // Отключаем звук на втором экране чтобы избежать дублирования
                        ClipToBounds = false // Отключаем обрезку для масштабирования
                    };
                    
                    contentGrid.Children.Add(_secondaryMediaElement);
                    _secondaryScreenWindow.Content = contentGrid;
                    
                    // Уведомляем о создании MediaElement
                    SetSecondaryMediaElement?.Invoke(_secondaryMediaElement);
                    
                    // Подготавливаем VLC VideoView для второго экрана (используем только при VLC-видео)
                    if (GetVlcSecondaryPlayer != null)
                    {
                        try
                        {
                            var secondaryPlayer = GetVlcSecondaryPlayer.Invoke();
                            if (secondaryPlayer != null)
                            {
                                // Создаем VLC VideoView для второго экрана
                                SecondaryVlcVideoView = new VideoView
                                {
                                    MediaPlayer = secondaryPlayer,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                    VerticalContentAlignment = VerticalAlignment.Stretch,
                                    Margin = new Thickness(0)
                                };
                                
                                SetSecondaryVlcVideoView?.Invoke(SecondaryVlcVideoView);
                                System.Diagnostics.Debug.WriteLine("SecondaryScreenService: VLC VideoView создан для второго экрана");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка при создании VLC VideoView для второго экрана: {ex.Message}");
                        }
                    }
                    
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
                        
                        // Используем уже вычисленные значения finalWidth и finalHeight
                        // Позиция уже учтена через position выше
                        var position = GetOutputPosition?.Invoke() ?? (0, 0);
                        
                        // Вычисляем финальные значения с учетом настроек
                        int targetX = screen.Bounds.X + position.x;
                        int targetY = screen.Bounds.Y + position.y;
                        // Используем уже вычисленные finalWidth и finalHeight
                        
                        System.Diagnostics.Debug.WriteLine($"Целевые координаты (screen.Bounds): X={targetX}, Y={targetY}, W={finalWidth}, H={finalHeight}");
                        System.Diagnostics.Debug.WriteLine($"WorkingArea: X={screen.WorkingArea.X}, Y={screen.WorkingArea.Y}, W={screen.WorkingArea.Width}, H={screen.WorkingArea.Height}");
                        
                        // Используем Win32 API для установки позиции и размера
                        // Это работает корректно независимо от расположения мониторов (включая отрицательные координаты)
                        // ВАЖНО: Используем только Win32 API для позиции, не устанавливаем через WPF Left/Top
                        // Используем SWP_FRAMECHANGED для обновления границ окна
                        const uint SWP_FRAMECHANGED = 0x0020;
                        bool result = SetWindowPos(handle, IntPtr.Zero, 
                            targetX, 
                            targetY, 
                            finalWidth, 
                            finalHeight, 
                            SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_FRAMECHANGED);
                        
                        System.Diagnostics.Debug.WriteLine($"Win32 SetWindowPos вызван: X={targetX}, Y={targetY}, W={finalWidth}, H={finalHeight}, Result={result}");
                        System.Diagnostics.Debug.WriteLine($"Настройки вывода: Позиция=({position.x}, {position.y}), Размер={size.width}x{size.height}, Скейл=({scale.scaleX}%, {scale.scaleY}%)");
                        
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
                                    finalWidth, 
                                    finalHeight, 
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
                    // Размер уже установлен выше с учетом настроек
                    
                    // Добавляем обработчик для проверки и корректировки после полной загрузки
                    _secondaryScreenWindow.Loaded += (s, e) =>
                    {
                        _secondaryScreenWindow.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // Повторно применяем настройки вывода после загрузки окна
                            ApplyOutputSettings();
                            
                            // Проверяем фактическую позицию
                            var hwnd = new WindowInteropHelper(_secondaryScreenWindow).Handle;
                            if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out RECT rect))
                            {
                                    int actualWidth = rect.Right - rect.Left;
                                    int actualHeight = rect.Bottom - rect.Top;
                                    System.Diagnostics.Debug.WriteLine($"Win32 GetWindowRect (Loaded): X={rect.Left}, Y={rect.Top}, W={actualWidth}, H={actualHeight}");
                                    
                                    // Получаем настройки для проверки
                                    var position = GetOutputPosition?.Invoke() ?? (0, 0);
                                    var size = GetOutputSize?.Invoke() ?? (1920, 1080);
                                    var scale = GetOutputScale?.Invoke() ?? (100.0, 100.0);
                                    var expectedWidth = (int)(size.width * (scale.scaleX / 100.0));
                                    var expectedHeight = (int)(size.height * (scale.scaleY / 100.0));
                                    var expectedX = screen.Bounds.X + position.x;
                                    var expectedY = screen.Bounds.Y + position.y;
                                    
                                    System.Diagnostics.Debug.WriteLine($"Ожидалось: X={expectedX}, Y={expectedY}, W={expectedWidth}, H={expectedHeight}");
                                    
                                    // Если размеры не совпадают, повторно применяем настройки
                                    if (Math.Abs(actualWidth - expectedWidth) > 5 || Math.Abs(actualHeight - expectedHeight) > 5)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"РАЗМЕРЫ НЕ СОВПАДАЮТ! Повторная попытка применения настроек...");
                                        ApplyOutputSettings();
                                    }
                                }
                            
                            // Размеры обновляются автоматически через Grid и Stretch alignment
                            
                            _secondaryScreenWindow.UpdateLayout();
                            
                            // Получаем настройки для логирования
                            var logPosition = GetOutputPosition?.Invoke() ?? (0, 0);
                            var logSize = GetOutputSize?.Invoke() ?? (1920, 1080);
                            var logScale = GetOutputScale?.Invoke() ?? (100.0, 100.0);
                            var logExpectedWidth = (int)(logSize.width * (logScale.scaleX / 100.0));
                            var logExpectedHeight = (int)(logSize.height * (logScale.scaleY / 100.0));
                            var logExpectedX = screen.Bounds.X + logPosition.x;
                            var logExpectedY = screen.Bounds.Y + logPosition.y;
                            
                            System.Diagnostics.Debug.WriteLine($"ОКНО СОЗДАНО НА ЭКРАНЕ {selectedScreenIndex + 1}:");
                            System.Diagnostics.Debug.WriteLine($"  Целевые координаты: X={logExpectedX}, Y={logExpectedY}");
                            System.Diagnostics.Debug.WriteLine($"  Целевой размер: {logExpectedWidth}x{logExpectedHeight}");
                            System.Diagnostics.Debug.WriteLine($"  Настройки: Позиция=({logPosition.x}, {logPosition.y}), Размер={logSize.width}x{logSize.height}, Скейл=({logScale.scaleX}%, {logScale.scaleY}%)");
                            System.Diagnostics.Debug.WriteLine($"  Фактическая позиция (WPF): X={_secondaryScreenWindow.Left}, Y={_secondaryScreenWindow.Top}");
                            System.Diagnostics.Debug.WriteLine($"  Фактический размер (WPF): {_secondaryScreenWindow.Width}x{_secondaryScreenWindow.Height}");
                            System.Diagnostics.Debug.WriteLine($"  Фактический размер (Actual): {_secondaryScreenWindow.ActualWidth}x{_secondaryScreenWindow.ActualHeight}");
                            System.Diagnostics.Debug.WriteLine($"  WindowState: {_secondaryScreenWindow.WindowState}");
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    };
                    
                    // Размеры обновляются автоматически через Grid и Stretch alignment
                    
                    // Синхронизируем медиа после создания окна (если есть текущее медиа)
                    _secondaryScreenWindow.Loaded += (s, e) =>
                    {
                        _secondaryScreenWindow.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // Применяем настройки вывода (позиция, размер, масштаб)
                            ApplyOutputSettings();
                            
                            // Синхронизируем медиа со вторым экраном после небольшой задержки
                            _secondaryScreenWindow.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                SyncMediaAfterWindowCreated?.Invoke();
                                
                                // Повторно применяем масштаб после синхронизации медиа, чтобы убедиться, что он применен
                                var scale = GetOutputScale?.Invoke() ?? (100.0, 100.0);
                                ApplyScaleToContent(scale.scaleX / 100.0, scale.scaleY / 100.0);
                            }), System.Windows.Threading.DispatcherPriority.Render);
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
                    var scale = GetOutputScale?.Invoke() ?? (100.0, 100.0);
                    
                    Image secondaryImageElement;
                    
                    // Если содержимое - Grid, добавляем Image в него
                    if (_secondaryScreenWindow.Content is Grid contentGrid)
                    {
                        contentGrid.Children.Clear();
                        secondaryImageElement = new Image
                        {
                            Source = new BitmapImage(new Uri(imagePath)),
                            Stretch = Stretch.Fill, // Используем Fill для правильного масштабирования
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Margin = new Thickness(0),
                            ClipToBounds = false // Отключаем обрезку для масштабирования
                        };
                        contentGrid.Children.Add(secondaryImageElement);
                    }
                    else
                    {
                        secondaryImageElement = new Image
                        {
                            Source = new BitmapImage(new Uri(imagePath)),
                            Stretch = Stretch.Fill, // Используем Fill для правильного масштабирования
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Margin = new Thickness(0),
                            ClipToBounds = false // Отключаем обрезку для масштабирования
                        };
                        _secondaryScreenWindow.Content = secondaryImageElement;
                    }
                    
                    // Применяем масштаб к изображению
                    var transform = new ScaleTransform(scale.scaleX / 100.0, scale.scaleY / 100.0);
                    secondaryImageElement.RenderTransform = transform;
                    secondaryImageElement.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
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
                    // Если содержимое - Grid, добавляем MediaElement в него
                    if (_secondaryScreenWindow.Content is Grid contentGrid)
                    {
                        if (!contentGrid.Children.Contains(_secondaryMediaElement))
                        {
                            contentGrid.Children.Clear();
                            contentGrid.Children.Add(_secondaryMediaElement);
                        }
                    }
                    else
                    {
                        // Создаем Grid если его нет
                        var grid = new Grid
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Margin = new Thickness(0),
                            Background = Brushes.Black
                        };
                        grid.Children.Add(_secondaryMediaElement);
                        _secondaryScreenWindow.Content = grid;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении вторичного экрана для видео: {ex.Message}");
            }
        }
    }
}


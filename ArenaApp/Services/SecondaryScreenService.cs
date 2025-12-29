using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления вторичным экраном
    /// </summary>
    public class SecondaryScreenService
    {
        private Window? _secondaryScreenWindow = null;
        private MediaElement? _secondaryMediaElement = null;
        
        // Делегаты для работы с устройствами
        public Func<bool>? GetUseSelectedScreen { get; set; }
        public Func<int>? GetSelectedScreenIndex { get; set; }
        public Func<bool>? GetUseUniformToFill { get; set; }
        
        // Делегаты для синхронизации с основным экраном
        public Func<MediaElement>? GetMainMediaElement { get; set; }
        public Func<Image>? GetMainImageElement { get; set; }
        public Action<MediaElement>? SetSecondaryMediaElement { get; set; }
        public Func<Window>? GetSecondaryScreenWindow { get; set; }
        
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
                    _secondaryScreenWindow = new Window
                    {
                        Title = "ArenaApp - Дополнительный экран",
                        WindowStyle = WindowStyle.None,
                        AllowsTransparency = false,
                        ResizeMode = ResizeMode.NoResize,
                        Topmost = true,
                        Background = Brushes.Black,
                        Left = screen.Bounds.X,
                        Top = screen.Bounds.Y,
                        Width = screen.Bounds.Width,
                        Height = screen.Bounds.Height,
                        WindowState = WindowState.Normal // Normal с точными координатами для правильного экрана
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
                    
                    // Добавляем обработчик для проверки позиции окна после создания
                    _secondaryScreenWindow.Loaded += (s, e) =>
                    {
                        // Принудительно устанавливаем позицию и размер для максимальной совместимости
                        _secondaryScreenWindow.Left = screen.Bounds.X;
                        _secondaryScreenWindow.Top = screen.Bounds.Y;
                        _secondaryScreenWindow.Width = screen.Bounds.Width;
                        _secondaryScreenWindow.Height = screen.Bounds.Height;
                        
                        System.Diagnostics.Debug.WriteLine($"ОКНО СОЗДАНО НА ЭКРАНЕ {selectedScreenIndex + 1}:");
                        System.Diagnostics.Debug.WriteLine($"  Целевые координаты: X={screen.Bounds.X}, Y={screen.Bounds.Y}");
                        System.Diagnostics.Debug.WriteLine($"  Целевой размер: {screen.Bounds.Width}x{screen.Bounds.Height}");
                        System.Diagnostics.Debug.WriteLine($"  Фактическая позиция: X={_secondaryScreenWindow.Left}, Y={_secondaryScreenWindow.Top}");
                        System.Diagnostics.Debug.WriteLine($"  Фактический размер: {_secondaryScreenWindow.Width}x{_secondaryScreenWindow.Height}");
                    };
                    
                    _secondaryScreenWindow.Show();
                    
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


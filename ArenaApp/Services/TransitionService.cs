using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ArenaApp.Models;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для управления переходами между медиа
    /// </summary>
    public class TransitionService
    {
        private GlobalSettings? _globalSettings;
        
        public void SetGlobalSettings(GlobalSettings? settings)
        {
            _globalSettings = settings;
        }
        
        public async Task ApplyTransition(Action transitionAction, Action? secondaryTransitionAction = null)
        {
            if (_globalSettings == null)
            {
                transitionAction();
                secondaryTransitionAction?.Invoke();
                return;
            }
            
            if (_globalSettings.TransitionType == TransitionType.Instant)
            {
                transitionAction();
                secondaryTransitionAction?.Invoke();
                return;
            }
            
            // Применяем переход
            switch (_globalSettings.TransitionType)
            {
                case TransitionType.Fade:
                    if (secondaryTransitionAction != null)
                    {
                        await ApplyFadeTransitionWithSecondaryScreen(transitionAction, secondaryTransitionAction, _globalSettings.TransitionDuration);
                    }
                    else
                    {
                        await ApplyFadeTransition(transitionAction, _globalSettings.TransitionDuration);
                    }
                    break;
                case TransitionType.Slide:
                    if (secondaryTransitionAction != null)
                    {
                        await ApplySlideTransitionWithSecondaryScreen(transitionAction, secondaryTransitionAction, _globalSettings.TransitionDuration);
                    }
                    else
                    {
                        await ApplySlideTransition(transitionAction, _globalSettings.TransitionDuration);
                    }
                    break;
                case TransitionType.Zoom:
                    if (secondaryTransitionAction != null)
                    {
                        await ApplyZoomTransitionWithSecondaryScreen(transitionAction, secondaryTransitionAction, _globalSettings.TransitionDuration);
                    }
                    else
                    {
                        await ApplyZoomTransition(transitionAction, _globalSettings.TransitionDuration);
                    }
                    break;
            }
        }
        
        // Делегаты для доступа к UI элементам
        public Func<FrameworkElement>? GetMediaBorder { get; set; }
        public Func<FrameworkElement?>? GetSecondaryScreenContent { get; set; }
        
        private async Task ApplyFadeTransition(Action transitionAction, double duration)
        {
            var mediaBorder = GetMediaBorder?.Invoke();
            if (mediaBorder == null) return;
            
            // Сохраняем текущую прозрачность
            double originalOpacity = mediaBorder.Opacity;
            
            // Плавно уменьшаем прозрачность
            for (int i = 0; i < 20; i++)
            {
                mediaBorder.Opacity = originalOpacity * (1.0 - (i / 20.0));
                await Task.Delay((int)(duration * 50)); // 20 шагов за duration секунд
            }
            
            // Выполняем переход
            transitionAction();
            
            // Плавно восстанавливаем прозрачность
            for (int i = 0; i < 20; i++)
            {
                mediaBorder.Opacity = originalOpacity * (i / 20.0);
                await Task.Delay((int)(duration * 50));
            }
        }
        
        private async Task ApplyFadeTransitionWithSecondaryScreen(Action transitionAction, Action secondaryTransitionAction, double duration)
        {
            var mediaBorder = GetMediaBorder?.Invoke();
            if (mediaBorder == null) return;
            
            // Сохраняем текущую прозрачность основного экрана
            double originalOpacity = mediaBorder.Opacity;
            
            // Сохраняем текущую прозрачность второго экрана
            double secondaryOriginalOpacity = 1.0;
            var secondaryElement = GetSecondaryScreenContent?.Invoke() as FrameworkElement;
            if (secondaryElement != null)
            {
                secondaryOriginalOpacity = secondaryElement.Opacity;
            }
            
            // Плавно уменьшаем прозрачность на обоих экранах одновременно
            for (int i = 0; i < 20; i++)
            {
                double fadeValue = 1.0 - (i / 20.0);
                mediaBorder.Opacity = originalOpacity * fadeValue;
                
                if (secondaryElement != null)
                {
                    secondaryElement.Opacity = secondaryOriginalOpacity * fadeValue;
                }
                
                await Task.Delay((int)(duration * 50)); // 20 шагов за duration секунд
            }
            
            // Выполняем переход на обоих экранах (когда экраны невидимы)
            transitionAction();
            secondaryTransitionAction();
            
            // Плавно восстанавливаем прозрачность на обоих экранах одновременно
            for (int i = 0; i < 20; i++)
            {
                double fadeValue = i / 20.0;
                mediaBorder.Opacity = originalOpacity * fadeValue;
                
                if (secondaryElement != null)
                {
                    secondaryElement.Opacity = secondaryOriginalOpacity * fadeValue;
                }
                
                await Task.Delay((int)(duration * 50));
            }
        }
        
        private async Task ApplySlideTransition(Action transitionAction, double duration)
        {
            var mediaBorder = GetMediaBorder?.Invoke();
            if (mediaBorder == null) return;
            
            // Сохраняем текущую позицию и объединяем с существующими трансформациями
            var originalTransform = mediaBorder.RenderTransform;
            var slideTransform = new TranslateTransform();
            
            // Если есть существующие трансформации, объединяем их
            if (originalTransform is TransformGroup existingGroup)
            {
                var newGroup = new TransformGroup();
                foreach (var transform in existingGroup.Children)
                {
                    if (!(transform is TranslateTransform)) // Исключаем старые TranslateTransform
                    {
                        newGroup.Children.Add(transform);
                    }
                }
                newGroup.Children.Add(slideTransform);
                mediaBorder.RenderTransform = newGroup;
            }
            else
            {
                mediaBorder.RenderTransform = slideTransform;
            }
            
            // Скольжение влево
            for (int i = 0; i < 20; i++)
            {
                slideTransform.X = -mediaBorder.ActualWidth * (i / 20.0);
                await Task.Delay((int)(duration * 50));
            }
            
            // Выполняем переход
            transitionAction();
            
            // Скольжение справа
            slideTransform.X = mediaBorder.ActualWidth;
            for (int i = 0; i < 20; i++)
            {
                slideTransform.X = mediaBorder.ActualWidth * (1.0 - (i / 20.0));
                await Task.Delay((int)(duration * 50));
            }
            
            // Восстанавливаем оригинальную трансформацию, но сохраняем масштаб и поворот
            if (originalTransform is TransformGroup originalGroup)
            {
                var restoredGroup = new TransformGroup();
                foreach (var transform in originalGroup.Children)
                {
                    if (!(transform is TranslateTransform)) // Исключаем TranslateTransform из анимации
                    {
                        restoredGroup.Children.Add(transform);
                    }
                }
                mediaBorder.RenderTransform = restoredGroup;
            }
            else
            {
                mediaBorder.RenderTransform = originalTransform;
            }
        }
        
        private async Task ApplySlideTransitionWithSecondaryScreen(Action transitionAction, Action secondaryTransitionAction, double duration)
        {
            var mediaBorder = GetMediaBorder?.Invoke();
            if (mediaBorder == null) return;
            
            // Сохраняем текущую позицию основного экрана и объединяем с существующими трансформациями
            var originalTransform = mediaBorder.RenderTransform;
            var slideTransform = new TranslateTransform();
            
            // Если есть существующие трансформации, объединяем их
            if (originalTransform is TransformGroup existingGroup)
            {
                var newGroup = new TransformGroup();
                foreach (var transform in existingGroup.Children)
                {
                    if (!(transform is TranslateTransform)) // Исключаем старые TranslateTransform
                    {
                        newGroup.Children.Add(transform);
                    }
                }
                newGroup.Children.Add(slideTransform);
                mediaBorder.RenderTransform = newGroup;
            }
            else
            {
                mediaBorder.RenderTransform = slideTransform;
            }
            
            // Сохраняем текущую позицию второго экрана и объединяем с существующими трансформациями
            var secondaryElement = GetSecondaryScreenContent?.Invoke() as FrameworkElement;
            var secondaryOriginalTransform = secondaryElement?.RenderTransform;
            var secondarySlideTransform = new TranslateTransform();
            
            if (secondaryElement != null)
            {
                if (secondaryOriginalTransform is TransformGroup secondaryExistingGroup)
                {
                    var secondaryNewGroup = new TransformGroup();
                    foreach (var transform in secondaryExistingGroup.Children)
                    {
                        if (!(transform is TranslateTransform)) // Исключаем старые TranslateTransform
                        {
                            secondaryNewGroup.Children.Add(transform);
                        }
                    }
                    secondaryNewGroup.Children.Add(secondarySlideTransform);
                    secondaryElement.RenderTransform = secondaryNewGroup;
                }
                else
                {
                    secondaryElement.RenderTransform = secondarySlideTransform;
                }
            }
            
            // Скольжение влево на обоих экранах одновременно
            for (int i = 0; i < 20; i++)
            {
                double slideValue = i / 20.0;
                slideTransform.X = -mediaBorder.ActualWidth * slideValue;
                
                if (secondaryElement != null)
                {
                    secondarySlideTransform.X = -secondaryElement.ActualWidth * slideValue;
                }
                
                await Task.Delay((int)(duration * 50));
            }
            
            // Выполняем переход на обоих экранах (когда экраны невидимы)
            transitionAction();
            secondaryTransitionAction();
            
            // Скольжение справа на обоих экранах одновременно
            slideTransform.X = mediaBorder.ActualWidth;
            if (secondaryElement != null)
            {
                secondarySlideTransform.X = secondaryElement.ActualWidth;
            }
            
            for (int i = 0; i < 20; i++)
            {
                double slideValue = 1.0 - (i / 20.0);
                slideTransform.X = mediaBorder.ActualWidth * slideValue;
                
                if (secondaryElement != null)
                {
                    secondarySlideTransform.X = secondaryElement.ActualWidth * slideValue;
                }
                
                await Task.Delay((int)(duration * 50));
            }
            
            // Восстанавливаем оригинальные трансформации, но сохраняем масштаб и поворот
            if (originalTransform is TransformGroup originalGroup)
            {
                var restoredGroup = new TransformGroup();
                foreach (var transform in originalGroup.Children)
                {
                    if (!(transform is TranslateTransform)) // Исключаем TranslateTransform из анимации
                    {
                        restoredGroup.Children.Add(transform);
                    }
                }
                mediaBorder.RenderTransform = restoredGroup;
            }
            else
            {
                mediaBorder.RenderTransform = originalTransform;
            }
            
            if (secondaryElement != null)
            {
                if (secondaryOriginalTransform is TransformGroup secondaryOriginalGroup)
                {
                    var secondaryRestoredGroup = new TransformGroup();
                    foreach (var transform in secondaryOriginalGroup.Children)
                    {
                        if (!(transform is TranslateTransform)) // Исключаем TranslateTransform из анимации
                        {
                            secondaryRestoredGroup.Children.Add(transform);
                        }
                    }
                    secondaryElement.RenderTransform = secondaryRestoredGroup;
                }
                else
                {
                    secondaryElement.RenderTransform = secondaryOriginalTransform;
                }
            }
        }
        
        private async Task ApplyZoomTransition(Action transitionAction, double duration)
        {
            var mediaBorder = GetMediaBorder?.Invoke();
            if (mediaBorder == null) return;
            
            // Сохраняем текущую трансформацию и объединяем с существующими трансформациями
            var originalTransform = mediaBorder.RenderTransform;
            var zoomTransform = new ScaleTransform();
            
            // Если есть существующие трансформации, объединяем их
            if (originalTransform is TransformGroup existingGroup)
            {
                var newGroup = new TransformGroup();
                foreach (var transform in existingGroup.Children)
                {
                    if (!(transform is ScaleTransform)) // Исключаем старые ScaleTransform
                    {
                        newGroup.Children.Add(transform);
                    }
                }
                newGroup.Children.Add(zoomTransform);
                mediaBorder.RenderTransform = newGroup;
            }
            else
            {
                mediaBorder.RenderTransform = zoomTransform;
            }
            
            // Уменьшение
            for (int i = 0; i < 20; i++)
            {
                double scale = 1.0 - (i / 20.0) * 0.5; // Уменьшаем до 50%
                zoomTransform.ScaleX = scale;
                zoomTransform.ScaleY = scale;
                await Task.Delay((int)(duration * 50));
            }
            
            // Выполняем переход
            transitionAction();
            
            // Увеличение
            for (int i = 0; i < 20; i++)
            {
                double scale = 0.5 + (i / 20.0) * 0.5; // Увеличиваем от 50% до 100%
                zoomTransform.ScaleX = scale;
                zoomTransform.ScaleY = scale;
                await Task.Delay((int)(duration * 50));
            }
            
            // Восстанавливаем оригинальную трансформацию, но сохраняем масштаб и поворот
            if (originalTransform is TransformGroup originalGroup)
            {
                var restoredGroup = new TransformGroup();
                foreach (var transform in originalGroup.Children)
                {
                    if (!(transform is ScaleTransform)) // Исключаем ScaleTransform из анимации
                    {
                        restoredGroup.Children.Add(transform);
                    }
                }
                mediaBorder.RenderTransform = restoredGroup;
            }
            else
            {
                mediaBorder.RenderTransform = originalTransform;
            }
        }
        
        private async Task ApplyZoomTransitionWithSecondaryScreen(Action transitionAction, Action secondaryTransitionAction, double duration)
        {
            var mediaBorder = GetMediaBorder?.Invoke();
            if (mediaBorder == null) return;
            
            // Сохраняем текущую трансформацию основного экрана и объединяем с существующими трансформациями
            var originalTransform = mediaBorder.RenderTransform;
            var zoomTransform = new ScaleTransform();
            
            // Если есть существующие трансформации, объединяем их
            if (originalTransform is TransformGroup existingGroup)
            {
                var newGroup = new TransformGroup();
                foreach (var transform in existingGroup.Children)
                {
                    if (!(transform is ScaleTransform)) // Исключаем старые ScaleTransform
                    {
                        newGroup.Children.Add(transform);
                    }
                }
                newGroup.Children.Add(zoomTransform);
                mediaBorder.RenderTransform = newGroup;
            }
            else
            {
                mediaBorder.RenderTransform = zoomTransform;
            }
            
            // Сохраняем текущую трансформацию второго экрана и объединяем с существующими трансформациями
            var secondaryElement = GetSecondaryScreenContent?.Invoke() as FrameworkElement;
            var secondaryOriginalTransform = secondaryElement?.RenderTransform;
            var secondaryZoomTransform = new ScaleTransform();
            
            if (secondaryElement != null)
            {
                if (secondaryOriginalTransform is TransformGroup secondaryExistingGroup)
                {
                    var secondaryNewGroup = new TransformGroup();
                    foreach (var transform in secondaryExistingGroup.Children)
                    {
                        if (!(transform is ScaleTransform)) // Исключаем старые ScaleTransform
                        {
                            secondaryNewGroup.Children.Add(transform);
                        }
                    }
                    secondaryNewGroup.Children.Add(secondaryZoomTransform);
                    secondaryElement.RenderTransform = secondaryNewGroup;
                }
                else
                {
                    secondaryElement.RenderTransform = secondaryZoomTransform;
                }
            }
            
            // Уменьшение на обоих экранах одновременно
            for (int i = 0; i < 20; i++)
            {
                double scale = 1.0 - (i / 20.0) * 0.5; // Уменьшаем до 50%
                zoomTransform.ScaleX = scale;
                zoomTransform.ScaleY = scale;
                
                if (secondaryElement != null)
                {
                    secondaryZoomTransform.ScaleX = scale;
                    secondaryZoomTransform.ScaleY = scale;
                }
                
                await Task.Delay((int)(duration * 50));
            }
            
            // Выполняем переход на обоих экранах (когда экраны невидимы)
            transitionAction();
            secondaryTransitionAction();
            
            // Увеличение на обоих экранах одновременно
            for (int i = 0; i < 20; i++)
            {
                double scale = 0.5 + (i / 20.0) * 0.5; // Увеличиваем от 50% до 100%
                zoomTransform.ScaleX = scale;
                zoomTransform.ScaleY = scale;
                
                if (secondaryElement != null)
                {
                    secondaryZoomTransform.ScaleX = scale;
                    secondaryZoomTransform.ScaleY = scale;
                }
                
                await Task.Delay((int)(duration * 50));
            }
            
            // Восстанавливаем оригинальные трансформации, но сохраняем масштаб и поворот
            if (originalTransform is TransformGroup originalGroup)
            {
                var restoredGroup = new TransformGroup();
                foreach (var transform in originalGroup.Children)
                {
                    if (!(transform is ScaleTransform)) // Исключаем ScaleTransform из анимации
                    {
                        restoredGroup.Children.Add(transform);
                    }
                }
                mediaBorder.RenderTransform = restoredGroup;
            }
            else
            {
                mediaBorder.RenderTransform = originalTransform;
            }
            
            if (secondaryElement != null)
            {
                if (secondaryOriginalTransform is TransformGroup secondaryOriginalGroup)
                {
                    var secondaryRestoredGroup = new TransformGroup();
                    foreach (var transform in secondaryOriginalGroup.Children)
                    {
                        if (!(transform is ScaleTransform)) // Исключаем ScaleTransform из анимации
                        {
                            secondaryRestoredGroup.Children.Add(transform);
                        }
                    }
                    secondaryElement.RenderTransform = secondaryRestoredGroup;
                }
                else
                {
                    secondaryElement.RenderTransform = secondaryOriginalTransform;
                }
            }
        }
    }
}


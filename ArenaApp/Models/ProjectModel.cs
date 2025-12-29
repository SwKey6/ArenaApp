using System.Collections.Generic;

namespace ArenaApp.Models
{
    public class ProjectModel
    {
        public string ProjectName { get; set; } = "";
        public string ProjectPath { get; set; } = "";
        public List<MediaSlot> MediaSlots { get; set; } = new List<MediaSlot>();
        public GlobalSettings GlobalSettings { get; set; } = new GlobalSettings();
    }

    public class MediaSlot
    {
        public int Column { get; set; }
        public int Row { get; set; }
        public string MediaPath { get; set; } = "";
        public MediaType Type { get; set; }
        public string PreviewPath { get; set; } = "";
        public bool IsTrigger { get; set; } = false;  // Является ли слот триггером
        
        // Настройки элемента
        public string DisplayName { get; set; } = "";  // Отображаемое имя элемента
        public double PlaybackSpeed { get; set; } = 1.0;  // Скорость воспроизведения (0.1 - 10.0)
        public double Opacity { get; set; } = 1.0;  // Прозрачность (0.0 - 1.0)
        public double Volume { get; set; } = 1.0;  // Уровень звука (0.0 - 1.0)
        public double Scale { get; set; } = 1.0;  // Масштаб (0.1 - 5.0)
        public double Rotation { get; set; } = 0.0;  // Поворот в градусах (-360 - 360)
        
        // Настройки для текстовых блоков
        public string TextContent { get; set; } = "";  // Содержимое текстового блока
        public string FontFamily { get; set; } = "Arial";  // Шрифт
        public double FontSize { get; set; } = 24;  // Размер шрифта
        public string FontColor { get; set; } = "White";  // Цвет текста
        public string BackgroundColor { get; set; } = "Transparent";  // Цвет фона
        public string TextPosition { get; set; } = "Center";  // Положение текста (TopLeft, TopCenter, TopRight, CenterLeft, Center, CenterRight, BottomLeft, BottomCenter, BottomRight)
        public double TextX { get; set; } = 0;  // Ручная настройка X позиции
        public double TextY { get; set; } = 0;  // Ручная настройка Y позиции
        public bool UseManualPosition { get; set; } = false;  // Использовать ручную настройку положения
        public bool IsTextVisible { get; set; } = true;  // Видимость текста на медиаплеере
        
        // Конструктор для установки DisplayName по умолчанию
        public MediaSlot()
        {
            // DisplayName будет установлено позже при загрузке файла
        }
    }

    public enum MediaType
    {
        Video,
        Image,
        Audio,
        Text
    }

    public class GlobalSettings
    {
        // Общие настройки громкости (приоритет над личными настройками)
        public double GlobalVolume { get; set; } = 1.0;  // Общая громкость (0.0 - 1.0)
        public bool UseGlobalVolume { get; set; } = false;  // Использовать ли общую громкость
        
        // Общие настройки прозрачности (приоритет над личными настройками)
        public double GlobalOpacity { get; set; } = 1.0;  // Общая прозрачность (0.0 - 1.0)
        public bool UseGlobalOpacity { get; set; } = false;  // Использовать ли общую прозрачность
        
        // Общие настройки масштаба (приоритет над личными настройками)
        public double GlobalScale { get; set; } = 1.0;  // Общий масштаб (0.1 - 5.0)
        public bool UseGlobalScale { get; set; } = false;  // Использовать ли общий масштаб
        
        // Общие настройки поворота (приоритет над личными настройками)
        public double GlobalRotation { get; set; } = 0.0;  // Общий поворот в градусах (-360 - 360)
        public bool UseGlobalRotation { get; set; } = false;  // Использовать ли общий поворот
        
        // Настройки переходов между медиа
        public TransitionType TransitionType { get; set; } = TransitionType.Instant;  // Тип перехода
        public double TransitionDuration { get; set; } = 0.5;  // Длительность перехода в секундах
        
        // Дополнительные общие настройки
        public bool AutoPlayNext { get; set; } = false;  // Автоматическое воспроизведение следующего элемента
        public bool LoopPlaylist { get; set; } = false;  // Зацикливание плейлиста
        
        // Позиции и размеры панелей
        public PanelLayout ElementSettingsPanel { get; set; } = new PanelLayout { Left = 0, Top = 0, Width = 400, Height = 500 };
        public PanelLayout GlobalSettingsPanel { get; set; } = new PanelLayout { Left = 420, Top = 0, Width = 400, Height = 500 };
        public PanelLayout MediaPlayerPanel { get; set; } = new PanelLayout { Left = 0, Top = 0, Width = 500, Height = 500 };
        public PanelLayout MediaCellsPanel { get; set; } = new PanelLayout { Left = 0, Top = 0, Width = 800, Height = 300 };
    }
    
    public class PanelLayout
    {
        public double Left { get; set; } = 0;
        public double Top { get; set; } = 0;
        public double Width { get; set; } = 400;
        public double Height { get; set; } = 500;
    }

    public enum TransitionType
    {
        Instant,        // Мгновенный переход
        Fade,           // Плавное затухание
        Slide,          // Скольжение
        Zoom            // Увеличение/уменьшение
    }
}

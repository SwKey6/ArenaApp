# ArenaApp - Документация проекта

## Сборка приложения

### Базовые команды сборки

#### Debug сборка
```bash
dotnet build
```
или
```bash
dotnet build -c Debug
```
Результат: `bin/Debug/net8.0-windows/ArenaApp.exe`

#### Release сборка
```bash
dotnet build -c Release
```
Результат: `bin/Release/net8.0-windows/ArenaApp.exe`

### Публикация приложения (для распространения)

#### Self-contained публикация (включает .NET runtime)
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```
Результат: `bin/Release/net8.0-windows/win-x64/`
- Включает все необходимые библиотеки и .NET runtime
- Размер больше, но не требует установленного .NET на целевом компьютере

#### Framework-dependent публикация (требует установленный .NET)
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```
Результат: `bin/Release/net8.0-windows/win-x64/`
- Меньший размер
- Требует установленный .NET 8.0 Runtime на целевом компьютере

### Очистка проекта
```bash
dotnet clean
```

### Восстановление зависимостей
```bash
dotnet restore
```

## Технические детали

- **Framework**: .NET 8.0 (Windows)
- **Тип приложения**: WPF (Windows Presentation Foundation)
- **Зависимости**:
  - NAudio 2.2.1
  - Microsoft.VisualBasic 10.3.0
  - System.Windows.Forms 4.0.0
  - LibVLCSharp / LibVLCSharp.WPF (встроенный видеодвижок)
  - VideoLAN.LibVLC.Windows (нативные библиотеки VLC)

## Важно про воспроизведение видео (без кодеков у пользователя)

Приложение использует **LibVLC (LibVLCSharp)** для воспроизведения видео, чтобы видео работало у любого пользователя без дополнительных установок кодеков (в отличие от стандартного WPF `MediaElement`, который зависит от Windows Media Foundation и может выдавать ошибки вроде `0xC00D109B`).

## Changelog

### [Дата не указана]
- Создана документация по сборке приложения
- Удален старый метод `LoadMediaFromSlotSelective_OLD` из `MainWindow.xaml.cs` после успешного переноса логики в `MediaPlayerService`
- Исправлено мигание видео при запуске: видео теперь скрывается перед переходом
- Исправлено вылезание изображений за рамки: убраны фиксированные размеры, добавлен `ClipToBounds` и ограничения `MaxWidth`/`MaxHeight`


using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ArenaApp.Services
{
    /// <summary>
    /// Сервис для создания колонок и слотов
    /// </summary>
    public class SlotCreationService
    {
        // Делегаты для работы с UI
        public Func<Grid>? GetBottomPanel { get; set; }
        public Func<object, Style>? GetTriggerButtonStyle { get; set; }
        
        // Делегаты для обработки событий
        public Action<object, RoutedEventArgs>? OnSlotClick { get; set; }
        public Func<Button, ContextMenu>? CreateContextMenu { get; set; }
        
        /// <summary>
        /// Создает указанное количество колонок со слотами
        /// </summary>
        public void CreateColumns(int columns)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CreateColumns: Starting to create {columns} columns");
                
                var bottomPanel = GetBottomPanel?.Invoke();
                if (bottomPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine("CreateColumns: BottomPanel is null!");
                    return;
                }
                
                for (int i = 0; i < columns; i++)
                {
                    bottomPanel.ColumnDefinitions.Add(new ColumnDefinition());

                    // внутри каждой колонки создаём Grid на 3 строки
                    Grid columnGrid = new Grid();
                    columnGrid.RowDefinitions.Add(new RowDefinition());
                    columnGrid.RowDefinitions.Add(new RowDefinition());
                    columnGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Автоматическая высота для триггера

                    // создаём 2 основные кнопки
                    for (int j = 0; j < 2; j++)
                    {
                        Button slot = new Button
                        {
                            Content = "Пусто",
                            MinWidth = 60,
                            MinHeight = 60,
                            Background = new SolidColorBrush(Color.FromRgb(48, 54, 61)), // #30363D - темная граница
                            Foreground = Brushes.White,
                            Margin = new Thickness(3),
                            FontSize = 10,
                            FontWeight = FontWeights.SemiBold,
                            Tag = $"Slot_{i + 1}_{j + 1}" // например Slot_3_2 (3 колонка, 2 строка)
                        };

                        if (OnSlotClick != null)
                        {
                            slot.Click += (s, e) => 
                            {
                                try
                                {
                                    OnSlotClick(s, e);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Ошибка при обработке клика по слоту: {ex.Message}");
                                }
                            };
                        }
                        
                        if (CreateContextMenu != null)
                        {
                            slot.ContextMenu = CreateContextMenu(slot);
                        }

                        Grid.SetRow(slot, j);
                        columnGrid.Children.Add(slot);
                    }

                    // создаём кнопку-триггер в третьей строке
                    // ЗАКОММЕНТИРОВАНО - триггеры отключены
                    /*
                    Button triggerButton = new Button
                    {
                        Style = GetTriggerButtonStyle != null ? GetTriggerButtonStyle(this) : null,
                        Tag = $"Trigger_{i + 1}" // например Trigger_3
                    };

                    // ЗАКОММЕНТИРОВАНО - триггеры отключены
                    // triggerButton.Click += Trigger_Click;
                    if (CreateContextMenu != null)
                    {
                        triggerButton.ContextMenu = CreateContextMenu(triggerButton);
                    }

                    Grid.SetRow(triggerButton, 2);
                    columnGrid.Children.Add(triggerButton);
                    */

                    // ставим колонку в нужное место
                    Grid.SetColumn(columnGrid, i);
                    bottomPanel.Children.Add(columnGrid);
                }
                
                System.Diagnostics.Debug.WriteLine($"CreateColumns: Successfully created {columns} columns");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateColumns: Error: {ex.Message}");
                MessageBox.Show($"Ошибка при создании колонок: {ex.Message}", "Ошибка");
            }
        }
        
        /// <summary>
        /// Очищает все колонки
        /// </summary>
        public void ClearColumns()
        {
            try
            {
                var bottomPanel = GetBottomPanel?.Invoke();
                if (bottomPanel != null)
                {
                    bottomPanel.Children.Clear();
                    bottomPanel.ColumnDefinitions.Clear();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearColumns: Error: {ex.Message}");
            }
        }
    }
}


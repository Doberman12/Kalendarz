using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Kalendarz
{
    public partial class MainWindow : Window
    {
        private Border _todayBorder;
        private Border _currentlySelectedBorder;
        private Settings _settings;
        private DateTime _currentDate;
        private DateTime? _selectedDate = null;

        
        private TasksContext _context = new TasksContext();
        
        public MainWindow()
        {
            InitializeComponent();
            _context.Database.EnsureCreated();
            _settings = Settings.Load();
            CityInput.Text = _settings.City;
            LoadWeather(_settings.City);
            _selectedDate = DateTime.Today;
            
            _currentDate = DateTime.Today;
            ///Generowanie kalendarza z zaznaczonym dniem
            GenerateCalendar(_currentDate);
            SelectedDateText.Text = DateTime.Today.ToString("d MMMM yyyy", new CultureInfo("pl-PL"));
            LoadTasks();
            this.Closing += MainWindow_Closing;
        }

        private void GenerateCalendar(DateTime date)
        {
            CalendarGrid.Children.Clear();
            CalendarGrid.RowDefinitions.Clear();
            CalendarGrid.ColumnDefinitions.Clear();
            var taskDates = _context.Tasks
                .Where(t => t.Date.Month == date.Month && t.Date.Year == date.Year)
                .Select(t => t.Date.Date)
                .Distinct()
                .ToHashSet();
            for (int i = 0; i < 7; i++)
            {
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            string[] days = { "Pn", "Wt", "Śr", "Cz", "Pt", "Sb", "Nd" };
            for (int i = 0; i < 7; i++)
            {
                TextBlock dayName = new TextBlock
                {
                    Text = days[i],
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(5)
                };
                if (i == 6)
                {
                    dayName.Foreground = Brushes.Red;
                }
                Grid.SetRow(dayName, 0);
                Grid.SetColumn(dayName, i);
                CalendarGrid.Children.Add(dayName);
            }

            MonthYearText.Text = date.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("pl-PL"));

            DateTime firstDay = new DateTime(date.Year, date.Month, 1);
            int startDay = ((int)firstDay.DayOfWeek + 6) % 7;
            int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);

            int row = 1;
            int column = startDay;

            for (int i = 1; i <= daysInMonth; i++)
            {
                if (CalendarGrid.RowDefinitions.Count < row + 1)
                    CalendarGrid.RowDefinitions.Add(new RowDefinition());

                var currentDay = new DateTime(date.Year, date.Month, i);

                var dayBorder = new Border
                {
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(5),
                    Margin = new Thickness(2),
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent
                };

                if (currentDay == DateTime.Today)
                {
                    dayBorder.BorderBrush = Brushes.Blue;
                    dayBorder.Background = Brushes.LightBlue;
                    _todayBorder = dayBorder;
                }

                /// Tekst z numerem dnia
                var dayText = new TextBlock
                {
                    Text = i.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = currentDay.DayOfWeek == DayOfWeek.Sunday ? Brushes.Red : Brushes.Black
                };

                /// ZNACZNIK (jeśli są zadania)
                var dayStack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                dayStack.Children.Add(dayText);
                ///Dodawanie znacznika dla dni z zadaniami
                if (taskDates.Contains(currentDay.Date))
                {
                    var dot = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = Brushes.Green,
                        Margin = new Thickness(0, 2, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    dayStack.Children.Add(dot);
                }

                dayBorder.Child = dayStack;

                dayBorder.MouseLeftButtonDown += (s, e) =>
                {
                    SelectDay(currentDay, dayBorder);
                    LoadTasks();
                };

                Grid.SetRow(dayBorder, row);
                Grid.SetColumn(dayBorder, column);
                CalendarGrid.Children.Add(dayBorder);

                column++;
                if (column > 6)
                {
                    column = 0;
                    row++;
                }
            }

        }
        /// <summary>
        /// Wczytywanie zadan z bazy danych
        /// </summary>
        private void LoadTasks()
        {
            TaskList.Items.Clear();

            if (_selectedDate != null)
            {
                var tasksForDate = _context.Tasks
                    .Where(t => t.Date.Date == _selectedDate.Value.Date)
                    .ToList();

                foreach (var task in tasksForDate)
                {
                    TaskList.Items.Add(new ListBoxItem { Content = task.Description, Tag = task.Id });
                }
            }
        }
        /// <summary>
        /// Dodawanie zadan do bazy danych
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDate == null || string.IsNullOrWhiteSpace(TaskInput.Text))
                return;

            var taskText = TaskInput.Text.Trim();

            var newTask = new TaskItem
            {
                Date = _selectedDate.Value.Date,
                Description = taskText
            };

            try
            {
                _context.Tasks.Add(newTask);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisu zadania: " + ex.Message);
            }

            TaskInput.Clear();
            LoadTasks();
            GenerateCalendar(_currentDate);
        }
        /// <summary>
        /// Usuwanie zadan z bazy danych
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDate == null || TaskList.SelectedItem == null)
                return;

            var selectedItem = TaskList.SelectedItem as ListBoxItem;
            if (selectedItem == null) return;

            int taskId = (int)selectedItem.Tag;

            var taskToDelete = _context.Tasks.Find(taskId);
            if (taskToDelete != null)
            {
                _context.Tasks.Remove(taskToDelete);
                _context.SaveChanges();
            }

            LoadTasks();
            GenerateCalendar(_currentDate);
        }

        private void PreviousMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddMonths(-1);
            GenerateCalendar(_currentDate);
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddMonths(1);
            GenerateCalendar(_currentDate);
        }
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _context.Dispose();
        }
        private async void LoadWeather(string city)
        {
            WeatherText.Text = "Ładowanie pogody...";
            var result = await WeatherService.GetWeatherAsync(city);
            WeatherText.Text = $"Pogoda w {city}: {result}";
        }
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            string city = CityInput.Text.Trim();
            if (string.IsNullOrEmpty(city)) return;

            _settings.City = city;
            _settings.Save();

            LoadWeather(city);
        }
        private async void LoadWeather_Click(object sender, RoutedEventArgs e)
        {
            string city = CityInput.Text.Trim();
            if (string.IsNullOrEmpty(city)) return;

            WeatherText.Text = "Ładowanie...";
            var result = await WeatherService.GetWeatherAsync(city);
            WeatherText.Text = $"Pogoda w {city}: {result}";
        }
        private void SelectDay(DateTime day, Border border)
        {
            _selectedDate = day;
            SelectedDateText.Text = day.ToString("d MMMM yyyy", new CultureInfo("pl-PL"));
            LoadTasks();

            /// Resetowanie poprzedniego zaznaczenia
            if (_currentlySelectedBorder != null)
            {
                
                if (_currentlySelectedBorder == _todayBorder)
                {
                    _todayBorder.Background = Brushes.LightBlue;
                }
                else
                {
                    _currentlySelectedBorder.Background = Brushes.Transparent;
                }
            }

            /// Zaznaczenie nowego
            border.Background = Brushes.LightGray;
            _currentlySelectedBorder = border;
        }
    }
}

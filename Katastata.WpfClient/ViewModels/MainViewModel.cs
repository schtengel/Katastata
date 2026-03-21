using Katastata.Models;
using Katastata.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Katastata.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AppMonitorService? _service;
        private readonly int _userId;

        public AppMonitorService Service => _service ?? throw new InvalidOperationException("Service is not initialized.");
        public int UserId => _userId;

        public ObservableCollection<Program> Programs { get; } = new ObservableCollection<Program>();
        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();

        // Свойства для фильтрации и сортировки
        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                ApplyFilterAndSort();
            }
        }

        private string _sortMode = "NameAsc";
        public string SortMode
        {
            get => _sortMode;
            set
            {
                _sortMode = value;
                OnPropertyChanged();
                ApplyFilterAndSort();
            }
        }

        // Команды (все — без generic)
        public RelayCommand? ScanCommand { get; }
        public RelayCommand? ShowSessionsCommand { get; }
        public RelayCommand? ShowStatisticsCommand { get; }
        public RelayCommand? ExportStatisticsExcelCommand { get; }
        public RelayCommand? ExportStatisticsWordCommand { get; }
        public RelayCommand? CreateCategoryCommand { get; }
        public RelayCommand? OpenSettingsCommand { get; }
        public RelayCommand? SortCommand { get; }
        public RelayCommand? OpenAboutCommand { get; }

        public MainViewModel() { }

        public MainViewModel(AppMonitorService service, int userId)
        {
            _service = service;
            _userId = userId;

            LoadCategories();

            ScanCommand = new RelayCommand(_ => ScanPrograms());
            ShowSessionsCommand = new RelayCommand(_ => ShowSessions());
            ShowStatisticsCommand = new RelayCommand(_ => ShowStatistics());
            ExportStatisticsExcelCommand = new RelayCommand(_ => ExportStatisticsExcel());
            ExportStatisticsWordCommand = new RelayCommand(_ => ExportStatisticsWord());
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
            OpenAboutCommand = new RelayCommand(_ => OpenAbout());

            // Команда сортировки: принимаем object, приводим к string
            SortCommand = new RelayCommand(parameter =>
            {
                if (parameter is string mode)
                {
                    SortMode = mode;
                }
            });

            LoadPrograms();
            Service.StartMonitoring(_userId);
        }

        private void LoadCategories()
        {
            var cats = Service.GetAllCategories();
            Categories.Clear();

            Categories.Add(Category.All);

            foreach (var cat in cats)
                Categories.Add(cat);
        }

        private void OpenAbout()
        {
            new AboutWindow().ShowDialog();
        }
        private void ScanPrograms()
        {
            try
            {
                Service.ScanRunningPrograms(_userId);
                LoadPrograms();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void LoadPrograms()
        {
            var items = Service.GetAllPrograms(_userId).ToList();
            ApplyFilterAndSort(items);
        }

        private void ApplyFilterAndSort(List<Program>? source = null)
        {
            var filtered = (source ?? Service.GetAllPrograms(_userId).ToList()).AsEnumerable();

            if (SelectedCategory != null && SelectedCategory.Id != -1) // -1 = "Все"
            {
                filtered = filtered.Where(p => p.CategoryId == SelectedCategory.Id);
            }

            switch (SortMode)
            {
                case "NameAsc":
                    filtered = filtered.OrderBy(p => p.Name);
                    break;
                case "NameDesc":
                    filtered = filtered.OrderByDescending(p => p.Name);
                    break;
                case "LastLaunchAsc":
                    filtered = filtered.OrderBy(p =>
                        p.Statistics?.Max(s => s.LastLaunch) ?? DateTime.MinValue);
                    break;
                case "LastLaunchDesc":
                    filtered = filtered.OrderByDescending(p =>
                        p.Statistics?.Max(s => s.LastLaunch) ?? DateTime.MinValue);
                    break;
            }

            Programs.Clear();
            foreach (var p in filtered)
                Programs.Add(p);
        }

        private void ShowSessions()
        {
            var sessions = Service.GetSessions(_userId);
            var sessionsWindow = new SessionsWindow(sessions);
            sessionsWindow.Show();
        }

        private void ShowStatistics()
        {
            var stats = Service.GetStatistics(_userId);
            var statsWindow = new StatisticsWindow(stats);
            statsWindow.Show();
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow(Service, _userId);
            settingsWindow.ShowDialog();
        }

        private void ExportStatisticsExcel()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Excel files (*.xlsx)|*.xlsx", DefaultExt = "xlsx" };
            if (dialog.ShowDialog() == true)
            {
                Service.ExportStatisticsToExcel(_userId, dialog.FileName);
                System.Windows.MessageBox.Show("Экспорт завершен");
            }
        }

        private void ExportStatisticsWord()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Word files (*.docx)|*.docx", DefaultExt = "docx" };
            if (dialog.ShowDialog() == true)
            {
                Service.ExportStatisticsToWord(_userId, dialog.FileName);
                System.Windows.MessageBox.Show("Экспорт завершен");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

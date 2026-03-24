using Katastata.Models;
using Katastata.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace Katastata.ViewModels
{
    public class SettingsViewModel
    {
        private readonly AppMonitorService _service;
        private readonly int _userId;

        public List<Statistics> StatisticsList { get; set; } = new();
        public List<Session> SessionsList { get; set; } = new();

        public ObservableCollection<StatisticsViewModelItem> Statistics { get; } = new ObservableCollection<StatisticsViewModelItem>();
        public ObservableCollection<SessionViewModel> Sessions { get; } = new ObservableCollection<SessionViewModel>();
        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();
        public RelayCommand ExportExcelCommand { get; }
        public RelayCommand ExportWordCommand { get; }
        public RelayCommand CreateCategoryCommand { get; }
        public RelayCommand DeleteCategoryCommand { get; }

        public SettingsViewModel(AppMonitorService service, int userId)
        {
            _service = service;
            _userId = userId;
            LoadData();

            ExportExcelCommand = new RelayCommand(_ => ExportToExcel());
            ExportWordCommand = new RelayCommand(_ => ExportToWord());
            CreateCategoryCommand = new RelayCommand(CreateCategory);
            DeleteCategoryCommand = new RelayCommand(DeleteCategory);
        }

        private void LoadData()
        {
            // Получаем списки из сервиса и сразу сохраняем в свойства для окон
            StatisticsList = _service.GetStatistics(_userId);
            SessionsList = _service.GetSessions(_userId);

            // Заполняем ObservableCollection для биндинга в интерфейсе
            Statistics.Clear();
            foreach (var stat in StatisticsList)
                Statistics.Add(new StatisticsViewModelItem(stat));

            Sessions.Clear();
            foreach (var session in SessionsList)
                Sessions.Add(new SessionViewModel(session));

            var categories = _service.GetAllCategories();
            Categories.Clear();
            foreach (var cat in categories)
                Categories.Add(cat);
        }

        private void CreateCategory(object? parameter)
        {
            // Теперь метод принимает object (как требует RelayCommand)
            var name = parameter as string ?? "";
            name = name.Trim();

            // Проверка на пустоту
            if (string.IsNullOrEmpty(name))
            {
                System.Windows.MessageBox.Show("Введите название категории.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка на существование
            if (_service.CategoryExists(name))
            {
                System.Windows.MessageBox.Show("Категория с таким названием уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Сохраняем
            try
            {
                _service.AddCategory(name);
                System.Windows.MessageBox.Show("Категория создана.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Обновляем список в UI
                LoadData();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при создании категории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCategory(object? parameter)
        {
            if (parameter == null)
                return;

            var categoryId = (int)parameter;

            try
            {
                // Проверяем, есть ли программы в этой категории
                var programsInCategory = _service.GetProgramsByCategory(categoryId);
                if (programsInCategory.Count > 0)
                {
                    System.Windows.MessageBox.Show(
                        $"Нельзя удалить категорию: в ней находятся {programsInCategory.Count} программ(ы).",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                _service.DeleteCategory(categoryId);
                System.Windows.MessageBox.Show("Категория удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData(); // Перезагружаем данные
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при удалении категории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportToExcel()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Excel files (*.xlsx)|*.xlsx", DefaultExt = "xlsx" };
            if (dialog.ShowDialog() == true)
            {
                _service.ExportStatisticsToExcel(_userId, dialog.FileName);
                System.Windows.MessageBox.Show("Экспорт завершен");
            }
        }

        public void ExportToWord()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Word files (*.docx)|*.docx", DefaultExt = "docx" };
            if (dialog.ShowDialog() == true)
            {
                _service.ExportStatisticsToWord(_userId, dialog.FileName);
                System.Windows.MessageBox.Show("Экспорт завершен");
            }
        }
    }
}
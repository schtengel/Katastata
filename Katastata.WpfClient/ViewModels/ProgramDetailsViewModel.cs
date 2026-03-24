using Katastata.Models;
using Katastata.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Katastata.ViewModels
{
    public class ProgramDetailsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string ProgramName { get; }
        public string ProgramPath { get; }
        public ObservableCollection<SessionViewModel> Sessions { get; } = new ObservableCollection<SessionViewModel>();
        public TimeSpan TotalTime { get; }
        public DateTime? LastLaunch { get; }


        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();
        public Category? SelectedCategory { get; set; }

        private readonly AppMonitorService _service;
        private readonly Program _program;

        private string _editableProgramName = string.Empty;
        public string EditableProgramName
        {
            get => _editableProgramName;
            set
            {
                if (_editableProgramName == value) return;
                _editableProgramName = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SaveCategoryCommand { get; }
        public RelayCommand SaveNameCommand { get; } // Новая команда


        public ProgramDetailsViewModel(Program program, List<Session> sessions, Statistics? stat, AppMonitorService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _program = program ?? throw new ArgumentNullException(nameof(program));

            ProgramName = program.Name ?? "Unknown";
            ProgramPath = program.Path ?? "";

            foreach (var session in sessions)
            {
                Sessions.Add(new SessionViewModel(session));
            }
            TotalTime = stat?.TotalTime ?? TimeSpan.Zero;
            LastLaunch = stat?.LastLaunch;

            LoadCategories();
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == program.CategoryId);

            SaveCategoryCommand = new RelayCommand(_ => SaveCategory());
            SaveNameCommand = new RelayCommand(_ => SaveProgramName()); // Инициализация команды

            EditableProgramName = ProgramName; // Синхронизация
        }

        private void LoadCategories()
        {
            Categories.Clear();
            var cats = _service.GetAllCategories();
            foreach (var cat in cats)
                Categories.Add(cat);
        }

        public void SaveCategory()
        {
            if (SelectedCategory != null)
            {
                _program.CategoryId = SelectedCategory.Id;
                _service.UpdateProgram(_program);
            }
        }

        private void SaveProgramName()
        {
            var newName = EditableProgramName?.Trim();

            // Валидация
            if (string.IsNullOrEmpty(newName))
            {
                System.Windows.MessageBox.Show("Название не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (newName.Length > 100)
            {
                System.Windows.MessageBox.Show("Название слишком длинное (максимум 100 символов).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _program.Name = newName;
                _service.UpdateProgram(_program);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

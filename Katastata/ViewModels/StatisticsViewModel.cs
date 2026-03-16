using Katastata.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace Katastata.ViewModels
{
    public class StatisticsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<StatisticsViewModelItem> Statistics { get; } = new ObservableCollection<StatisticsViewModelItem>();
        public ICollectionView FilteredStatistics { get; }

        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged();
                FilteredStatistics.Refresh();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public StatisticsViewModel(List<Statistics> stats)
        {
            foreach (var stat in stats)
                Statistics.Add(new StatisticsViewModelItem(stat));

            FilteredStatistics = CollectionViewSource.GetDefaultView(Statistics);
            FilteredStatistics.Filter = FilterStatistics;
        }

        private bool FilterStatistics(object item)
        {
            if (string.IsNullOrWhiteSpace(FilterText)) return true;
            var statItem = item as StatisticsViewModelItem;
            return statItem != null && statItem.ProgramName.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
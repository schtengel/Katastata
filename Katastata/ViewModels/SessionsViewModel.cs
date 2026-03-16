using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using Katastata.Models;

namespace Katastata.ViewModels
{
    public class SessionsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SessionViewModel> Sessions { get; } = new ObservableCollection<SessionViewModel>();
        public ICollectionView FilteredSessions { get; }

        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged();
                FilteredSessions.Refresh();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public SessionsViewModel(List<Session> sessions)
        {
            foreach (var session in sessions)
                Sessions.Add(new SessionViewModel(session));

            FilteredSessions = CollectionViewSource.GetDefaultView(Sessions);
            FilteredSessions.Filter = FilterSessions;
        }

        private bool FilterSessions(object item)
        {
            if (string.IsNullOrWhiteSpace(FilterText)) return true;
            var sessionItem = item as SessionViewModel;
            return sessionItem != null && (sessionItem.ProgramName.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                   sessionItem.CategoryName.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
        }
    }
}

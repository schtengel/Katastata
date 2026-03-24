using Katastata.Models;
using Katastata.ViewModels;
using System.Collections.Generic;
using System.Windows;

namespace Katastata
{
    public partial class SessionsWindow : Window
    {
        public SessionsWindow(List<Session> sessions)
        {
            InitializeComponent();
            DataContext = new SessionsViewModel(sessions);
        }
    }
}

using Katastata.Models;
using Katastata.Services;
using Katastata.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace Katastata
{
    public partial class ProgramDetailsWindow : Window
    {
        public ProgramDetailsWindow(Program program, int userId, AppMonitorService service)
        {
            InitializeComponent(); // Сначала инициализация XAML

            var sessions = service.GetSessions(userId).Where(s => s.ProgramId == program.Id).ToList();
            var stat = service.GetStatistics(userId).FirstOrDefault(st => st.ProgramId == program.Id);

            DataContext = new ProgramDetailsViewModel(program, sessions, stat, service); // Потом DataContext
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.Focus();
                e.Handled = true;
            }
        }
    }
}

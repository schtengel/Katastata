using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Katastata
{
    /// <summary>
    /// Логика взаимодействия для ExportWindow.xaml
    /// </summary>
    public partial class ExportWindow : Window
    {

        public string SelectedFormat { get; private set; }

        public ExportWindow()
        {
            InitializeComponent();
        }

        private void Word_Click(object sender, RoutedEventArgs e)
        {
            SelectedFormat = "Word";
            DialogResult = true;
        }

        private void Excel_Click(object sender, RoutedEventArgs e)
        {
            SelectedFormat = "Excel";
            DialogResult = true;
        }
    }
}

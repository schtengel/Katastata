using Katastata.Models;
using Katastata.ViewModels;
using System.Collections.Generic;
using System.Windows;

namespace Katastata
{
    public partial class StatisticsWindow : Window
    {
        public StatisticsWindow(List<Statistics> stats)
        {
            InitializeComponent();
            DataContext = new StatisticsViewModel(stats);
        }
    }
}

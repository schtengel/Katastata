using Katastata.Models;
using Katastata.Services;
using Katastata.ViewModels;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;  // Для tray
using System.Windows.Input;
using System.Windows.Media;

namespace Katastata
{
    public partial class MainWindow : Window
    {
        private NotifyIcon trayIcon;
        private bool isFullscreen = false;

        private Dictionary<int, ProgramDetailsWindow> _openDetailsWindows = new Dictionary<int, ProgramDetailsWindow>();

        public MainWindow(int userId, ApiClient apiClient)
        {
            InitializeComponent();
            var service = new AppMonitorService(apiClient);
            DataContext = new MainViewModel(service, userId);
            HighlightActiveTheme("Dark");

            trayIcon = new NotifyIcon();
            trayIcon.Icon = new Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/Logo/app.ico")).Stream);
            trayIcon.Text = "Katastata";
            trayIcon.Visible = false;


            // Контекстное меню для выхода
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Показать", null, (s, e) => TrayIcon_Click());
            contextMenu.Items.Add("Выход", null, (s, e) => System.Windows.Application.Current.Shutdown());
            trayIcon.ContextMenuStrip = contextMenu;
        }

        // Для дизайнера оставим пустой ctor
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new AppMonitorService(new ApiClient("http://localhost:5099"));
            DataContext = viewModel;
        }

        private void ApplyTheme(string themePath)
        {
            // 1. Очищаем ВСЕ ресурсы приложения (не только MergedDictionaries)
            System.Windows.Application.Current.Resources.Clear();

            // 2. Загружаем новый словарь тем
            var themeDict = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };

            // 3. Добавляем его в ресурсы
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(themeDict);

            // 4. Принудительно обновляем все элементы в текущем окне
            UpdateThemeForAllElements(this);
        }

        // Вспомогательный метод: рекурсивно обновляет стили всех элементов
        private void UpdateThemeForAllElements(Visual container)
        {
            if (container == null) return;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++)
            {
                var child = VisualTreeHelper.GetChild(container, i) as Visual;
                if (child != null)
                {
                    // Принудительно перепривязываем стили
                    if (child is FrameworkElement fe)
                    {
                        fe.InvalidateProperty(FrameworkElement.StyleProperty);
                        fe.InvalidateProperty(System.Windows.Controls.Control.ForegroundProperty);
                        fe.InvalidateProperty(System.Windows.Controls.Control.BackgroundProperty);
                        fe.InvalidateProperty(System.Windows.Controls.Panel.BackgroundProperty);
                    }
                    // Рекурсия для дочерних элементов
                    UpdateThemeForAllElements(child);
                }
            }
        }


        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            ApplyTheme("Assets/Themes/Light.xaml");
            HighlightActiveTheme("Light");
        }

        // Переключение на тёмную тему
        private void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            ApplyTheme("Assets/Themes/Dark.xaml");
            HighlightActiveTheme("Dark");
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"\"{exePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true, // скрываем окно CMD
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                System.Diagnostics.Process.Start(startInfo);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Не удалось перезапустить приложение: " + ex.Message);
            }
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            foreach (var window in _openDetailsWindows.Values)
            {
                window.Close();
            }
            _openDetailsWindows.Clear();

            e.Cancel = true;
            this.Hide();
            trayIcon.Visible = true;  
        }

        private void TrayIcon_Click()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            trayIcon.Visible = false;
        }

        private void FullscreenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isFullscreen)
            {
                // Сохраняем текущие размеры и положение, если нужно
                this.WindowState = WindowState.Maximized;
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
                FullscreenBtn.Content = "🗗"; // символ выхода из полноэкрана
                FullscreenBtn.ToolTip = "Выйти из полноэкранного режима";
                isFullscreen = true;
            }
            else
            {
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.None; // чтобы осталась твоя кастомная шапка
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
                FullscreenBtn.Content = "🗖"; // символ входа в полноэкран
                FullscreenBtn.ToolTip = "Полноэкранный режим";
                isFullscreen = false;
            }
        }

        private void HighlightActiveTheme(string activeTheme)
        {
            System.Windows.Media.Brush accent = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["AccentBrushActive"];
            System.Windows.Media.Brush normal = System.Windows.Media.Brushes.Transparent;
            // Сброс фона
            LightThemeBtn.Background = normal;
            DarkThemeBtn.Background = normal;
            // Подсветка активной
            if (activeTheme == "Light")
                LightThemeBtn.Background = accent;
            else if (activeTheme == "Dark")
                DarkThemeBtn.Background = accent;
        }

        private void ScannerBtn_Click(object sender, RoutedEventArgs e)
        {
            ScanText.Text = "Сканирование завершено!";
        }

        private void SortByName_Click(object sender, RoutedEventArgs e)
        {
            var currentMode = (string)DataContext?.GetType().GetProperty("SortMode")?.GetValue(DataContext);
            string newMode = currentMode == "NameAsc" ? "NameDesc" : "NameAsc";
            DataContext?.GetType().GetMethod("set_SortMode")?.Invoke(DataContext, new object[] { newMode });
        }

        private void SortByLastLaunch_Click(object sender, RoutedEventArgs e)
        {
            var currentMode = (string)DataContext?.GetType().GetProperty("SortMode")?.GetValue(DataContext);
            string newMode = currentMode == "LastLaunchAsc" ? "LastLaunchDesc" : "LastLaunchAsc";
            DataContext?.GetType().GetMethod("set_SortMode")?.Invoke(DataContext, new object[] { newMode });
        }



        private void ProgramTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Program program)
            {
                var vm = (MainViewModel)DataContext;

                if (_openDetailsWindows.ContainsKey(program.Id))
                {
                    var window = _openDetailsWindows[program.Id];
                    window.Activate();
                    window.WindowState = WindowState.Normal;
                    return;
                }

                var detailsWindow = new ProgramDetailsWindow(program, vm.UserId, vm.Service);
                detailsWindow.Owner = this;

                detailsWindow.Closed += (s, ev) =>
                {
                    _openDetailsWindows.Remove(program.Id);
                };

                _openDetailsWindows[program.Id] = detailsWindow;

                detailsWindow.Show();
            }
        }
    }
}
// =======================================================================
// MainWindow.xaml.cs
// Chức năng: CHỈ chứa lifecycle hooks (constructor, Window events)
//            KHÔNG được thêm logic vào file này.
// Cập nhật gần đây:
//   - 2026-03-25: Added UpdateThemeToggleButtonState() call in Window_Loaded
//                 to initialize Theme Toggle Button state
//   - 2026-03-05: Tái cấu trúc theo AI_WORKFLOW.md — xóa toàn bộ logic,
//                 chuyển sang các partial class phù hợp
// =======================================================================
using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace GMTPC.Tool
{
    public partial class MainWindow : Window
    {
        public string TestProperty { get; set; } = "Hello";

        public MainWindow()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
            System.Net.ServicePointManager.Expect100Continue = false;
            System.Net.ServicePointManager.UseNagleAlgorithm = false;
            InitializeComponent();
            UpdateStatus("Chọn một tùy chọn từ menu bên trên hoặc nhấn phím số tương ứng (1-6, 0):", "Cyan");
            this.Closing += MainWindow_Closing;
            UpdateInstallButtonState();
            SetBuildNumber();

            if (CboDPIValue != null && CboDPIValue.Items.Count > 0)
            {
                CboDPIValue.SelectedIndex = 5;
            }

            AddDefenderExclusion();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            Keyboard.Focus(this);
            SetupInitialOrientation();
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            try { PopulateSystemInfo(); } catch { }
            PopulateTempFolderComboBox();
            
            // Khởi tạo trạng thái Theme Toggle Button
            UpdateThemeToggleButtonState();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            RemoveDefenderExclusion();
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            Application.Current.Shutdown();
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                SetupInitialOrientation();
                ApplyDPIScale();
            });
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Key == Key.Add || e.Key == Key.OemPlus)
                {
                    BtnDPIPlus_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
                {
                    BtnDPIMinus_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
            }
        }

        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                BtnDPIPlus_Click(sender, new RoutedEventArgs());
            else if (e.Delta < 0)
                BtnDPIMinus_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}

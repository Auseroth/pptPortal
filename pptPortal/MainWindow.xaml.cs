using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using pptPortal.Services;
using pptPortal.ViewModels;
using pptPortal.Views;

namespace pptPortal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly ConfigService _configService;
        private readonly CredentialService _credentialService;
        private readonly AdminElevationService _adminService;

        // Windows DWM API for title bar customization
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        public MainWindow(
            ConfigService configService,
            CredentialService credentialService,
            FileTransferService fileTransferService,
            AdminElevationService adminService)
        {
            InitializeComponent();

            _configService = configService;
            _credentialService = credentialService;
            _adminService = adminService;

            _viewModel = new MainViewModel(configService, fileTransferService);
            DataContext = _viewModel;

            _viewModel.SettingsRequested += OnSettingsRequested;

            // Set title bar color when window is loaded
            Loaded += (s, e) =>
            {
                DisableSystemBackdrop();
                SetTitleBarColor(44, 62, 80); // Dark blue-gray matching header
            };
        }

        /// <summary>
        /// Disables Windows 11 system backdrop (Mica/Acrylic) to prevent translucency
        /// </summary>
        private void DisableSystemBackdrop()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                // 1 = None (disable system backdrop)
                int backdropType = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
            }
            catch
            {
                // Silently fail on older Windows versions
            }
        }

        /// <summary>
        /// Sets the Windows title bar color (works on Windows 10/11)
        /// </summary>
        /// <param name="r">Red (0-255)</param>
        /// <param name="g">Green (0-255)</param>
        /// <param name="b">Blue (0-255)</param>
        private void SetTitleBarColor(byte r, byte g, byte b)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                // DWM expects COLORREF (0x00BBGGRR) format
                int colorValue = (b << 16) | (g << 8) | r;
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref colorValue, sizeof(int));
            }
            catch
            {
                // Silently fail on older Windows versions or if API is unavailable
            }
        }

        private void OnSettingsRequested(object? sender, EventArgs e)
        {
            var settingsWindow = new SettingsWindow(_configService, _credentialService, _adminService)
            {
                Owner = this
            };

            settingsWindow.ShowDialog();

            // Refresh main view after settings close
            _viewModel.RefreshConfig();
        }
    }
}

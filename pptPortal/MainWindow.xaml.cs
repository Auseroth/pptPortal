using System.Windows;
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

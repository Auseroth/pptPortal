using System.Windows;
using pptPortal.Services;
using pptPortal.Views;

namespace pptPortal
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ConfigService? _configService;
        private CredentialService? _credentialService;
        private FileTransferService? _fileTransferService;
        private AdminElevationService? _adminService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize services
            _configService = new ConfigService();
            _credentialService = new CredentialService();
            _fileTransferService = new FileTransferService(_credentialService);
            _adminService = new AdminElevationService();

            // Check if config exists
            if (!_configService.ConfigExists())
            {
                // First run - show settings window
                var settingsWindow = new SettingsWindow(_configService, _credentialService, _adminService);
                var result = settingsWindow.ShowDialog();

                // If settings were cancelled or not saved, exit app
                if (result != true && !_configService.ConfigExists())
                {
                    MessageBox.Show(
                        "Configuration is required to run the application.",
                        "Setup Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Shutdown();
                    return;
                }
            }

            // Show main window
            var mainWindow = new MainWindow(
                _configService,
                _credentialService,
                _fileTransferService,
                _adminService);

            mainWindow.Show();
        }
    }
}


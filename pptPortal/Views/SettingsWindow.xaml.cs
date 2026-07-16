using System.Windows;
using pptPortal.Services;
using pptPortal.ViewModels;

namespace pptPortal.Views;

public partial class SettingsWindow : Window
{
    private readonly AdminElevationService _adminService;
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(ConfigService configService, CredentialService credentialService, AdminElevationService adminService)
    {
        InitializeComponent();

        _adminService = adminService;
        _viewModel = new SettingsViewModel(configService, credentialService);
        DataContext = _viewModel;

        _viewModel.SaveCompleted += (s, e) => Close();
        _viewModel.CancelRequested += (s, e) => Close();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Check if running as admin
        if (!_adminService.IsRunningAsAdmin())
        {
            var result = MessageBox.Show(
                _adminService.GetAdminRequiredMessage(),
                "Administrator Required",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.OK)
            {
                if (_adminService.RestartAsAdmin())
                {
                    // Close this window and the app will restart elevated
                    Application.Current.Shutdown();
                }
                else
                {
                    // User cancelled UAC prompt
                    MessageBox.Show(
                        "Administrator privileges are required to modify settings. The window will close.",
                        "Access Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Close();
                }
            }
            else
            {
                Close();
            }
        }
    }
}

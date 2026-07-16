using System.Windows;
using pptPortal.Models;

namespace pptPortal.Views;

public partial class ProfileEditDialog : Window
{
    public TargetProfile? Profile { get; private set; }
    public string? Password { get; private set; }

    public ProfileEditDialog(TargetProfile? existingProfile = null)
    {
        InitializeComponent();

        if (existingProfile != null)
        {
            // Edit mode
            ProfileNameTextBox.Text = existingProfile.ProfileName;
            ComputerIdentifierTextBox.Text = existingProfile.ComputerIdentifier;
            TargetUncPathTextBox.Text = existingProfile.TargetUncPath;
            TargetFileNameTextBox.Text = existingProfile.TargetFileName;
            AdminUsernameTextBox.Text = existingProfile.AdminUsername;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(ProfileNameTextBox.Text))
        {
            MessageBox.Show("Profile Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ComputerIdentifierTextBox.Text))
        {
            MessageBox.Show("Computer Identifier is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TargetUncPathTextBox.Text))
        {
            MessageBox.Show("Target UNC Path is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TargetFileNameTextBox.Text))
        {
            MessageBox.Show("Target Filename is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(AdminUsernameTextBox.Text))
        {
            MessageBox.Show("Admin Username is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Create profile
        Profile = new TargetProfile
        {
            ProfileName = ProfileNameTextBox.Text.Trim(),
            ComputerIdentifier = ComputerIdentifierTextBox.Text.Trim(),
            TargetUncPath = TargetUncPathTextBox.Text.Trim(),
            TargetFileName = TargetFileNameTextBox.Text.Trim(),
            AdminUsername = AdminUsernameTextBox.Text.Trim()
        };

        Password = PasswordBox.Password;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

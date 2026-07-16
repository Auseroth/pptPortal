using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using pptPortal.Models;
using pptPortal.Services;
using pptPortal.Views;

namespace pptPortal.ViewModels;

/// <summary>
/// ViewModel for the Settings window
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly CredentialService _credentialService;
    private AppConfig _config;
    private TargetProfile? _selectedProfile;
    private bool _isDirty;

    public SettingsViewModel(ConfigService configService, CredentialService credentialService)
    {
        _configService = configService;
        _credentialService = credentialService;

        // Load existing config or create new
        _config = _configService.ConfigExists() 
            ? _configService.LoadConfig() 
            : new AppConfig();

        Profiles = new ObservableCollection<TargetProfile>(_config.Profiles);

        // Commands
        AddProfileCommand = new RelayCommand(AddProfile);
        EditProfileCommand = new RelayCommand(EditProfile, () => SelectedProfile != null);
        RemoveProfileCommand = new RelayCommand(RemoveProfile, () => SelectedProfile != null);
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
    }

    public ObservableCollection<TargetProfile> Profiles { get; }

    public TargetProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value))
            {
                ((RelayCommand)EditProfileCommand).RaiseCanExecuteChanged();
                ((RelayCommand)RemoveProfileCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool UseDPAPI
    {
        get => _config.CredentialMode == CredentialStorageMode.DPAPI;
        set
        {
            if (value && _config.CredentialMode != CredentialStorageMode.DPAPI)
            {
                _config.CredentialMode = CredentialStorageMode.DPAPI;
                _isDirty = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseCredentialManager));
            }
        }
    }

    public bool UseCredentialManager
    {
        get => _config.CredentialMode == CredentialStorageMode.CredentialManager;
        set
        {
            if (value && _config.CredentialMode != CredentialStorageMode.CredentialManager)
            {
                _config.CredentialMode = CredentialStorageMode.CredentialManager;
                _isDirty = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseDPAPI));
            }
        }
    }

    public ICommand AddProfileCommand { get; }
    public ICommand EditProfileCommand { get; }
    public ICommand RemoveProfileCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler? SaveCompleted;
    public event EventHandler? CancelRequested;

    private void AddProfile()
    {
        var dialog = new ProfileEditDialog();
        if (dialog.ShowDialog() == true && dialog.Profile != null)
        {
            var profile = dialog.Profile;

            // Save credential
            if (!string.IsNullOrEmpty(dialog.Password))
            {
                _credentialService.SaveCredential(profile, dialog.Password, _config.CredentialMode);
            }

            Profiles.Add(profile);
            _isDirty = true;
        }
    }

    private void EditProfile()
    {
        if (SelectedProfile == null) return;

        var dialog = new ProfileEditDialog(SelectedProfile);
        if (dialog.ShowDialog() == true && dialog.Profile != null)
        {
            var index = Profiles.IndexOf(SelectedProfile);
            if (index >= 0)
            {
                // Update credential if password was changed
                if (!string.IsNullOrEmpty(dialog.Password))
                {
                    _credentialService.SaveCredential(dialog.Profile, dialog.Password, _config.CredentialMode);
                }

                Profiles[index] = dialog.Profile;
                _isDirty = true;
            }
        }
    }

    private void RemoveProfile()
    {
        if (SelectedProfile == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to remove profile '{SelectedProfile.ProfileName}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            // Delete credential
            _credentialService.DeleteCredential(SelectedProfile, _config.CredentialMode);

            Profiles.Remove(SelectedProfile);
            _isDirty = true;
        }
    }

    private void Save()
    {
        try
        {
            // Update config from UI
            _config.Profiles = Profiles.ToList();

            // Save to disk
            _configService.SaveConfig(_config);

            MessageBox.Show(
                "Configuration saved successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _isDirty = false;
            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(
                ex.Message,
                "Access Denied",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save configuration: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Cancel()
    {
        if (_isDirty)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Are you sure you want to cancel?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
                return;
        }

        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Simple relay command implementation
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

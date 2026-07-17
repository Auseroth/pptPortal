using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;
using pptPortal.Models;
using pptPortal.Services;

namespace pptPortal.ViewModels;

/// <summary>
/// ViewModel for the main window
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly FileTransferService _fileTransferService;
    private readonly UserPreferencesService _preferencesService;
    private AppConfig? _config;
    private UserPreferences? _preferences;
    private string? _selectedFilePath;
    private string _statusMessage = "Ready";
    private bool _isUploading;
    private readonly string _machineName = Environment.MachineName;

    public MainViewModel(
        ConfigService configService, 
        FileTransferService fileTransferService,
        UserPreferencesService preferencesService)
    {
        _configService = configService;
        _fileTransferService = fileTransferService;
        _preferencesService = preferencesService;

        // Commands - INITIALIZE FIRST
        BrowseFileCommand = new RelayCommand(BrowseFile);
        UploadFileCommand = new RelayCommand(UploadFile, CanUploadFile);
        OpenSettingsCommand = new RelayCommand(OpenSettings);

        // Load config and preferences - THEN LOAD DATA
        LoadConfig();
        LoadPreferences();
    }

    public string? SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            if (SetProperty(ref _selectedFilePath, value))
            {
                ((RelayCommand)UploadFileCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsUploading
    {
        get => _isUploading;
        set
        {
            if (SetProperty(ref _isUploading, value))
            {
                ((RelayCommand)UploadFileCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string? CurrentProfileName => _config?.SelectedProfile?.ProfileName;

    public List<string> RecentFiles => _preferences?.RecentFiles ?? new List<string>();

    public string MachineName => _machineName;

    public List<TargetProfile> AvailableProfiles => _config?.Profiles ?? new List<TargetProfile>();

    public TargetProfile? SelectedProfile
    {
        get => _config?.SelectedProfile;
        set
        {
            if (_config != null && value != null)
            {
                var index = _config.Profiles.IndexOf(value);
                if (index >= 0)
                {
                    _config.SelectedProfileIndex = index;
                    
                    // Save to user preferences instead of config
                    SaveSelectedProfileToPreferences(index);
                    
                    OnPropertyChanged(nameof(SelectedProfile));
                    OnPropertyChanged(nameof(CurrentProfileName));
                    ((RelayCommand)UploadFileCommand).RaiseCanExecuteChanged();
                }
            }
        }
    }

    public ICommand BrowseFileCommand { get; }
    public ICommand UploadFileCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    public event EventHandler? SettingsRequested;

    private void LoadConfig()
    {
        try
        {
            if (_configService.ConfigExists())
            {
                _config = _configService.LoadConfig();

                // Restore selected profile from user preferences
                if (_preferences != null && _preferences.SelectedProfileIndex.HasValue 
                    && _preferences.SelectedProfileIndex.Value >= 0 
                    && _preferences.SelectedProfileIndex.Value < _config.Profiles.Count)
                {
                    _config.SelectedProfileIndex = _preferences.SelectedProfileIndex.Value;
                }

                if (_config.SelectedProfile == null)
                {
                    StatusMessage = "No profile selected. Please configure settings.";
                }
                else
                {
                    StatusMessage = $"Ready - Profile: {_config.SelectedProfile.ProfileName}";
                }
            }
            else
            {
                StatusMessage = "Configuration not found. Please open Settings.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading configuration: {ex.Message}";
        }

        OnPropertyChanged(nameof(CurrentProfileName));
        OnPropertyChanged(nameof(AvailableProfiles));
        OnPropertyChanged(nameof(SelectedProfile));
    }

    private void LoadPreferences()
    {
        _preferences = _preferencesService.LoadPreferences();

        // Load computer-specific or general last file path
        if (_preferences.ComputerSpecificSettings.TryGetValue(_machineName, out var computerPrefs) 
            && !string.IsNullOrEmpty(computerPrefs.LastFilePath) 
            && File.Exists(computerPrefs.LastFilePath))
        {
            SelectedFilePath = computerPrefs.LastFilePath;
        }
        else if (!string.IsNullOrEmpty(_preferences.LastFilePath) && File.Exists(_preferences.LastFilePath))
        {
            SelectedFilePath = _preferences.LastFilePath;
        }

        OnPropertyChanged(nameof(RecentFiles));
    }

    private void SaveSelectedProfileToPreferences(int profileIndex)
    {
        if (_preferences == null) return;

        try
        {
            _preferences.SelectedProfileIndex = profileIndex;
            _preferencesService.SavePreferences(_preferences);
            StatusMessage = $"Switched to profile: {_config?.SelectedProfile?.ProfileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save profile preference:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Reload to revert UI
            LoadPreferences();
            OnPropertyChanged(nameof(SelectedProfile));
        }
    }

    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PowerPoint Files|*.ppt;*.pptx|All Files|*.*",
            Title = "Select PowerPoint File"
        };

        // Set initial directory from preferences
        if (_preferences?.ComputerSpecificSettings.TryGetValue(_machineName, out var computerPrefs) == true
            && !string.IsNullOrEmpty(computerPrefs.DefaultDirectory))
        {
            dialog.InitialDirectory = computerPrefs.DefaultDirectory;
        }
        else if (!string.IsNullOrEmpty(_preferences?.LastFilePath))
        {
            var lastDir = Path.GetDirectoryName(_preferences.LastFilePath);
            if (!string.IsNullOrEmpty(lastDir) && Directory.Exists(lastDir))
            {
                dialog.InitialDirectory = lastDir;
            }
        }

        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
            SaveFilePreference(dialog.FileName);
        }
    }

    private void SaveFilePreference(string filePath)
    {
        if (_preferences == null) return;

        // Update general last file
        _preferences.LastFilePath = filePath;

        // Update computer-specific settings
        if (!_preferences.ComputerSpecificSettings.ContainsKey(_machineName))
        {
            _preferences.ComputerSpecificSettings[_machineName] = new ComputerPreferences();
        }

        _preferences.ComputerSpecificSettings[_machineName].LastFilePath = filePath;
        _preferences.ComputerSpecificSettings[_machineName].DefaultDirectory = Path.GetDirectoryName(filePath);
        _preferences.ComputerSpecificSettings[_machineName].PreferredProfile = _config?.SelectedProfile?.ProfileName;

        // Maintain recent files list (max 10)
        if (!_preferences.RecentFiles.Contains(filePath))
        {
            _preferences.RecentFiles.Insert(0, filePath);
            if (_preferences.RecentFiles.Count > 10)
            {
                _preferences.RecentFiles.RemoveAt(_preferences.RecentFiles.Count - 1);
            }
        }

        _preferencesService.SavePreferences(_preferences);
        OnPropertyChanged(nameof(RecentFiles));
    }

    private bool CanUploadFile()
    {
        return !string.IsNullOrEmpty(SelectedFilePath) 
               && _config?.SelectedProfile != null 
               && !IsUploading;
    }

    private async void UploadFile()
    {
        if (_config?.SelectedProfile == null)
        {
            MessageBox.Show(
                "No profile selected. Please configure settings first.",
                "No Profile",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(SelectedFilePath) || !File.Exists(SelectedFilePath))
        {
            MessageBox.Show(
                "Please select a valid file first.",
                "Invalid File",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        IsUploading = true;
        StatusMessage = "Uploading...";

        try
        {
            var result = await Task.Run(() => 
                _fileTransferService.UploadFile(
                    SelectedFilePath, 
                    _config.SelectedProfile, 
                    _config.CredentialMode));

            if (result.Success)
            {
                StatusMessage = $"Upload successful! File saved to: {result.TargetPath}";
                MessageBox.Show(
                    $"File uploaded successfully!\n\nTarget: {result.TargetPath}",
                    "Upload Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = $"Upload failed: {result.ErrorMessage}";
                MessageBox.Show(
                    $"Upload failed:\n\n{result.ErrorMessage}",
                    "Upload Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Upload error: {ex.Message}";
            MessageBox.Show(
                $"An error occurred during upload:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsUploading = false;
        }
    }

    private void OpenSettings()
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);

        // Reload config after settings are closed
        LoadConfig();
    }

    public void RefreshConfig()
    {
        LoadConfig();
        OnPropertyChanged(nameof(CurrentProfileName));
        OnPropertyChanged(nameof(AvailableProfiles));
        OnPropertyChanged(nameof(SelectedProfile));
    }
}

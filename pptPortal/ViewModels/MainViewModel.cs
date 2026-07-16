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
    private AppConfig? _config;
    private string? _selectedFilePath;
    private string _statusMessage = "Ready";
    private bool _isUploading;

    public MainViewModel(ConfigService configService, FileTransferService fileTransferService)
    {
        _configService = configService;
        _fileTransferService = fileTransferService;

        // Load config
        LoadConfig();

        // Commands
        BrowseFileCommand = new RelayCommand(BrowseFile);
        UploadFileCommand = new RelayCommand(UploadFile, CanUploadFile);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
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
    }

    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select PowerPoint File",
            Filter = "PowerPoint Files (*.pptx;*.ppt)|*.pptx;*.ppt|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
            StatusMessage = $"Selected: {Path.GetFileName(dialog.FileName)}";
        }
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
    }
}

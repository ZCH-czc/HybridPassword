using PasswordVault.PasskeyCompanion.Models;
using PasswordVault.PasskeyCompanion.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace PasswordVault.PasskeyCompanion;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly CompanionStatusService _statusService;
    private readonly PluginRegistrationService _registrationService;
    private CompanionStatusSnapshot _snapshot = new();

    public MainWindow()
        : this(new CompanionStatusService())
    {
    }

    public MainWindow(CompanionStatusService statusService)
    {
        _statusService = statusService;
        _registrationService = new PluginRegistrationService(
            statusService,
            new PluginActivationService());
        InitializeComponent();
        DataContext = this;
        RefreshSnapshot();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> MissingExports { get; } = [];

    public ObservableCollection<string> NextSteps { get; } = [];

    public ObservableCollection<string> Responsibilities { get; } = [];

    public string StatusSummary => _snapshot.StatusSummary;

    public string DetailMessage => _snapshot.DetailMessage;

    public string CheckedAtText => $"Checked at {Snapshot.CheckedAt:yyyy-MM-dd HH:mm:ss}";

    public string WindowsBuildText => $"{Snapshot.BuildNumber}.{Snapshot.Ubr}";

    public string PackagedText => Snapshot.IsPackagedProcess ? "Yes" : "No";

    public string PackageFamilyNameText => string.IsNullOrWhiteSpace(Snapshot.PackageFamilyName)
        ? "Not available"
        : Snapshot.PackageFamilyName;

    public string PackageFullNameText => string.IsNullOrWhiteSpace(Snapshot.PackageFullName)
        ? "Not available"
        : Snapshot.PackageFullName;

    public string WebAuthnText => Snapshot.WebAuthnLibraryAvailable ? "Available" : "Missing";

    public string PluginExportsText => Snapshot.PluginExportsAvailable ? "Ready" : "Missing";

    public bool IsCompanionReady => Snapshot.IsCompanionReady;

    public Brush StatusTagBackground => Snapshot.IsCompanionReady
        ? new SolidColorBrush(Color.FromRgb(13, 94, 69))
        : new SolidColorBrush(Color.FromRgb(48, 58, 74));

    public string MissingExportsEmptyText => MissingExports.Count == 0
        ? "No missing exports were detected."
        : string.Empty;

    private CompanionStatusSnapshot Snapshot
    {
        get => _snapshot;
        set
        {
            _snapshot = value;
            OnPropertyChanged(nameof(StatusSummary));
            OnPropertyChanged(nameof(DetailMessage));
            OnPropertyChanged(nameof(CheckedAtText));
            OnPropertyChanged(nameof(WindowsBuildText));
            OnPropertyChanged(nameof(PackagedText));
            OnPropertyChanged(nameof(PackageFamilyNameText));
            OnPropertyChanged(nameof(PackageFullNameText));
            OnPropertyChanged(nameof(WebAuthnText));
            OnPropertyChanged(nameof(PluginExportsText));
            OnPropertyChanged(nameof(IsCompanionReady));
            OnPropertyChanged(nameof(StatusTagBackground));
            OnPropertyChanged(nameof(MissingExportsEmptyText));
        }
    }

    private void RefreshSnapshot()
    {
        Snapshot = _statusService.Probe();
        var workflow = _registrationService.GetWorkflowSnapshot();

        ReplaceItems(MissingExports, Snapshot.MissingExports);
        ReplaceItems(NextSteps, Snapshot.NextSteps);
        ReplaceItems(
            Responsibilities,
            [
                $"Workflow mode: {workflow.WorkflowMode}",
                $"Registration status: {workflow.RegistrationStatus}",
                $"COM skeleton ready: {workflow.ComSkeletonReady}",
                $"COM class matches manifest: {workflow.ComClassIdMatchesManifest}",
                $"Create requests captured: {workflow.CreateRequestCount}",
            ]);
    }

    private static void ReplaceItems(ObservableCollection<string> target, IReadOnlyList<string> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshSnapshot();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

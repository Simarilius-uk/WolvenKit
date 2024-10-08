using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using WolvenKit.App.Models.Docking;
using WolvenKit.Core.Interfaces;

namespace WolvenKit.App.ViewModels.Tools;

/// <summary>
/// Implements the viewmodel that drives the log view.
/// </summary>
public partial class LogViewModel : ToolViewModel
{
    #region Fields

    /// <summary>
    /// Identifies the <see ref="ContentId"/> of this tool window.
    /// </summary>
    public const string ToolContentId = "Log_Tool";

    /// <summary>
    /// Identifies the caption string used for this tool window.
    /// </summary>
    public const string ToolTitle = "Log";

    private readonly ILoggerService _loggerService;

    // private readonly ReadOnlyObservableCollection<LogEntry> _logEntries;
    // public ReadOnlyObservableCollection<LogEntry> LogEntries => _logEntries;

    [ObservableProperty] private FlowDocument _document = new();

    #endregion Fields

    public LogViewModel(
        ILoggerService loggerService
        ) : base(ToolTitle)
    {
        _loggerService = loggerService;

        SetupToolDefaults();
        SideInDockedMode = DockSide.Bottom;

        //filter, sort and populate reactive list,
        // _loggerService.Connect() //connect to the cache
        //     .ObserveOn(RxApp.MainThreadScheduler)
        //     .Bind(out _logEntries)
        //     .Subscribe(OnNext);
    }


    private void SetupToolDefaults() => ContentId = ToolContentId;
}

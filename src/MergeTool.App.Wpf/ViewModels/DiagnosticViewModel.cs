using System.Windows;
using MergeTool.Core;
using MergeTool.Core.Diagnostics;

namespace MergeTool.App.Wpf.ViewModels;

/// <summary>
/// Presents one diagnostic: the safe message is always visible; technical detail is shown
/// only when the user explicitly expands it.
/// </summary>
public sealed class DiagnosticViewModel : ViewModelBase
{
    private bool _showTechnicalDetail;

    public DiagnosticViewModel(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        Diagnostic = diagnostic;
        CopyDetailsCommand = new RelayCommand(_ => Clipboard.SetText(Diagnostic.ToCopyPayload()));
    }

    public Diagnostic Diagnostic { get; }

    public DiagnosticSeverity Severity => Diagnostic.Severity;

    public string Code => Diagnostic.Code;

    public string Message => Diagnostic.Message;

    public string? Path => Diagnostic.Path;

    public string? RecoveryHint => DiagnosticRecovery.TryGetRecoveryHint(Diagnostic.Code);

    public bool HasRecoveryHint => !string.IsNullOrEmpty(RecoveryHint);

    public bool HasTechnicalDetail => !string.IsNullOrEmpty(Diagnostic.TechnicalDetail);

    public RelayCommand CopyDetailsCommand { get; }

    /// <summary>Technical detail is exposed only while expanded.</summary>
    public string? VisibleTechnicalDetail => ShowTechnicalDetail ? Diagnostic.TechnicalDetail : null;

    public bool ShowTechnicalDetail
    {
        get => _showTechnicalDetail;
        set
        {
            if (SetProperty(ref _showTechnicalDetail, value))
            {
                RaisePropertyChanged(nameof(VisibleTechnicalDetail));
            }
        }
    }
}

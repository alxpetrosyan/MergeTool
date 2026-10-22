namespace MergeTool.Core;

public enum DiagnosticSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
}

/// <summary>
/// Side of a comparison a diagnostic refers to, when relevant.
/// </summary>
public enum ComparisonSide
{
    None = 0,
    Left = 1,
    Right = 2,
    Base = 3,
}

/// <summary>
/// A structured, immutable diagnostic. <see cref="Message"/> is always safe to show to the
/// user; exception text and other internals belong in <see cref="TechnicalDetail"/> only.
/// </summary>
public sealed record Diagnostic
{
    public Diagnostic(
        DiagnosticSeverity severity,
        string code,
        string message,
        string? technicalDetail = null,
        string? path = null,
        ComparisonSide side = ComparisonSide.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Severity = severity;
        Code = code;
        Message = message;
        TechnicalDetail = technicalDetail;
        Path = path;
        Side = side;
    }

    public DiagnosticSeverity Severity { get; }

    /// <summary>Stable machine-readable code, e.g. <c>MT-IO-001</c>.</summary>
    public string Code { get; }

    /// <summary>User-safe message. Must not contain raw exception details.</summary>
    public string Message { get; }

    /// <summary>Optional technical detail for reports/logs; never shown directly in UI.</summary>
    public string? TechnicalDetail { get; }

    /// <summary>Optional normalized full path this diagnostic refers to.</summary>
    public string? Path { get; }

    public ComparisonSide Side { get; }

    public static Diagnostic Info(string code, string message, string? path = null, ComparisonSide side = ComparisonSide.None)
        => new(DiagnosticSeverity.Info, code, message, null, path, side);

    public static Diagnostic Warning(string code, string message, string? technicalDetail = null, string? path = null, ComparisonSide side = ComparisonSide.None)
        => new(DiagnosticSeverity.Warning, code, message, technicalDetail, path, side);

    public static Diagnostic Error(string code, string message, string? technicalDetail = null, string? path = null, ComparisonSide side = ComparisonSide.None)
        => new(DiagnosticSeverity.Error, code, message, technicalDetail, path, side);

    /// <summary>A copy of this diagnostic attributed to the given comparison side.</summary>
    public Diagnostic WithSide(ComparisonSide side)
        => new(Severity, Code, Message, TechnicalDetail, Path, side);

    /// <summary>Plain-text payload for clipboard copy: code, safe message, optional path, then technical detail.</summary>
    public string ToCopyPayload()
    {
        var lines = new List<string> { Code, Message };
        if (!string.IsNullOrEmpty(Path))
        {
            lines.Add($"Path: {Path}");
        }

        if (!string.IsNullOrEmpty(TechnicalDetail))
        {
            lines.Add(TechnicalDetail);
        }

        return string.Join(Environment.NewLine, lines);
    }
}

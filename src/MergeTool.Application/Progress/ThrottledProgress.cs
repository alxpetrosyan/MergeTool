namespace MergeTool.Application.Progress;

/// <summary>
/// Forwards progress reports at most once per interval to avoid flooding the UI thread.
/// </summary>
public sealed class ThrottledProgress<T> : IProgress<T>
{
    private readonly IProgress<T> _inner;
    private readonly TimeSpan _minimumInterval;
    private readonly object _gate = new();
    private DateTime _lastReportUtc = DateTime.MinValue;
    private T? _pending;

    public ThrottledProgress(IProgress<T> inner, TimeSpan minimumInterval)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _minimumInterval = minimumInterval;
    }

    public void Report(T value)
    {
        lock (_gate)
        {
            DateTime now = DateTime.UtcNow;
            if (now - _lastReportUtc >= _minimumInterval)
            {
                _lastReportUtc = now;
                _pending = default;
                _inner.Report(value);
                return;
            }

            _pending = value;
        }
    }

    /// <summary>Flushes the most recent pending report, if any.</summary>
    public void Flush()
    {
        lock (_gate)
        {
            if (_pending is null)
            {
                return;
            }

            _lastReportUtc = DateTime.UtcNow;
            _inner.Report(_pending);
            _pending = default;
        }
    }
}

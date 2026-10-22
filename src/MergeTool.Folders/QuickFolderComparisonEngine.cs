using MergeTool.Core;

namespace MergeTool.Folders;

/// <summary>
/// Recursive folder comparison using quick metadata and optional SHA-256 content hashing.
/// Metadata matches compare file length and UTC last-write time only; they never prove
/// content equality unless <see cref="FolderComparisonMode.ContentHash"/> is selected.
/// </summary>
public sealed class QuickFolderComparisonEngine
{
    public const string LeftRootMissingCode = "MT-FLD-001";
    public const string RightRootMissingCode = "MT-FLD-002";
    public const string LeftRootNotDirectoryCode = "MT-FLD-003";
    public const string RightRootNotDirectoryCode = "MT-FLD-004";
    public const string SameRootCode = "MT-FLD-005";
    public const string SameRootMessage =
        "The same folder cannot be compared with itself. Choose two different folders.";
    public const string BaseRootMissingCode = "MT-FLD-007";
    public const string BaseRootNotDirectoryCode = "MT-FLD-008";
    public const string DuplicateRootCode = "MT-FLD-009";
    public const string DuplicateRootMessage =
        "Folder roots must be three different folders.";

    private readonly IFolderScanAccess _access;
    private readonly IFileContentHashAccess _hashAccess;

    public QuickFolderComparisonEngine(
        IFolderScanAccess? access = null,
        IFileContentHashAccess? hashAccess = null)
    {
        _access = access ?? new PhysicalFolderScanAccess();
        _hashAccess = hashAccess ?? new PhysicalFileContentHashAccess();
    }

    public OperationResult<FolderComparisonResult> Compare(
        FolderComparisonRequest request,
        IProgress<FolderComparisonProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsThreeWay)
        {
            return CompareThreeWay(request, progress, cancellationToken);
        }

        OperationResult<(string LeftRoot, string RightRoot)> validation = ValidateTwoWayRoots(request);
        if (!validation.IsCompleted)
        {
            return OperationResult<FolderComparisonResult>.Failed(validation.Diagnostics);
        }

        (string leftRoot, string rightRoot) = validation.Value;
        List<Diagnostic> scanDiagnostics = [];
        PathAccumulator accumulator = new();

        try
        {
            ScanSide(leftRoot, string.Empty, ComparisonSide.Left, accumulator, scanDiagnostics, progress, cancellationToken);
            ScanSide(rightRoot, string.Empty, ComparisonSide.Right, accumulator, scanDiagnostics, progress, cancellationToken);

            IReadOnlyList<FolderComparisonRow> rows = BuildRows(accumulator, isThreeWay: false, request.Options);
            rows = ApplyContentHashing(rows, request, scanDiagnostics, progress, cancellationToken);
            return OperationResult<FolderComparisonResult>.Completed(
                new FolderComparisonResult(rows, scanDiagnostics, accumulator.EntriesScanned),
                scanDiagnostics);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<FolderComparisonResult>.Cancelled(scanDiagnostics);
        }
    }

    private OperationResult<FolderComparisonResult> CompareThreeWay(
        FolderComparisonRequest request,
        IProgress<FolderComparisonProgress>? progress,
        CancellationToken cancellationToken)
    {
        OperationResult<(string BaseRoot, string LeftRoot, string RightRoot)> validation = ValidateThreeWayRoots(request);
        if (!validation.IsCompleted)
        {
            return OperationResult<FolderComparisonResult>.Failed(validation.Diagnostics);
        }

        (string baseRoot, string leftRoot, string rightRoot) = validation.Value;
        List<Diagnostic> scanDiagnostics = [];
        PathAccumulator accumulator = new();

        try
        {
            ScanSide(baseRoot, string.Empty, ComparisonSide.Base, accumulator, scanDiagnostics, progress, cancellationToken);
            ScanSide(leftRoot, string.Empty, ComparisonSide.Left, accumulator, scanDiagnostics, progress, cancellationToken);
            ScanSide(rightRoot, string.Empty, ComparisonSide.Right, accumulator, scanDiagnostics, progress, cancellationToken);

            IReadOnlyList<FolderComparisonRow> rows = BuildRows(accumulator, isThreeWay: true, request.Options);
            rows = ApplyContentHashing(rows, request, scanDiagnostics, progress, cancellationToken);
            return OperationResult<FolderComparisonResult>.Completed(
                new FolderComparisonResult(rows, scanDiagnostics, accumulator.EntriesScanned),
                scanDiagnostics);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<FolderComparisonResult>.Cancelled(scanDiagnostics);
        }
    }

    private OperationResult<(string LeftRoot, string RightRoot)> ValidateTwoWayRoots(FolderComparisonRequest request)
    {
        string leftRoot;
        string rightRoot;
        try
        {
            leftRoot = PathNormalization.NormalizeFullPath(request.LeftRoot);
            rightRoot = PathNormalization.NormalizeFullPath(request.RightRoot);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<(string, string)>.Failed(
                Diagnostic.Error("MT-FLD-006", "One of the folder paths is invalid.", technicalDetail: exception.Message));
        }

        if (string.Equals(leftRoot, rightRoot, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<(string, string)>.Failed(
                Diagnostic.Error(SameRootCode, SameRootMessage, path: leftRoot));
        }

        if (!_access.DirectoryExists(leftRoot))
        {
            return OperationResult<(string, string)>.Failed(
                Diagnostic.Error(LeftRootMissingCode, "The left folder does not exist.", path: leftRoot, side: ComparisonSide.Left));
        }

        if (!_access.DirectoryExists(rightRoot))
        {
            return OperationResult<(string, string)>.Failed(
                Diagnostic.Error(RightRootMissingCode, "The right folder does not exist.", path: rightRoot, side: ComparisonSide.Right));
        }

        return OperationResult<(string, string)>.Completed((leftRoot, rightRoot));
    }

    private OperationResult<(string BaseRoot, string LeftRoot, string RightRoot)> ValidateThreeWayRoots(
        FolderComparisonRequest request)
    {
        string baseRoot;
        string leftRoot;
        string rightRoot;
        try
        {
            baseRoot = PathNormalization.NormalizeFullPath(request.BaseRoot!);
            leftRoot = PathNormalization.NormalizeFullPath(request.LeftRoot);
            rightRoot = PathNormalization.NormalizeFullPath(request.RightRoot);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<(string, string, string)>.Failed(
                Diagnostic.Error("MT-FLD-006", "One of the folder paths is invalid.", technicalDetail: exception.Message));
        }

        if (string.Equals(baseRoot, leftRoot, StringComparison.OrdinalIgnoreCase)
            || string.Equals(baseRoot, rightRoot, StringComparison.OrdinalIgnoreCase)
            || string.Equals(leftRoot, rightRoot, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<(string, string, string)>.Failed(
                Diagnostic.Error(DuplicateRootCode, DuplicateRootMessage));
        }

        if (!_access.DirectoryExists(baseRoot))
        {
            return OperationResult<(string, string, string)>.Failed(
                Diagnostic.Error(BaseRootMissingCode, "The base folder does not exist.", path: baseRoot, side: ComparisonSide.Base));
        }

        if (!_access.DirectoryExists(leftRoot))
        {
            return OperationResult<(string, string, string)>.Failed(
                Diagnostic.Error(LeftRootMissingCode, "The left folder does not exist.", path: leftRoot, side: ComparisonSide.Left));
        }

        if (!_access.DirectoryExists(rightRoot))
        {
            return OperationResult<(string, string, string)>.Failed(
                Diagnostic.Error(RightRootMissingCode, "The right folder does not exist.", path: rightRoot, side: ComparisonSide.Right));
        }

        return OperationResult<(string, string, string)>.Completed((baseRoot, leftRoot, rightRoot));
    }

    private void ScanSide(
        string root,
        string relativePath,
        ComparisonSide side,
        PathAccumulator accumulator,
        List<Diagnostic> scanDiagnostics,
        IProgress<FolderComparisonProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = string.IsNullOrEmpty(relativePath)
            ? root
            : Path.Combine(root, relativePath);

        if (!_access.TryEnumerateDirectory(fullPath, out IReadOnlyList<FolderScanEntry> entries, out Diagnostic? enumerateError))
        {
            if (!string.IsNullOrEmpty(relativePath))
            {
                accumulator.AddError(relativePath, side, fullPath, enumerateError!);
                scanDiagnostics.Add(enumerateError!.WithSide(side));
            }
            else
            {
                scanDiagnostics.Add(enumerateError!.WithSide(side));
            }

            return;
        }

        bool producedChild = false;
        foreach (FolderScanEntry entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string childRelative = string.IsNullOrEmpty(relativePath)
                ? entry.Name
                : Path.Combine(relativePath, entry.Name);
            string childFull = Path.Combine(root, childRelative);

            if (entry.IsDirectory)
            {
                if (_access.IsReparsePointDirectory(childFull))
                {
                    producedChild = true;
                    accumulator.AddReparsePoint(childRelative, side, childFull);
                    ReportProgress(accumulator, progress, childRelative);
                    continue;
                }

                producedChild = true;
                ScanSide(root, childRelative, side, accumulator, scanDiagnostics, progress, cancellationToken);
                continue;
            }

            producedChild = true;
            if (!_access.TryGetFileMetadata(childFull, out long length, out DateTime lastWriteUtc, out Diagnostic? metadataError))
            {
                accumulator.AddError(childRelative, side, childFull, metadataError!);
                scanDiagnostics.Add(metadataError!.WithSide(side));
            }
            else
            {
                accumulator.AddFile(childRelative, side, childFull, length, lastWriteUtc);
            }

            ReportProgress(accumulator, progress, childRelative);
        }

        if (!string.IsNullOrEmpty(relativePath) && !producedChild)
        {
            accumulator.AddEmptyDirectory(relativePath, side, fullPath);
            ReportProgress(accumulator, progress, relativePath);
        }
    }

    private static void ReportProgress(
        PathAccumulator accumulator,
        IProgress<FolderComparisonProgress>? progress,
        string relativePath)
    {
        progress?.Report(new FolderComparisonProgress(accumulator.EntriesScanned, relativePath));
    }

    private static IReadOnlyList<FolderComparisonRow> BuildRows(
        PathAccumulator accumulator,
        bool isThreeWay,
        FolderComparisonOptions options)
    {
        List<FolderComparisonRow> rows = [];
        foreach (MergedPath merged in accumulator.GetMergedPaths())
        {
            rows.Add(isThreeWay ? CreateThreeWayRow(merged, options) : CreateTwoWayRow(merged, options));
        }

        rows.Sort(CompareRows);
        return rows;
    }

    private static FolderComparisonRow CreateTwoWayRow(MergedPath merged, FolderComparisonOptions options)
    {
        if (merged.StateOverride is FolderRowState overrideState)
        {
            return BuildRow(merged, overrideState);
        }

        if (merged.HasErrors)
        {
            return BuildRow(merged, FolderRowState.Error);
        }

        bool hasLeft = merged.LeftFullPath is not null;
        bool hasRight = merged.RightFullPath is not null;

        if (hasLeft && !hasRight)
        {
            return BuildRow(merged, FolderRowState.LeftOnly);
        }

        if (!hasLeft && hasRight)
        {
            return BuildRow(merged, FolderRowState.RightOnly);
        }

        if (merged.IsDirectory)
        {
            return BuildRow(merged, FolderRowState.MetadataMatch);
        }

        bool metadataMatch = MetadataMatches(
            merged.LeftSizeBytes,
            merged.RightSizeBytes,
            merged.LeftLastWriteTimeUtc,
            merged.RightLastWriteTimeUtc,
            options.MetadataTimestampTolerance);

        return BuildRow(merged, metadataMatch ? FolderRowState.MetadataMatch : FolderRowState.MetadataDifferent);
    }

    private static FolderComparisonRow CreateThreeWayRow(MergedPath merged, FolderComparisonOptions options)
    {
        if (merged.StateOverride is FolderRowState overrideState)
        {
            return BuildRow(merged, overrideState);
        }

        if (merged.HasErrors)
        {
            return BuildRow(merged, FolderRowState.Error);
        }

        bool hasBase = merged.BaseFullPath is not null;
        bool hasLeft = merged.LeftFullPath is not null;
        bool hasRight = merged.RightFullPath is not null;
        int presentCount = (hasBase ? 1 : 0) + (hasLeft ? 1 : 0) + (hasRight ? 1 : 0);

        if (presentCount == 1)
        {
            FolderRowState state = hasBase
                ? FolderRowState.BaseOnly
                : hasLeft
                    ? FolderRowState.LeftOnly
                    : FolderRowState.RightOnly;
            return BuildRow(merged, state);
        }

        if (presentCount == 2)
        {
            FolderRowState state = hasBase && hasLeft
                ? FolderRowState.BaseLeftOnly
                : hasBase && hasRight
                    ? FolderRowState.BaseRightOnly
                    : FolderRowState.LeftRightOnly;
            return BuildRow(merged, state);
        }

        if (merged.IsDirectory)
        {
            return BuildRow(merged, FolderRowState.MetadataMatch);
        }

        bool metadataMatch = AllThreeMetadataMatches(merged, options.MetadataTimestampTolerance);

        return BuildRow(merged, metadataMatch ? FolderRowState.MetadataMatch : FolderRowState.MetadataDifferent);
    }

    private IReadOnlyList<FolderComparisonRow> ApplyContentHashing(
        IReadOnlyList<FolderComparisonRow> rows,
        FolderComparisonRequest request,
        List<Diagnostic> diagnostics,
        IProgress<FolderComparisonProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (request.Options.Mode != FolderComparisonMode.ContentHash)
        {
            return rows;
        }

        FolderContentHashSessionCache cache = new();
        List<FolderComparisonRow> hashedRows = [];
        int hashedCount = 0;

        foreach (FolderComparisonRow row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool eligible = !row.IsDirectory
                && row.State is not FolderRowState.Error and not FolderRowState.SkippedReparsePoint
                && (request.IsThreeWay ? row.HasTripledFiles : row.HasPairedFiles);

            if (!eligible)
            {
                hashedRows.Add(row);
                continue;
            }

            progress?.Report(new FolderComparisonProgress(
                hashedCount,
                row.RelativePath,
                FolderComparisonPhase.ContentHash));

            if (request.IsThreeWay)
            {
                if (!TryGetHash(row.BaseFullPath!, row.BaseSizeBytes!.Value, row.BaseLastWriteTimeUtc!.Value, cache, diagnostics, cancellationToken, out string? baseHash)
                    || !TryGetHash(row.LeftFullPath!, row.LeftSizeBytes!.Value, row.LeftLastWriteTimeUtc!.Value, cache, diagnostics, cancellationToken, out string? leftHash)
                    || !TryGetHash(row.RightFullPath!, row.RightSizeBytes!.Value, row.RightLastWriteTimeUtc!.Value, cache, diagnostics, cancellationToken, out string? rightHash))
                {
                    hashedRows.Add(WithState(row, FolderRowState.Error));
                    continue;
                }

                FolderRowState state = baseHash == leftHash && baseHash == rightHash
                    ? FolderRowState.ContentMatch
                    : FolderRowState.ContentDifferent;
                hashedRows.Add(WithState(row, state));
            }
            else
            {
                if (!TryGetHash(row.LeftFullPath!, row.LeftSizeBytes!.Value, row.LeftLastWriteTimeUtc!.Value, cache, diagnostics, cancellationToken, out string? leftHash)
                    || !TryGetHash(row.RightFullPath!, row.RightSizeBytes!.Value, row.RightLastWriteTimeUtc!.Value, cache, diagnostics, cancellationToken, out string? rightHash))
                {
                    hashedRows.Add(WithState(row, FolderRowState.Error));
                    continue;
                }

                FolderRowState state = leftHash == rightHash
                    ? FolderRowState.ContentMatch
                    : FolderRowState.ContentDifferent;
                hashedRows.Add(WithState(row, state));
            }

            hashedCount++;
        }

        return hashedRows;
    }

    private bool TryGetHash(
        string filePath,
        long lengthBytes,
        DateTime lastWriteTimeUtc,
        FolderContentHashSessionCache cache,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken,
        out string? sha256Hex)
    {
        if (cache.TryGet(filePath, lengthBytes, lastWriteTimeUtc, out string cached))
        {
            sha256Hex = cached;
            return true;
        }

        if (!_hashAccess.TryComputeSha256(filePath, cancellationToken, out string computed, out Diagnostic? error))
        {
            sha256Hex = null;
            diagnostics.Add(error!);
            return false;
        }

        cache.Store(filePath, lengthBytes, lastWriteTimeUtc, computed);
        sha256Hex = computed;
        return true;
    }

    private static bool MetadataMatches(
        long? leftSize,
        long? rightSize,
        DateTime? leftTime,
        DateTime? rightTime,
        TimeSpan tolerance)
    {
        if (leftSize != rightSize)
        {
            return false;
        }

        return MetadataTimestampsMatch(leftTime, rightTime, tolerance);
    }

    private static bool AllThreeMetadataMatches(MergedPath merged, TimeSpan tolerance)
    {
        if (merged.BaseSizeBytes != merged.LeftSizeBytes || merged.BaseSizeBytes != merged.RightSizeBytes)
        {
            return false;
        }

        DateTime?[] times =
        [
            merged.BaseLastWriteTimeUtc,
            merged.LeftLastWriteTimeUtc,
            merged.RightLastWriteTimeUtc,
        ];
        DateTime[] present = times.Where(time => time is not null).Select(time => time!.Value).ToArray();
        if (present.Length < 2)
        {
            return true;
        }

        if (tolerance <= TimeSpan.Zero)
        {
            return present.All(time => time == present[0]);
        }

        TimeSpan spread = present.Max() - present.Min();
        return spread <= tolerance;
    }

    private static bool MetadataTimestampsMatch(DateTime? left, DateTime? right, TimeSpan tolerance)
    {
        if (left is null || right is null)
        {
            return left == right;
        }

        if (left == right)
        {
            return true;
        }

        if (tolerance <= TimeSpan.Zero)
        {
            return false;
        }

        TimeSpan delta = left.Value >= right.Value ? left.Value - right.Value : right.Value - left.Value;
        return delta <= tolerance;
    }

    private static FolderComparisonRow WithState(FolderComparisonRow row, FolderRowState state)
        => new(
            row.RelativePath,
            state,
            row.LeftFullPath,
            row.RightFullPath,
            row.LeftSizeBytes,
            row.RightSizeBytes,
            row.LeftLastWriteTimeUtc,
            row.RightLastWriteTimeUtc,
            row.IsDirectory,
            row.RowDiagnostics,
            row.BaseFullPath,
            row.BaseSizeBytes,
            row.BaseLastWriteTimeUtc);

    private static FolderComparisonRow BuildRow(MergedPath merged, FolderRowState state)
        => new(
            merged.DisplayRelativePath,
            state,
            merged.LeftFullPath,
            merged.RightFullPath,
            merged.LeftSizeBytes,
            merged.RightSizeBytes,
            merged.LeftLastWriteTimeUtc,
            merged.RightLastWriteTimeUtc,
            merged.IsDirectory,
            merged.RowDiagnostics,
            merged.BaseFullPath,
            merged.BaseSizeBytes,
            merged.BaseLastWriteTimeUtc);

    private static int CompareRows(FolderComparisonRow left, FolderComparisonRow right)
    {
        int pathCompare = StringComparer.OrdinalIgnoreCase.Compare(left.RelativePath, right.RelativePath);
        if (pathCompare != 0)
        {
            return pathCompare;
        }

        pathCompare = StringComparer.Ordinal.Compare(left.RelativePath, right.RelativePath);
        if (pathCompare != 0)
        {
            return pathCompare;
        }

        return left.State.CompareTo(right.State);
    }

    private sealed class PathAccumulator
    {
        private readonly Dictionary<string, SideEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

        public int EntriesScanned { get; private set; }

        public void AddFile(
            string relativePath,
            ComparisonSide side,
            string fullPath,
            long lengthBytes,
            DateTime lastWriteTimeUtc)
        {
            SideEntry entry = GetOrCreate(relativePath, isDirectory: false);
            entry.ApplyDisplayPath(relativePath, side);
            entry.ApplySide(side, fullPath, lengthBytes, lastWriteTimeUtc);
            EntriesScanned++;
        }

        public void AddEmptyDirectory(string relativePath, ComparisonSide side, string fullPath)
        {
            SideEntry entry = GetOrCreate(relativePath, isDirectory: true);
            entry.ApplyDisplayPath(relativePath, side);
            entry.ApplySide(side, fullPath, null, null);
            EntriesScanned++;
        }

        public void AddReparsePoint(string relativePath, ComparisonSide side, string fullPath)
        {
            SideEntry entry = GetOrCreate(relativePath, isDirectory: true);
            entry.ApplyDisplayPath(relativePath, side);
            entry.StateOverride = FolderRowState.SkippedReparsePoint;
            entry.ApplySide(side, fullPath, null, null);
            EntriesScanned++;
        }

        public void AddError(string relativePath, ComparisonSide side, string fullPath, Diagnostic diagnostic)
        {
            SideEntry entry = GetOrCreate(relativePath, isDirectory: false);
            entry.ApplyDisplayPath(relativePath, side);
            entry.HasErrors = true;
            entry.RowDiagnostics.Add(diagnostic.WithSide(side));
            entry.ApplySide(side, fullPath, null, null);
            EntriesScanned++;
        }

        public IEnumerable<MergedPath> GetMergedPaths()
            => _entries.Values.Select(entry => entry.ToMergedPath());

        private SideEntry GetOrCreate(string relativePath, bool isDirectory)
        {
            if (!_entries.TryGetValue(relativePath, out SideEntry? entry))
            {
                entry = new SideEntry { IsDirectory = isDirectory, DisplayRelativePath = relativePath };
                _entries[relativePath] = entry;
            }
            else if (isDirectory)
            {
                entry.IsDirectory = true;
            }

            return entry;
        }

        private sealed class SideEntry
        {
            public string DisplayRelativePath { get; set; } = string.Empty;

            public string? BaseFullPath { get; set; }

            public string? LeftFullPath { get; set; }

            public string? RightFullPath { get; set; }

            public long? BaseSizeBytes { get; set; }

            public long? LeftSizeBytes { get; set; }

            public long? RightSizeBytes { get; set; }

            public DateTime? BaseLastWriteTimeUtc { get; set; }

            public DateTime? LeftLastWriteTimeUtc { get; set; }

            public DateTime? RightLastWriteTimeUtc { get; set; }

            public bool IsDirectory { get; set; }

            public bool HasErrors { get; set; }

            public FolderRowState? StateOverride { get; set; }

            public List<Diagnostic> RowDiagnostics { get; } = [];

            public void ApplyDisplayPath(string relativePath, ComparisonSide side)
            {
                if (side == ComparisonSide.Left || string.IsNullOrEmpty(DisplayRelativePath))
                {
                    DisplayRelativePath = relativePath;
                }
            }

            public void ApplySide(
                ComparisonSide side,
                string fullPath,
                long? lengthBytes,
                DateTime? lastWriteTimeUtc)
            {
                switch (side)
                {
                    case ComparisonSide.Base:
                        BaseFullPath = fullPath;
                        BaseSizeBytes = lengthBytes;
                        BaseLastWriteTimeUtc = lastWriteTimeUtc;
                        break;
                    case ComparisonSide.Left:
                        LeftFullPath = fullPath;
                        LeftSizeBytes = lengthBytes;
                        LeftLastWriteTimeUtc = lastWriteTimeUtc;
                        break;
                    case ComparisonSide.Right:
                        RightFullPath = fullPath;
                        RightSizeBytes = lengthBytes;
                        RightLastWriteTimeUtc = lastWriteTimeUtc;
                        break;
                }
            }

            public MergedPath ToMergedPath()
                => new(
                    DisplayRelativePath,
                    BaseFullPath,
                    LeftFullPath,
                    RightFullPath,
                    BaseSizeBytes,
                    LeftSizeBytes,
                    RightSizeBytes,
                    BaseLastWriteTimeUtc,
                    LeftLastWriteTimeUtc,
                    RightLastWriteTimeUtc,
                    IsDirectory,
                    HasErrors,
                    StateOverride,
                    RowDiagnostics);
        }
    }

    private sealed record MergedPath(
        string DisplayRelativePath,
        string? BaseFullPath,
        string? LeftFullPath,
        string? RightFullPath,
        long? BaseSizeBytes,
        long? LeftSizeBytes,
        long? RightSizeBytes,
        DateTime? BaseLastWriteTimeUtc,
        DateTime? LeftLastWriteTimeUtc,
        DateTime? RightLastWriteTimeUtc,
        bool IsDirectory,
        bool HasErrors,
        FolderRowState? StateOverride,
        IReadOnlyList<Diagnostic> RowDiagnostics);
}

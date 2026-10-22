using MergeTool.Text.Diff;

namespace MergeTool.Application.Sessions;

/// <summary>
/// Derived view over a comparison result: maps source lines to blocks and supplies safe
/// scroll/navigation anchors for both sides. It never mutates documents or diff blocks; the
/// UI renders overlays from block ranges and must not rescan editor text for differences.
/// All anchors returned are valid display line indices (clamped; 0 for an empty document).
/// </summary>
public sealed class ComparisonPresentationMap
{
    private readonly TwoWayTextComparisonResult _result;
    private readonly int[] _blockOrdinalByLeftLine;
    private readonly int[] _blockOrdinalByRightLine;
    private readonly int _leftLineCount;
    private readonly int _rightLineCount;

    private ComparisonPresentationMap(TwoWayTextComparisonResult result, int leftLineCount, int rightLineCount)
    {
        _result = result;
        _leftLineCount = leftLineCount;
        _rightLineCount = rightLineCount;
        _blockOrdinalByLeftLine = new int[leftLineCount];
        _blockOrdinalByRightLine = new int[rightLineCount];

        List<int> differenceOrdinals = [];
        foreach (DiffBlock block in result.Blocks)
        {
            for (int i = block.LeftRange.StartIndex; i < block.LeftRange.EndExclusive; i++)
            {
                _blockOrdinalByLeftLine[i] = block.Ordinal;
            }

            for (int i = block.RightRange.StartIndex; i < block.RightRange.EndExclusive; i++)
            {
                _blockOrdinalByRightLine[i] = block.Ordinal;
            }

            if (block.IsDifference)
            {
                differenceOrdinals.Add(block.Ordinal);
            }
        }

        DifferenceBlockOrdinals = differenceOrdinals;
    }

    public static ComparisonPresentationMap Create(TwoWayTextComparisonResult result, int leftLineCount, int rightLineCount)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfNegative(leftLineCount);
        ArgumentOutOfRangeException.ThrowIfNegative(rightLineCount);
        return new ComparisonPresentationMap(result, leftLineCount, rightLineCount);
    }

    public IReadOnlyList<DiffBlock> Blocks => _result.Blocks;

    /// <summary>Ordinals of all non-equal blocks, in document order (navigation sequence).</summary>
    public IReadOnlyList<int> DifferenceBlockOrdinals { get; }

    public bool TryGetBlock(int blockOrdinal, out DiffBlock? block)
    {
        foreach (DiffBlock candidate in _result.Blocks)
        {
            if (candidate.Ordinal == blockOrdinal)
            {
                block = candidate;
                return true;
            }
        }

        block = null;
        return false;
    }

    /// <summary>
    /// The block containing (or, for out-of-range input, clamped to) this left line.
    /// Null only when the comparison has no blocks at all (two empty documents).
    /// </summary>
    public DiffBlock? GetBlockForLeftLine(int leftLine)
    {
        if (_result.Blocks.Count == 0)
        {
            return null;
        }

        if (_leftLineCount == 0)
        {
            return _result.Blocks[0];
        }

        return _result.Blocks[_blockOrdinalByLeftLine[Math.Clamp(leftLine, 0, _leftLineCount - 1)]];
    }

    /// <summary>
    /// The block containing (or, for out-of-range input, clamped to) this right line.
    /// Null only when the comparison has no blocks at all (two empty documents).
    /// </summary>
    public DiffBlock? GetBlockForRightLine(int rightLine)
    {
        if (_result.Blocks.Count == 0)
        {
            return null;
        }

        if (_rightLineCount == 0)
        {
            return _result.Blocks[0];
        }

        return _result.Blocks[_blockOrdinalByRightLine[Math.Clamp(rightLine, 0, _rightLineCount - 1)]];
    }

    /// <summary>
    /// Safe left-side display anchor for a block: the block's left start, clamped to a valid
    /// line. For a block with no left lines (insertion) this is the left boundary line.
    /// </summary>
    public int GetLeftAnchorForBlock(int blockOrdinal)
        => ClampLine(_result.Blocks[blockOrdinal].LeftRange.StartIndex, _leftLineCount);

    /// <summary>
    /// Safe right-side display anchor for a block: the block's right start, clamped to a
    /// valid line. For a block with no right lines (deletion) this is the right boundary line.
    /// </summary>
    public int GetRightAnchorForBlock(int blockOrdinal)
        => ClampLine(_result.Blocks[blockOrdinal].RightRange.StartIndex, _rightLineCount);

    /// <summary>
    /// Maps a visible left line to the corresponding right scroll anchor: same relative
    /// offset within the block where possible, otherwise the block boundary in the right
    /// document (synchronized scrolling contract).
    /// </summary>
    public int MapLeftLineToRightAnchor(int leftLine)
    {
        DiffBlock? block = GetBlockForLeftLine(leftLine);
        if (block is null)
        {
            return 0;
        }

        if (block.RightRange.IsEmpty)
        {
            return ClampLine(block.RightRange.StartIndex, _rightLineCount);
        }

        int offset = Math.Clamp(leftLine - block.LeftRange.StartIndex, 0, block.RightRange.Length - 1);
        return ClampLine(block.RightRange.StartIndex + offset, _rightLineCount);
    }

    /// <summary>Maps a visible right line to the corresponding left scroll anchor.</summary>
    public int MapRightLineToLeftAnchor(int rightLine)
    {
        DiffBlock? block = GetBlockForRightLine(rightLine);
        if (block is null)
        {
            return 0;
        }

        if (block.LeftRange.IsEmpty)
        {
            return ClampLine(block.LeftRange.StartIndex, _leftLineCount);
        }

        int offset = Math.Clamp(rightLine - block.RightRange.StartIndex, 0, block.LeftRange.Length - 1);
        return ClampLine(block.LeftRange.StartIndex + offset, _leftLineCount);
    }

    private static int ClampLine(int line, int lineCount)
        => lineCount == 0 ? 0 : Math.Clamp(line, 0, lineCount - 1);
}

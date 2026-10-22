namespace MergeTool.Text.Diff;

/// <summary>
/// Deterministic, iterative line diff (patience style):
/// 1. Lines become integer keys (exact ordinal content; a missing final terminator makes the
///    last line distinct from a terminated line with the same content; terminator style is
///    not a difference because it is preserved on save).
/// 2. Each window trims common prefix/suffix, then anchors on lines that occur exactly once
///    on both sides (longest increasing subsequence of those pairs) and splits around them
///    using an explicit stack — no recursion, no overflow on large inputs.
/// 3. A window without unique anchors is emitted whole: both sides non-empty → one
///    <see cref="DiffBlockKind.Modified"/> replacement block; one side empty →
///    Inserted/Deleted. This is the documented "ambiguous pairing" fallback.
/// </summary>
public sealed class PatienceLineDiffEngine : ITextDiffEngine
{
    public IReadOnlyList<DiffBlock> Compare(
        IReadOnlyList<TextLine> leftLines,
        IReadOnlyList<TextLine> rightLines,
        TextComparisonOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(leftLines);
        ArgumentNullException.ThrowIfNull(rightLines);
        ArgumentNullException.ThrowIfNull(options);

        int[] leftKeys = BuildKeys(leftLines, rightLines, out int[] rightKeys);
        List<(int LeftIndex, int RightIndex)> matches = ComputeMatches(leftKeys, rightKeys, cancellationToken);
        return BuildBlocks(matches, leftKeys.Length, rightKeys.Length);
    }

    private static int[] BuildKeys(IReadOnlyList<TextLine> leftLines, IReadOnlyList<TextLine> rightLines, out int[] rightKeys)
    {
        Dictionary<string, int> keyByContent = new(StringComparer.Ordinal);
        int[] leftKeys = new int[leftLines.Count];
        rightKeys = new int[rightLines.Count];

        for (int i = 0; i < leftLines.Count; i++)
        {
            leftKeys[i] = GetKey(keyByContent, leftLines[i]);
        }

        for (int i = 0; i < rightLines.Count; i++)
        {
            rightKeys[i] = GetKey(keyByContent, rightLines[i]);
        }

        return leftKeys;
    }

    private static int GetKey(Dictionary<string, int> keyByContent, TextLine line)
    {
        // '\0' cannot occur in loaded text (NUL-heavy data is rejected), so this suffix
        // cannot collide with real content.
        string comparisonText = line.Terminator == LineTerminator.None
            ? line.Content + "\0<no-eol>"
            : line.Content;

        if (!keyByContent.TryGetValue(comparisonText, out int key))
        {
            key = keyByContent.Count;
            keyByContent.Add(comparisonText, key);
        }

        return key;
    }

    private static List<(int LeftIndex, int RightIndex)> ComputeMatches(int[] leftKeys, int[] rightKeys, CancellationToken cancellationToken)
    {
        List<(int, int)> matches = [];
        Stack<(int LeftStart, int LeftEnd, int RightStart, int RightEnd)> windows = new();
        windows.Push((0, leftKeys.Length, 0, rightKeys.Length));

        while (windows.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (int leftStart, int leftEnd, int rightStart, int rightEnd) = windows.Pop();

            // Trim common prefix.
            while (leftStart < leftEnd && rightStart < rightEnd && leftKeys[leftStart] == rightKeys[rightStart])
            {
                matches.Add((leftStart, rightStart));
                leftStart++;
                rightStart++;
            }

            // Trim common suffix.
            while (leftEnd > leftStart && rightEnd > rightStart && leftKeys[leftEnd - 1] == rightKeys[rightEnd - 1])
            {
                leftEnd--;
                rightEnd--;
                matches.Add((leftEnd, rightEnd));
            }

            if (leftStart >= leftEnd || rightStart >= rightEnd)
            {
                continue; // One side exhausted: pure insertion/deletion window.
            }

            List<(int LeftIndex, int RightIndex)> anchors = FindUniqueAnchors(leftKeys, rightKeys, leftStart, leftEnd, rightStart, rightEnd);
            if (anchors.Count == 0)
            {
                continue; // Ambiguous pairing: the whole window becomes one replacement block.
            }

            // Split around anchors; push in reverse so processing order stays deterministic.
            int previousLeft = leftStart;
            int previousRight = rightStart;
            List<(int LeftStart, int LeftEnd, int RightStart, int RightEnd)> subWindows = [];
            foreach ((int anchorLeft, int anchorRight) in anchors)
            {
                subWindows.Add((previousLeft, anchorLeft, previousRight, anchorRight));
                matches.Add((anchorLeft, anchorRight));
                previousLeft = anchorLeft + 1;
                previousRight = anchorRight + 1;
            }

            subWindows.Add((previousLeft, leftEnd, previousRight, rightEnd));

            for (int i = subWindows.Count - 1; i >= 0; i--)
            {
                (int ls, int le, int rs, int re) = subWindows[i];
                if (ls < le || rs < re)
                {
                    windows.Push((ls, le, rs, re));
                }
            }
        }

        matches.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        return matches;
    }

    /// <summary>
    /// Pairs of positions whose key occurs exactly once on each side of the window, reduced
    /// to the longest increasing subsequence by right index (classic patience sorting).
    /// </summary>
    private static List<(int LeftIndex, int RightIndex)> FindUniqueAnchors(
        int[] leftKeys,
        int[] rightKeys,
        int leftStart,
        int leftEnd,
        int rightStart,
        int rightEnd)
    {
        Dictionary<int, (int Count, int Index)> leftOccurrences = [];
        for (int i = leftStart; i < leftEnd; i++)
        {
            leftOccurrences[leftKeys[i]] = leftOccurrences.TryGetValue(leftKeys[i], out (int Count, int Index) seen)
                ? (seen.Count + 1, seen.Index)
                : (1, i);
        }

        Dictionary<int, (int Count, int Index)> rightOccurrences = [];
        for (int i = rightStart; i < rightEnd; i++)
        {
            rightOccurrences[rightKeys[i]] = rightOccurrences.TryGetValue(rightKeys[i], out (int Count, int Index) seen)
                ? (seen.Count + 1, seen.Index)
                : (1, i);
        }

        List<(int LeftIndex, int RightIndex)> candidates = [];
        for (int i = leftStart; i < leftEnd; i++)
        {
            if (leftOccurrences[leftKeys[i]].Count == 1
                && rightOccurrences.TryGetValue(leftKeys[i], out (int Count, int Index) right)
                && right.Count == 1)
            {
                candidates.Add((i, right.Index));
            }
        }

        return LongestIncreasingByRight(candidates);
    }

    private static List<(int LeftIndex, int RightIndex)> LongestIncreasingByRight(List<(int LeftIndex, int RightIndex)> candidates)
    {
        if (candidates.Count == 0)
        {
            return candidates;
        }

        // candidates are ordered by LeftIndex; find the LIS over RightIndex.
        int[] tailIndices = new int[candidates.Count];
        int[] previousIndex = new int[candidates.Count];
        int length = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            int low = 0, high = length;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (candidates[tailIndices[mid]].RightIndex < candidates[i].RightIndex)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            previousIndex[i] = low > 0 ? tailIndices[low - 1] : -1;
            tailIndices[low] = i;
            if (low == length)
            {
                length++;
            }
        }

        List<(int LeftIndex, int RightIndex)> result = new(length);
        for (int i = tailIndices[length - 1]; i >= 0; i = previousIndex[i])
        {
            result.Add(candidates[i]);
        }

        result.Reverse();
        return result;
    }

    private static List<DiffBlock> BuildBlocks(List<(int LeftIndex, int RightIndex)> matches, int leftCount, int rightCount)
    {
        List<DiffBlock> blocks = [];
        int leftCursor = 0;
        int rightCursor = 0;
        int matchCursor = 0;

        while (leftCursor < leftCount || rightCursor < rightCount)
        {
            if (matchCursor < matches.Count
                && matches[matchCursor].LeftIndex == leftCursor
                && matches[matchCursor].RightIndex == rightCursor)
            {
                // Consume a run of consecutive matches as one Equal block.
                int runStartLeft = leftCursor;
                int runStartRight = rightCursor;
                while (matchCursor < matches.Count
                    && matches[matchCursor].LeftIndex == leftCursor
                    && matches[matchCursor].RightIndex == rightCursor)
                {
                    leftCursor++;
                    rightCursor++;
                    matchCursor++;
                }

                blocks.Add(new DiffBlock(
                    blocks.Count,
                    DiffBlockKind.Equal,
                    new LineRange(runStartLeft, leftCursor - runStartLeft),
                    new LineRange(runStartRight, rightCursor - runStartRight)));
                continue;
            }

            // Difference region: everything up to the next match (or the end).
            int nextMatchLeft = matchCursor < matches.Count ? matches[matchCursor].LeftIndex : leftCount;
            int nextMatchRight = matchCursor < matches.Count ? matches[matchCursor].RightIndex : rightCount;

            int leftLength = nextMatchLeft - leftCursor;
            int rightLength = nextMatchRight - rightCursor;

            DiffBlockKind kind = (leftLength > 0, rightLength > 0) switch
            {
                (true, true) => DiffBlockKind.Modified,
                (true, false) => DiffBlockKind.Deleted,
                (false, true) => DiffBlockKind.Inserted,
                _ => throw new InvalidOperationException("A difference region must have lines on at least one side."),
            };

            blocks.Add(new DiffBlock(
                blocks.Count,
                kind,
                new LineRange(leftCursor, leftLength),
                new LineRange(rightCursor, rightLength)));

            leftCursor = nextMatchLeft;
            rightCursor = nextMatchRight;
        }

        return blocks;
    }
}

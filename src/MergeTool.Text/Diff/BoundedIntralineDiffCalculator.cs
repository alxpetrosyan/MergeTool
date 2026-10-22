namespace MergeTool.Text.Diff;

/// <summary>
/// Computes bounded character-level diffs for paired modified lines using LCS over
/// Unicode scalar values (surrogate pairs are never split).
/// </summary>
public sealed class BoundedIntralineDiffCalculator
{
    public InlineDiffLinePairResult Compute(
        string leftLine,
        string rightLine,
        int leftLineIndex,
        int rightLineIndex,
        IntralineDiffBounds bounds)
    {
        ArgumentNullException.ThrowIfNull(leftLine);
        ArgumentNullException.ThrowIfNull(rightLine);
        ArgumentNullException.ThrowIfNull(bounds);

        if (leftLine.Length > bounds.MaxLineLength || rightLine.Length > bounds.MaxLineLength)
        {
            return InlineDiffLinePairResult.WholeLineFallback(leftLineIndex, rightLineIndex);
        }

        IReadOnlyList<TextElement> leftElements = TextElementTokenizer.Tokenize(leftLine);
        IReadOnlyList<TextElement> rightElements = TextElementTokenizer.Tokenize(rightLine);

        long workUnits = (long)leftElements.Count * rightElements.Count;
        if (workUnits > bounds.MaxWorkBudget)
        {
            return InlineDiffLinePairResult.WholeLineFallback(leftLineIndex, rightLineIndex);
        }

        if (leftElements.Count == 0 && rightElements.Count == 0)
        {
            return new InlineDiffLinePairResult(leftLineIndex, rightLineIndex, [], [], IsWholeLineFallback: false);
        }

        int[,] lcs = BuildLcsTable(leftElements, rightElements);
        IReadOnlyList<AtomicOperation> operations = BacktrackOperations(leftElements, rightElements, lcs);

        IReadOnlyList<InlineDiffSegment> leftSegments = BuildSideSegments(operations, isLeft: true);
        IReadOnlyList<InlineDiffSegment> rightSegments = BuildSideSegments(operations, isLeft: false);

        return new InlineDiffLinePairResult(
            leftLineIndex,
            rightLineIndex,
            leftSegments,
            rightSegments,
            IsWholeLineFallback: false);
    }

    private static int[,] BuildLcsTable(IReadOnlyList<TextElement> left, IReadOnlyList<TextElement> right)
    {
        int rows = left.Count + 1;
        int cols = right.Count + 1;
        int[,] dp = new int[rows, cols];

        for (int i = 1; i < rows; i++)
        {
            for (int j = 1; j < cols; j++)
            {
                if (left[i - 1].CodePoint == right[j - 1].CodePoint)
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }

        return dp;
    }

    private static IReadOnlyList<AtomicOperation> BacktrackOperations(
        IReadOnlyList<TextElement> left,
        IReadOnlyList<TextElement> right,
        int[,] dp)
    {
        var reversed = new List<AtomicOperation>();
        int i = left.Count;
        int j = right.Count;

        while (i > 0 || j > 0)
        {
            if (i > 0 && j > 0 && left[i - 1].CodePoint == right[j - 1].CodePoint)
            {
                reversed.Add(AtomicOperation.Equal(left[i - 1], right[j - 1]));
                i--;
                j--;
            }
            else if (j > 0 && (i == 0 || dp[i, j - 1] >= dp[i - 1, j]))
            {
                reversed.Add(AtomicOperation.RightOnly(right[j - 1]));
                j--;
            }
            else
            {
                reversed.Add(AtomicOperation.LeftOnly(left[i - 1]));
                i--;
            }
        }

        reversed.Reverse();
        return reversed;
    }

    private static IReadOnlyList<InlineDiffSegment> BuildSideSegments(
        IReadOnlyList<AtomicOperation> operations,
        bool isLeft)
    {
        var segments = new List<InlineDiffSegment>();
        int index = 0;

        while (index < operations.Count)
        {
            AtomicOperation current = operations[index];
            if (current.Kind == AtomicOperationKind.Equal)
            {
                TextElement element = isLeft ? current.Left!.Value : current.Right!.Value;
                int start = element.StartOffset;
                int end = element.EndOffset;
                index++;

                while (index < operations.Count && operations[index].Kind == AtomicOperationKind.Equal)
                {
                    element = isLeft ? operations[index].Left!.Value : operations[index].Right!.Value;
                    end = element.EndOffset;
                    index++;
                }

                segments.Add(new InlineDiffSegment(start, end - start, InlineDiffSegmentKind.Unchanged));
                continue;
            }

            var leftRun = new List<TextElement>();
            var rightRun = new List<TextElement>();
            while (index < operations.Count && operations[index].Kind != AtomicOperationKind.Equal)
            {
                AtomicOperation op = operations[index];
                if (op.Kind == AtomicOperationKind.LeftOnly)
                {
                    leftRun.Add(op.Left!.Value);
                }
                else if (op.Kind == AtomicOperationKind.RightOnly)
                {
                    rightRun.Add(op.Right!.Value);
                }

                index++;
            }

            if (isLeft)
            {
                if (leftRun.Count > 0 && rightRun.Count > 0)
                {
                    segments.Add(CreateSpan(leftRun, InlineDiffSegmentKind.Replacement));
                }
                else if (leftRun.Count > 0)
                {
                    segments.Add(CreateSpan(leftRun, InlineDiffSegmentKind.Deletion));
                }
            }
            else if (rightRun.Count > 0 && leftRun.Count > 0)
            {
                segments.Add(CreateSpan(rightRun, InlineDiffSegmentKind.Replacement));
            }
            else if (rightRun.Count > 0)
            {
                segments.Add(CreateSpan(rightRun, InlineDiffSegmentKind.Insertion));
            }
        }

        return segments;
    }

    private static InlineDiffSegment CreateSpan(IReadOnlyList<TextElement> run, InlineDiffSegmentKind kind)
    {
        int start = run[0].StartOffset;
        int end = run[^1].EndOffset;
        return new InlineDiffSegment(start, end - start, kind);
    }

    private enum AtomicOperationKind
    {
        Equal,
        LeftOnly,
        RightOnly,
    }

    private sealed class AtomicOperation
    {
        private AtomicOperation(AtomicOperationKind kind, TextElement? left, TextElement? right)
        {
            Kind = kind;
            Left = left;
            Right = right;
        }

        public AtomicOperationKind Kind { get; }

        public TextElement? Left { get; }

        public TextElement? Right { get; }

        public static AtomicOperation Equal(TextElement left, TextElement right) => new(AtomicOperationKind.Equal, left, right);

        public static AtomicOperation LeftOnly(TextElement left) => new(AtomicOperationKind.LeftOnly, left, null);

        public static AtomicOperation RightOnly(TextElement right) => new(AtomicOperationKind.RightOnly, null, right);
    }

    private readonly record struct TextElement(int StartOffset, int EndOffset, int CodePoint);

    private static class TextElementTokenizer
    {
        public static IReadOnlyList<TextElement> Tokenize(string text)
        {
            var elements = new List<TextElement>();
            for (int i = 0; i < text.Length;)
            {
                int start = i;
                int codePoint = char.ConvertToUtf32(text, i);
                i += char.IsSurrogatePair(text, i) ? 2 : 1;
                elements.Add(new TextElement(start, i, codePoint));
            }

            return elements;
        }
    }
}

namespace MergeTool.Application.Requests;

/// <summary>Request to open a paired file comparison for two existing file paths.</summary>
public sealed record OpenPairedFileComparisonRequest(string LeftPath, string RightPath);

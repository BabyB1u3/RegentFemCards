namespace PortraitModGenerator.Core.Abstractions;

public sealed class MappingAnalysisResult
{
    public required string ScanResultPath { get; init; }

    public required string OfficialCardIndexPath { get; init; }

    public required string OutputJsonPath { get; init; }

    public required int TotalAssets { get; init; }

    public required int MatchedAssets { get; init; }

    public required int IgnoredAssets { get; init; }

    public required IReadOnlyList<MappingCandidate> Candidates { get; init; }
}

namespace PortraitModGenerator.Core.Abstractions;

public sealed class MappingAnalysisRequest
{
    public required string ScanResultPath { get; init; }

    public required string OfficialCardIndexPath { get; init; }

    public required string OutputJsonPath { get; init; }
}

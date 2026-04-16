namespace PortraitModGenerator.Core.Abstractions;

public sealed class PckImportRequest
{
    public required string SourcePckPath { get; init; }

    public required string OutputDirectory { get; init; }

    public required string GdreToolsPath { get; init; }

    public string? LogFilePath { get; init; }

    public bool OverwriteOutput { get; init; }
}

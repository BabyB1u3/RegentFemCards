namespace PortraitModGenerator.Core.Abstractions;

public sealed class PckImportResult
{
    public required string SourcePckPath { get; init; }

    public required string ExtractRoot { get; init; }

    public required string GdreToolsPath { get; init; }

    public required string LogFilePath { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public required DateTimeOffset EndedAt { get; init; }

    public required int ExitCode { get; init; }

    public required bool Success { get; init; }

    public required string CommandLine { get; init; }
}

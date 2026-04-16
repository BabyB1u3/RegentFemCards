namespace PortraitModGenerator.Core.Abstractions;

public sealed class ModBuildResult
{
    public required string ProjectFilePath { get; init; }

    public required string ArtifactOutputDirectory { get; init; }

    public required string LogFilePath { get; init; }

    public required string CommandLine { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public required DateTimeOffset EndedAt { get; init; }

    public required int ExitCode { get; init; }

    public required bool Success { get; init; }
}

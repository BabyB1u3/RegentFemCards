namespace PortraitModGenerator.Core.Abstractions;

public sealed class ModBuildRequest
{
    public required string ProjectFilePath { get; init; }

    public required string ArtifactOutputDirectory { get; init; }

    public required string LogFilePath { get; init; }

    public required string DotnetCliHome { get; init; }

    public string Configuration { get; init; } = "Release";
}

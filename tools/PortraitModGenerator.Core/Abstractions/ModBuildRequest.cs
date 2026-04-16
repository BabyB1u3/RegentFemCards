namespace PortraitModGenerator.Core.Abstractions;

public sealed class ModBuildRequest
{
    public required string ProjectFilePath { get; init; }

    public required string ArtifactOutputDirectory { get; init; }

    public required string LogFilePath { get; init; }

    public required string DotnetCliHome { get; init; }

    public required string DotnetExecutablePath { get; init; }

    public required string RestoreConfigFilePath { get; init; }

    public required string GodotExecutablePath { get; init; }

    public string Configuration { get; init; } = "Release";
}

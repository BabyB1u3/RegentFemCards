namespace PortraitModGenerator.Core.Abstractions;

public sealed class TemplateGenerationResult
{
    public required string TemplateId { get; init; }

    public required string TemplateVersion { get; init; }

    public required string OutputDirectory { get; init; }

    public required string EntryProjectPath { get; init; }

    public required string ManifestPath { get; init; }
}

namespace PortraitModGenerator.Core.Abstractions;

public sealed class TemplateGenerationRequest
{
    public required string TemplateDirectory { get; init; }

    public required string OutputDirectory { get; init; }

    public required IReadOnlyDictionary<string, string> TokenValues { get; init; }

    public bool OverwriteExistingOutput { get; init; }
}

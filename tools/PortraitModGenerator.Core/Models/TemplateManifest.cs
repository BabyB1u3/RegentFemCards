using System.Text.Json.Serialization;

namespace PortraitModGenerator.Core.Models;

public sealed class TemplateManifest
{
    [JsonPropertyName("templateId")]
    public string TemplateId { get; init; } = string.Empty;

    [JsonPropertyName("templateName")]
    public string TemplateName { get; init; } = string.Empty;

    [JsonPropertyName("templateVersion")]
    public string TemplateVersion { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("entryProject")]
    public string EntryProject { get; init; } = string.Empty;

    [JsonPropertyName("manifestFile")]
    public string ManifestFile { get; init; } = string.Empty;

    [JsonPropertyName("sourceRoot")]
    public string SourceRoot { get; init; } = "src";

    [JsonPropertyName("tokens")]
    public List<string> Tokens { get; init; } = [];

    [JsonPropertyName("renameTokens")]
    public List<string> RenameTokens { get; init; } = [];

    [JsonPropertyName("defaultValues")]
    public Dictionary<string, string> DefaultValues { get; init; } = new(StringComparer.Ordinal);
}

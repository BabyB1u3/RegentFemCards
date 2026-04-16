using System.Text.Json;
using PortraitModGenerator.Core.Models;

namespace PortraitModGenerator.Core.Services;

public sealed class TemplateManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public TemplateManifest Load(string templateDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateDirectory);

        string manifestPath = Path.Combine(templateDirectory, "template.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Template manifest not found.", manifestPath);
        }

        string json = File.ReadAllText(manifestPath);
        TemplateManifest? manifest = JsonSerializer.Deserialize<TemplateManifest>(json, JsonOptions);
        if (manifest is null)
        {
            throw new InvalidOperationException($"Failed to deserialize template manifest: {manifestPath}");
        }

        if (string.IsNullOrWhiteSpace(manifest.TemplateId))
        {
            throw new InvalidOperationException("Template manifest is missing templateId.");
        }

        if (string.IsNullOrWhiteSpace(manifest.TemplateVersion))
        {
            throw new InvalidOperationException("Template manifest is missing templateVersion.");
        }

        if (string.IsNullOrWhiteSpace(manifest.SourceRoot))
        {
            throw new InvalidOperationException("Template manifest is missing sourceRoot.");
        }

        return manifest;
    }
}

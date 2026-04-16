using PortraitModGenerator.Core.Abstractions;
using PortraitModGenerator.Core.Models;

namespace PortraitModGenerator.Core.Services;

public sealed class TemplateProjectGenerator
{
    private static readonly HashSet<string> TextFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs",
        ".csproj",
        ".json",
        ".props",
        ".targets",
        ".godot",
        ".md",
        ".txt",
        ".yml",
        ".yaml",
        ".gitignore"
    };

    private readonly TemplateManifestLoader _manifestLoader;

    public TemplateProjectGenerator()
        : this(new TemplateManifestLoader())
    {
    }

    public TemplateProjectGenerator(TemplateManifestLoader manifestLoader)
    {
        _manifestLoader = manifestLoader;
    }

    public TemplateGenerationResult Generate(TemplateGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string templateDirectory = Path.GetFullPath(request.TemplateDirectory);
        string outputDirectory = Path.GetFullPath(request.OutputDirectory);

        TemplateManifest manifest = _manifestLoader.Load(templateDirectory);
        string sourceRoot = Path.Combine(templateDirectory, manifest.SourceRoot);
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"Template source root not found: {sourceRoot}");
        }

        IReadOnlyDictionary<string, string> resolvedTokenValues = BuildResolvedTokens(manifest, request.TokenValues);
        PrepareOutputDirectory(outputDirectory, request.OverwriteExistingOutput);
        CopyDirectory(sourceRoot, outputDirectory, manifest, resolvedTokenValues);

        string entryProjectPath = Path.Combine(outputDirectory, ReplaceRenameTokens(manifest.EntryProject, manifest, resolvedTokenValues));
        string manifestPath = Path.Combine(outputDirectory, ReplaceRenameTokens(manifest.ManifestFile, manifest, resolvedTokenValues));

        return new TemplateGenerationResult
        {
            TemplateId = manifest.TemplateId,
            TemplateVersion = manifest.TemplateVersion,
            OutputDirectory = outputDirectory,
            EntryProjectPath = entryProjectPath,
            ManifestPath = manifestPath
        };
    }

    private static IReadOnlyDictionary<string, string> BuildResolvedTokens(
        TemplateManifest manifest,
        IReadOnlyDictionary<string, string> requestTokenValues)
    {
        Dictionary<string, string> tokens = new(manifest.DefaultValues, StringComparer.Ordinal);
        foreach ((string key, string value) in requestTokenValues)
        {
            tokens[key] = value;
        }

        foreach (string token in manifest.Tokens)
        {
            if (!tokens.TryGetValue(token, out string? value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Missing required token value for '{token}'.");
            }
        }

        return tokens;
    }

    private static void PrepareOutputDirectory(string outputDirectory, bool overwriteExistingOutput)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            return;
        }

        if (!overwriteExistingOutput && Directory.EnumerateFileSystemEntries(outputDirectory).Any())
        {
            throw new InvalidOperationException($"Output directory already exists and is not empty: {outputDirectory}");
        }
    }

    private static void CopyDirectory(
        string sourceDirectory,
        string destinationDirectory,
        TemplateManifest manifest,
        IReadOnlyDictionary<string, string> tokenValues)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (string directory in Directory.GetDirectories(sourceDirectory))
        {
            string directoryName = Path.GetFileName(directory);
            string destinationName = ReplaceRenameTokens(directoryName, manifest, tokenValues);
            CopyDirectory(directory, Path.Combine(destinationDirectory, destinationName), manifest, tokenValues);
        }

        foreach (string filePath in Directory.GetFiles(sourceDirectory))
        {
            string fileName = Path.GetFileName(filePath);
            string destinationName = ReplaceRenameTokens(fileName, manifest, tokenValues);
            string destinationPath = Path.Combine(destinationDirectory, destinationName);

            if (ShouldTreatAsText(filePath))
            {
                string contents = File.ReadAllText(filePath);
                string replaced = ReplaceAllTokens(contents, tokenValues);
                File.WriteAllText(destinationPath, replaced);
                continue;
            }

            File.Copy(filePath, destinationPath, overwrite: true);
        }
    }

    private static string ReplaceRenameTokens(
        string value,
        TemplateManifest manifest,
        IReadOnlyDictionary<string, string> tokenValues)
    {
        string result = value;
        foreach (string token in manifest.RenameTokens)
        {
            if (tokenValues.TryGetValue(token, out string? replacement))
            {
                result = result.Replace(token, replacement, StringComparison.Ordinal);
            }
        }

        return result;
    }

    private static string ReplaceAllTokens(string value, IReadOnlyDictionary<string, string> tokenValues)
    {
        string result = value;
        foreach ((string token, string replacement) in tokenValues)
        {
            result = result.Replace(token, replacement, StringComparison.Ordinal);
        }

        return result;
    }

    private static bool ShouldTreatAsText(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return TextFileExtensions.Contains(extension) || string.IsNullOrEmpty(extension);
    }
}

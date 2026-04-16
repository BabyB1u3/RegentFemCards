using PortraitModGenerator.Core.Abstractions;
using PortraitModGenerator.Core.Services;

Dictionary<string, string> arguments;

try
{
    arguments = ParseArguments(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    PrintUsage();
    return 1;
}

if (args.Length == 0 || arguments.ContainsKey("--help"))
{
    PrintUsage();
    return 0;
}

try
{
    TemplateProjectGenerator generator = new();
    TemplateGenerationRequest request = new()
    {
        TemplateDirectory = GetRequired(arguments, "--template"),
        OutputDirectory = GetRequired(arguments, "--output"),
        OverwriteExistingOutput = arguments.ContainsKey("--overwrite"),
        TokenValues = BuildTokenValues(arguments)
    };

    TemplateGenerationResult result = generator.Generate(request);

    Console.WriteLine("Template generation completed.");
    Console.WriteLine($"Template: {result.TemplateId} v{result.TemplateVersion}");
    Console.WriteLine($"Output: {result.OutputDirectory}");
    Console.WriteLine($"Project: {result.EntryProjectPath}");
    Console.WriteLine($"Manifest: {result.ManifestPath}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Generation failed: {ex.Message}");
    return 1;
}

static Dictionary<string, string> ParseArguments(string[] args)
{
    Dictionary<string, string> parsed = new(StringComparer.OrdinalIgnoreCase);

    for (int i = 0; i < args.Length; i++)
    {
        string current = args[i];
        if (!current.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unexpected argument '{current}'.");
        }

        if (string.Equals(current, "--overwrite", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(current, "--help", StringComparison.OrdinalIgnoreCase))
        {
            parsed[current] = "true";
            continue;
        }

        if (i + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for argument '{current}'.");
        }

        parsed[current] = args[++i];
    }

    return parsed;
}

static IReadOnlyDictionary<string, string> BuildTokenValues(IReadOnlyDictionary<string, string> arguments)
{
    string modId = GetRequired(arguments, "--mod-id");

    return new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["__MOD_ID__"] = modId,
        ["__MOD_NAME__"] = GetOptional(arguments, "--mod-name", modId),
        ["__AUTHOR__"] = GetOptional(arguments, "--author", "Unknown Author"),
        ["__DESCRIPTION__"] = GetOptional(arguments, "--description", "Generated portrait replacement mod"),
        ["__VERSION__"] = GetOptional(arguments, "--version", "v0.1.0")
    };
}

static string GetRequired(IReadOnlyDictionary<string, string> arguments, string key)
{
    if (!arguments.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
    {
        throw new ArgumentException($"Missing required argument '{key}'.");
    }

    return value;
}

static string GetOptional(IReadOnlyDictionary<string, string> arguments, string key, string fallback)
{
    return arguments.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : fallback;
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project tools/PortraitModGenerator.Cli -- \\");
    Console.WriteLine("    --template <templateDir> \\");
    Console.WriteLine("    --output <outputDir> \\");
    Console.WriteLine("    --mod-id <modId> [options]");
    Console.WriteLine();
    Console.WriteLine("Required:");
    Console.WriteLine("  --template       Path to a template directory containing template.json");
    Console.WriteLine("  --output         Output directory for the generated mod project");
    Console.WriteLine("  --mod-id         Mod identifier used for project, manifest and resource paths");
    Console.WriteLine();
    Console.WriteLine("Optional:");
    Console.WriteLine("  --mod-name       Display name for the generated mod");
    Console.WriteLine("  --author         Author name");
    Console.WriteLine("  --description    Mod description");
    Console.WriteLine("  --version        Mod version (default: v0.1.0)");
    Console.WriteLine("  --overwrite      Allow using an existing output directory");
    Console.WriteLine("  --help           Show this help");
}

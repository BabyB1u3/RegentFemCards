using System.Diagnostics;
using System.Text;
using System.Text.Json;
using PortraitModGenerator.Core.Abstractions;

namespace PortraitModGenerator.Core.Services;

public sealed class GdrePckImporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public PckImportResult Import(PckImportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string sourcePckPath = Path.GetFullPath(request.SourcePckPath);
        string outputDirectory = Path.GetFullPath(request.OutputDirectory);
        string gdreToolsPath = Path.GetFullPath(request.GdreToolsPath);

        if (!File.Exists(sourcePckPath))
        {
            throw new FileNotFoundException("PCK file not found.", sourcePckPath);
        }

        if (!File.Exists(gdreToolsPath))
        {
            throw new FileNotFoundException("GDRETools executable not found.", gdreToolsPath);
        }

        PrepareOutputDirectory(outputDirectory, request.OverwriteOutput);

        string logFilePath = string.IsNullOrWhiteSpace(request.LogFilePath)
            ? Path.Combine(outputDirectory, "gdre_import.log")
            : Path.GetFullPath(request.LogFilePath);

        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

        string arguments =
            $"--headless --quiet --no-header --log-file \"{logFilePath}\" --quit --recover=\"{sourcePckPath}\" --output=\"{outputDirectory}\"";

        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        ProcessStartInfo startInfo = new()
        {
            FileName = gdreToolsPath,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(gdreToolsPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        string gdreUserRoot = Path.Combine(outputDirectory, ".gdre_user");
        string gdreAppData = Path.Combine(gdreUserRoot, "Roaming");
        string gdreLocalAppData = Path.Combine(gdreUserRoot, "Local");
        Directory.CreateDirectory(gdreAppData);
        Directory.CreateDirectory(gdreLocalAppData);
        startInfo.Environment["APPDATA"] = gdreAppData;
        startInfo.Environment["LOCALAPPDATA"] = gdreLocalAppData;

        using Process process = new() { StartInfo = startInfo };
        StringBuilder logBuilder = new();
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                logBuilder.AppendLine(eventArgs.Data);
            }
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                logBuilder.AppendLine(eventArgs.Data);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start GDRETools process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        DateTimeOffset endedAt = DateTimeOffset.UtcNow;

        if (logBuilder.Length > 0)
        {
            File.AppendAllText(logFilePath, Environment.NewLine + logBuilder);
        }

        bool success = process.ExitCode == 0 && Directory.Exists(outputDirectory);
        PckImportResult result = new()
        {
            SourcePckPath = sourcePckPath,
            ExtractRoot = outputDirectory,
            GdreToolsPath = gdreToolsPath,
            LogFilePath = logFilePath,
            StartedAt = startedAt,
            EndedAt = endedAt,
            ExitCode = process.ExitCode,
            Success = success,
            CommandLine = $"\"{gdreToolsPath}\" {arguments}"
        };

        string resultPath = Path.Combine(outputDirectory, "gdre_import_result.json");
        File.WriteAllText(resultPath, JsonSerializer.Serialize(result, JsonOptions));

        if (!success)
        {
            throw new InvalidOperationException(
                $"GDRETools import failed with exit code {process.ExitCode}. See log: {logFilePath}");
        }

        return result;
    }

    private static void PrepareOutputDirectory(string outputDirectory, bool overwriteOutput)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            return;
        }

        if (!overwriteOutput && Directory.EnumerateFileSystemEntries(outputDirectory).Any())
        {
            throw new InvalidOperationException(
                $"Output directory already exists and is not empty: {outputDirectory}");
        }
    }
}

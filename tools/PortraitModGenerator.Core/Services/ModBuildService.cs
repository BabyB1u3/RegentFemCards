using System.Diagnostics;
using System.Text;
using System.Text.Json;
using PortraitModGenerator.Core.Abstractions;

namespace PortraitModGenerator.Core.Services;

public sealed class ModBuildService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ModBuildResult Build(ModBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string projectFilePath = Path.GetFullPath(request.ProjectFilePath);
        string artifactOutputDirectory = Path.GetFullPath(request.ArtifactOutputDirectory);
        string logFilePath = Path.GetFullPath(request.LogFilePath);
        string dotnetCliHome = Path.GetFullPath(request.DotnetCliHome);

        if (!File.Exists(projectFilePath))
        {
            throw new FileNotFoundException("Project file was not found.", projectFilePath);
        }

        Directory.CreateDirectory(artifactOutputDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
        Directory.CreateDirectory(dotnetCliHome);

        string workingDirectory = Path.GetDirectoryName(projectFilePath)!;
        string arguments = $"build \"{projectFilePath}\" -c {request.Configuration}";

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.Environment["DOTNET_CLI_HOME"] = dotnetCliHome;
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";

        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        using Process process = new() { StartInfo = startInfo };
        StringBuilder outputBuilder = new();

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                outputBuilder.AppendLine(eventArgs.Data);
            }
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                outputBuilder.AppendLine(eventArgs.Data);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start dotnet build process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        DateTimeOffset endedAt = DateTimeOffset.UtcNow;

        File.WriteAllText(logFilePath, outputBuilder.ToString(), Encoding.UTF8);

        if (process.ExitCode == 0)
        {
            CollectArtifacts(projectFilePath, request.Configuration, artifactOutputDirectory);
        }

        ModBuildResult result = new()
        {
            ProjectFilePath = projectFilePath,
            ArtifactOutputDirectory = artifactOutputDirectory,
            LogFilePath = logFilePath,
            CommandLine = $"dotnet {arguments}",
            StartedAt = startedAt,
            EndedAt = endedAt,
            ExitCode = process.ExitCode,
            Success = process.ExitCode == 0
        };

        string resultPath = Path.Combine(artifactOutputDirectory, "mod_build_result.json");
        File.WriteAllText(resultPath, JsonSerializer.Serialize(result, JsonOptions));

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"dotnet build failed with exit code {result.ExitCode}. See log: {logFilePath}");
        }

        return result;
    }

    private static void CollectArtifacts(string projectFilePath, string configuration, string artifactOutputDirectory)
    {
        string projectDirectory = Path.GetDirectoryName(projectFilePath)!;
        string projectName = Path.GetFileNameWithoutExtension(projectFilePath);
        string manifestPath = Path.Combine(projectDirectory, $"{projectName}.json");

        CopyIfExists(manifestPath, Path.Combine(artifactOutputDirectory, Path.GetFileName(manifestPath)));

        string? builtDllPath = FindNewestFile(
            Path.Combine(projectDirectory, ".godot"),
            $"{projectName}.dll");
        if (builtDllPath is null)
        {
            builtDllPath = FindNewestFile(projectDirectory, $"{projectName}.dll");
        }

        string? builtPckPath = FindNewestFile(
            Path.Combine(projectDirectory, ".godot"),
            $"{projectName}.pck");

        if (builtDllPath is not null)
        {
            CopyIfExists(builtDllPath, Path.Combine(artifactOutputDirectory, Path.GetFileName(builtDllPath)));
        }

        if (builtPckPath is not null)
        {
            CopyIfExists(builtPckPath, Path.Combine(artifactOutputDirectory, Path.GetFileName(builtPckPath)));
        }
    }

    private static string? FindNewestFile(string rootDirectory, string fileName)
    {
        if (!Directory.Exists(rootDirectory))
        {
            return null;
        }

        return Directory.EnumerateFiles(rootDirectory, fileName, SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .Select(info => info.FullName)
            .FirstOrDefault();
    }

    private static void CopyIfExists(string sourcePath, string destinationPath)
    {
        if (!File.Exists(sourcePath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourcePath, destinationPath, overwrite: true);
    }
}

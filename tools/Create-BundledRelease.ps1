param(
    [string]$OutputDir = (Join-Path $PSScriptRoot "..\dist\StS2PortraitModGenerator"),
    [string]$Configuration = "Release",
    [Parameter(Mandatory = $true)]
    [string]$BundledDotnetDir,
    [Parameter(Mandatory = $true)]
    [string]$BundledGodotDir
)

$ErrorActionPreference = "Stop"

function Resolve-FullPath {
    param([string]$PathValue)

    return [System.IO.Path]::GetFullPath($PathValue)
}

function Assert-FileExists {
    param(
        [string]$PathValue,
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "$Label not found: $PathValue"
    }
}

function Assert-DirectoryExists {
    param(
        [string]$PathValue,
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $PathValue -PathType Container)) {
        throw "$Label not found: $PathValue"
    }
}

function Copy-Tree {
    param(
        [string]$Source,
        [string]$Destination
    )

    Assert-DirectoryExists -PathValue $Source -Label "Required directory"
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Copy-Item -LiteralPath (Join-Path $Source "*") -Destination $Destination -Recurse -Force
}

function Publish-Project {
    param(
        [string]$ProjectPath,
        [string]$OutputPath,
        [string]$ConfigurationName
    )

    & dotnet publish $ProjectPath -c $ConfigurationName -o $OutputPath --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $ProjectPath"
    }
}

$repoRoot = Resolve-FullPath (Join-Path $PSScriptRoot "..")
$outputRoot = Resolve-FullPath $OutputDir
$bundledDotnetRoot = Resolve-FullPath $BundledDotnetDir
$bundledGodotRoot = Resolve-FullPath $BundledGodotDir

Assert-DirectoryExists -PathValue $repoRoot -Label "Repository root"
Assert-FileExists -PathValue (Join-Path $bundledDotnetRoot "dotnet.exe") -Label "Bundled dotnet executable"
Assert-DirectoryExists -PathValue $bundledGodotRoot -Label "Bundled Godot directory"
Assert-FileExists -PathValue (Join-Path $repoRoot "config\NuGet.config") -Label "Bundled NuGet config"
Assert-FileExists -PathValue (Join-Path $repoRoot "gdre\gdre_tools.exe") -Label "GDRETools executable"
Assert-DirectoryExists -PathValue (Join-Path $repoRoot "packages") -Label "Bundled packages directory"
Assert-FileExists -PathValue (Join-Path $repoRoot "templates\PortraitReplacementTemplate\template.json") -Label "Template manifest"
Assert-FileExists -PathValue (Join-Path $repoRoot "data\official_card_index.json") -Label "Official card index"

if (Test-Path -LiteralPath $outputRoot) {
    $existingEntries = Get-ChildItem -LiteralPath $outputRoot -Force
    if ($existingEntries.Count -gt 0) {
        throw "Output directory is not empty: $outputRoot"
    }
}
else {
    New-Item -ItemType Directory -Path $outputRoot | Out-Null
}

$appDir = Join-Path $outputRoot "app"
$toolsDir = Join-Path $outputRoot "tools"
$dotnetOutDir = Join-Path $toolsDir "dotnet"
$godotOutDir = Join-Path $toolsDir "godot"
$gdreOutDir = Join-Path $toolsDir "gdre"
$templatesOutDir = Join-Path $outputRoot "templates"
$dataOutDir = Join-Path $outputRoot "data"
$packagesOutDir = Join-Path $outputRoot "packages"
$configOutDir = Join-Path $outputRoot "config"

New-Item -ItemType Directory -Force -Path $appDir | Out-Null

Publish-Project -ProjectPath (Join-Path $repoRoot "tools\PortraitModGenerator.Gui\PortraitModGenerator.Gui.csproj") -OutputPath $appDir -ConfigurationName $Configuration
Publish-Project -ProjectPath (Join-Path $repoRoot "tools\PortraitModGenerator.Cli\PortraitModGenerator.Cli.csproj") -OutputPath $appDir -ConfigurationName $Configuration

Copy-Tree -Source (Join-Path $repoRoot "templates") -Destination $templatesOutDir
Copy-Tree -Source (Join-Path $repoRoot "data") -Destination $dataOutDir
Copy-Tree -Source (Join-Path $repoRoot "packages") -Destination $packagesOutDir
Copy-Tree -Source (Join-Path $repoRoot "config") -Destination $configOutDir
Copy-Tree -Source (Join-Path $repoRoot "gdre") -Destination $gdreOutDir
Copy-Tree -Source $bundledDotnetRoot -Destination $dotnetOutDir
Copy-Tree -Source $bundledGodotRoot -Destination $godotOutDir

foreach ($workingDir in @("cache", "generated", "artifacts", "logs")) {
    New-Item -ItemType Directory -Force -Path (Join-Path $outputRoot $workingDir) | Out-Null
}

Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination (Join-Path $outputRoot "README.md") -Force

Write-Host ""
Write-Host "Bundled release created:"
Write-Host "  $outputRoot"
Write-Host ""
Write-Host "Bundled tool roots:"
Write-Host "  dotnet: $dotnetOutDir"
Write-Host "  godot : $godotOutDir"
Write-Host "  gdre  : $gdreOutDir"

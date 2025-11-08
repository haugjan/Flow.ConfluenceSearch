# Build and package Flow Launcher plugin
# This script builds the plugin and creates a ZIP file ready for Flow Launcher installation

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\dist"
)

$ErrorActionPreference = "Stop"

# Plugin information from plugin.json
$PluginId = "BD32A62C-6F98-4541-AE8E-17B46458595F"
$PluginName = "Flow.ConfluenceSearch"

Write-Host "Building Flow Launcher Plugin: $PluginName" -ForegroundColor Green

# Resolve to absolute paths
$OutputPath = Resolve-Path $OutputPath -ErrorAction SilentlyContinue
if (-not $OutputPath) {
    $OutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath(".\dist")
}

# Clean and create output directory
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Build the project
Write-Host "Building project in $Configuration configuration..." -ForegroundColor Yellow
dotnet build "Flow.ConfluenceSearch.csproj" --configuration $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

# Define source and target paths
$BinPath = "bin\$Configuration\net9.0-windows"
$TempPluginDir = Join-Path $OutputPath "temp\$PluginName"

# Create temporary plugin directory structure
New-Item -ItemType Directory -Path $TempPluginDir -Force | Out-Null

# Copy required files to temp directory
Write-Host "Copying plugin files..." -ForegroundColor Yellow

# Copy main DLL and dependencies
Copy-Item "$BinPath\Flow.ConfluenceSearch.dll" -Destination $TempPluginDir
Copy-Item "$BinPath\Flow.ConfluenceSearch.deps.json" -Destination $TempPluginDir -ErrorAction SilentlyContinue

# Copy plugin.json
Copy-Item "plugin.json" -Destination $TempPluginDir

# Copy Images directory
if (Test-Path "Images") {
    Copy-Item "Images" -Destination $TempPluginDir -Recurse
}

# Copy required NuGet packages (exclude system assemblies)
$ExcludePatterns = @(
    "Microsoft.WindowsDesktop.App.*",
    "Microsoft.NETCore.App.*",
    "System.*",
    "netstandard.*",
    "mscorlib.*",
    "WindowsBase.*",
    "PresentationCore.*",
    "PresentationFramework.*"
)

Get-ChildItem "$BinPath\*.dll" | Where-Object {
    $fileName = $_.Name
    $exclude = $false
    foreach ($pattern in $ExcludePatterns) {
        if ($fileName -like $pattern) {
            $exclude = $true
            break
        }
    }
    return !$exclude -and $fileName -ne "Flow.ConfluenceSearch.dll"
} | ForEach-Object {
    Copy-Item $_.FullName -Destination $TempPluginDir
    Write-Host "  Copied dependency: $($_.Name)" -ForegroundColor Gray
}

# Create ZIP file
$ZipFileName = "$PluginName-v1.0.1.zip"
$ZipPath = Join-Path $OutputPath $ZipFileName

Write-Host "Creating ZIP package: $ZipFileName" -ForegroundColor Yellow
Write-Host "Source directory: $TempPluginDir" -ForegroundColor Gray
Write-Host "Target ZIP path: $ZipPath" -ForegroundColor Gray

# Ensure the output directory exists
$ZipDir = Split-Path $ZipPath -Parent
if (-not (Test-Path $ZipDir)) {
    New-Item -ItemType Directory -Path $ZipDir -Force | Out-Null
}

# Use .NET compression to create ZIP
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($TempPluginDir, $ZipPath)

# Clean up temp directory
Remove-Item (Join-Path $OutputPath "temp") -Recurse -Force

# Verify ZIP contents
Write-Host "ZIP package created successfully!" -ForegroundColor Green
Write-Host "Package location: $ZipPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Package contents:" -ForegroundColor Yellow

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
$zip.Entries | ForEach-Object {
    Write-Host "  $($_.FullName)" -ForegroundColor Gray
}
$zip.Dispose()

Write-Host ""
Write-Host "Installation instructions:" -ForegroundColor Green
Write-Host "1. Open Flow Launcher" -ForegroundColor White
Write-Host "2. Go to Settings > Plugins" -ForegroundColor White  
Write-Host "3. Click 'Install Plugin'" -ForegroundColor White
Write-Host "4. Select the ZIP file: $ZipPath" -ForegroundColor White
Write-Host "5. Restart Flow Launcher" -ForegroundColor White

# ===============================
# Build-And-Restart-FlowLauncher.ps1
# ===============================
# Stops Flow Launcher, builds the plugin and restarts Flow Launcher
# ===============================
param (
    [string]$SolutionPath = ".\Flow.ConfluenceSearch.csproj",          # Path to solution or csproj file
    [string]$BuildConfig = "Debug",                              # Or "Release"
    [string]$PluginFolderName,                                   # Plugin folder name under Plugins; derived from plugin.json if omitted
    [string]$FlowLauncherPath = "$env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe"  # Default path
)

if (-not $PluginFolderName) {
    $manifestPath = Join-Path -Path (Split-Path $SolutionPath) -ChildPath "plugin.json"
    if (-not (Test-Path $manifestPath)) {
        $manifestPath = Join-Path -Path $PSScriptRoot -ChildPath "plugin.json"
    }
    $manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json
    $PluginFolderName = "$($manifest.Name)-$($manifest.Version)"
}

Write-Host "----------------------------------------"
Write-Host "🔧 Flow Launcher Plugin Build Script"
Write-Host "----------------------------------------"

# 1️⃣ Stop Flow Launcher
Write-Host "🛑 Stopping Flow Launcher..."
Get-Process "Flow.Launcher" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# 2️⃣ Build solution/project
Write-Host "⚙️  Building solution: $SolutionPath ($BuildConfig)"
dotnet build $SolutionPath -c $BuildConfig

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# 3️⃣ Copy build output
Write-Host "📂 Copying build output to Flow Launcher plugins folder..."
$buildOutput = Join-Path -Path (Split-Path $SolutionPath) -ChildPath "bin\$BuildConfig\net9.0-windows"
$pluginTarget = Join-Path "$env:APPDATA\FlowLauncher\Plugins" $PluginFolderName

if (-not (Test-Path $pluginTarget)) {
    New-Item -ItemType Directory -Path $pluginTarget | Out-Null
}

Copy-Item -Path "$buildOutput\*" -Destination $pluginTarget -Recurse -Force
Write-Host "✅ Files copied to:`n$pluginTarget"

# 4️⃣ Restart Flow Launcher
Write-Host "🚀 Starting Flow Launcher..."
Start-Process $FlowLauncherPath

Write-Host "----------------------------------------"
Write-Host "🎉 Done! Flow Launcher has been restarted."
Write-Host "----------------------------------------"
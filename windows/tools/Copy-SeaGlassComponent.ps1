<#
.SYNOPSIS
    Copies standalone SeaGlass XAML and C# components into a consumer WinUI 3 project (shadcn/ui style),
    enabling consumers to own the component source without referencing the SeaGlass.WinUI binary NuGet package.

.PARAMETER Component
    The component to extract (e.g. Button, Card, Switch, Slider, Badge, Chip, Dialog, or All).

.PARAMETER DestinationDirectory
    Target directory in consumer project.

.PARAMETER IncludeCoreSource
    When true, extracts SeaGlass.Core C# source files directly for zero package references.

.EXAMPLE
    ./Copy-SeaGlassComponent.ps1 -Component Button -DestinationDirectory C:/MyApp/SeaGlass
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet(
        "Button", "Card", "Switch", "Slider", "Badge", "Chip", "Dialog",
        "FluidBar", "TopAppBar", "Navigation",
        "Inputs", "TextField",
        "LinearProgressIndicator", "Feedback",
        "MultiShapeMorph", "Morphing",
        "FloatingSheet", "Overlays",
        "TitleBar", "NavigationView", "Extras",
        "Surfaces", "All"
    )]
    [string]$Component,

    [Parameter(Mandatory=$true)]
    [string]$DestinationDirectory,

    [Parameter(Mandatory=$false)]
    [switch]$IncludeCoreSource
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$scriptDir/../..").Path
$winuiSrc = "$repoRoot/windows/SeaGlass.WinUI"
$coreSrc  = "$repoRoot/windows/SeaGlass.Core"

$targetDir = (New-Item -ItemType Directory -Force -Path $DestinationDirectory).FullName
Write-Host "Extracting SeaGlass [$Component] into $targetDir..." -ForegroundColor Cyan

# 1. Tier 0: Design Tokens
New-Item -ItemType Directory -Force -Path "$targetDir/Themes" | Out-Null
Copy-Item "$winuiSrc/Themes/Tokens.xaml" "$targetDir/Themes/Tokens.xaml" -Force

# 2. Tier 1: Foundation Primitives, Root Models & Platform Interop
New-Item -ItemType Directory -Force -Path "$targetDir/Controls/Surfaces" | Out-Null
New-Item -ItemType Directory -Force -Path "$targetDir/Controls/Buttons" | Out-Null
New-Item -ItemType Directory -Force -Path "$targetDir/Controls/Feedback" | Out-Null
New-Item -ItemType Directory -Force -Path "$targetDir/Platform" | Out-Null
New-Item -ItemType Directory -Force -Path "$targetDir/Ergonomics" | Out-Null
New-Item -ItemType Directory -Force -Path "$targetDir/Automation" | Out-Null
New-Item -ItemType Directory -Force -Path "$targetDir/Rendering" | Out-Null
New-Item -ItemType Directory -Force -Path "$targetDir/Coalescing" | Out-Null
Copy-Item "$winuiSrc/Automation/SeaGlassSurfaceAutomationPeers.cs" "$targetDir/Automation" -Force
Copy-Item "$winuiSrc/Automation/SeaGlassButtonAutomationPeers.cs" "$targetDir/Automation" -Force
Copy-Item "$winuiSrc/Rendering/*.cs" "$targetDir/Rendering" -Force
Copy-Item "$winuiSrc/Coalescing/*.cs" "$targetDir/Coalescing" -Force


Get-ChildItem -Path $winuiSrc -Filter "*.cs" | Copy-Item -Destination $targetDir -Force
Copy-Item "$winuiSrc/Controls/Surfaces/*.cs" "$targetDir/Controls/Surfaces" -Force
Copy-Item "$winuiSrc/Controls/Buttons/*.cs" "$targetDir/Controls/Buttons" -Force
Copy-Item "$winuiSrc/Controls/Feedback/SeaGlassIcon.cs" "$targetDir/Controls/Feedback/SeaGlassIcon.cs" -Force
Copy-Item "$winuiSrc/Controls/SeaGlassEnums.cs" "$targetDir/Controls/SeaGlassEnums.cs" -Force

New-Item -ItemType Directory -Force -Path "$targetDir/Motion" | Out-Null
Copy-Item "$winuiSrc/Motion/SeaGlassMotionCore.cs" "$targetDir/Motion/SeaGlassMotionCore.cs" -Force

# Platform interop
Copy-Item "$winuiSrc/Platform/ISeaGlassPlatformSettings.cs" "$targetDir/Platform/ISeaGlassPlatformSettings.cs" -Force
Copy-Item "$winuiSrc/Platform/WindowsPlatformSettings.cs" "$targetDir/Platform/WindowsPlatformSettings.cs" -Force
Copy-Item "$winuiSrc/Platform/InjectablePlatformSettings.cs" "$targetDir/Platform/InjectablePlatformSettings.cs" -Force

# Ergonomics extensions
Copy-Item "$winuiSrc/Ergonomics/SeaGlassOpticsExtensions.cs" "$targetDir/Ergonomics/SeaGlassOpticsExtensions.cs" -Force
Copy-Item "$winuiSrc/Ergonomics/SeaGlassMotionExtensions.cs" "$targetDir/Ergonomics/SeaGlassMotionExtensions.cs" -Force
Copy-Item "$winuiSrc/Ergonomics/SeaGlassControlBuilders.Layout.cs" "$targetDir/Ergonomics/SeaGlassControlBuilders.Layout.cs" -Force

# 3. Component-Specific Leaf Files (8 categories + All)
switch ($Component) {
    { $_ -in "Card", "Dialog", "Surfaces" } {
        New-Item -ItemType Directory -Force -Path "$targetDir/Controls/Surfaces" | Out-Null
        Copy-Item "$winuiSrc/Controls/Surfaces/*.cs" "$targetDir/Controls/Surfaces" -Force
        Copy-Item "$winuiSrc/Themes/Surfaces.xaml" "$targetDir/Themes/Surfaces.xaml" -Force
        Copy-Item "$winuiSrc/Ergonomics/SeaGlassControlBuilders.Surfaces.cs" "$targetDir/Ergonomics/SeaGlassControlBuilders.Surfaces.cs" -Force
    }
    { $_ -in "Button", "Badge", "Chip" } {
        New-Item -ItemType Directory -Force -Path "$targetDir/Controls/Buttons" | Out-Null
        Copy-Item "$winuiSrc/Controls/Buttons/*.cs" "$targetDir/Controls/Buttons" -Force
        Copy-Item "$winuiSrc/Themes/Buttons.xaml" "$targetDir/Themes/Buttons.xaml" -Force
        Copy-Item "$winuiSrc/Ergonomics/SeaGlassControlBuilders.Buttons.cs" "$targetDir/Ergonomics/SeaGlassControlBuilders.Buttons.cs" -Force
    }
    { $_ -in "Switch", "Slider", "TextField", "Inputs" } {
        New-Item -ItemType Directory -Force -Path "$targetDir/Controls/Inputs" | Out-Null
        Copy-Item "$winuiSrc/Controls/Inputs/*.cs" "$targetDir/Controls/Inputs" -Force
        Copy-Item "$winuiSrc/Themes/Inputs.xaml" "$targetDir/Themes/Inputs.xaml" -Force
        Copy-Item "$winuiSrc/Ergonomics/SeaGlassControlBuilders.Inputs.cs" "$targetDir/Ergonomics/SeaGlassControlBuilders.Inputs.cs" -Force
        Copy-Item "$winuiSrc/Automation/SeaGlassInputAutomationPeers.cs" "$targetDir/Automation" -Force
    }
    { $_ -in "FluidBar", "TopAppBar", "Navigation" } {
        New-Item -ItemType Directory -Force -Path "$targetDir/Controls/Navigation" | Out-Null
        Copy-Item "$winuiSrc/Controls/Navigation/*.cs" "$targetDir/Controls/Navigation" -Force
        Copy-Item "$winuiSrc/Themes/Navigation.xaml" "$targetDir/Themes/Navigation.xaml" -Force
        Copy-Item "$winuiSrc/Automation/SeaGlassNavigationAutomationPeers.cs" "$targetDir/Automation" -Force
    }
    { $_ -in "LinearProgressIndicator", "Feedback" } {
        New-Item -ItemType Directory -Force -Path "$targetDir/Controls/Feedback" | Out-Null
        Copy-Item "$winuiSrc/Controls/Feedback/*.cs" "$targetDir/Controls/Feedback" -Force
        Copy-Item "$winuiSrc/Themes/Feedback.xaml" "$targetDir/Themes/Feedback.xaml" -Force
        Copy-Item "$winuiSrc/Automation/SeaGlassFeedbackAutomationPeers.cs" "$targetDir/Automation" -Force
    }
    { $_ -in "MultiShapeMorph", "Morphing" } {
        New-Item -ItemType Directory -Force -Path "$targetDir/Controls/Morphing" | Out-Null
        Copy-Item "$winuiSrc/Controls/Morphing/*.cs" "$targetDir/Controls/Morphing" -Force
        Copy-Item "$winuiSrc/Themes/Morphing.xaml" "$targetDir/Themes/Morphing.xaml" -Force
        Copy-Item "$winuiSrc/Automation/SeaGlassMorphingAutomationPeers.cs" "$targetDir/Automation" -Force
    }
    { $_ -in "FloatingSheet", "Overlays" } {
        New-Item -ItemType Directory -Force -Path "$targetDir/Controls/Overlays" | Out-Null
        Copy-Item "$winuiSrc/Controls/Overlays/*.cs" "$targetDir/Controls/Overlays" -Force
        Copy-Item "$winuiSrc/Themes/Overlays.xaml" "$targetDir/Themes/Overlays.xaml" -Force
        Copy-Item "$winuiSrc/Automation/SeaGlassOverlayAutomationPeers.cs" "$targetDir/Automation" -Force
    }
    { $_ -in "TitleBar", "NavigationView", "Extras" } {
        New-Item -ItemType Directory -Force -Path "$targetDir/Controls/Extras" | Out-Null
        Copy-Item "$winuiSrc/Controls/Extras/*.cs" "$targetDir/Controls/Extras" -Force
        Copy-Item "$winuiSrc/Themes/Extras.xaml" "$targetDir/Themes/Extras.xaml" -Force
        Copy-Item "$winuiSrc/Automation/SeaGlassExtrasAutomationPeers.cs" "$targetDir/Automation" -Force
    }
    "All" {
        Get-ChildItem -Path $winuiSrc -Filter "*.cs" | Copy-Item -Destination $targetDir -Force
        Copy-Item "$winuiSrc/Controls" "$targetDir" -Recurse -Force
        Copy-Item "$winuiSrc/Themes" "$targetDir" -Recurse -Force
        Copy-Item "$winuiSrc/Ergonomics" "$targetDir" -Recurse -Force
        Copy-Item "$winuiSrc/Coalescing" "$targetDir" -Recurse -Force
        Copy-Item "$winuiSrc/Rendering" "$targetDir" -Recurse -Force
        Copy-Item "$winuiSrc/Platform" "$targetDir" -Recurse -Force
        Copy-Item "$winuiSrc/Motion" "$targetDir" -Recurse -Force
        Copy-Item "$winuiSrc/Automation" "$targetDir" -Recurse -Force
    }
}

# 4. Optional: Copy Core Source Files (for 100% zero-project/package adoption)
if ($IncludeCoreSource) {
    New-Item -ItemType Directory -Force -Path "$targetDir/Core" | Out-Null
    Get-ChildItem -Path $coreSrc -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "bin|obj" } | ForEach-Object {
        Copy-Item $_.FullName "$targetDir/Core/$($_.Name)" -Force
    }
}

Write-Host "Successfully extracted SeaGlass [$Component] into $targetDir" -ForegroundColor Green

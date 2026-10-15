$scriptDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$scriptDir/../..").Path
$sourceFile = "$repoRoot/windows/SeaGlass.WinUI/Automation/SeaGlassAutomationPeers.cs"

if (-not (Test-Path $sourceFile)) {
    Write-Host "File not found: $sourceFile"
    exit 0
}

$lines = Get-Content $sourceFile

$header = @"
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SeaGlass.WinUI;

namespace SeaGlass.WinUI.Automation;

"@

# 1. Surface Peers: 11-34 (idx 10..33) and 1465-1508 (idx 1464..lines.Length-1)
$surf = ($lines[10..33] + $lines[1464..($lines.Length-1)]) -join "`r`n"
Set-Content -Path "$repoRoot/windows/SeaGlass.WinUI/Automation/SeaGlassSurfaceAutomationPeers.cs" -Value ($header + $surf)

# 2. Button Peers: 35-273 (idx 34..272)
$btn = $lines[34..272] -join "`r`n"
Set-Content -Path "$repoRoot/windows/SeaGlass.WinUI/Automation/SeaGlassButtonAutomationPeers.cs" -Value ($header + $btn)

# 3. Input Peers: 274-503 (idx 273..502)
$inp = $lines[273..502] -join "`r`n"
Set-Content -Path "$repoRoot/windows/SeaGlass.WinUI/Automation/SeaGlassInputAutomationPeers.cs" -Value ($header + $inp)

# 4. Navigation Peers: 504-639 (idx 503..638)
$nav = $lines[503..638] -join "`r`n"
Set-Content -Path "$repoRoot/windows/SeaGlass.WinUI/Automation/SeaGlassNavigationAutomationPeers.cs" -Value ($header + $nav)

# 5. Feedback Peers: 640-761 (idx 639..760)
$fb = $lines[639..760] -join "`r`n"
Set-Content -Path "$repoRoot/windows/SeaGlass.WinUI/Automation/SeaGlassFeedbackAutomationPeers.cs" -Value ($header + $fb)

# 6. Morphing Peers: 762-967 (idx 761..966)
$morph = $lines[761..966] -join "`r`n"
Set-Content -Path "$repoRoot/windows/SeaGlass.WinUI/Automation/SeaGlassMorphingAutomationPeers.cs" -Value ($header + $morph)

# 7. Overlay Peers: 968-1286 (idx 967..1285)
$over = $lines[967..1285] -join "`r`n"
Set-Content -Path "$repoRoot/windows/SeaGlass.WinUI/Automation/SeaGlassOverlayAutomationPeers.cs" -Value ($header + $over)

# 8. Extras Peers: 1287-1464 (idx 1286..1463)
$ext = $lines[1286..1463] -join "`r`n"
Set-Content -Path "$repoRoot/windows/SeaGlass.WinUI/Automation/SeaGlassExtrasAutomationPeers.cs" -Value ($header + $ext)

Remove-Item $sourceFile -Force
Write-Host "Successfully split SeaGlassAutomationPeers.cs into 8 modular files."

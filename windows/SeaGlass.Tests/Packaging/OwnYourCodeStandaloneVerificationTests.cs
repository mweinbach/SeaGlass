using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace SeaGlass.Tests.Packaging;

public sealed class OwnYourCodeStandaloneVerificationTests
{
    [Theory]
    [InlineData("Card")]                    // Category 1: Surfaces
    [InlineData("Button")]                  // Category 2: Buttons
    [InlineData("FluidBar")]                // Category 3: Navigation
    [InlineData("Switch")]                  // Category 4: Inputs
    [InlineData("LinearProgressIndicator")] // Category 5: Feedback
    [InlineData("MultiShapeMorph")]         // Category 6: Morphing
    [InlineData("FloatingSheet")]           // Category 7: Overlays
    [InlineData("TitleBar")]                // Category 8: Windows Extras
    [InlineData("All")]                     // All categories
    public void OwnYourCode_StandaloneExtraction_BuildsCleanlyWithoutWinUiPackage(string component)
    {
        string repoRoot = LocateRepoRoot();
        string sandboxDir = Path.Combine(Path.GetTempPath(), "seaglass_own_your_code_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandboxDir);

        try
        {
            string scriptPath = File.Exists(Path.Combine(repoRoot, "windows", "tools", "Copy-SeaGlassComponent.ps1"))
                ? Path.Combine(repoRoot, "windows", "tools", "Copy-SeaGlassComponent.ps1")
                : Path.Combine(repoRoot, "tools", "Copy-SeaGlassComponent.ps1");
            string shell = Environment.OSVersion.Platform == PlatformID.Win32NT ? "powershell.exe" : "pwsh";

            var scriptPsi = new ProcessStartInfo
            {
                FileName = shell,
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Component {component} -DestinationDirectory \"{sandboxDir}\"",
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var scriptProc = Process.Start(scriptPsi)!)
            {
                string scriptOut = scriptProc.StandardOutput.ReadToEnd();
                string scriptErr = scriptProc.StandardError.ReadToEnd();
                scriptProc.WaitForExit(30000);
                Assert.True(scriptProc.ExitCode == 0, $"Copy-SeaGlassComponent.ps1 -Component {component} failed with code {scriptProc.ExitCode}.\nOUT: {scriptOut}\nERR: {scriptErr}");
            }

            // 2. Write an isolated WinUI 3 project that references only Core and Windows App SDK (NO SeaGlass.WinUI)
            string csprojPath = Path.Combine(sandboxDir, "StandaloneConsumer.csproj");
            string coreProjPath = Path.Combine(repoRoot, "windows", "SeaGlass.Core", "SeaGlass.Core.csproj");

            string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <WindowsPackageType>None</WindowsPackageType>
    <EnableMsixTooling>true</EnableMsixTooling>
    <GenerateLibraryLayout>true</GenerateLibraryLayout>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <RootNamespace>StandaloneConsumer</RootNamespace>
    <AssemblyName>StandaloneConsumer</AssemblyName>
    <Win2DWarnNoPlatform>false</Win2DWarnNoPlatform>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.WindowsAppSDK"" Version=""2.5.1"" />
    <PackageReference Include=""Microsoft.Windows.SDK.BuildTools"" Version=""10.0.28000.2705"" />
    <PackageReference Include=""Microsoft.Graphics.Win2D"" Version=""1.4.0"" />
    <ProjectReference Include=""{coreProjPath}"" />
  </ItemGroup>
</Project>";
            File.WriteAllText(csprojPath, csprojContent);

            // 3. Compile the standalone project using dotnet build in its own isolated artifacts directory
            string buildArtifacts = Path.Combine(sandboxDir, "bin");
            var buildPsi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{csprojPath}\" -c Release --artifacts-path \"{buildArtifacts}\"",
                WorkingDirectory = sandboxDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var buildProc = Process.Start(buildPsi)!)
            {
                string stdout = buildProc.StandardOutput.ReadToEnd();
                string stderr = buildProc.StandardError.ReadToEnd();
                buildProc.WaitForExit(120000);

                Assert.True(buildProc.ExitCode == 0, $"Standalone build for [{component}] failed with exit code {buildProc.ExitCode}.\nSTDOUT: {stdout}\nSTDERR: {stderr}");
            }

            // 4. Assert that SeaGlass.WinUI.dll was NOT compiled or referenced, while StandaloneConsumer.dll was generated
            var builtDlls = Directory.EnumerateFiles(buildArtifacts, "*.dll", SearchOption.AllDirectories)
                .Select(Path.GetFileName)
                .ToList();

            Assert.DoesNotContain("SeaGlass.WinUI.dll", builtDlls, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("StandaloneConsumer.dll", builtDlls, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(sandboxDir, true); } catch { }
        }
    }

    private static string LocateRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "windows", "SeaGlass.sln")) ||
                Directory.Exists(Path.Combine(current, ".git")))
            {
                return current;
            }
            if (File.Exists(Path.Combine(current, "SeaGlass.sln")))
            {
                var p = Directory.GetParent(current)?.FullName;
                if (p != null) return p;
            }
            string? parent = Directory.GetParent(current)?.FullName;
            if (parent == null || parent == current) break;
            current = parent;
        }
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}

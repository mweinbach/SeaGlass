using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using Xunit;

namespace SeaGlass.Tests.Packaging;

public sealed class PackageIntegrityVerificationTests
{
    private static readonly string[] RequiredCategoryDictionaries =
    [
        "Surfaces.xaml",
        "Buttons.xaml",
        "Navigation.xaml",
        "Inputs.xaml",
        "Feedback.xaml",
        "Morphing.xaml",
        "Overlays.xaml",
        "Extras.xaml"
    ];

    private static readonly string[] Expected58ControlTypes =
    [
        // Group 1: Foundations & Surfaces (9)
        "SeaGlassTheme",
        "SeaGlassStage",
        "SeaGlassFluidGroup",
        "SeaGlassSurface",
        "SeaGlassCard",
        "SeaGlassDialogCard",
        "SeaGlassBottomSheetSurface",
        "SeaGlassCornerEmbeddedPod",
        "SeaGlassAdaptiveCornerLabCard",

        // Group 2: Buttons & Chips (8)
        "SeaGlassButton",
        "SeaGlassIconButton",
        "SeaGlassExtendedFab",
        "SeaGlassPill",
        "SeaGlassFilterChip",
        "SeaGlassBadge",
        "SeaGlassSegmentedButtonRow",
        "SeaGlassPebbleOrb",

        // Group 3: Navigation & Bars (7)
        "SeaGlassTopAppBar",
        "SeaGlassFluidBar",
        "SeaGlassMiniPlayerBar",
        "SeaGlassFloatingToolbar",
        "SeaGlassPillNavBar",
        "SeaGlassPromptBar",
        "SeaGlassSearchBar",

        // Group 4: Inputs & Toggles (5)
        "SeaGlassTextField",
        "SeaGlassSwitch",
        "SeaGlassCheckbox",
        "SeaGlassRadioButton",
        "SeaGlassLiquidSlider",

        // Group 5: Feedback & Lists (5)
        "SeaGlassLinearProgressIndicator",
        "SeaGlassCircularProgressIndicator",
        "SeaGlassListItem",
        "SeaGlassSnackbar",
        "SeaGlassIcon",

        // Group 6: Dynamic Morphing Suite (8)
        "SeaGlassTwoElementMorph",
        "SeaGlassBuddingCluster",
        "SeaGlassMorphingButton",
        "SeaGlassChoreographedShapeOrb",
        "SeaGlassMultiShapeMorph",
        "SeaGlassMorphingContextMenu",
        "SeaGlassCascadedModelMorph",
        "SeaGlassContextRingMorph",

        // Group 7: iOS 27 & Advanced Overlays (8)
        "SeaGlassFloatingSheet",
        "SeaGlassContextMenuOverlay",
        "SeaGlassActionSheet",
        "SeaGlassLiveActivityPill",
        "SeaGlassMagnifierScrubber",
        "SeaGlassNotificationStack",
        "SeaGlassWheelPicker",
        "SeaGlassControlCenterGrid",

        // Group 8: Windows Native Extras (8)
        "SeaGlassTitleBar",
        "SeaGlassNavigationView",
        "SeaGlassTabView",
        "SeaGlassCommandBar",
        "SeaGlassInfoBar",
        "SeaGlassTeachingTip",
        "SeaGlassFlyout",
        "SeaGlassToolTip"
    ];

    private static string FindNupkgPath()
    {
        // 1. Environment variable override
        string? envPath = Environment.GetEnvironmentVariable("SEAGLASS_NUPKG_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath)) return envPath;

        // 2. Search upwards from AppContext.BaseDirectory and CurrentDirectory
        string[] startDirs = [AppContext.BaseDirectory, Directory.GetCurrentDirectory()];
        foreach (string startDir in startDirs)
        {
            DirectoryInfo? dirInfo = null;
            try { dirInfo = new DirectoryInfo(startDir); } catch { }
            int depth = 0;
            while (dirInfo != null && depth < 8)
            {
                // Check artifacts folder
                string artifacts = Path.Combine(dirInfo.FullName, "artifacts");
                if (Directory.Exists(artifacts))
                {
                    try
                    {
                        var found = Directory.EnumerateFiles(artifacts, "SeaGlass.WinUI.*.nupkg", SearchOption.AllDirectories)
                            .Where(f => !f.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                            .FirstOrDefault();
                        if (found != null) return found;
                    }
                    catch { }
                }

                // Check standard bin/Release
                string winuiBin = Path.Combine(dirInfo.FullName, "windows", "SeaGlass.WinUI", "bin");
                if (Directory.Exists(winuiBin))
                {
                    try
                    {
                        var found = Directory.EnumerateFiles(winuiBin, "SeaGlass.WinUI.*.nupkg", SearchOption.AllDirectories)
                            .Where(f => !f.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                            .FirstOrDefault();
                        if (found != null) return found;
                    }
                    catch { }
                }

                dirInfo = dirInfo.Parent;
                depth++;
            }
        }

        // 3. Fallback: on clean clone / clean state without pre-built nupkg, pack on demand
        string? repoRoot = null;
        DirectoryInfo? cur = new DirectoryInfo(AppContext.BaseDirectory);
        while (cur != null)
        {
            if (File.Exists(Path.Combine(cur.FullName, "SeaGlass.sln")) || File.Exists(Path.Combine(cur.FullName, "windows", "SeaGlass.sln")))
            {
                repoRoot = cur.FullName;
                break;
            }
            cur = cur.Parent;
        }

        if (repoRoot != null)
        {
            string projPath = File.Exists(Path.Combine(repoRoot, "windows", "SeaGlass.WinUI", "SeaGlass.WinUI.csproj"))
                ? Path.Combine(repoRoot, "windows", "SeaGlass.WinUI", "SeaGlass.WinUI.csproj")
                : Path.Combine(repoRoot, "SeaGlass.WinUI", "SeaGlass.WinUI.csproj");

            if (File.Exists(projPath))
            {
                string outDir = Path.Combine(Path.GetDirectoryName(projPath)!, "bin", "Release");
                Directory.CreateDirectory(outDir);
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"pack \"{projPath}\" -c Release -o \"{outDir}\"",
                    WorkingDirectory = repoRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(45000);

                var generated = Directory.EnumerateFiles(outDir, "SeaGlass.WinUI.*.nupkg", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                    .FirstOrDefault();
                if (generated != null) return generated;
            }
        }

        throw new FileNotFoundException("Could not locate or generate SeaGlass.WinUI.*.nupkg");
    }

    [Fact]
    public void Nupkg_Contains_GenericXaml_TokensXaml_And_All_8_Category_Dictionaries()
    {
        string nupkgPath = FindNupkgPath();
        using var zip = ZipFile.OpenRead(nupkgPath);

        var entryNames = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToList();

        // 1. Verify Generic.xaml is packaged
        Assert.Contains(entryNames, name => name.EndsWith("Themes/Generic.xaml", StringComparison.OrdinalIgnoreCase));

        // 2. Verify Tokens.xaml is packaged
        Assert.Contains(entryNames, name => name.EndsWith("Themes/Tokens.xaml", StringComparison.OrdinalIgnoreCase));

        // 3. Verify all 8 category XAML dictionaries are packaged
        foreach (string categoryDict in RequiredCategoryDictionaries)
        {
            Assert.Contains(entryNames, name => name.EndsWith($"Themes/{categoryDict}", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Nupkg_Nuspec_Declares_Win2D_Dependency()
    {
        string nupkgPath = FindNupkgPath();
        using var zip = ZipFile.OpenRead(nupkgPath);

        var nuspecEntry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(nuspecEntry);

        using var stream = nuspecEntry.Open();
        var doc = XDocument.Load(stream);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        var dependencies = doc.Descendants(ns + "dependency").ToList();
        var win2dDep = dependencies.FirstOrDefault(d => string.Equals((string?)d.Attribute("id"), "Microsoft.Graphics.Win2D", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(win2dDep);
        string? version = (string?)win2dDep.Attribute("version");
        Assert.False(string.IsNullOrWhiteSpace(version), "Win2D dependency version must be specified in .nuspec");
    }

    [Fact]
    public void Packaged_Assembly_Contains_All_58_Public_Controls()
    {
        string nupkgPath = FindNupkgPath();
        using var zip = ZipFile.OpenRead(nupkgPath);

        var dllEntry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith("SeaGlass.WinUI.dll", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(dllEntry);

        using var dllStream = dllEntry.Open();
        using var ms = new MemoryStream();
        dllStream.CopyTo(ms);
        ms.Position = 0;

        using var peReader = new PEReader(ms);
        var metadataReader = peReader.GetMetadataReader();

        var publicTypeNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in metadataReader.TypeDefinitions)
        {
            var typeDef = metadataReader.GetTypeDefinition(handle);
            if ((typeDef.Attributes & System.Reflection.TypeAttributes.Public) != 0)
            {
                string name = metadataReader.GetString(typeDef.Name);
                string ns = metadataReader.GetString(typeDef.Namespace);
                if (ns.StartsWith("SeaGlass.WinUI", StringComparison.Ordinal))
                {
                    publicTypeNames.Add(name);
                }
            }
        }

        Assert.Equal(58, Expected58ControlTypes.Length);

        var missingControls = new List<string>();
        foreach (string expectedControl in Expected58ControlTypes)
        {
            if (!publicTypeNames.Contains(expectedControl))
            {
                missingControls.Add(expectedControl);
            }
        }

        Assert.True(missingControls.Count == 0, $"Missing controls in packaged DLL: {string.Join(", ", missingControls)}");
    }

    [Fact]
    public void SeaGlassFluidBar_HeadlessVerification_Succeeds_When_XamlCore_STA_Thread_Is_Active()
    {
        Controls.WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassStageState stage = new();
            stage.BindBackdropTarget("RootBackdrop", new(0, 0), new(1920, 1080));

            foreach (SeaGlassPalette palette in SeaGlassPalette.Entries)
            {
                SeaGlassTheme.Current.SetPalette(palette);
                SeaGlassFluidBar bar = new(
                    ["Optics", "Palettes", "Fluid", "UI Kit"],
                    selectedIndex: 1,
                    onSelect: _ => { },
                    stageState: stage,
                    showActionOrb: true,
                    actionDetached: false);
                SeaGlassSurfaceFrameState frame = bar.StepAndEvaluate(1f / 60f);
                Assert.Equal(SeaGlassUniformBlock.ByteSize, frame.UniformBlock.CreateDirectStagingBuffer().Length);
            }
        });
    }
}


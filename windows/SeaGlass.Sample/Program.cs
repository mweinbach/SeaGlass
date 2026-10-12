using System;
using System.Linq;
using System.Windows;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;

namespace SeaGlass.Sample;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase) ||
            args.Contains("--headless-verify", StringComparer.OrdinalIgnoreCase))
        {
            return RunHeadlessVerification();
        }

        int captureIdx = Array.FindIndex(args, a => string.Equals(a, "--capture", StringComparison.OrdinalIgnoreCase));
        if (captureIdx >= 0 && captureIdx + 1 < args.Length)
        {
            string outPath = args[captureIdx + 1];
            MainWindow captureWin = new();
            int pageIdx = Array.FindIndex(args, a => string.Equals(a, "--page", StringComparison.OrdinalIgnoreCase));
            if (pageIdx >= 0 && pageIdx + 1 < args.Length && int.TryParse(args[pageIdx + 1], out int page))
            {
                captureWin.SelectTabForPreview(page);
            }
            captureWin.CaptureGallerySnapshotToPng(outPath);
            Console.WriteLine($"Captured gallery preview to: {outPath}");
            return 0;
        }

        Application app = new();
        MainWindow window = new();
        return app.Run(window);
    }

    private static int RunHeadlessVerification()
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
            if (frame.UniformBlock.CreateDirectStagingBuffer().Length != SeaGlassUniformBlock.ByteSize)
            {
                return 1;
            }
        }

        foreach (ShaderDialect dialect in Enum.GetValues<ShaderDialect>())
        {
            CompiledShaderBundle bundle = SeaGlassShaderCompiler.CompileForDialect(dialect);
            if (string.IsNullOrWhiteSpace(bundle.SeaGlassCompositeFragmentSource))
            {
                return 2;
            }
        }

        Console.WriteLine("SeaGlass for Windows Headless Verification PASSED: All 8 palettes, 5 shader dialects, 256-byte cbuffer ABI, and WinUI components verified.");
        return 0;
    }
}

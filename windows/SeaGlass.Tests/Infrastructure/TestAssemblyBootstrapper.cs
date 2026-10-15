using System.Runtime.CompilerServices;
using SeaGlass.Tests.Controls;
using SeaGlass.WinUI;

namespace SeaGlass.Tests.Infrastructure;

public static class TestAssemblyBootstrapper
{
    [ModuleInitializer]
    public static void Initialize()
    {
        try
        {
            SeaGlassTheme.RegisterDependencyProperties();
        }
        catch { }

        try
        {
            WinUiControlTestHarness.EnsureApplication();
        }
        catch { }
    }
}

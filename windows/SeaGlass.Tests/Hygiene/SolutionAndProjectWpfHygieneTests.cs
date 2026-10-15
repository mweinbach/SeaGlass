using System;
using System.IO;
using System.Linq;
using Xunit;

namespace SeaGlass.Tests.Hygiene;

public sealed class SolutionAndProjectWpfHygieneTests
{
    private static string FindWindowsRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "SeaGlass.sln")))
            {
                return dir;
            }
            if (File.Exists(Path.Combine(dir, "windows", "SeaGlass.sln")))
            {
                return Path.Combine(dir, "windows");
            }
            dir = Path.GetDirectoryName(dir);
        }

        string cur = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(cur, "SeaGlass.sln"))) return cur;
        if (File.Exists(Path.Combine(cur, "windows", "SeaGlass.sln"))) return Path.Combine(cur, "windows");

        throw new InvalidOperationException("Could not locate SeaGlass.sln from test base directory.");
    }

    [Fact]
    public void Assert_No_Project_In_Solution_Has_UseWPF_Flag()
    {
        string windowsRoot = FindWindowsRoot();
        string[] csprojFiles = Directory.GetFiles(windowsRoot, "*.csproj", SearchOption.AllDirectories);
        Assert.NotEmpty(csprojFiles);

        foreach (string csproj in csprojFiles)
        {
            string content = File.ReadAllText(csproj);
            Assert.DoesNotContain("<UseWPF>true</UseWPF>", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<UseWPF>", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Assert_No_Source_File_Imports_WPF_Namespaces()
    {
        string windowsRoot = FindWindowsRoot();
        string[] csFiles = Directory.GetFiles(windowsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
            .ToArray();

        Assert.NotEmpty(csFiles);

        string[] forbiddenWpfNamespaces =
        [
            "using System.Windows;",
            "using System.Windows.Controls;",
            "using System.Windows.Media;",
            "using System.Windows.Shapes;",
            "using System.Windows.Input;",
            "using System.Windows.Data;"
        ];

        foreach (string csFile in csFiles)
        {
            if (csFile.EndsWith("SolutionAndProjectWpfHygieneTests.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string content = File.ReadAllText(csFile);
            foreach (string forbidden in forbiddenWpfNamespaces)
            {
                Assert.False(
                    content.Contains(forbidden),
                    $"File '{Path.GetRelativePath(windowsRoot, csFile)}' illegally imports WPF namespace: '{forbidden}'");
            }
        }
    }

    [Fact]
    public void Assert_SeaGlassTests_Does_Not_Reference_SeaGlassSample()
    {
        string windowsRoot = FindWindowsRoot();
        string testCsproj = Path.Combine(windowsRoot, "SeaGlass.Tests", "SeaGlass.Tests.csproj");
        Assert.True(File.Exists(testCsproj), "SeaGlass.Tests.csproj must exist");

        string content = File.ReadAllText(testCsproj);
        // Any reference to SeaGlass.Sample must strictly be a build dependency (ReferenceOutputAssembly="false")
        // and must never link or consume SeaGlass.Sample as a compiled code/assembly reference.
        if (content.Contains("SeaGlass.Sample.csproj", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("ReferenceOutputAssembly=\"false\"", content, StringComparison.OrdinalIgnoreCase);
        }
    }
}

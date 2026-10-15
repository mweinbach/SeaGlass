using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SeaGlass.Tests.Infrastructure;

public sealed record ControlVerificationOutcome(
    string Name,
    int ResolvedParts,
    int ExpectedParts,
    string Status,
    string? Error);

public sealed record InteractionVerificationOutcome(
    string Name,
    string Status,
    string? Error);

public sealed class HostSelfTestResult
{
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = string.Empty;
    public string StandardError { get; init; } = string.Empty;
    public Dictionary<string, ControlVerificationOutcome> Controls { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, InteractionVerificationOutcome> Interactions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool PalettesAndOpticsPassed { get; init; }
}

public static class HostSelfTestRunner
{
    private static readonly Lazy<HostSelfTestResult> DefaultRunLazy = new(() => RunHost(canaryFail: false));

    public static HostSelfTestResult DefaultResult => DefaultRunLazy.Value;

    private static bool IsValidExecutable(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!File.Exists(path)) return false;
        string companionDll = Path.ChangeExtension(path, ".dll");
        return File.Exists(companionDll);
    }

    public static string LocateSampleExecutable()
    {
        // 1. Check environment variable override
        string? envExe = Environment.GetEnvironmentVariable("SEAGLASS_SAMPLE_EXE");
        if (!string.IsNullOrWhiteSpace(envExe) && IsValidExecutable(envExe))
        {
            return Path.GetFullPath(envExe);
        }

        string baseDir = AppContext.BaseDirectory;
        string currentDir = Directory.GetCurrentDirectory();

        bool isRelease = baseDir.IndexOf("release", StringComparison.OrdinalIgnoreCase) >= 0;
        string preferredConfig = isRelease ? "Release" : "Debug";
        string fallbackConfig = isRelease ? "Debug" : "Release";
        string[] configs = [preferredConfig, fallbackConfig, preferredConfig.ToLowerInvariant(), fallbackConfig.ToLowerInvariant()];

        var candidates = new List<string>();
        void AddCandidate(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                string full = Path.GetFullPath(path);
                if (!candidates.Contains(full, StringComparer.OrdinalIgnoreCase))
                {
                    candidates.Add(full);
                }
            }
            catch
            {
                // Ignore invalid path syntax
            }
        }

        // 2. Direct probe in AppContext.BaseDirectory and immediate siblings
        AddCandidate(Path.Combine(baseDir, "SeaGlass.Sample.exe"));

        // Direct substitution in BaseDirectory (works for both standard bin/ and --artifacts-path outputs)
        string sampleFromBase = Regex.Replace(baseDir, "SeaGlass\\.Tests", "SeaGlass.Sample", RegexOptions.IgnoreCase);
        AddCandidate(Path.Combine(sampleFromBase, "SeaGlass.Sample.exe"));
        AddCandidate(Path.Combine(Regex.Replace(sampleFromBase, "debug", "release", RegexOptions.IgnoreCase), "SeaGlass.Sample.exe"));
        AddCandidate(Path.Combine(Regex.Replace(sampleFromBase, "release", "debug", RegexOptions.IgnoreCase), "SeaGlass.Sample.exe"));

        // 3. Probes walking up the directory hierarchy from BaseDirectory and CurrentDirectory
        string[] rootDirs = [baseDir, currentDir];
        foreach (string startDir in rootDirs)
        {
            DirectoryInfo? dirInfo = null;
            try { dirInfo = new DirectoryInfo(startDir); } catch { }

            int depth = 0;
            while (dirInfo != null && depth < 8)
            {
                string dir = dirInfo.FullName;

                AddCandidate(Path.Combine(dir, "SeaGlass.Sample.exe"));

                foreach (string cfg in configs)
                {
                    // Standard build outputs
                    AddCandidate(Path.Combine(dir, "SeaGlass.Sample", "bin", cfg, "net10.0-windows10.0.26100.0", "SeaGlass.Sample.exe"));
                    AddCandidate(Path.Combine(dir, "SeaGlass.Sample", "bin", cfg, "net10.0-windows", "SeaGlass.Sample.exe"));
                    AddCandidate(Path.Combine(dir, "SeaGlass.Sample", "bin", cfg, "SeaGlass.Sample.exe"));
                    AddCandidate(Path.Combine(dir, "windows", "SeaGlass.Sample", "bin", cfg, "net10.0-windows10.0.26100.0", "SeaGlass.Sample.exe"));
                    AddCandidate(Path.Combine(dir, "windows", "SeaGlass.Sample", "bin", cfg, "net10.0-windows", "SeaGlass.Sample.exe"));
                    AddCandidate(Path.Combine(dir, "windows", "SeaGlass.Sample", "bin", cfg, "SeaGlass.Sample.exe"));

                    // Modern .NET artifacts-path outputs (e.g. artifacts/bin/SeaGlass.Sample/debug/...)
                    AddCandidate(Path.Combine(dir, "bin", "SeaGlass.Sample", cfg, "SeaGlass.Sample.exe"));
                    AddCandidate(Path.Combine(dir, "bin", "SeaGlass.Sample", "SeaGlass.Sample.exe"));
                    AddCandidate(Path.Combine(dir, "artifacts", "bin", "SeaGlass.Sample", cfg, "SeaGlass.Sample.exe"));
                    AddCandidate(Path.Combine(dir, "artifacts", "bin", "SeaGlass.Sample", "SeaGlass.Sample.exe"));
                    AddCandidate(Path.Combine(dir, "artifacts", "test-isolated", "bin", "SeaGlass.Sample", cfg, "SeaGlass.Sample.exe"));
                    AddCandidate(Path.Combine(dir, "artifacts", "test-isolated", "bin", "SeaGlass.Sample", "SeaGlass.Sample.exe"));
                }

                dirInfo = dirInfo.Parent;
                depth++;
            }
        }

        // Test each prioritized candidate - MUST have companion .dll
        foreach (string candidate in candidates)
        {
            if (IsValidExecutable(candidate))
            {
                return candidate;
            }
        }

        // 4. Check any artifacts folder recursively
        foreach (string startDir in rootDirs)
        {
            DirectoryInfo? dirInfo = null;
            try { dirInfo = new DirectoryInfo(startDir); } catch { }

            int depth = 0;
            while (dirInfo != null && depth < 8)
            {
                string artifactsCandidate = Path.Combine(dirInfo.FullName, "artifacts");
                if (Directory.Exists(artifactsCandidate))
                {
                    try
                    {
                        var foundInArtifacts = Directory.EnumerateFiles(artifactsCandidate, "SeaGlass.Sample.exe", SearchOption.AllDirectories)
                            .Where(IsValidExecutable)
                            .Select(p => new FileInfo(p))
                            .OrderByDescending(f => f.FullName.IndexOf(preferredConfig, StringComparison.OrdinalIgnoreCase) >= 0)
                            .ThenByDescending(f => f.LastWriteTimeUtc)
                            .FirstOrDefault();

                        if (foundInArtifacts != null && IsValidExecutable(foundInArtifacts.FullName))
                        {
                            return foundInArtifacts.FullName;
                        }
                    }
                    catch
                    {
                        // Ignore traversal exceptions
                    }
                }

                dirInfo = dirInfo.Parent;
                depth++;
            }
        }

        // 5. Fallback: search anywhere under the repository's windows/ tree
        foreach (string startDir in rootDirs)
        {
            DirectoryInfo? dirInfo = null;
            try { dirInfo = new DirectoryInfo(startDir); } catch { }

            int depth = 0;
            while (dirInfo != null && depth < 8)
            {
                string windowsDir = Path.Combine(dirInfo.FullName, "windows");
                string searchDir = Directory.Exists(windowsDir) ? windowsDir : (File.Exists(Path.Combine(dirInfo.FullName, "SeaGlass.sln")) ? dirInfo.FullName : string.Empty);

                if (!string.IsNullOrEmpty(searchDir) && Directory.Exists(searchDir))
                {
                    try
                    {
                        var foundUnderRepo = Directory.EnumerateFiles(searchDir, "SeaGlass.Sample.exe", SearchOption.AllDirectories)
                            .Where(p => !p.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar) && IsValidExecutable(p))
                            .Select(p => new FileInfo(p))
                            .OrderByDescending(f => f.FullName.IndexOf(preferredConfig, StringComparison.OrdinalIgnoreCase) >= 0)
                            .ThenByDescending(f => f.LastWriteTimeUtc)
                            .FirstOrDefault();

                        if (foundUnderRepo != null && IsValidExecutable(foundUnderRepo.FullName))
                        {
                            return foundUnderRepo.FullName;
                        }
                    }
                    catch
                    {
                        // Ignore traversal exceptions
                    }
                }

                dirInfo = dirInfo.Parent;
                depth++;
            }
        }

        throw new FileNotFoundException($"Could not locate valid SeaGlass.Sample.exe (with companion .dll). Checked candidates ({candidates.Count}): {string.Join("; ", candidates.Take(12))}...");
    }

    public static HostSelfTestResult RunHost(bool canaryFail = false)
    {
        string exePath = LocateSampleExecutable();
        string args = canaryFail ? "--self-test --canary-fail" : "--self-test";

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {exePath}");

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(45000);

        var result = new HostSelfTestResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout,
            StandardError = stderr,
            PalettesAndOpticsPassed = stdout.Contains("SeaGlass for Windows Headless Verification PASSED")
        };

        using var reader = new StringReader(stdout);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith("CONTROL: ", StringComparison.OrdinalIgnoreCase))
            {
                ParseControlLine(line, result.Controls);
            }
            else if (line.StartsWith("INTERACTION: ", StringComparison.OrdinalIgnoreCase))
            {
                ParseInteractionLine(line, result.Interactions);
            }
        }

        return result;
    }

    private static void ParseControlLine(string line, Dictionary<string, ControlVerificationOutcome> dict)
    {
        string[] parts = line.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length < 3) return;

        string name = parts[0].Substring("CONTROL:".Length).Trim();
        string resolvedStr = parts[1].Substring("PARTS_RESOLVED:".Length).Trim();
        string status = parts[2].Substring("STATUS:".Length).Trim();
        string? error = parts.Length > 3 && parts[3].StartsWith("ERROR:") ? parts[3].Substring("ERROR:".Length).Trim() : null;

        int resolved = 0;
        int expected = 0;
        string[] ratio = resolvedStr.Split('/');
        if (ratio.Length == 2 && int.TryParse(ratio[0], out int r) && int.TryParse(ratio[1], out int exp))
        {
            resolved = r;
            expected = exp;
        }

        dict[name] = new ControlVerificationOutcome(name, resolved, expected, status, error);
    }

    private static void ParseInteractionLine(string line, Dictionary<string, InteractionVerificationOutcome> dict)
    {
        string[] parts = line.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return;

        string name = parts[0].Substring("INTERACTION:".Length).Trim();
        string status = parts[1].Substring("STATUS:".Length).Trim();
        string? error = parts.Length > 2 && parts[2].StartsWith("ERROR:") ? parts[2].Substring("ERROR:".Length).Trim() : null;

        dict[name] = new InteractionVerificationOutcome(name, status, error);
    }
}

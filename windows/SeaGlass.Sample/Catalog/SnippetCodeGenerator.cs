using System;
using System.Text;
using SeaGlass.Sample.Common;

namespace SeaGlass.Sample.Catalog;

/// <summary>
/// Dynamic code snippet generator emitting synchronized XAML and Fluent C# syntax
/// based on active tuning parameters.
/// </summary>
public static class SnippetCodeGenerator
{
    public static string GenerateXaml(ControlShowcaseDescriptor descriptor, TuningViewModel tuning)
    {
        if (descriptor.CustomXamlSnippet != null)
            return descriptor.CustomXamlSnippet(tuning);

        var sb = new StringBuilder();
        sb.AppendLine($"<sg:{descriptor.Name}");
        sb.AppendLine($"    Tone=\"{tuning.Tone}\"");
        sb.AppendLine($"    Variant=\"{tuning.Variant}\"");
        sb.AppendLine($"    Size=\"{tuning.Size}\"");
        sb.AppendLine($"    Coarseness=\"{tuning.Coarseness:F2}\"");
        sb.AppendLine($"    ThicknessDp=\"{tuning.ThicknessDp:F1}\"");
        sb.AppendLine($"    Ior=\"{tuning.Ior:F2}\"");
        sb.AppendLine($"    RimChalkiness=\"{tuning.RimChalkiness:F2}\"");
        sb.AppendLine($"    TintDensity=\"{tuning.TintDensity:F2}\" />");
        return sb.ToString().TrimEnd();
    }

    public static string GenerateFluent(ControlShowcaseDescriptor descriptor, TuningViewModel tuning)
    {
        if (descriptor.CustomFluentSnippet != null)
            return descriptor.CustomFluentSnippet(tuning);

        var sb = new StringBuilder();
        sb.AppendLine("using SeaGlass.WinUI;");
        sb.AppendLine("using SeaGlass.Core;");
        sb.AppendLine();
        sb.AppendLine($"var element = new {descriptor.Name}()");
        sb.AppendLine($"    .Tone(SeaGlassTone.{tuning.Tone})");
        sb.AppendLine($"    .Variant(SeaGlassVariant.{tuning.Variant})");
        sb.AppendLine($"    .Size(SeaGlassControlSize.{tuning.Size})");
        sb.AppendLine($"    .Coarseness({tuning.Coarseness:F2})");
        sb.AppendLine($"    .ThicknessDp({tuning.ThicknessDp:F1})");
        sb.AppendLine($"    .Ior({tuning.Ior:F2})");
        sb.AppendLine($"    .RimChalkiness({tuning.RimChalkiness:F2})");
        sb.AppendLine($"    .TintDensity({tuning.TintDensity:F2});");
        return sb.ToString().TrimEnd();
    }
}

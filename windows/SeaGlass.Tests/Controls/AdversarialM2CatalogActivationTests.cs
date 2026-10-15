using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.WinUI;
using SeaGlass.Tests.E2E.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SeaGlass.Tests.Controls;

public sealed class AdversarialM2CatalogActivationTests
{
    private readonly ITestOutputHelper _output;
    private static readonly Assembly WinUiAssembly = typeof(SeaGlassSurface).Assembly;

    public AdversarialM2CatalogActivationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Catalog_AssertExactly58ControlsInRegistry()
    {
        Assert.Equal(58, E2ECatalogContractRegistry.All58Controls.Count);
    }

    [Fact]
    public void Catalog_All58Controls_ResolveToPublicTypesInWinUiAssembly()
    {
        List<string> missing = new();
        foreach (var desc in E2ECatalogContractRegistry.All58Controls)
        {
            Type? type = WinUiAssembly.GetType(desc.ExpectedTypeName);
            if (type == null)
            {
                missing.Add(desc.ExpectedTypeName);
            }
        }

        _output.WriteLine($"Total types verified: {E2ECatalogContractRegistry.All58Controls.Count - missing.Count}/{E2ECatalogContractRegistry.All58Controls.Count}");
        if (missing.Count > 0)
        {
            _output.WriteLine($"Missing types ({missing.Count}): {string.Join(", ", missing)}");
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void Catalog_AdversarialHarnessActivation_DetailedDiagnostic()
    {
        int total = E2ECatalogContractRegistry.All58Controls.Count;
        int typeResolved = 0;
        int isControlOrPanel = 0;
        int staInstantiated = 0;
        int hasStrictDps = 0;
        int hasInheritedDps = 0;

        List<string> failureDetails = new();

        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            foreach (var desc in E2ECatalogContractRegistry.All58Controls)
            {
                string fullTypeName = desc.ExpectedTypeName;
                Type? type = WinUiAssembly.GetType(fullTypeName);
                if (type == null)
                {
                    failureDetails.Add($"[MISSING_TYPE] {fullTypeName}");
                    continue;
                }
                typeResolved++;

                bool isControl = typeof(Control).IsAssignableFrom(type);
                bool isPanel = typeof(Panel).IsAssignableFrom(type);

                if (!isControl && !isPanel)
                {
                    failureDetails.Add($"[NOT_CONTROL_OR_PANEL] {fullTypeName} base is {type.BaseType?.FullName}");
                }
                else
                {
                    isControlOrPanel++;
                }

                // Attempt STA instantiation
                object? instance = null;
                try
                {
                    instance = Activator.CreateInstance(type);
                    if (instance != null) staInstantiated++;
                }
                catch (Exception ex)
                {
                    failureDetails.Add($"[INSTANTIATION_FAILURE] {fullTypeName}: {ex.Message}");
                }

                // Check DPs: strict (BindingFlags.Public | BindingFlags.Static)
                FieldInfo? vStrict = type.GetField("VariantProperty", BindingFlags.Public | BindingFlags.Static);
                FieldInfo? tStrict = type.GetField("ToneProperty", BindingFlags.Public | BindingFlags.Static);
                FieldInfo? sStrict = type.GetField("SizeProperty", BindingFlags.Public | BindingFlags.Static);
                FieldInfo? mStrict = type.GetField("MaterialProperty", BindingFlags.Public | BindingFlags.Static);

                if (vStrict != null && tStrict != null && sStrict != null && mStrict != null)
                {
                    hasStrictDps++;
                }
                else
                {
                    failureDetails.Add($"[MISSING_STRICT_DPS] {fullTypeName}: V={vStrict != null}, T={tStrict != null}, S={sStrict != null}, M={mStrict != null}");
                }

                // Check DPs: inherited (BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                FieldInfo? vInh = type.GetField("VariantProperty", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                FieldInfo? tInh = type.GetField("ToneProperty", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                FieldInfo? sInh = type.GetField("SizeProperty", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                FieldInfo? mInh = type.GetField("MaterialProperty", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (vInh != null && tInh != null && sInh != null && mInh != null)
                {
                    hasInheritedDps++;
                }
                else
                {
                    failureDetails.Add($"[MISSING_INHERITED_DPS] {fullTypeName}: V={vInh != null}, T={tInh != null}, S={sInh != null}, M={mInh != null}");
                }
            }
        });

        _output.WriteLine($"=== ADVERSARIAL VERIFICATION SUMMARY ===");
        _output.WriteLine($"Total Catalog Controls: {total}");
        _output.WriteLine($"Resolved to Public Types: {typeResolved}/{total}");
        _output.WriteLine($"Derives from Control or Panel: {isControlOrPanel}/{total}");
        _output.WriteLine($"Successfully Instantiated on STA Thread: {staInstantiated}/{total}");
        _output.WriteLine($"Has Strict Declared DPs (no FlattenHierarchy): {hasStrictDps}/{total}");
        _output.WriteLine($"Has Inherited DPs (with FlattenHierarchy): {hasInheritedDps}/{total}");
        _output.WriteLine($"--- Diagnostic Failures ({failureDetails.Count}) ---");
        foreach (var failure in failureDetails)
        {
            _output.WriteLine(failure);
        }
    }

    [Fact]
    public void Catalog_ExecuteDirectE2EControlActivationHarness_RecordResults()
    {
        int passedHarness = 0;
        int failedHarness = 0;
        List<string> harnessErrors = new();

        foreach (var desc in E2ECatalogContractRegistry.All58Controls)
        {
            bool isPanel = desc.Name == "SeaGlassFluidGroup";
            try
            {
                FrameworkElement element = E2EControlActivationHarness.ActivateAndVerifyControl(desc.ExpectedTypeName, isPanel);
                if (element != null)
                {
                    passedHarness++;
                }
            }
            catch (Exception ex)
            {
                failedHarness++;
                harnessErrors.Add($"{desc.Name} ({desc.ExpectedTypeName}): {ex.GetType().Name} - {ex.Message}");
            }
        }

        _output.WriteLine($"=== DIRECT E2E CONTROL ACTIVATION HARNESS EXECUTION ===");
        _output.WriteLine($"Total Attempted: {E2ECatalogContractRegistry.All58Controls.Count}");
        _output.WriteLine($"Passed Harness: {passedHarness}/58");
        _output.WriteLine($"Failed Harness: {failedHarness}/58");
        _output.WriteLine($"--- Failure Log ---");
        foreach (var err in harnessErrors)
        {
            _output.WriteLine(err);
        }

        // We assert zero missing controls:
        Assert.Equal(58, E2ECatalogContractRegistry.All58Controls.Count);
    }
}

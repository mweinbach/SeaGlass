using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.WinUI;
using SeaGlass.Tests.Controls;
using Xunit;

namespace SeaGlass.Tests.E2E.Infrastructure;

public static class E2EControlActivationHarness
{
    private static readonly Assembly WinUiAssembly = typeof(SeaGlassTheme).Assembly;
    private static bool _themeResourcesMerged;

    /// <summary>
    /// Guarantees that SeaGlassThemeResources is merged into Application.Current.Resources on the STA thread.
    /// </summary>
    public static void EnsureThemeResourcesMerged()
    {
        if (_themeResourcesMerged) return;
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            if (Application.Current != null)
            {
                bool exists = Application.Current.Resources.MergedDictionaries
                    .Any(d => d is SeaGlassThemeResources || d.Source?.OriginalString?.Contains("Generic.xaml") == true);
                if (!exists)
                {
                    Application.Current.Resources.MergedDictionaries.Add(new SeaGlassThemeResources());
                }
                _themeResourcesMerged = true;
            }
        });
    }

    /// <summary>
    /// Invokes protected Control.GetTemplateChild via reflection.
    /// </summary>
    public static DependencyObject? GetTemplateChildSafe(this Control control, string childName)
    {
        MethodInfo? method = typeof(Control).GetMethod("GetTemplateChild", BindingFlags.NonPublic | BindingFlags.Instance);
        return method?.Invoke(control, new object[] { childName }) as DependencyObject;
    }

    /// <summary>
    /// Instantiates a control on the STA thread, ensures theme resources are merged,
    /// applies its control template, asserts ALL declared [TemplatePart] attributes,
    /// and verifies DependencyProperties.
    /// </summary>
    public static FrameworkElement ActivateAndVerifyControl(
        string fullTypeName,
        bool isPanel = false)
    {
        Type? controlType = WinUiAssembly.GetType(fullTypeName);
        Assert.True(controlType != null,
            $"CRITICAL ACCEPTANCE FAILURE: Public WinUI control type '{fullTypeName}' is missing from SeaGlass.WinUI assembly!");

        if (isPanel)
        {
            Assert.True(typeof(Panel).IsAssignableFrom(controlType),
                $"Control '{fullTypeName}' must inherit from Microsoft.UI.Xaml.Controls.Panel");
        }
        else
        {
            Assert.True(typeof(Control).IsAssignableFrom(controlType),
                $"Control '{fullTypeName}' must inherit from Microsoft.UI.Xaml.Controls.Control");
        }

        FrameworkElement? instance = null;
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            EnsureThemeResourcesMerged();

            instance = Activator.CreateInstance(controlType) as FrameworkElement;
            Assert.NotNull(instance);

            if (instance is Control ctrl)
            {
                // Verify standard styling DPs
                BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                FieldInfo? variantDp = controlType.GetField("VariantProperty", flags);
                FieldInfo? toneDp = controlType.GetField("ToneProperty", flags);
                FieldInfo? sizeDp = controlType.GetField("SizeProperty", flags);
                FieldInfo? materialDp = controlType.GetField("MaterialProperty", flags);

                Assert.True(variantDp != null, $"Control '{fullTypeName}' must expose public static DependencyProperty VariantProperty");
                Assert.True(toneDp != null, $"Control '{fullTypeName}' must expose public static DependencyProperty ToneProperty");
                Assert.True(sizeDp != null, $"Control '{fullTypeName}' must expose public static DependencyProperty SizeProperty");
                Assert.True(materialDp != null, $"Control '{fullTypeName}' must expose public static DependencyProperty MaterialProperty");

                // Layout pass to ensure style resolution and template inflation
                ctrl.Measure(new Windows.Foundation.Size(400, 300));
                ctrl.ApplyTemplate();

                // Inspect all declared [TemplatePart] attributes
                var templatePartAttrs = controlType.GetCustomAttributes(typeof(TemplatePartAttribute), inherit: true)
                    .Cast<TemplatePartAttribute>()
                    .ToList();

                // Verify each declared part
                foreach (var partAttr in templatePartAttrs)
                {
                    DependencyObject? child = ctrl.GetTemplateChildSafe(partAttr.Name);
                    Assert.True(child != null,
                        $"CRITICAL TEMPLATE PART FAILURE: Declared [TemplatePart(Name = \"{partAttr.Name}\", Type = typeof({partAttr.Type.Name}))] on control '{fullTypeName}' returned NULL after ApplyTemplate()! The control template in Themes/ is a stub missing this required part.");

                    Assert.True(partAttr.Type.IsAssignableFrom(child.GetType()),
                        $"CRITICAL TEMPLATE PART TYPE MISMATCH: Template part '{partAttr.Name}' on '{fullTypeName}' returned instance of '{child.GetType().Name}', which is not assignable to declared type '{partAttr.Type.Name}'.");
                }
            }
            else if (instance is Panel panel)
            {
                // For panels like SeaGlassFluidGroup, verify layout overrides and children capability
                Border testChild = new() { Width = 50, Height = 50 };
                panel.Children.Add(testChild);
                panel.Measure(new Windows.Foundation.Size(200, 200));
                panel.Arrange(new Windows.Foundation.Rect(0, 0, 200, 200));
                Assert.Single(panel.Children);
            }
        });

        return instance!;
    }

    public static T ActivateAndVerifyControl<T>(bool isPanel = false) where T : FrameworkElement
    {
        return (T)ActivateAndVerifyControl(typeof(T).FullName ?? typeof(T).Name, isPanel);
    }
}

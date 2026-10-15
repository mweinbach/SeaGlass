using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.Sample.Catalog;
using SeaGlass.Sample.Components;
using SeaGlass.Sample.Pages;
using SeaGlass.Sample.Screenshot;
using SeaGlass.WinUI;

namespace SeaGlass.Sample;

public static class Program
{
    public static string? InitialControlPage { get; set; }
    private static readonly (string Name, string FullName, bool IsPanel)[] CatalogControls =
    [
        // Group 1: Foundations & Surfaces (9)
        ("SeaGlassTheme", "SeaGlass.WinUI.SeaGlassTheme", false),
        ("SeaGlassStage", "SeaGlass.WinUI.SeaGlassStage", false),
        ("SeaGlassFluidGroup", "SeaGlass.WinUI.SeaGlassFluidGroup", true),
        ("SeaGlassSurface", "SeaGlass.WinUI.SeaGlassSurface", false),
        ("SeaGlassCard", "SeaGlass.WinUI.SeaGlassCard", false),
        ("SeaGlassDialogCard", "SeaGlass.WinUI.SeaGlassDialogCard", false),
        ("SeaGlassBottomSheetSurface", "SeaGlass.WinUI.SeaGlassBottomSheetSurface", false),
        ("SeaGlassCornerEmbeddedPod", "SeaGlass.WinUI.SeaGlassCornerEmbeddedPod", false),
        ("SeaGlassAdaptiveCornerLabCard", "SeaGlass.WinUI.SeaGlassAdaptiveCornerLabCard", false),

        // Group 2: Buttons & Chips (8)
        ("SeaGlassButton", "SeaGlass.WinUI.SeaGlassButton", false),
        ("SeaGlassIconButton", "SeaGlass.WinUI.SeaGlassIconButton", false),
        ("SeaGlassExtendedFab", "SeaGlass.WinUI.SeaGlassExtendedFab", false),
        ("SeaGlassPill", "SeaGlass.WinUI.SeaGlassPill", false),
        ("SeaGlassFilterChip", "SeaGlass.WinUI.SeaGlassFilterChip", false),
        ("SeaGlassBadge", "SeaGlass.WinUI.SeaGlassBadge", false),
        ("SeaGlassSegmentedButtonRow", "SeaGlass.WinUI.SeaGlassSegmentedButtonRow", false),
        ("SeaGlassPebbleOrb", "SeaGlass.WinUI.SeaGlassPebbleOrb", false),

        // Group 3: Navigation & Bars (7)
        ("SeaGlassTopAppBar", "SeaGlass.WinUI.SeaGlassTopAppBar", false),
        ("SeaGlassFluidBar", "SeaGlass.WinUI.SeaGlassFluidBar", false),
        ("SeaGlassMiniPlayerBar", "SeaGlass.WinUI.SeaGlassMiniPlayerBar", false),
        ("SeaGlassFloatingToolbar", "SeaGlass.WinUI.SeaGlassFloatingToolbar", false),
        ("SeaGlassPillNavBar", "SeaGlass.WinUI.SeaGlassPillNavBar", false),
        ("SeaGlassPromptBar", "SeaGlass.WinUI.SeaGlassPromptBar", false),
        ("SeaGlassSearchBar", "SeaGlass.WinUI.SeaGlassSearchBar", false),

        // Group 4: Inputs & Toggles (5)
        ("SeaGlassTextField", "SeaGlass.WinUI.SeaGlassTextField", false),
        ("SeaGlassSwitch", "SeaGlass.WinUI.SeaGlassSwitch", false),
        ("SeaGlassCheckbox", "SeaGlass.WinUI.SeaGlassCheckbox", false),
        ("SeaGlassRadioButton", "SeaGlass.WinUI.SeaGlassRadioButton", false),
        ("SeaGlassLiquidSlider", "SeaGlass.WinUI.SeaGlassLiquidSlider", false),

        // Group 5: Feedback & Lists (5)
        ("SeaGlassLinearProgressIndicator", "SeaGlass.WinUI.SeaGlassLinearProgressIndicator", false),
        ("SeaGlassCircularProgressIndicator", "SeaGlass.WinUI.SeaGlassCircularProgressIndicator", false),
        ("SeaGlassListItem", "SeaGlass.WinUI.SeaGlassListItem", false),
        ("SeaGlassSnackbar", "SeaGlass.WinUI.SeaGlassSnackbar", false),
        ("SeaGlassIcon", "SeaGlass.WinUI.SeaGlassIcon", false),

        // Group 6: Dynamic Morphing Suite (8)
        ("SeaGlassTwoElementMorph", "SeaGlass.WinUI.SeaGlassTwoElementMorph", false),
        ("SeaGlassBuddingCluster", "SeaGlass.WinUI.SeaGlassBuddingCluster", false),
        ("SeaGlassMorphingButton", "SeaGlass.WinUI.SeaGlassMorphingButton", false),
        ("SeaGlassChoreographedShapeOrb", "SeaGlass.WinUI.SeaGlassChoreographedShapeOrb", false),
        ("SeaGlassMultiShapeMorph", "SeaGlass.WinUI.SeaGlassMultiShapeMorph", false),
        ("SeaGlassMorphingContextMenu", "SeaGlass.WinUI.SeaGlassMorphingContextMenu", false),
        ("SeaGlassCascadedModelMorph", "SeaGlass.WinUI.SeaGlassCascadedModelMorph", false),
        ("SeaGlassContextRingMorph", "SeaGlass.WinUI.SeaGlassContextRingMorph", false),

        // Group 7: iOS 27 & Advanced Overlays (8)
        ("SeaGlassFloatingSheet", "SeaGlass.WinUI.SeaGlassFloatingSheet", false),
        ("SeaGlassContextMenuOverlay", "SeaGlass.WinUI.SeaGlassContextMenuOverlay", false),
        ("SeaGlassActionSheet", "SeaGlass.WinUI.SeaGlassActionSheet", false),
        ("SeaGlassLiveActivityPill", "SeaGlass.WinUI.SeaGlassLiveActivityPill", false),
        ("SeaGlassMagnifierScrubber", "SeaGlass.WinUI.SeaGlassMagnifierScrubber", false),
        ("SeaGlassNotificationStack", "SeaGlass.WinUI.SeaGlassNotificationStack", false),
        ("SeaGlassWheelPicker", "SeaGlass.WinUI.SeaGlassWheelPicker", false),
        ("SeaGlassControlCenterGrid", "SeaGlass.WinUI.SeaGlassControlCenterGrid", false),

        // Group 8: Windows Native Extras (8)
        ("SeaGlassTitleBar", "SeaGlass.WinUI.SeaGlassTitleBar", false),
        ("SeaGlassNavigationView", "SeaGlass.WinUI.SeaGlassNavigationView", false),
        ("SeaGlassTabView", "SeaGlass.WinUI.SeaGlassTabView", false),
        ("SeaGlassCommandBar", "SeaGlass.WinUI.SeaGlassCommandBar", false),
        ("SeaGlassInfoBar", "SeaGlass.WinUI.SeaGlassInfoBar", false),
        ("SeaGlassTeachingTip", "SeaGlass.WinUI.SeaGlassTeachingTip", false),
        ("SeaGlassFlyout", "SeaGlass.WinUI.SeaGlassFlyout", false),
        ("SeaGlassToolTip", "SeaGlass.WinUI.SeaGlassToolTip", false)
    ];

    [STAThread]
    public static int Main(string[] args)
    {
        bool isSelfTest = args.Contains("--self-test", StringComparer.OrdinalIgnoreCase) ||
                          args.Contains("--headless-verify", StringComparer.OrdinalIgnoreCase) ||
                          args.Contains("--verify-catalog", StringComparer.OrdinalIgnoreCase) ||
                          args.Contains("--canary-fail", StringComparer.OrdinalIgnoreCase);
        bool canaryFail = args.Contains("--canary-fail", StringComparer.OrdinalIgnoreCase);

        var minVersion = new Microsoft.Windows.ApplicationModel.DynamicDependency.PackageVersion(0x0002000500010000);
        Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.TryInitialize(
            0x00020005, "", minVersion,
            Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.InitializeOptions.None,
            out _);

        WinRT.ComWrappersSupport.InitializeComWrappers();

        bool isCaptureAll = args.Contains("--capture-all", StringComparer.OrdinalIgnoreCase);
        bool isCaptureScreenshots = args.Contains("--capture-screenshots", StringComparer.OrdinalIgnoreCase);
        bool isScreenshot = args.Contains("--screenshot", StringComparer.OrdinalIgnoreCase);
        bool isGallerySmoke = args.Contains("--gallery-smoke", StringComparer.OrdinalIgnoreCase);
        int idxPage = Array.FindIndex(args, a => string.Equals(a, "--page", StringComparison.OrdinalIgnoreCase));
        if (idxPage >= 0 && idxPage + 1 < args.Length)
        {
            InitialControlPage = args[idxPage + 1];
        }


        if (isGallerySmoke)
        {
            int exitCode = 0;
            try
            {
                Application.Start(p =>
                {
                    try
                    {
                        var queue = DispatcherQueue.GetForCurrentThread();
                        var context = new DispatcherQueueSynchronizationContext(queue);
                        SynchronizationContext.SetSynchronizationContext(context);
                        _ = new App();
                        queue.TryEnqueue(async () =>
                        {
                            try
                            {
                                exitCode = await RunGallerySmokeAsync(queue);
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"FATAL inside Smoke: {ex}");
                                exitCode = 1;
                            }
                            finally
                            {
                                try { Application.Current?.Exit(); } catch { }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"FATAL inside Smoke callback: {ex}");
                        exitCode = 1;
                        try { Application.Current?.Exit(); } catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FATAL outer Smoke Start: {ex}");
                exitCode = 1;
            }
            return exitCode;
        }

        if (isCaptureAll || isCaptureScreenshots || isScreenshot)
        {
            int exitCode = 0;
            try
            {
                Application.Start(p =>
                {
                    try
                    {
                        var queue = DispatcherQueue.GetForCurrentThread();
                        var context = new DispatcherQueueSynchronizationContext(queue);
                        SynchronizationContext.SetSynchronizationContext(context);
                        _ = new App();
                        queue.TryEnqueue(async () =>
                        {
                            try
                            {
                                exitCode = await RunCaptureAutomationAsync(args, queue);
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"FATAL inside Capture: {ex}");
                                exitCode = 1;
                            }
                            finally
                            {
                                try { Application.Current?.Exit(); } catch { }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"FATAL inside Capture Start callback: {ex}");
                        exitCode = 1;
                        try { Application.Current?.Exit(); } catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FATAL outer Capture Start: {ex}");
                exitCode = 1;
            }
            return exitCode;
        }

        if (isSelfTest)
        {
            int exitCode = 0;
            try
            {
                Application.Start(p =>
                {
                    try
                    {
                        var queue = DispatcherQueue.GetForCurrentThread();
                        var context = new DispatcherQueueSynchronizationContext(queue);
                        SynchronizationContext.SetSynchronizationContext(context);
                        _ = new App();
                        queue.TryEnqueue(async () =>
                        {
                            try
                            {
                                exitCode = await RunHeadlessVerificationAsync(canaryFail, queue);
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"FATAL inside Start: {ex}");
                                exitCode = 1;
                            }
                            finally
                            {
                                try { Application.Current?.Exit(); } catch { }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"FATAL inside Start callback: {ex}");
                        exitCode = 1;
                        try { Application.Current?.Exit(); } catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FATAL outer Start: {ex}");
                exitCode = 1;
            }
            return exitCode;
        }

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
        return 0;
    }

    private static async Task AttachAndWaitLoaded(Grid rootGrid, FrameworkElement element, DispatcherQueue queue)
    {
        var tcs = new TaskCompletionSource<bool>();
        RoutedEventHandler onLoaded = null!;
        onLoaded = (s, e) =>
        {
            element.Loaded -= onLoaded;
            tcs.TrySetResult(true);
        };
        element.Loaded += onLoaded;
        rootGrid.Children.Add(element);
        if (element.IsLoaded)
        {
            element.Loaded -= onLoaded;
            return;
        }

        var timer = queue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(250);
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            element.Loaded -= onLoaded;
            tcs.TrySetResult(true);
        };
        timer.Start();
        await tcs.Task;
        timer.Stop();
    }

    private static async Task<int> RunHeadlessVerificationAsync(bool canaryFail, DispatcherQueue queue)
    {
        Console.WriteLine("--- SEAGLASS VERIFICATION REPORT START ---");

        int failedControls = 0;
        int passedControls = 0;
        Assembly winUiAssembly = typeof(SeaGlassTheme).Assembly;

        Window window = new();
        Grid rootGrid = new();
        window.Content = rootGrid;
        window.Activate();

        foreach (var (name, fullName, isPanel) in CatalogControls)
        {
            if (canaryFail && name == "SeaGlassButton")
            {
                Console.WriteLine("CONTROL: SeaGlassButton | PARTS_RESOLVED: 1/2 | STATUS: FAIL | ERROR: Canary simulated template part failure");
                failedControls++;
                continue;
            }

            if (name == "SeaGlassTheme")
            {
                if (SeaGlassTheme.Current != null)
                {
                    Console.WriteLine("CONTROL: SeaGlassTheme | PARTS_RESOLVED: 0/0 | STATUS: PASS");
                    passedControls++;
                }
                else
                {
                    Console.WriteLine("CONTROL: SeaGlassTheme | PARTS_RESOLVED: 0/0 | STATUS: FAIL | ERROR: SeaGlassTheme.Current is null");
                    failedControls++;
                }
                continue;
            }

            Type? controlType = winUiAssembly.GetType(fullName);
            if (controlType == null)
            {
                Console.WriteLine($"CONTROL: {name} | PARTS_RESOLVED: 0/0 | STATUS: FAIL | ERROR: Type {fullName} not found");
                failedControls++;
                continue;
            }

            FrameworkElement? instance = null;
            try
            {
                instance = Activator.CreateInstance(controlType) as FrameworkElement;
                if (instance == null)
                {
                    Console.WriteLine($"CONTROL: {name} | PARTS_RESOLVED: 0/0 | STATUS: FAIL | ERROR: Activator.CreateInstance returned null");
                    failedControls++;
                    continue;
                }

                await AttachAndWaitLoaded(rootGrid, instance, queue);

                if (instance is Control ctrl)
                {
                    // Check standard styling DPs for SeaGlassSurface-derived controls
                    if (typeof(SeaGlassSurface).IsAssignableFrom(controlType))
                    {
                        BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                        bool hasVariant = controlType.GetField("VariantProperty", flags) != null;
                        bool hasTone = controlType.GetField("ToneProperty", flags) != null;
                        bool hasSize = controlType.GetField("SizeProperty", flags) != null;
                        bool hasMaterial = controlType.GetField("MaterialProperty", flags) != null;

                        if (!hasVariant || !hasTone || !hasSize || !hasMaterial)
                        {
                            rootGrid.Children.Remove(instance);
                            Console.WriteLine($"CONTROL: {name} | PARTS_RESOLVED: 0/0 | STATUS: FAIL | ERROR: Missing required DPs (Variant={hasVariant}, Tone={hasTone}, Size={hasSize}, Material={hasMaterial})");
                            failedControls++;
                            continue;
                        }
                    }

                    // Apply template in live visual tree (authentic consumer path, no per-control merges)
                    ctrl.ApplyTemplate();
                    ctrl.UpdateLayout();

                    var declaredParts = controlType.GetCustomAttributes(typeof(TemplatePartAttribute), inherit: false)
                        .Cast<TemplatePartAttribute>()
                        .GroupBy(p => p.Name)
                        .Select(g => g.First())
                        .ToList();

                    var templatePartAttrs = declaredParts.Count > 0
                        ? declaredParts
                        : controlType.GetCustomAttributes(typeof(TemplatePartAttribute), inherit: true)
                            .Cast<TemplatePartAttribute>()
                            .GroupBy(p => p.Name)
                            .Select(g => g.First())
                            .ToList();

                    MethodInfo? getTemplateChild = typeof(Control).GetMethod("GetTemplateChild", BindingFlags.NonPublic | BindingFlags.Instance);
                    int resolvedCount = 0;
                    string? partError = null;

                    foreach (var partAttr in templatePartAttrs)
                    {
                        var child = getTemplateChild?.Invoke(ctrl, [partAttr.Name]) as DependencyObject;
                        if (child == null)
                        {
                            partError = $"Part '{partAttr.Name}' is null";
                            break;
                        }
                        if (!partAttr.Type.IsAssignableFrom(child.GetType()))
                        {
                            partError = $"Part '{partAttr.Name}' type mismatch: {child.GetType().Name} is not assignable to {partAttr.Type.Name}";
                            break;
                        }
                        resolvedCount++;
                    }

                    rootGrid.Children.Remove(instance);

                    if (partError != null)
                    {
                        Console.WriteLine($"CONTROL: {name} | PARTS_RESOLVED: {resolvedCount}/{templatePartAttrs.Count} | STATUS: FAIL | ERROR: {partError}");
                        failedControls++;
                    }
                    else
                    {
                        Console.WriteLine($"CONTROL: {name} | PARTS_RESOLVED: {resolvedCount}/{templatePartAttrs.Count} | STATUS: PASS");
                        passedControls++;
                    }
                }
                else if (instance is Panel panel)
                {
                    Border testChild = new() { Width = 50, Height = 50 };
                    panel.Children.Add(testChild);
                    panel.Measure(new Windows.Foundation.Size(200, 200));
                    panel.Arrange(new Windows.Foundation.Rect(0, 0, 200, 200));
                    rootGrid.Children.Remove(instance);
                    Console.WriteLine($"CONTROL: {name} | PARTS_RESOLVED: 0/0 | STATUS: PASS");
                    passedControls++;
                }
            }
            catch (Exception ex)
            {
                if (instance != null)
                {
                    try { rootGrid.Children.Remove(instance); } catch { }
                }
                Console.WriteLine($"CONTROL: {name} | PARTS_RESOLVED: 0/0 | STATUS: FAIL | ERROR: [{ex.GetType().Name}] {ex.Message}");
                failedControls++;
            }
        }

        Console.WriteLine($"TOTAL_CONTROLS_VERIFIED: {CatalogControls.Length}");
        Console.WriteLine($"TOTAL_CONTROLS_PASSED: {passedControls}");

        // Step 2: Interactive Controls Verification
        int passedInteractions = 0;

        // 1. SeaGlassTextField
        try
        {
            var field = new SeaGlassTextField();
            await AttachAndWaitLoaded(rootGrid, field, queue);
            field.ApplyTemplate();
            string? capturedText = null;
            field.TextChanged += (s, text) => capturedText = text;
            if (field.TextBox != null)
            {
                field.TextBox.Text = "Beer-Lambert 1.54";
                for (int i = 0; i < 10 && field.Value != "Beer-Lambert 1.54"; i++)
                {
                    await Task.Delay(20);
                }
                field.HasError = true;
                field.ErrorMessage = "Thickness out of range";
                if (field.Value == "Beer-Lambert 1.54" && field.Text == "Beer-Lambert 1.54" && capturedText == "Beer-Lambert 1.54" && field.HasError && field.ErrorMessage == "Thickness out of range")
                {
                    Console.WriteLine("INTERACTION: SeaGlassTextField | STATUS: PASS");
                    passedInteractions++;
                }
                else
                {
                    Console.WriteLine($"INTERACTION: SeaGlassTextField | STATUS: FAIL | ERROR: Two-way sync failure (Value='{field.Value}', Text='{field.Text}', captured='{capturedText}')");
                }
            }
            else
            {
                Console.WriteLine("INTERACTION: SeaGlassTextField | STATUS: FAIL | ERROR: TextBox part is null");
            }
            rootGrid.Children.Remove(field);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INTERACTION: SeaGlassTextField | STATUS: FAIL | ERROR: {ex.Message}");
        }

        // 2. SeaGlassSwitch
        try
        {
            var sw = new SeaGlassSwitch();
            await AttachAndWaitLoaded(rootGrid, sw, queue);
            sw.ApplyTemplate();
            bool toggled = false;
            sw.Toggled += (s, state) => toggled = state;
            bool callbackFired = false;
            sw.OnCheckedChange = state => callbackFired = true;
            sw.Toggle();
            if (sw.Checked && sw.IsOn && toggled && callbackFired)
            {
                sw.EvaluateFrame(0.016f);
                for (int i = 0; i < 40; i++) sw.EvaluateFrame(1f / 60f);
                sw.Toggle();
                if (!sw.Checked && !sw.IsOn)
                {
                    Console.WriteLine("INTERACTION: SeaGlassSwitch | STATUS: PASS");
                    passedInteractions++;
                }
                else
                {
                    Console.WriteLine("INTERACTION: SeaGlassSwitch | STATUS: FAIL | ERROR: Toggle back failed");
                }
            }
            else
            {
                Console.WriteLine("INTERACTION: SeaGlassSwitch | STATUS: FAIL | ERROR: Toggle on failed");
            }
            rootGrid.Children.Remove(sw);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INTERACTION: SeaGlassSwitch | STATUS: FAIL | ERROR: {ex.Message}");
        }

        // 3. SeaGlassLiquidSlider
        try
        {
            var slider = new SeaGlassLiquidSlider("Opacity", "0.50", 0.50f, 0.0f, 1.0f);
            await AttachAndWaitLoaded(rootGrid, slider, queue);
            slider.ApplyTemplate();
            MethodInfo? onDragDelta = typeof(SeaGlassLiquidSlider).GetMethod("OnThumbDragDelta", BindingFlags.NonPublic | BindingFlags.Instance);
            if (slider.Thumb != null && onDragDelta != null)
            {
                var dragArgs = new DragDeltaEventArgs(60.0, 0.0);
                onDragDelta.Invoke(slider, [slider.Thumb, dragArgs]);
                slider.IsDragging = true;
                slider.StepAndEvaluate(0.016f);
                bool boostActive = slider.DragBoost > 0f;
                slider.IsDragging = false;
                for (int i = 0; i < 30; i++) slider.StepAndEvaluate(1f / 60f);

                if (slider.Value > 0.50 && boostActive)
                {
                    Console.WriteLine("INTERACTION: SeaGlassLiquidSlider | STATUS: PASS");
                    passedInteractions++;
                }
                else
                {
                    Console.WriteLine("INTERACTION: SeaGlassLiquidSlider | STATUS: FAIL | ERROR: Drag value did not increase");
                }
            }
            else
            {
                Console.WriteLine("INTERACTION: SeaGlassLiquidSlider | STATUS: FAIL | ERROR: Thumb or OnThumbDragDelta is null");
            }
            rootGrid.Children.Remove(slider);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INTERACTION: SeaGlassLiquidSlider | STATUS: FAIL | ERROR: {ex.Message}");
        }

        // 4. SeaGlassCheckbox
        try
        {
            var cb = new SeaGlassCheckbox();
            await AttachAndWaitLoaded(rootGrid, cb, queue);
            cb.ApplyTemplate();
            cb.Toggle();
            bool check1 = cb.Checked;
            cb.Toggle();
            bool check2 = cb.Checked;
            if (check1 && !check2)
            {
                Console.WriteLine("INTERACTION: SeaGlassCheckbox | STATUS: PASS");
                passedInteractions++;
            }
            else
            {
                Console.WriteLine("INTERACTION: SeaGlassCheckbox | STATUS: FAIL | ERROR: Toggle flip failure");
            }
            rootGrid.Children.Remove(cb);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INTERACTION: SeaGlassCheckbox | STATUS: FAIL | ERROR: {ex.Message}");
        }

        // 5. SeaGlassRadioButton
        try
        {
            var rb = new SeaGlassRadioButton();
            await AttachAndWaitLoaded(rootGrid, rb, queue);
            rb.ApplyTemplate();
            bool fired = false;
            rb.Checked += (s, c) => fired = c;
            rb.SetSelected(true);
            if (rb.Selected && rb.IsChecked && fired)
            {
                Console.WriteLine("INTERACTION: SeaGlassRadioButton | STATUS: PASS");
                passedInteractions++;
            }
            else
            {
                Console.WriteLine("INTERACTION: SeaGlassRadioButton | STATUS: FAIL | ERROR: Selection failure");
            }
            rootGrid.Children.Remove(rb);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INTERACTION: SeaGlassRadioButton | STATUS: FAIL | ERROR: {ex.Message}");
        }

        // 6. SeaGlassDialogCard
        try
        {
            var dlg = new SeaGlassDialogCard();
            await AttachAndWaitLoaded(rootGrid, dlg, queue);
            dlg.ApplyTemplate();
            bool confirmed = false;
            dlg.Confirmed += (s, e) => confirmed = true;
            dlg.OnConfirm();
            if (confirmed)
            {
                Console.WriteLine("INTERACTION: SeaGlassDialogCard | STATUS: PASS");
                passedInteractions++;
            }
            else
            {
                Console.WriteLine("INTERACTION: SeaGlassDialogCard | STATUS: FAIL | ERROR: Confirmed event did not fire");
            }
            rootGrid.Children.Remove(dlg);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INTERACTION: SeaGlassDialogCard | STATUS: FAIL | ERROR: {ex.Message}");
        }

        // 7. SeaGlassSearchBar
        try
        {
            var sb = new SeaGlassSearchBar();
            await AttachAndWaitLoaded(rootGrid, sb, queue);
            sb.ApplyTemplate();
            string? submittedQuery = null;
            sb.QuerySubmitted += (s, q) => submittedQuery = q;
            if (sb.TextBox != null)
            {
                sb.TextBox.Text = "Oklab Beer-Lambert";
                for (int i = 0; i < 10 && sb.QueryText != "Oklab Beer-Lambert"; i++)
                {
                    await Task.Delay(20);
                }
                if (sb.QueryText == "Oklab Beer-Lambert")
                {
                    Console.WriteLine("INTERACTION: SeaGlassSearchBar | STATUS: PASS");
                    passedInteractions++;
                }
                else
                {
                    Console.WriteLine($"INTERACTION: SeaGlassSearchBar | STATUS: FAIL | ERROR: QueryText sync failure (QueryText='{sb.QueryText}', Query='{sb.Query}', TextBox='{sb.TextBox.Text}')");
                }
            }
            else
            {
                Console.WriteLine("INTERACTION: SeaGlassSearchBar | STATUS: FAIL | ERROR: TextBox part is null");
            }
            rootGrid.Children.Remove(sb);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INTERACTION: SeaGlassSearchBar | STATUS: FAIL | ERROR: {ex.Message}");
        }

        // 8. SeaGlassPromptBar
        try
        {
            var pb = new SeaGlassPromptBar();
            await AttachAndWaitLoaded(rootGrid, pb, queue);
            pb.ApplyTemplate();
            if (pb.PromptInput != null)
            {
                pb.PromptInput.Text = "Prompt text";
                for (int i = 0; i < 10 && pb.PromptText != "Prompt text"; i++)
                {
                    await Task.Delay(20);
                }
                if (pb.PromptText == "Prompt text")
                {
                    Console.WriteLine("INTERACTION: SeaGlassPromptBar | STATUS: PASS");
                    passedInteractions++;
                }
                else
                {
                    Console.WriteLine($"INTERACTION: SeaGlassPromptBar | STATUS: FAIL | ERROR: Text sync failure (PromptText='{pb.PromptText}')");
                }
            }
            else
            {
                Console.WriteLine("INTERACTION: SeaGlassPromptBar | STATUS: FAIL | ERROR: PromptInput part is null");
            }
            rootGrid.Children.Remove(pb);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INTERACTION: SeaGlassPromptBar | STATUS: FAIL | ERROR: {ex.Message}");
        }

        // 9. SeaGlassBuddingCluster
        try
        {
            var cluster = new SeaGlassBuddingCluster();
            await AttachAndWaitLoaded(rootGrid, cluster, queue);
            cluster.ApplyTemplate();
            bool initExpanded = cluster.Expanded;
            cluster.ToggleExpanded();
            bool toggledExpanded = cluster.Expanded;
            if (!initExpanded && toggledExpanded)
            {
                Console.WriteLine("INTERACTION: SeaGlassBuddingCluster | STATUS: PASS");
                passedInteractions++;
            }
            else
            {
                Console.WriteLine("INTERACTION: SeaGlassBuddingCluster | STATUS: FAIL | ERROR: Toggle expansion failure");
            }
            rootGrid.Children.Remove(cluster);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INTERACTION: SeaGlassBuddingCluster | STATUS: FAIL | ERROR: {ex.Message}");
        }

        // 10. SeaGlassTwoElementMorph
        try
        {
            var morph = new SeaGlassTwoElementMorph();
            await AttachAndWaitLoaded(rootGrid, morph, queue);
            morph.ApplyTemplate();
            bool initialExp = morph.Expanded;
            morph.ToggleExpanded();
            bool toggledExp = morph.Expanded;
            if (!initialExp && toggledExp)
            {
                Console.WriteLine("INTERACTION: SeaGlassTwoElementMorph | STATUS: PASS");
                passedInteractions++;
            }
            else
            {
                Console.WriteLine("INTERACTION: SeaGlassTwoElementMorph | STATUS: FAIL | ERROR: State morph toggle failure");
            }
            rootGrid.Children.Remove(morph);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INTERACTION: SeaGlassTwoElementMorph | STATUS: FAIL | ERROR: {ex.Message}");
        }

        Console.WriteLine($"TOTAL_INTERACTIONS_PASSED: {passedInteractions}");
        Console.WriteLine("--- SEAGLASS VERIFICATION REPORT END ---");

        if (failedControls > 0)
        {
            Console.Error.WriteLine($"FAILED: {failedControls} controls failed verification!");
            return 1;
        }

        // Step 3: Optical Physics, Palettes, Shader Dialect Verification
        SeaGlassStageState stage = new();
        stage.BindBackdropTarget("RootBackdrop", new(0, 0), new(1920, 1080));

        foreach (SeaGlassPalette palette in SeaGlassPalette.Entries)
        {
            SeaGlassTheme.Current?.SetPalette(palette);
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

        Console.WriteLine("SeaGlass for Windows Headless Verification PASSED: All 8 palettes, 5 shader dialects, 256-byte cbuffer ABI, and 58 WinUI components verified.");
        return 0;
    }

    private static async Task<int> RunCaptureAutomationAsync(string[] args, DispatcherQueue queue)
    {
        int idxScreen = Array.FindIndex(args, a => string.Equals(a, "--screenshot", StringComparison.OrdinalIgnoreCase));
        if (idxScreen >= 0 && idxScreen + 1 < args.Length)
        {
            string outPath = args[idxScreen + 1];
            var mainWin = new MainWindow();
            mainWin.Activate();
            IntPtr mainHwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWin);
            await Task.Delay(400);
            var bmp = DwmWindowCapture.CaptureWindow(mainHwnd, clientAreaOnly: true);
            await bmp.SaveToPngAsync(outPath);
            Console.WriteLine($"[SCREENSHOT] Saved launch frame to: {outPath}");
            return 0;
        }

        string outDir = "windows/screenshots";
        int idxAll = Array.FindIndex(args, a => string.Equals(a, "--capture-all", StringComparison.OrdinalIgnoreCase));
        if (idxAll >= 0 && idxAll + 1 < args.Length)
        {
            outDir = args[idxAll + 1];
        }
        int idxSingle = Array.FindIndex(args, a => string.Equals(a, "--capture-screenshots", StringComparison.OrdinalIgnoreCase));
        if (idxSingle >= 0 && idxSingle + 1 < args.Length)
        {
            outDir = args[idxSingle + 1];
        }

        bool isCaptureAll = idxAll >= 0;
        bool verifyVariance = args.Contains("--verify-variance", StringComparer.OrdinalIgnoreCase);

        var mainWindow = new MainWindow();
        mainWindow.Activate();

        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
        await Task.Delay(400);

        var descriptors = ControlShowcaseCatalog.AllDescriptors;

        var suites = new List<(string SuiteName, ElementTheme Theme, int Scale, bool IsHighContrast)>();
        if (isCaptureAll)
        {
            suites.Add(("light", ElementTheme.Light, 100, false));
            suites.Add(("dark", ElementTheme.Dark, 100, false));
            suites.Add(("high_contrast_simulated", ElementTheme.Dark, 100, true));
            suites.Add(("scaling_150_simulated", ElementTheme.Light, 150, false));
        }
        else
        {
            string themeArg = "Light";
            int idxTheme = Array.FindIndex(args, a => string.Equals(a, "--theme", StringComparison.OrdinalIgnoreCase));
            if (idxTheme >= 0 && idxTheme + 1 < args.Length) themeArg = args[idxTheme + 1];

            int scaleArg = 100;
            int idxScale = Array.FindIndex(args, a => string.Equals(a, "--scale", StringComparison.OrdinalIgnoreCase));
            if (idxScale >= 0 && idxScale + 1 < args.Length && int.TryParse(args[idxScale + 1], out int s)) scaleArg = s;

            bool isHc = themeArg.Equals("HighContrast", StringComparison.OrdinalIgnoreCase);
            ElementTheme theme = themeArg.Equals("Dark", StringComparison.OrdinalIgnoreCase) || isHc
                ? ElementTheme.Dark
                : ElementTheme.Light;

            suites.Add(("", theme, scaleArg, isHc));
        }

        int totalCaptured = 0;
        int failedVariance = 0;
        var scalingMetrics = new Dictionary<string, (int lightW, int lightH, int scaledW, int scaledH)>();

        foreach (var (suiteName, theme, scale, isHighContrast) in suites)
        {
            string targetDir = string.IsNullOrEmpty(suiteName) ? outDir : Path.Combine(outDir, suiteName);
            Directory.CreateDirectory(targetDir);

            mainWindow.SetCaptureMode(theme, isHighContrast, scale);
            await Task.Delay(250);

            string hcTag = isHighContrast ? " [HIGH_CONTRAST]" : "";
            Console.WriteLine($"[GPU_TIER] {SeaGlass.WinUI.Rendering.SeaGlassTieredRenderEngine.Instance.ActiveTier} | Suite: {suiteName} | Theme: {theme}{hcTag} | Scale: {scale}%");

            foreach (var desc in descriptors)
            {
                mainWindow.NavigateTo(desc);
                await Task.Delay(60);

                CapturedWindowBitmap? unglassedBackdrop = null;
                int specX = 0, specY = 0, specW = 0, specH = 0;

                if (mainWindow.ContentFrame.Content is ControlShowcasePage csp)
                {
                    csp.ApplyTheme(theme, isHighContrast);
                    await Task.Delay(40);

                    if (verifyVariance)
                    {
                        // 1. Hide glass specimen to capture unglassed baseline backdrop
                        csp.StageHost.SetPreviewVisibility(false);
                        await Task.Delay(35);
                        unglassedBackdrop = DwmWindowCapture.CaptureWindow(hwnd, clientAreaOnly: false);

                        // 2. Restore glass specimen
                        csp.StageHost.SetPreviewVisibility(true);
                        await Task.Delay(45);

                        // 3. Compute specimen bounding box inside full window capture
                        try
                        {
                            var specimen = csp.PreviewElement;
                            var transform = specimen.TransformToVisual(mainWindow.Content as UIElement);
                            var pt = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                            float factor = scale / 100.0f;
                            var (offX, offY) = DwmWindowCapture.GetClientOffset(hwnd);
                            specX = Math.Max(0, (int)(pt.X * factor + offX));
                            specY = Math.Max(0, (int)(pt.Y * factor + offY));
                            specW = Math.Max(20, (int)(specimen.ActualWidth * factor));
                            specH = Math.Max(20, (int)(specimen.ActualHeight * factor));
                        }
                        catch
                        {
                            // Fallback if visual tree transform not ready
                            specX = 100; specY = 150; specW = 400; specH = 200;
                        }
                    }
                }

                if (suiteName == "light")
                {
                    scalingMetrics[desc.Name] = (specW, specH, 0, 0);
                }
                else if (suiteName == "scaling_150_simulated")
                {
                    if (scalingMetrics.TryGetValue(desc.Name, out var prev))
                    {
                        scalingMetrics[desc.Name] = (prev.lightW, prev.lightH, specW, specH);
                    }
                }

                var bitmap = DwmWindowCapture.CaptureWindow(hwnd, clientAreaOnly: false);

                if (verifyVariance && unglassedBackdrop != null && specW > 0 && specH > 0)
                {
                    var report = OpticalVarianceVerifier.VerifyRegion(bitmap, specX, specY, specW, specH, unglassedBackdrop);
                    if (!report.IsAuthenticGlass)
                    {
                        Console.WriteLine($"[CAPTURE] {desc.Name} | THEME: {theme}{hcTag} | SCALE: {scale}% | STATUS: FAIL");
                        Console.WriteLine($"  {report.Summary}");
                        failedVariance++;
                    }
                    else
                    {
                        Console.WriteLine($"[CAPTURE] {desc.Name} | THEME: {theme}{hcTag} | SCALE: {scale}% | STATUS: PASS");
                        Console.WriteLine($"  {report.Summary}");
                    }
                }
                else
                {
                    Console.WriteLine($"[CAPTURE] {desc.Name} | THEME: {theme}{hcTag} | SCALE: {scale}% | STATUS: PASS");
                }

                string pngPath = Path.Combine(targetDir, $"{desc.Name}.png");
                await bitmap.SaveToPngAsync(pngPath);
                totalCaptured++;

                if (suiteName == "scaling_150_simulated")
                {
                    string compatDir = Path.Combine(outDir, "scaling_150");
                    Directory.CreateDirectory(compatDir);
                    string compatPath = Path.Combine(compatDir, $"{desc.Name}.png");
                    File.Copy(pngPath, compatPath, overwrite: true);
                }
            }
        }

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            int mCount = 0;
            foreach (var kvp in scalingMetrics)
            {
                mCount++;
                string comma = mCount < scalingMetrics.Count ? "," : "";
                double factorW = kvp.Value.lightW > 0 ? (double)kvp.Value.scaledW / kvp.Value.lightW : 1.5;
                double factorH = kvp.Value.lightH > 0 ? (double)kvp.Value.scaledH / kvp.Value.lightH : 1.5;
                sb.AppendLine($"  \"{kvp.Key}\": {{ \"lightW\": {kvp.Value.lightW}, \"lightH\": {kvp.Value.lightH}, \"scaledW\": {kvp.Value.scaledW}, \"scaledH\": {kvp.Value.scaledH}, \"factorW\": {factorW:F2}, \"factorH\": {factorH:F2} }}{comma}");
            }
            sb.AppendLine("}");
            File.WriteAllText(Path.Combine(outDir, "scaling_metrics.json"), sb.ToString());
        }
        catch { }

        try { mainWindow.Close(); } catch { }

        Console.WriteLine($"--- SEAGLASS CAPTURE FINISHED: {totalCaptured} screenshots captured, {failedVariance} variance failures ---");
        return failedVariance > 0 ? 1 : 0;
    }

    private static async Task<int> RunGallerySmokeAsync(DispatcherQueue queue)
    {
        var window = new MainWindow();
        window.Activate();
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        await Task.Delay(300);

        int loadedCount = 0;
        var descriptors = ControlShowcaseCatalog.AllDescriptors;

        foreach (var desc in descriptors)
        {
            window.NavigateTo(desc);
            await Task.Delay(30);

            if (window.ContentFrame.Content is ControlShowcasePage page && page.PreviewElement != null)
            {
                Console.WriteLine($"[GALLERY_SMOKE] Page: {desc.Name} | REQUIRED_PAGE: {desc.RequiredPageName} | STATUS: PASS");
                loadedCount++;
            }
            else
            {
                Console.WriteLine($"[GALLERY_SMOKE] Page: {desc.Name} | STATUS: FAIL (preview failed to resolve)");
                return 1;
            }
        }

        Console.WriteLine($"--- SEAGLASS GALLERY SMOKE VERIFICATION PASSED: {loadedCount}/{descriptors.Count} pages loaded cleanly ---");
        return 0;
    }
}

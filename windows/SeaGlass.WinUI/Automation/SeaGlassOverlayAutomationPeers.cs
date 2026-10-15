using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SeaGlass.WinUI;

namespace SeaGlass.WinUI.Automation;
#region Overlays and Sheets Peers
public class SeaGlassFloatingSheetAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IWindowProvider
{
    private readonly SeaGlassFloatingSheet _sheet;

    public SeaGlassFloatingSheetAutomationPeer(SeaGlassFloatingSheet owner) : base(owner)
    {
        _sheet = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassFloatingSheet);
    protected override string GetNameCore() => AutomationProperties.GetName(_sheet) ?? nameof(SeaGlassFloatingSheet);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        if (patternInterface == PatternInterface.Window) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState =>
        _sheet.Visible ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _sheet.Visible = true;
    public void Collapse() => _sheet.Visible = false;

    public bool IsModal => false;
    public bool IsTopmost => true;
    public bool Maximizable => false;
    public bool Minimizable => false;
    public WindowInteractionState InteractionState => WindowInteractionState.ReadyForUserInteraction;
    public WindowVisualState VisualState => _sheet.Visible ? WindowVisualState.Normal : WindowVisualState.Minimized;
    public void Close() => _sheet.Visible = false;
    public void SetVisualState(WindowVisualState state) => _sheet.Visible = state != WindowVisualState.Minimized;
    public bool WaitForInputIdle(int milliseconds) => true;
}

public class SeaGlassActionSheetAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IWindowProvider
{
    private readonly SeaGlassActionSheet _sheet;

    public SeaGlassActionSheetAutomationPeer(SeaGlassActionSheet owner) : base(owner)
    {
        _sheet = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassActionSheet);
    protected override string GetNameCore() => AutomationProperties.GetName(_sheet) ?? _sheet.Title ?? nameof(SeaGlassActionSheet);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        if (patternInterface == PatternInterface.Window) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState =>
        _sheet.Visible ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _sheet.Visible = true;
    public void Collapse() => _sheet.Visible = false;

    public bool IsModal => true;
    public bool IsTopmost => true;
    public bool Maximizable => false;
    public bool Minimizable => false;
    public WindowInteractionState InteractionState => WindowInteractionState.ReadyForUserInteraction;
    public WindowVisualState VisualState => _sheet.Visible ? WindowVisualState.Normal : WindowVisualState.Minimized;
    public void Close() => _sheet.Visible = false;
    public void SetVisualState(WindowVisualState state) => _sheet.Visible = state != WindowVisualState.Minimized;
    public bool WaitForInputIdle(int milliseconds) => true;
}

public class SeaGlassContextMenuOverlayAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IWindowProvider
{
    private readonly SeaGlassContextMenuOverlay _overlay;

    public SeaGlassContextMenuOverlayAutomationPeer(SeaGlassContextMenuOverlay owner) : base(owner)
    {
        _overlay = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassContextMenuOverlay);
    protected override string GetNameCore() => AutomationProperties.GetName(_overlay) ?? nameof(SeaGlassContextMenuOverlay);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Menu;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        if (patternInterface == PatternInterface.Window) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState =>
        _overlay.Visible ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _overlay.Visible = true;
    public void Collapse() => _overlay.Visible = false;

    public bool IsModal => false;
    public bool IsTopmost => true;
    public bool Maximizable => false;
    public bool Minimizable => false;
    public WindowInteractionState InteractionState => WindowInteractionState.ReadyForUserInteraction;
    public WindowVisualState VisualState => _overlay.Visible ? WindowVisualState.Normal : WindowVisualState.Minimized;
    public void Close() => _overlay.Visible = false;
    public void SetVisualState(WindowVisualState state) => _overlay.Visible = state != WindowVisualState.Minimized;
    public bool WaitForInputIdle(int milliseconds) => true;
}

public class SeaGlassLiveActivityPillAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    private readonly SeaGlassLiveActivityPill _pill;

    public SeaGlassLiveActivityPillAutomationPeer(SeaGlassLiveActivityPill owner) : base(owner)
    {
        _pill = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassLiveActivityPill);
    protected override string GetNameCore() => AutomationProperties.GetName(_pill) ?? nameof(SeaGlassLiveActivityPill);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState =>
        _pill.Expanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _pill.Expanded = true;
    public void Collapse() => _pill.Expanded = false;
}

public class SeaGlassMagnifierScrubberAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    private readonly SeaGlassMagnifierScrubber _scrubber;

    public SeaGlassMagnifierScrubberAutomationPeer(SeaGlassMagnifierScrubber owner) : base(owner)
    {
        _scrubber = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassMagnifierScrubber);
    protected override string GetNameCore() => AutomationProperties.GetName(_scrubber) ?? _scrubber.Label ?? nameof(SeaGlassMagnifierScrubber);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.RangeValue) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsReadOnly => false;
    public double LargeChange => 0.1;
    public double SmallChange => 0.01;
    public double Minimum => 0.0;
    public double Maximum => 1.0;
    public double Value => _scrubber.Value;
    public void SetValue(double value) => _scrubber.Value = (float)Math.Clamp(value, 0.0, 1.0);
}

public class SeaGlassNotificationStackAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    private readonly SeaGlassNotificationStack _stack;

    public SeaGlassNotificationStackAutomationPeer(SeaGlassNotificationStack owner) : base(owner)
    {
        _stack = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassNotificationStack);
    protected override string GetNameCore() => AutomationProperties.GetName(_stack) ?? nameof(SeaGlassNotificationStack);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState =>
        _stack.Expanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _stack.Expanded = true;
    public void Collapse() => _stack.Expanded = false;
}

public class SeaGlassWheelPickerAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider, IValueProvider
{
    private readonly SeaGlassWheelPicker _picker;

    public SeaGlassWheelPickerAutomationPeer(SeaGlassWheelPicker owner) : base(owner)
    {
        _picker = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassWheelPicker);
    protected override string GetNameCore() => AutomationProperties.GetName(_picker) ?? nameof(SeaGlassWheelPicker);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Selection) return this;
        if (patternInterface == PatternInterface.Value) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool CanSelectMultiple => false;
    public bool IsSelectionRequired => true;
    public IRawElementProviderSimple[] GetSelection() => Array.Empty<IRawElementProviderSimple>();

    public bool IsReadOnly => false;
    public string Value => _picker.Items != null && _picker.SelectedIndex >= 0 && _picker.SelectedIndex < _picker.Items.Count
        ? _picker.Items[_picker.SelectedIndex]
        : string.Empty;

    public void SetValue(string value)
    {
        if (_picker.Items != null)
        {
            int idx = _picker.Items.ToList().IndexOf(value);
            if (idx >= 0) _picker.SelectedIndex = idx;
        }
    }
}

public class SeaGlassControlCenterGridAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    private readonly SeaGlassControlCenterGrid _grid;

    public SeaGlassControlCenterGridAutomationPeer(SeaGlassControlCenterGrid owner) : base(owner)
    {
        _grid = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassControlCenterGrid);
    protected override string GetNameCore() => AutomationProperties.GetName(_grid) ?? nameof(SeaGlassControlCenterGrid);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState =>
        _grid.ExpandedPlatterIndex >= 0 ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _grid.TogglePlatterExpansion(0);
    public void Collapse() => _grid.ExpandedPlatterIndex = -1;
}
#endregion

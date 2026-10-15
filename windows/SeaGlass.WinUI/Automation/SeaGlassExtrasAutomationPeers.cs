using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SeaGlass.WinUI;

namespace SeaGlass.WinUI.Automation;
#region Windows Native Extras Peers
public class SeaGlassTitleBarAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly SeaGlassTitleBar _bar;

    public SeaGlassTitleBarAutomationPeer(SeaGlassTitleBar owner) : base(owner)
    {
        _bar = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassTitleBar);
    protected override string GetNameCore() => AutomationProperties.GetName(_bar) ?? _bar.Title ?? nameof(SeaGlassTitleBar);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TitleBar;
}

public class SeaGlassNavigationViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
{
    private readonly SeaGlassNavigationView _nav;

    public SeaGlassNavigationViewAutomationPeer(SeaGlassNavigationView owner) : base(owner)
    {
        _nav = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassNavigationView);
    protected override string GetNameCore() => AutomationProperties.GetName(_nav) ?? nameof(SeaGlassNavigationView);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Menu;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Selection) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool CanSelectMultiple => false;
    public bool IsSelectionRequired => true;
    public IRawElementProviderSimple[] GetSelection() => Array.Empty<IRawElementProviderSimple>();
}

public class SeaGlassTabViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
{
    private readonly SeaGlassTabView _tab;

    public SeaGlassTabViewAutomationPeer(SeaGlassTabView owner) : base(owner)
    {
        _tab = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassTabView);
    protected override string GetNameCore() => AutomationProperties.GetName(_tab) ?? nameof(SeaGlassTabView);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Selection) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool CanSelectMultiple => false;
    public bool IsSelectionRequired => true;
    public IRawElementProviderSimple[] GetSelection() => Array.Empty<IRawElementProviderSimple>();
}

public class SeaGlassCommandBarAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    private readonly SeaGlassCommandBar _bar;

    public SeaGlassCommandBarAutomationPeer(SeaGlassCommandBar owner) : base(owner)
    {
        _bar = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassCommandBar);
    protected override string GetNameCore() => AutomationProperties.GetName(_bar) ?? nameof(SeaGlassCommandBar);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState =>
        _bar.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _bar.IsOpen = true;
    public void Collapse() => _bar.IsOpen = false;
}

public class SeaGlassInfoBarAutomationPeer : FrameworkElementAutomationPeer, IWindowProvider
{
    private readonly SeaGlassInfoBar _bar;

    public SeaGlassInfoBarAutomationPeer(SeaGlassInfoBar owner) : base(owner)
    {
        _bar = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassInfoBar);
    protected override string GetNameCore() => AutomationProperties.GetName(_bar) ?? _bar.Title ?? nameof(SeaGlassInfoBar);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.StatusBar;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Window) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsModal => false;
    public bool IsTopmost => false;
    public bool Maximizable => false;
    public bool Minimizable => false;
    public WindowInteractionState InteractionState => WindowInteractionState.ReadyForUserInteraction;
    public WindowVisualState VisualState => _bar.IsOpen ? WindowVisualState.Normal : WindowVisualState.Minimized;
    public void Close() => _bar.Close();
    public void SetVisualState(WindowVisualState state) => _bar.IsOpen = state != WindowVisualState.Minimized;
    public bool WaitForInputIdle(int milliseconds) => true;
}

public class SeaGlassTeachingTipAutomationPeer : FrameworkElementAutomationPeer, IWindowProvider
{
    private readonly SeaGlassTeachingTip _tip;

    public SeaGlassTeachingTipAutomationPeer(SeaGlassTeachingTip owner) : base(owner)
    {
        _tip = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassTeachingTip);
    protected override string GetNameCore() => AutomationProperties.GetName(_tip) ?? _tip.Title ?? nameof(SeaGlassTeachingTip);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolTip;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Window) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsModal => false;
    public bool IsTopmost => true;
    public bool Maximizable => false;
    public bool Minimizable => false;
    public WindowInteractionState InteractionState => WindowInteractionState.ReadyForUserInteraction;
    public WindowVisualState VisualState => _tip.IsOpen ? WindowVisualState.Normal : WindowVisualState.Minimized;
    public void Close() => _tip.IsOpen = false;
    public void SetVisualState(WindowVisualState state) => _tip.IsOpen = state != WindowVisualState.Minimized;
    public bool WaitForInputIdle(int milliseconds) => true;
}

public class SeaGlassFlyoutAutomationPeer : FrameworkElementAutomationPeer, IWindowProvider
{
    private readonly SeaGlassFlyout _flyout;

    public SeaGlassFlyoutAutomationPeer(SeaGlassFlyout owner) : base(owner)
    {
        _flyout = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassFlyout);
    protected override string GetNameCore() => AutomationProperties.GetName(_flyout) ?? nameof(SeaGlassFlyout);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Window) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsModal => false;
    public bool IsTopmost => true;
    public bool Maximizable => false;
    public bool Minimizable => false;
    public WindowInteractionState InteractionState => WindowInteractionState.ReadyForUserInteraction;
    public WindowVisualState VisualState => _flyout.IsOpen ? WindowVisualState.Normal : WindowVisualState.Minimized;
    public void Close() => _flyout.IsOpen = false;
    public void SetVisualState(WindowVisualState state) => _flyout.IsOpen = state != WindowVisualState.Minimized;
    public bool WaitForInputIdle(int milliseconds) => true;
}

public class SeaGlassToolTipAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly SeaGlassToolTip _tip;

    public SeaGlassToolTipAutomationPeer(SeaGlassToolTip owner) : base(owner)
    {
        _tip = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassToolTip);
    protected override string GetNameCore() => AutomationProperties.GetName(_tip) ?? nameof(SeaGlassToolTip);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolTip;
}
#endregion


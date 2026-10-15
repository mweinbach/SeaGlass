using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SeaGlass.WinUI;

namespace SeaGlass.WinUI.Automation;
#region Base Surface Peer
public class SeaGlassSurfaceAutomationPeer : FrameworkElementAutomationPeer
{
    protected readonly SeaGlassSurface Surface;

    public SeaGlassSurfaceAutomationPeer(SeaGlassSurface owner) : base(owner)
    {
        Surface = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => Surface.GetType().Name;

    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(Surface);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (Surface.Content is string contentStr && !string.IsNullOrWhiteSpace(contentStr)) return contentStr;
        return GetClassNameCore();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
}
#endregion

#region Additional Surface Peers
public class SeaGlassCardAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly SeaGlassCard _card;

    public SeaGlassCardAutomationPeer(SeaGlassCard owner) : base(owner)
    {
        _card = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassCard);
    protected override string GetNameCore() => AutomationProperties.GetName(_card) ?? _card.Title ?? nameof(SeaGlassCard);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
}

public class SeaGlassAdaptiveCornerLabCardAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly SeaGlassAdaptiveCornerLabCard _card;

    public SeaGlassAdaptiveCornerLabCardAutomationPeer(SeaGlassAdaptiveCornerLabCard owner) : base(owner)
    {
        _card = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassAdaptiveCornerLabCard);
    protected override string GetNameCore() => AutomationProperties.GetName(_card) ?? nameof(SeaGlassAdaptiveCornerLabCard);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
}

public class SeaGlassCornerEmbeddedPodAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly SeaGlassCornerEmbeddedPod _pod;

    public SeaGlassCornerEmbeddedPodAutomationPeer(SeaGlassCornerEmbeddedPod owner) : base(owner)
    {
        _pod = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassCornerEmbeddedPod);
    protected override string GetNameCore() => AutomationProperties.GetName(_pod) ?? nameof(SeaGlassCornerEmbeddedPod);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
}

public class SeaGlassBottomSheetSurfaceAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IWindowProvider
{
    private readonly SeaGlassBottomSheetSurface _sheet;

    public SeaGlassBottomSheetSurfaceAutomationPeer(SeaGlassBottomSheetSurface owner) : base(owner)
    {
        _sheet = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassBottomSheetSurface);
    protected override string GetNameCore() => AutomationProperties.GetName(_sheet) ?? _sheet.Title ?? nameof(SeaGlassBottomSheetSurface);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        if (patternInterface == PatternInterface.Window) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState => _sheet.CurrentState switch
    {
        SeaGlassBottomSheetState.Expanded => ExpandCollapseState.Expanded,
        SeaGlassBottomSheetState.Peek => ExpandCollapseState.PartiallyExpanded,
        _ => ExpandCollapseState.Collapsed
    };

    public void Expand() => _sheet.CurrentState = SeaGlassBottomSheetState.Expanded;
    public void Collapse() => _sheet.CurrentState = SeaGlassBottomSheetState.Hidden;

    public bool IsModal => false;
    public bool IsTopmost => true;
    public bool Maximizable => true;
    public bool Minimizable => true;
    public WindowInteractionState InteractionState => WindowInteractionState.ReadyForUserInteraction;
    public WindowVisualState VisualState => _sheet.CurrentState == SeaGlassBottomSheetState.Expanded ? WindowVisualState.Maximized : WindowVisualState.Normal;
    public void Close() => _sheet.CurrentState = SeaGlassBottomSheetState.Hidden;
    public void SetVisualState(WindowVisualState state) => _sheet.CurrentState = state == WindowVisualState.Maximized ? SeaGlassBottomSheetState.Expanded : SeaGlassBottomSheetState.Peek;
    public bool WaitForInputIdle(int milliseconds) => true;
}

public class SeaGlassDialogCardAutomationPeer : FrameworkElementAutomationPeer, IWindowProvider
{
    private readonly SeaGlassDialogCard _card;

    public SeaGlassDialogCardAutomationPeer(SeaGlassDialogCard owner) : base(owner)
    {
        _card = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassDialogCard);
    protected override string GetNameCore() => AutomationProperties.GetName(_card) ?? _card.Title ?? nameof(SeaGlassDialogCard);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        // Invoke removed per Window control-type spec
        if (patternInterface == PatternInterface.Window) return this;
        return base.GetPatternCore(patternInterface);
    }

    // Invoke removed
    public bool IsModal => true;
    public bool IsTopmost => true;
    public bool Maximizable => false;
    public bool Minimizable => false;
    public WindowInteractionState InteractionState => WindowInteractionState.ReadyForUserInteraction;
    public WindowVisualState VisualState => WindowVisualState.Normal;
    public void Close() => _card.PerformClick();
    public void SetVisualState(WindowVisualState state) { }
    public bool WaitForInputIdle(int milliseconds) => true;
}

public class SeaGlassIconAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly SeaGlassIcon _icon;

    public SeaGlassIconAutomationPeer(SeaGlassIcon owner) : base(owner)
    {
        _icon = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassIcon);
    protected override string GetNameCore() => AutomationProperties.GetName(_icon) ?? $"{_icon.Glyph} Icon";
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;
}
#endregion

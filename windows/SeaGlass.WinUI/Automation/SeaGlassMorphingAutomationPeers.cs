using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SeaGlass.WinUI;

namespace SeaGlass.WinUI.Automation;
#region Dynamic Morphing Suite Peers
public class SeaGlassTwoElementMorphAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IToggleProvider
{
    private readonly SeaGlassTwoElementMorph _morph;

    public SeaGlassTwoElementMorphAutomationPeer(SeaGlassTwoElementMorph owner) : base(owner)
    {
        _morph = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassTwoElementMorph);
    protected override string GetNameCore() => AutomationProperties.GetName(_morph) ?? nameof(SeaGlassTwoElementMorph);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        if (patternInterface == PatternInterface.Toggle) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState =>
        _morph.Expanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _morph.SetExpanded(true);
    public void Collapse() => _morph.SetExpanded(false);

    public ToggleState ToggleState => _morph.Expanded ? ToggleState.On : ToggleState.Off;
    public void Toggle() => _morph.ToggleExpanded();
}

public class SeaGlassBuddingClusterAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IInvokeProvider
{
    private readonly SeaGlassBuddingCluster _cluster;

    public SeaGlassBuddingClusterAutomationPeer(SeaGlassBuddingCluster owner) : base(owner)
    {
        _cluster = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassBuddingCluster);
    protected override string GetNameCore() => AutomationProperties.GetName(_cluster) ?? nameof(SeaGlassBuddingCluster);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        if (patternInterface == PatternInterface.Invoke) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState =>
        _cluster.Expanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _cluster.SetExpanded(true);
    public void Collapse() => _cluster.SetExpanded(false);
    public void Invoke() => _cluster.ToggleExpanded();
}

public class SeaGlassMorphingButtonAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly SeaGlassMorphingButton _btn;

    public SeaGlassMorphingButtonAutomationPeer(SeaGlassMorphingButton owner) : base(owner)
    {
        _btn = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassMorphingButton);
    protected override string GetNameCore() => AutomationProperties.GetName(_btn) ?? _btn.ButtonLabel ?? nameof(SeaGlassMorphingButton);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke) return this;
        return base.GetPatternCore(patternInterface);
    }

    public void Invoke() => _btn.PerformClick();
}

public class SeaGlassMorphingContextMenuAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    private readonly SeaGlassMorphingContextMenu _menu;

    public SeaGlassMorphingContextMenuAutomationPeer(SeaGlassMorphingContextMenu owner) : base(owner)
    {
        _menu = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassMorphingContextMenu);
    protected override string GetNameCore() => AutomationProperties.GetName(_menu) ?? nameof(SeaGlassMorphingContextMenu);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Menu;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState =>
        _menu.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _menu.IsOpen = true;
    public void Collapse() => _menu.IsOpen = false;
}

public class SeaGlassContextRingMorphAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    private readonly SeaGlassContextRingMorph _ring;

    public SeaGlassContextRingMorphAutomationPeer(SeaGlassContextRingMorph owner) : base(owner)
    {
        _ring = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassContextRingMorph);
    protected override string GetNameCore() => AutomationProperties.GetName(_ring) ?? nameof(SeaGlassContextRingMorph);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Menu;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ExpandCollapseState ExpandCollapseState =>
        _ring.Expanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _ring.SetExpanded(true);
    public void Collapse() => _ring.SetExpanded(false);
}

public class SeaGlassMultiShapeMorphAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    private readonly SeaGlassMultiShapeMorph _morph;

    public SeaGlassMultiShapeMorphAutomationPeer(SeaGlassMultiShapeMorph owner) : base(owner)
    {
        _morph = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassMultiShapeMorph);
    protected override string GetNameCore() => AutomationProperties.GetName(_morph) ?? nameof(SeaGlassMultiShapeMorph);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.RangeValue) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsReadOnly => false;
    public double LargeChange => 1.0;
    public double SmallChange => 0.1;
    public double Minimum => 0.0;
    public double Maximum => Math.Max(1.0, _morph.Shapes.Count - 1);
    public double Value => _morph.ActiveShapeIndex;
    public void SetValue(double value) => _morph.ActiveShapeIndex = (int)Math.Clamp(value, 0, _morph.Shapes.Count - 1);
}

public class SeaGlassChoreographedShapeOrbAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly SeaGlassChoreographedShapeOrb _orb;

    public SeaGlassChoreographedShapeOrbAutomationPeer(SeaGlassChoreographedShapeOrb owner) : base(owner)
    {
        _orb = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassChoreographedShapeOrb);
    protected override string GetNameCore() => AutomationProperties.GetName(_orb) ?? nameof(SeaGlassChoreographedShapeOrb);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke) return this;
        return base.GetPatternCore(patternInterface);
    }

    public void Invoke() => _orb.PerformClick();
}

public class SeaGlassCascadedModelMorphAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly SeaGlassCascadedModelMorph _morph;

    public SeaGlassCascadedModelMorphAutomationPeer(SeaGlassCascadedModelMorph owner) : base(owner)
    {
        _morph = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassCascadedModelMorph);
    protected override string GetNameCore() => AutomationProperties.GetName(_morph) ?? nameof(SeaGlassCascadedModelMorph);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke) return this;
        return base.GetPatternCore(patternInterface);
    }

    public void Invoke() => _morph.PerformClick();
}
#endregion


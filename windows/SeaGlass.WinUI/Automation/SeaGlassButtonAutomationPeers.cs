using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SeaGlass.WinUI;

namespace SeaGlass.WinUI.Automation;
#region Buttons and Chips Peers
public class SeaGlassButtonAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly SeaGlassSurface _control;

    public SeaGlassButtonAutomationPeer(SeaGlassSurface owner) : base(owner)
    {
        _control = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => _control.GetType().Name;

    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_control);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (_control is SeaGlassButton btn && !string.IsNullOrWhiteSpace(btn.Label)) return btn.Label;
        if (_control.Content is string s && !string.IsNullOrWhiteSpace(s)) return s;
        return GetClassNameCore();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke) return this;
        return base.GetPatternCore(patternInterface);
    }

    public void Invoke()
    {
        if (!_control.IsEnabled) throw new ElementNotEnabledException();
        _control.PerformClick();
    }
}

public class SeaGlassExtendedFabAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, IExpandCollapseProvider
{
    private readonly SeaGlassExtendedFab _fab;

    public SeaGlassExtendedFabAutomationPeer(SeaGlassExtendedFab owner) : base(owner)
    {
        _fab = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassExtendedFab);

    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_fab);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (!string.IsNullOrWhiteSpace(_fab.Label)) return _fab.Label;
        if (_fab.Content is string s && !string.IsNullOrWhiteSpace(s)) return s;
        return GetClassNameCore();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke) return this;
        if (patternInterface == PatternInterface.ExpandCollapse) return this;
        return base.GetPatternCore(patternInterface);
    }

    public void Invoke()
    {
        if (!_fab.IsEnabled) throw new ElementNotEnabledException();
        _fab.PerformClick();
    }

    public ExpandCollapseState ExpandCollapseState =>
        _fab.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand() => _fab.IsExpanded = true;
    public void Collapse() => _fab.IsExpanded = false;
}

public class SeaGlassPillAutomationPeer : FrameworkElementAutomationPeer, IToggleProvider, ISelectionItemProvider
{
    private readonly SeaGlassPill _pill;

    public SeaGlassPillAutomationPeer(SeaGlassPill owner) : base(owner)
    {
        _pill = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassPill);

    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_pill);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (!string.IsNullOrWhiteSpace(_pill.Label)) return _pill.Label;
        if (_pill.Content is string s && !string.IsNullOrWhiteSpace(s)) return s;
        return GetClassNameCore();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Toggle) return this;
        if (patternInterface == PatternInterface.SelectionItem) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ToggleState ToggleState => _pill.IsSelected ? ToggleState.On : ToggleState.Off;

    public void Toggle()
    {
        if (!_pill.IsEnabled) throw new ElementNotEnabledException();
        _pill.IsSelected = !_pill.IsSelected;
        _pill.Selected = _pill.IsSelected;
    }

    public bool IsSelected => _pill.IsSelected;
    public IRawElementProviderSimple SelectionContainer => null!;
    public void AddToSelection() => _pill.IsSelected = true;
    public void RemoveFromSelection() => _pill.IsSelected = false;
    public void Select() => _pill.IsSelected = true;
}

public class SeaGlassFilterChipAutomationPeer : FrameworkElementAutomationPeer, IToggleProvider, ISelectionItemProvider
{
    private readonly SeaGlassFilterChip _chip;

    public SeaGlassFilterChipAutomationPeer(SeaGlassFilterChip owner) : base(owner)
    {
        _chip = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassFilterChip);

    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_chip);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (!string.IsNullOrWhiteSpace(_chip.Label)) return _chip.Label;
        if (_chip.Content is string s && !string.IsNullOrWhiteSpace(s)) return s;
        return GetClassNameCore();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Toggle) return this;
        if (patternInterface == PatternInterface.SelectionItem) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ToggleState ToggleState => _chip.IsSelected ? ToggleState.On : ToggleState.Off;

    public void Toggle()
    {
        if (!_chip.IsEnabled) throw new ElementNotEnabledException();
        _chip.IsSelected = !_chip.IsSelected;
        _chip.Selected = _chip.IsSelected;
    }

    public bool IsSelected => _chip.IsSelected;
    public IRawElementProviderSimple SelectionContainer => null!;
    public void AddToSelection() => _chip.IsSelected = true;
    public void RemoveFromSelection() => _chip.IsSelected = false;
    public void Select() => _chip.IsSelected = true;
}

public class SeaGlassSegmentedButtonRowAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
{
    private readonly SeaGlassSegmentedButtonRow _row;

    public SeaGlassSegmentedButtonRowAutomationPeer(SeaGlassSegmentedButtonRow owner) : base(owner)
    {
        _row = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassSegmentedButtonRow);
    protected override string GetNameCore() => AutomationProperties.GetName(_row) ?? nameof(SeaGlassSegmentedButtonRow);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Selection) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool CanSelectMultiple => false;
    public bool IsSelectionRequired => true;
    public IRawElementProviderSimple[] GetSelection() => Array.Empty<IRawElementProviderSimple>();
}

public class SeaGlassSegmentedButtonItemAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider, IInvokeProvider
{
    private readonly SeaGlassSegmentedButtonItem _item;

    public SeaGlassSegmentedButtonItemAutomationPeer(SeaGlassSegmentedButtonItem owner) : base(owner)
    {
        _item = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassSegmentedButtonItem);
    protected override string GetNameCore() => AutomationProperties.GetName(_item) ?? _item.Label ?? (_item.Content as string) ?? nameof(SeaGlassSegmentedButtonItem);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.SelectionItem) return this;
        if (patternInterface == PatternInterface.Invoke) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsSelected => _item.IsSelected;
    public IRawElementProviderSimple SelectionContainer => null!;
    public void Select()
    {
        _item.IsSelected = true;
        _item.PerformClick();
    }
    public void AddToSelection() => Select();
    public void RemoveFromSelection() => _item.IsSelected = false;
    public void Invoke() => Select();
}

public class SeaGlassBadgeAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly SeaGlassBadge _badge;

    public SeaGlassBadgeAutomationPeer(SeaGlassBadge owner) : base(owner)
    {
        _badge = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassBadge);
    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_badge);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (!string.IsNullOrWhiteSpace(_badge.Text)) return _badge.Text;
        if (_badge.Count > 0) return _badge.Count.ToString();
        if (_badge.Content is string s && !string.IsNullOrWhiteSpace(s)) return s;
        return nameof(SeaGlassBadge);
    }
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
}

public class SeaGlassPebbleOrbAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly SeaGlassPebbleOrb _orb;

    public SeaGlassPebbleOrbAutomationPeer(SeaGlassPebbleOrb owner) : base(owner)
    {
        _orb = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassPebbleOrb);
    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_orb);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        return nameof(SeaGlassPebbleOrb);
    }
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke) return this;
        return base.GetPatternCore(patternInterface);
    }

    public void Invoke() => _orb.PerformClick();
}
#endregion


using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SeaGlass.WinUI;

namespace SeaGlass.WinUI.Automation;
#region Navigation and Bars Peers
public class SeaGlassTopAppBarAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly SeaGlassTopAppBar _bar;

    public SeaGlassTopAppBarAutomationPeer(SeaGlassTopAppBar owner) : base(owner)
    {
        _bar = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassTopAppBar);
    protected override string GetNameCore() => AutomationProperties.GetName(_bar) ?? _bar.Title ?? nameof(SeaGlassTopAppBar);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.AppBar;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke) return this;
        return base.GetPatternCore(patternInterface);
    }

    public void Invoke() => _bar.OnBack();
}

public class SeaGlassFluidBarAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
{
    private readonly SeaGlassFluidBar _bar;

    public SeaGlassFluidBarAutomationPeer(SeaGlassFluidBar owner) : base(owner)
    {
        _bar = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassFluidBar);
    protected override string GetNameCore() => AutomationProperties.GetName(_bar) ?? nameof(SeaGlassFluidBar);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Selection) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool CanSelectMultiple => false;
    public bool IsSelectionRequired => true;
    public IRawElementProviderSimple[] GetSelection() => Array.Empty<IRawElementProviderSimple>();
}

public class SeaGlassMiniPlayerBarAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly SeaGlassMiniPlayerBar _bar;

    public SeaGlassMiniPlayerBarAutomationPeer(SeaGlassMiniPlayerBar owner) : base(owner)
    {
        _bar = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassMiniPlayerBar);
    protected override string GetNameCore() => AutomationProperties.GetName(_bar) ?? _bar.Title ?? nameof(SeaGlassMiniPlayerBar);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke) return this;
        return base.GetPatternCore(patternInterface);
    }

    public void Invoke() => _bar.PerformClick();
}

public class SeaGlassFloatingToolbarAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider, IInvokeProvider
{
    private readonly SeaGlassFloatingToolbar _bar;

    public SeaGlassFloatingToolbarAutomationPeer(SeaGlassFloatingToolbar owner) : base(owner)
    {
        _bar = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassFloatingToolbar);
    protected override string GetNameCore() => AutomationProperties.GetName(_bar) ?? nameof(SeaGlassFloatingToolbar);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Selection) return this;
        if (patternInterface == PatternInterface.Invoke) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool CanSelectMultiple => false;
    public bool IsSelectionRequired => false;
    public IRawElementProviderSimple[] GetSelection() => Array.Empty<IRawElementProviderSimple>();
    public void Invoke() => _bar.PerformClick();
}

public class SeaGlassPillNavBarAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
{
    private readonly SeaGlassPillNavBar _bar;

    public SeaGlassPillNavBarAutomationPeer(SeaGlassPillNavBar owner) : base(owner)
    {
        _bar = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassPillNavBar);
    protected override string GetNameCore() => AutomationProperties.GetName(_bar) ?? nameof(SeaGlassPillNavBar);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Selection) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool CanSelectMultiple => false;
    public bool IsSelectionRequired => true;
    public IRawElementProviderSimple[] GetSelection() => Array.Empty<IRawElementProviderSimple>();
}

public class SeaGlassPromptBarAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
    private readonly SeaGlassPromptBar _bar;

    public SeaGlassPromptBarAutomationPeer(SeaGlassPromptBar owner) : base(owner)
    {
        _bar = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassPromptBar);
    protected override string GetNameCore() => AutomationProperties.GetName(_bar) ?? _bar.PromptText ?? _bar.PlaceholderText ?? nameof(SeaGlassPromptBar);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Value) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsReadOnly => false;
    public string Value => _bar.PromptText ?? string.Empty;
    public void SetValue(string value) => _bar.PromptText = value;
}


public class SeaGlassSearchBarAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
    private readonly SeaGlassSearchBar _searchBar;

    public SeaGlassSearchBarAutomationPeer(SeaGlassSearchBar owner) : base(owner)
    {
        _searchBar = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassSearchBar);

    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_searchBar);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (!string.IsNullOrWhiteSpace(_searchBar.Placeholder)) return _searchBar.Placeholder;
        return GetClassNameCore();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Value) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsReadOnly => false;
    public string Value => _searchBar.QueryText ?? _searchBar.Query ?? string.Empty;

    public void SetValue(string value)
    {
        if (!_searchBar.IsEnabled) throw new ElementNotEnabledException();
        _searchBar.QueryText = value;
        _searchBar.Query = value;
    }
}
#endregion


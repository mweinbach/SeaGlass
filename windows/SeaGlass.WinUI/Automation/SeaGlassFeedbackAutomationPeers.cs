using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SeaGlass.WinUI;

namespace SeaGlass.WinUI.Automation;
#region Feedback and Lists Peers
public class SeaGlassLinearProgressIndicatorAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    private readonly SeaGlassLinearProgressIndicator _progress;

    public SeaGlassLinearProgressIndicatorAutomationPeer(SeaGlassLinearProgressIndicator owner) : base(owner)
    {
        _progress = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassLinearProgressIndicator);
    protected override string GetNameCore() => AutomationProperties.GetName(_progress) ?? nameof(SeaGlassLinearProgressIndicator);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.RangeValue) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsReadOnly => true;
    public double LargeChange => 0.1;
    public double SmallChange => 0.01;
    public double Minimum => 0.0;
    public double Maximum => 1.0;
    public double Value => _progress.Progress;
    public void SetValue(double value) => throw new InvalidOperationException("ProgressBar is read-only per UIA control-type spec.");
}

public class SeaGlassCircularProgressIndicatorAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    private readonly SeaGlassCircularProgressIndicator _progress;

    public SeaGlassCircularProgressIndicatorAutomationPeer(SeaGlassCircularProgressIndicator owner) : base(owner)
    {
        _progress = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassCircularProgressIndicator);
    protected override string GetNameCore() => AutomationProperties.GetName(_progress) ?? nameof(SeaGlassCircularProgressIndicator);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.RangeValue) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsReadOnly => true;
    public double LargeChange => 0.1;
    public double SmallChange => 0.01;
    public double Minimum => 0.0;
    public double Maximum => 1.0;
    public double Value => _progress.Progress;
    public void SetValue(double value) => throw new InvalidOperationException("ProgressBar is read-only per UIA control-type spec.");
}

public class SeaGlassListItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, ISelectionItemProvider
{
    private readonly SeaGlassListItem _item;

    public SeaGlassListItemAutomationPeer(SeaGlassListItem owner) : base(owner)
    {
        _item = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassListItem);
    protected override string GetNameCore() => AutomationProperties.GetName(_item) ?? _item.PrimaryText ?? _item.HeadlineText ?? nameof(SeaGlassListItem);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke) return this;
        if (patternInterface == PatternInterface.SelectionItem) return this;
        return base.GetPatternCore(patternInterface);
    }

    public void Invoke() => _item.PerformClick();
    public bool IsSelected => _item.IsSelected;
    public IRawElementProviderSimple SelectionContainer => null!;
    public void Select() => _item.IsSelected = true;
    public void AddToSelection() => _item.IsSelected = true;
    public void RemoveFromSelection() => _item.IsSelected = false;
}

public class SeaGlassSnackbarAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly SeaGlassSnackbar _bar;

    public SeaGlassSnackbarAutomationPeer(SeaGlassSnackbar owner) : base(owner)
    {
        _bar = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassSnackbar);
    protected override string GetNameCore() => AutomationProperties.GetName(_bar) ?? _bar.Message ?? nameof(SeaGlassSnackbar);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.StatusBar;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke) return this;
        if (patternInterface == PatternInterface.Invoke) return this;
        return base.GetPatternCore(patternInterface);
    }

    public void Invoke() => _bar.PerformClick();
}
#endregion

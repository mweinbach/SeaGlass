using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SeaGlass.WinUI;

namespace SeaGlass.WinUI.Automation;
#region Inputs and Toggles Peers
public class SeaGlassSwitchAutomationPeer : FrameworkElementAutomationPeer, IToggleProvider
{
    private readonly SeaGlassSwitch _switch;

    public SeaGlassSwitchAutomationPeer(SeaGlassSwitch owner) : base(owner)
    {
        _switch = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassSwitch);

    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_switch);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (_switch.Content is string s && !string.IsNullOrWhiteSpace(s)) return s;
        return GetClassNameCore();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Toggle) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ToggleState ToggleState => _switch.IsOn ? ToggleState.On : ToggleState.Off;

    public void Toggle()
    {
        if (!_switch.IsEnabled) throw new ElementNotEnabledException();
        _switch.IsOn = !_switch.IsOn;
    }
}

public class SeaGlassCheckboxAutomationPeer : FrameworkElementAutomationPeer, IToggleProvider
{
    private readonly SeaGlassCheckbox _checkbox;

    public SeaGlassCheckboxAutomationPeer(SeaGlassCheckbox owner) : base(owner)
    {
        _checkbox = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassCheckbox);

    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_checkbox);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (_checkbox.Content is string s && !string.IsNullOrWhiteSpace(s)) return s;
        return GetClassNameCore();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Toggle) return this;
        return base.GetPatternCore(patternInterface);
    }

    public ToggleState ToggleState => _checkbox.IsChecked switch
    {
        true => ToggleState.On,
        false => ToggleState.Off,
        null => ToggleState.Indeterminate
    };

    public void Toggle()
    {
        if (!_checkbox.IsEnabled) throw new ElementNotEnabledException();
        _checkbox.IsChecked = !(_checkbox.IsChecked == true);
    }
}

public class SeaGlassRadioButtonAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider
{
    private readonly SeaGlassRadioButton _radio;

    public SeaGlassRadioButtonAutomationPeer(SeaGlassRadioButton owner) : base(owner)
    {
        _radio = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassRadioButton);

    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_radio);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (_radio.Content is string s && !string.IsNullOrWhiteSpace(s)) return s;
        return GetClassNameCore();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.RadioButton;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.SelectionItem) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsSelected => _radio.IsChecked;
    public IRawElementProviderSimple SelectionContainer => null!;
    public void Select() => _radio.IsChecked = true;
    public void AddToSelection() => _radio.IsChecked = true;
    public void RemoveFromSelection() => _radio.IsChecked = false;
}

public class SeaGlassLiquidSliderAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    private readonly SeaGlassLiquidSlider _slider;

    public SeaGlassLiquidSliderAutomationPeer(SeaGlassLiquidSlider owner) : base(owner)
    {
        _slider = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassLiquidSlider);

    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_slider);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (!string.IsNullOrWhiteSpace(_slider.Title)) return _slider.Title;
        return GetClassNameCore();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.RangeValue) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsReadOnly => false;
    public double LargeChange => 10.0;
    public double SmallChange => 1.0;
    public double Minimum => _slider.Minimum;
    public double Maximum => _slider.Maximum;
    public double Value => _slider.Value;

    public void SetValue(double value)
    {
        if (!_slider.IsEnabled) throw new ElementNotEnabledException();
        _slider.Value = Math.Clamp(value, _slider.Minimum, _slider.Maximum);
    }
}

public class SeaGlassTextFieldAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
    private readonly SeaGlassTextField _textField;

    public SeaGlassTextFieldAutomationPeer(SeaGlassTextField owner) : base(owner)
    {
        _textField = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override string GetClassNameCore() => nameof(SeaGlassTextField);

    protected override string GetNameCore()
    {
        string? name = AutomationProperties.GetName(_textField);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (!string.IsNullOrWhiteSpace(_textField.Label)) return _textField.Label;
        if (!string.IsNullOrWhiteSpace(_textField.Placeholder)) return _textField.Placeholder;
        return GetClassNameCore();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Value) return this;
        return base.GetPatternCore(patternInterface);
    }

    public bool IsReadOnly => false;
    public string Value => _textField.Text ?? string.Empty;

    public void SetValue(string value)
    {
        if (!_textField.IsEnabled) throw new ElementNotEnabledException();
        _textField.Text = value;
    }
}


#endregion


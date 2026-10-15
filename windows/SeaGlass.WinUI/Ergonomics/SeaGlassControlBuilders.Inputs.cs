using System;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

public static partial class SeaGlassControlBuilders
{
    // ==========================================
    // SeaGlassSwitch
    // ==========================================
    public static SeaGlassSwitch IsOn(this SeaGlassSwitch toggle, bool isOn = true)
    {
        toggle.IsOn = isOn;
        return toggle;
    }

    public static SeaGlassSwitch OnToggled(this SeaGlassSwitch toggle, Action<bool> handler)
    {
        toggle.Toggled += (_, on) => handler(on);
        return toggle;
    }

    public static SeaGlassSwitch OnToggled(this SeaGlassSwitch toggle, EventHandler<bool> handler)
    {
        toggle.Toggled += handler;
        return toggle;
    }

    public static SeaGlassSwitch BindIsOn(this SeaGlassSwitch toggle, Func<bool> getter, Action<bool> setter)
    {
        toggle.IsOn = getter();
        toggle.Toggled += (_, on) => setter(on);
        return toggle;
    }

    public static SeaGlassSwitch IsOn(this SeaGlassSwitch toggle, Func<bool> getter, Action<bool> setter) =>
        toggle.BindIsOn(getter, setter);

    // ==========================================
    // SeaGlassLiquidSlider
    // ==========================================
    public static SeaGlassLiquidSlider Value(this SeaGlassLiquidSlider slider, double value)
    {
        slider.Value = value;
        return slider;
    }

    public static SeaGlassLiquidSlider Range(this SeaGlassLiquidSlider slider, double min, double max)
    {
        slider.Minimum = min;
        slider.Maximum = max;
        return slider;
    }

    public static SeaGlassLiquidSlider Step(this SeaGlassLiquidSlider slider, double step)
    {
        slider.Step = step;
        return slider;
    }

    public static SeaGlassLiquidSlider Title(this SeaGlassLiquidSlider slider, string title)
    {
        slider.Title = title;
        return slider;
    }

    public static SeaGlassLiquidSlider ValueText(this SeaGlassLiquidSlider slider, string valueText)
    {
        slider.ValueText = valueText;
        return slider;
    }

    public static SeaGlassLiquidSlider OnValueChanged(this SeaGlassLiquidSlider slider, Action<double> handler)
    {
        slider.ValueChanged += (_, val) => handler(val);
        return slider;
    }

    public static SeaGlassLiquidSlider OnValueChanged(this SeaGlassLiquidSlider slider, EventHandler<double> handler)
    {
        slider.ValueChanged += handler;
        return slider;
    }

    public static SeaGlassLiquidSlider BindValue(this SeaGlassLiquidSlider slider, Func<double> getter, Action<double> setter)
    {
        slider.Value = getter();
        slider.ValueChanged += (_, val) => setter(val);
        return slider;
    }

    public static SeaGlassLiquidSlider Value(this SeaGlassLiquidSlider slider, Func<double> getter, Action<double> setter) =>
        slider.BindValue(getter, setter);

    // ==========================================
    // SeaGlassTextField
    // ==========================================
    public static SeaGlassTextField Text(this SeaGlassTextField textField, string text)
    {
        textField.Text = text;
        return textField;
    }

    public static SeaGlassTextField Placeholder(this SeaGlassTextField textField, string placeholder)
    {
        textField.Placeholder = placeholder;
        return textField;
    }

    public static SeaGlassTextField Label(this SeaGlassTextField textField, string label)
    {
        textField.Label = label;
        return textField;
    }

    public static SeaGlassTextField LeadingGlyph(this SeaGlassTextField textField, SeaGlassGlyph? glyph)
    {
        textField.LeadingGlyph = glyph;
        return textField;
    }

    public static SeaGlassTextField HasError(this SeaGlassTextField textField, bool hasError = true)
    {
        textField.HasError = hasError;
        return textField;
    }

    public static SeaGlassTextField ErrorMessage(this SeaGlassTextField textField, string? errorMessage)
    {
        textField.ErrorMessage = errorMessage;
        return textField;
    }

    public static SeaGlassTextField OnTextChanged(this SeaGlassTextField textField, Action<string> handler)
    {
        textField.TextChanged += (_, txt) => handler(txt);
        return textField;
    }

    public static SeaGlassTextField OnTextChanged(this SeaGlassTextField textField, EventHandler<string> handler)
    {
        textField.TextChanged += handler;
        return textField;
    }

    public static SeaGlassTextField BindText(this SeaGlassTextField textField, Func<string> getter, Action<string> setter)
    {
        textField.Text = getter();
        textField.TextChanged += (_, txt) => setter(txt);
        return textField;
    }

    public static SeaGlassTextField Text(this SeaGlassTextField textField, Func<string> getter, Action<string> setter) =>
        textField.BindText(getter, setter);

    // ==========================================
    // SeaGlassCheckbox
    // ==========================================
    public static SeaGlassCheckbox Checked(this SeaGlassCheckbox checkbox, bool isChecked = true)
    {
        checkbox.Checked = isChecked;
        return checkbox;
    }

    public static SeaGlassCheckbox IsChecked(this SeaGlassCheckbox checkbox, bool? isChecked)
    {
        checkbox.IsChecked = isChecked;
        return checkbox;
    }

    public static SeaGlassCheckbox IsThreeState(this SeaGlassCheckbox checkbox, bool isThreeState = true)
    {
        checkbox.IsThreeState = isThreeState;
        return checkbox;
    }

    public static SeaGlassCheckbox OnCheckChanged(this SeaGlassCheckbox checkbox, Action<bool> handler)
    {
        checkbox.OnCheckedChange = handler;
        return checkbox;
    }

    public static SeaGlassCheckbox OnCheckStateChanged(this SeaGlassCheckbox checkbox, EventHandler<bool?> handler)
    {
        checkbox.CheckStateChanged += handler;
        return checkbox;
    }

    public static SeaGlassCheckbox BindChecked(this SeaGlassCheckbox checkbox, Func<bool> getter, Action<bool> setter)
    {
        checkbox.Checked = getter();
        checkbox.CheckStateChanged += (_, chk) =>
        {
            if (chk.HasValue) setter(chk.Value);
        };
        return checkbox;
    }

    public static SeaGlassCheckbox Checked(this SeaGlassCheckbox checkbox, Func<bool> getter, Action<bool> setter) =>
        checkbox.BindChecked(getter, setter);

    // ==========================================
    // SeaGlassRadioButton
    // ==========================================
    public static SeaGlassRadioButton Selected(this SeaGlassRadioButton radio, bool selected = true)
    {
        radio.Selected = selected;
        return radio;
    }

    public static SeaGlassRadioButton GroupName(this SeaGlassRadioButton radio, string groupName)
    {
        radio.GroupName = groupName;
        return radio;
    }

    public static SeaGlassRadioButton OnChecked(this SeaGlassRadioButton radio, EventHandler<bool> handler)
    {
        radio.Checked += handler;
        return radio;
    }

    public static SeaGlassRadioButton OnSelect(this SeaGlassRadioButton radio, Action handler)
    {
        radio.OnSelect = handler;
        return radio;
    }

    public static SeaGlassRadioButton BindSelected(this SeaGlassRadioButton radio, Func<bool> getter, Action<bool> setter)
    {
        radio.Selected = getter();
        radio.Checked += (_, sel) => setter(sel);
        return radio;
    }

    public static SeaGlassRadioButton Selected(this SeaGlassRadioButton radio, Func<bool> getter, Action<bool> setter) =>
        radio.BindSelected(getter, setter);
}

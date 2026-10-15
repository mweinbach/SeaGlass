using System;
using System.Collections.Generic;
using ICommand = global::System.Windows.Input.ICommand;
using Microsoft.UI.Xaml;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

public static partial class SeaGlassControlBuilders
{
    // ==========================================
    // SeaGlassButton
    // ==========================================
    public static SeaGlassButton Label(this SeaGlassButton button, string text)
    {
        button.Label = text;
        button.Content = text;
        return button;
    }

    public static SeaGlassButton LeadingGlyph(this SeaGlassButton button, SeaGlassGlyph? glyph)
    {
        button.LeadingGlyph = glyph;
        return button;
    }

    public static SeaGlassButton OnClick(this SeaGlassButton button, Action onClick)
    {
        button.OnClick = onClick;
        return button;
    }

    public static SeaGlassButton OnClick(this SeaGlassButton button, RoutedEventHandler handler)
    {
        button.Click += handler;
        return button;
    }

    public static SeaGlassButton Command(this SeaGlassButton button, ICommand command, object? parameter = null)
    {
        button.Command = command;
        button.CommandParameter = parameter;
        return button;
    }

    // ==========================================
    // SeaGlassIconButton
    // ==========================================
    public static SeaGlassIconButton Glyph(this SeaGlassIconButton button, SeaGlassGlyph glyph)
    {
        button.Glyph = glyph;
        return button;
    }

    public static SeaGlassIconButton OnClick(this SeaGlassIconButton button, Action onClick)
    {
        button.OnClick = onClick;
        return button;
    }

    public static SeaGlassIconButton OnClick(this SeaGlassIconButton button, RoutedEventHandler handler)
    {
        button.Click += handler;
        return button;
    }

    public static SeaGlassIconButton Selected(this SeaGlassIconButton button, bool selected = true)
    {
        button.Selected = selected;
        return button;
    }

    // ==========================================
    // SeaGlassPill
    // ==========================================
    public static SeaGlassPill Selected(this SeaGlassPill pill, bool isSelected = true)
    {
        pill.Selected = isSelected;
        return pill;
    }

    public static SeaGlassPill Kind(this SeaGlassPill pill, SeaGlassPillKind kind)
    {
        pill.Kind = kind;
        return pill;
    }

    public static SeaGlassPill OnSelectedChanged(this SeaGlassPill pill, Action<bool> handler)
    {
        pill.SelectedChanged += (_, sel) => handler(sel);
        return pill;
    }

    public static SeaGlassPill OnSelectedChanged(this SeaGlassPill pill, EventHandler<bool> handler)
    {
        pill.SelectedChanged += handler;
        return pill;
    }

    public static SeaGlassPill BindSelected(this SeaGlassPill pill, Func<bool> getter, Action<bool> setter)
    {
        pill.Selected = getter();
        pill.SelectedChanged += (_, sel) => setter(sel);
        return pill;
    }

    public static SeaGlassPill Selected(this SeaGlassPill pill, Func<bool> getter, Action<bool> setter) =>
        pill.BindSelected(getter, setter);

    // ==========================================
    // SeaGlassFilterChip
    // ==========================================
    public static SeaGlassFilterChip Selected(this SeaGlassFilterChip chip, bool isSelected = true)
    {
        chip.Selected = isSelected;
        return chip;
    }

    public static SeaGlassFilterChip ShowLeadingCheck(this SeaGlassFilterChip chip, bool show = true)
    {
        chip.ShowLeadingCheck = show;
        return chip;
    }

    public static SeaGlassFilterChip OnSelectedChanged(this SeaGlassFilterChip chip, Action<bool> handler)
    {
        chip.SelectedChanged += (_, sel) => handler(sel);
        return chip;
    }

    public static SeaGlassFilterChip OnSelectedChanged(this SeaGlassFilterChip chip, EventHandler<bool> handler)
    {
        chip.SelectedChanged += handler;
        return chip;
    }

    public static SeaGlassFilterChip BindSelected(this SeaGlassFilterChip chip, Func<bool> getter, Action<bool> setter)
    {
        chip.Selected = getter();
        chip.SelectedChanged += (_, sel) => setter(sel);
        return chip;
    }

    public static SeaGlassFilterChip Selected(this SeaGlassFilterChip chip, Func<bool> getter, Action<bool> setter) =>
        chip.BindSelected(getter, setter);

    // ==========================================
    // SeaGlassBadge
    // ==========================================
    public static SeaGlassBadge Label(this SeaGlassBadge badge, string text)
    {
        badge.Label = text;
        return badge;
    }

    public static SeaGlassBadge Count(this SeaGlassBadge badge, int count)
    {
        badge.Count = count;
        return badge;
    }

    public static SeaGlassBadge IsDot(this SeaGlassBadge badge, bool isDot = true)
    {
        badge.IsDot = isDot;
        return badge;
    }

    // ==========================================
    // SeaGlassExtendedFab
    // ==========================================
    public static SeaGlassExtendedFab Label(this SeaGlassExtendedFab fab, string text)
    {
        fab.Label = text;
        return fab;
    }

    public static SeaGlassExtendedFab Glyph(this SeaGlassExtendedFab fab, SeaGlassGlyph? glyph)
    {
        fab.LeadingGlyph = glyph;
        return fab;
    }

    public static SeaGlassExtendedFab OnClick(this SeaGlassExtendedFab fab, Action onClick)
    {
        fab.OnClick = onClick;
        return fab;
    }

    public static SeaGlassExtendedFab OnClick(this SeaGlassExtendedFab fab, RoutedEventHandler handler)
    {
        fab.Click += handler;
        return fab;
    }

    // ==========================================
    // SeaGlassSegmentedButtonRow
    // ==========================================
    public static SeaGlassSegmentedButtonRow SelectedIndex(this SeaGlassSegmentedButtonRow row, int index)
    {
        row.SelectedIndex = index;
        return row;
    }

    public static SeaGlassSegmentedButtonRow Options(this SeaGlassSegmentedButtonRow row, IReadOnlyList<string> options)
    {
        row.Options = options;
        return row;
    }

    public static SeaGlassSegmentedButtonRow OnSelectedIndexChanged(this SeaGlassSegmentedButtonRow row, Action<int> handler)
    {
        row.RegisterPropertyChangedCallback(SeaGlassSegmentedButtonRow.SelectedIndexProperty, (s, dp) =>
        {
            if (s is SeaGlassSegmentedButtonRow r) handler(r.SelectedIndex);
        });
        return row;
    }

    public static SeaGlassSegmentedButtonRow BindSelectedIndex(this SeaGlassSegmentedButtonRow row, Func<int> getter, Action<int> setter)
    {
        row.SelectedIndex = getter();
        row.RegisterPropertyChangedCallback(SeaGlassSegmentedButtonRow.SelectedIndexProperty, (s, dp) =>
        {
            if (s is SeaGlassSegmentedButtonRow r) setter(r.SelectedIndex);
        });
        return row;
    }

    // ==========================================
    // SeaGlassPebbleOrb
    // ==========================================
    public static SeaGlassPebbleOrb Glyph(this SeaGlassPebbleOrb orb, SeaGlassGlyph glyph)
    {
        orb.Glyph = glyph;
        return orb;
    }

    public static SeaGlassPebbleOrb OrbSize(this SeaGlassPebbleOrb orb, double sizeDp)
    {
        orb.OrbSizeDp = sizeDp;
        return orb;
    }

    public static SeaGlassPebbleOrb OnClick(this SeaGlassPebbleOrb orb, Action onClick)
    {
        orb.OnClick = onClick;
        return orb;
    }
}

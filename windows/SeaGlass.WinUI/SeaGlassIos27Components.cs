using System;
using System.Collections.Generic;

namespace SeaGlass.WinUI;

/// <summary>
/// Quick-action button inside a <see cref="SeaGlassFloatingSheet"/> action strip.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassSheetQuickAction</c> on Android.
/// </summary>
public sealed record SeaGlassSheetQuickAction(
    string Id,
    string Label,
    SeaGlassGlyph Glyph,
    bool Active = false);

/// <summary>
/// Single item inside an iOS-style <see cref="SeaGlassContextMenuOverlay"/>.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassContextMenuItem</c> on Android.
/// </summary>
public sealed record SeaGlassContextMenuItem(
    string Id,
    string Title,
    SeaGlassGlyph Glyph,
    string? Subtitle = null,
    bool IsDestructive = false,
    bool StartsNewSection = false,
    bool Checked = false,
    string? SubmenuTitle = null,
    IReadOnlyList<SeaGlassContextMenuItem>? SubmenuItems = null)
{
    public bool HasSubmenu => SubmenuItems is { Count: > 0 };
}

/// <summary>
/// Single notification item inside a <see cref="SeaGlassNotificationStack"/>.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassNotificationItem</c> on Android.
/// </summary>
public sealed record SeaGlassNotificationItem(
    string Id,
    string AppName,
    string TimeLabel,
    string Title,
    string Body,
    SeaGlassGlyph Glyph);

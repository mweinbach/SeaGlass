using System;
using Microsoft.UI.Xaml;

namespace SeaGlass.WinUI;

public static partial class SeaGlassLayout
{
    public static SeaGlassCard Card(UIElement? content = null, string? title = null, string? subtitle = null)
    {
        var card = new SeaGlassCard();
        if (content != null) card.Content = content;
        if (!string.IsNullOrEmpty(title)) card.Title = title;
        if (!string.IsNullOrEmpty(subtitle)) card.Subtitle = subtitle;
        return card;
    }

    public static SeaGlassCard Card(string title, UIElement content) =>
        Card(content, title, null);

    public static SeaGlassCard Card(string title, string subtitle, UIElement content) =>
        Card(content, title, subtitle);
}

public static partial class SeaGlassControlBuilders
{
    // Layout builder forwards for Card
    public static SeaGlassCard Card(UIElement? content = null, string? title = null, string? subtitle = null) =>
        SeaGlassLayout.Card(content, title, subtitle);

    public static SeaGlassCard Card(string title, UIElement content) =>
        SeaGlassLayout.Card(title, content);

    public static SeaGlassCard Card(string title, string subtitle, UIElement content) =>
        SeaGlassLayout.Card(title, subtitle, content);

    // ==========================================
    // SeaGlassCard
    // ==========================================
    public static SeaGlassCard Title(this SeaGlassCard card, string title)
    {
        card.Title = title;
        return card;
    }

    public static SeaGlassCard WithTitle(this SeaGlassCard card, string title) =>
        card.Title(title);

    public static SeaGlassCard Subtitle(this SeaGlassCard card, string subtitle)
    {
        card.Subtitle = subtitle;
        return card;
    }

    public static SeaGlassCard WithSubtitle(this SeaGlassCard card, string subtitle) =>
        card.Subtitle(subtitle);

    public static SeaGlassCard Header(this SeaGlassCard card, object header)
    {
        card.Header = header;
        return card;
    }

    public static SeaGlassCard WithHeader(this SeaGlassCard card, object header) =>
        card.Header(header);

    public static SeaGlassCard ElevationDepth(this SeaGlassCard card, double depthDp)
    {
        card.ElevationDepthDp = depthDp;
        return card;
    }

    public static SeaGlassCard WithElevationDepth(this SeaGlassCard card, double depthDp) =>
        card.ElevationDepth(depthDp);

    // ==========================================
    // SeaGlassDialogCard
    // ==========================================
    public static SeaGlassDialogCard Title(this SeaGlassDialogCard dialog, string title)
    {
        dialog.Title = title;
        return dialog;
    }

    public static SeaGlassDialogCard Body(this SeaGlassDialogCard dialog, string body)
    {
        dialog.Body = body;
        return dialog;
    }

    public static SeaGlassDialogCard Glyph(this SeaGlassDialogCard dialog, SeaGlassGlyph glyph)
    {
        dialog.Glyph = glyph;
        return dialog;
    }

    public static SeaGlassDialogCard IsOpen(this SeaGlassDialogCard dialog, bool isOpen = true)
    {
        dialog.IsOpen = isOpen;
        return dialog;
    }

    public static SeaGlassDialogCard OnConfirmed(this SeaGlassDialogCard dialog, Action onConfirm)
    {
        dialog.Confirmed += (_, _) => onConfirm();
        return dialog;
    }

    public static SeaGlassDialogCard OnDismissed(this SeaGlassDialogCard dialog, Action onDismiss)
    {
        dialog.Dismissed += (_, _) => onDismiss();
        return dialog;
    }

    public static SeaGlassDialogCard ConfirmAction(this SeaGlassDialogCard dialog, string label, Action? onConfirm = null)
    {
        dialog.ConfirmLabel = label;
        if (onConfirm != null) dialog.Confirmed += (_, _) => onConfirm();
        return dialog;
    }

    public static SeaGlassDialogCard DismissAction(this SeaGlassDialogCard dialog, string label, Action? onDismiss = null)
    {
        dialog.DismissLabel = label;
        if (onDismiss != null) dialog.Dismissed += (_, _) => onDismiss();
        return dialog;
    }
}

using System.Collections.Generic;

namespace SeaGlass.Sample.Catalog;

public sealed record CategoryInfo(
    string Key,
    string DisplayName,
    string Glyph,
    string Description);

public static class CategoryDefinitions
{
    public const string Surfaces = "Surfaces & Foundations";
    public const string ButtonsAndChips = "Buttons & Chips";
    public const string NavigationAndBars = "Navigation & Bars";
    public const string InputsAndToggles = "Inputs & Toggles";
    public const string FeedbackAndProgress = "Feedback & Progress";
    public const string MorphingSuite = "Dynamic Morphing Suite";
    public const string OverlaysAndSheets = "Overlays & Sheets";
    public const string WindowsExtras = "Windows Native Extras";

    public static readonly IReadOnlyList<CategoryInfo> AllCategories =
    [
        new(Surfaces, Surfaces, "🪟", "Volumetric sea-glass containers, cards, adaptive corner pods, and backdrop hosts."),
        new(ButtonsAndChips, ButtonsAndChips, "🔘", "Tactile buttons, filter chips, badges, and morphing actions with 3D press response."),
        new(NavigationAndBars, NavigationAndBars, "🧭", "Top app bars, coalescing fluid navigation bars, mini players, and search bars."),
        new(InputsAndToggles, InputsAndToggles, "🎛️", "Sliders, switches, checkboxes, text fields, and cylindrical drum pickers."),
        new(FeedbackAndProgress, FeedbackAndProgress, "📊", "Progress bars, spinners, list items, snackbars, and rich status callouts."),
        new(MorphingSuite, MorphingSuite, "🌀", "2D SDF smooth-union shape morphing, budding satellite droplets, and choreography."),
        new(OverlaysAndSheets, OverlaysAndSheets, "📑", "Frosted glass bottom sheets, flyouts, floating overlays, and live activities."),
        new(WindowsExtras, WindowsExtras, "🪟", "Deep Windows 11 platform integration: TitleBar, NavigationView, and TabView.")
    ];
}

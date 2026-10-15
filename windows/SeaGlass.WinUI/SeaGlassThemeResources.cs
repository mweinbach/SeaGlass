using System;
using Microsoft.UI.Xaml;

namespace SeaGlass.WinUI;

/// <summary>
/// Single drop-in theme resource dictionary for SeaGlass WinUI 3.
/// Allows consumers to reference SeaGlass styles via App.xaml:
/// <code>
/// &lt;Application.Resources&gt;
///     &lt;ResourceDictionary&gt;
///         &lt;ResourceDictionary.MergedDictionaries&gt;
///             &lt;controls:XamlControlsResources /&gt;
///             &lt;sg:SeaGlassThemeResources /&gt;
///         &lt;/ResourceDictionary.MergedDictionaries&gt;
///     &lt;/ResourceDictionary&gt;
/// &lt;/Application.Resources&gt;
/// </code>
/// Note: Standard WinUI 3 controls benefit from declaring &lt;controls:XamlControlsResources /&gt;
/// in Application.Resources. SeaGlass also provides internal token fallbacks for standard brushes
/// and metrics in Tokens.xaml to prevent crashes even if XamlControlsResources is omitted.
/// </summary>
public sealed class SeaGlassThemeResources : ResourceDictionary
{
    public SeaGlassThemeResources()
    {
        string[] candidates =
        [
            "ms-appx:///SeaGlass.WinUI/Themes/Generic.xaml",
            "ms-appx:///Themes/Generic.xaml",
            "ms-appx:///testhost/Themes/Generic.xaml"
        ];

        foreach (string uriStr in candidates)
        {
            try
            {
                Source = new Uri(uriStr);
                return;
            }
            catch
            {
                // Try next URI candidate
            }
        }
    }
}

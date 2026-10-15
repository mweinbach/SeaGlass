using System;
using Microsoft.UI.Xaml;

namespace SeaGlass.Sample.Common;

/// <summary>
/// Descriptor representing a single SeaGlass control in the gallery catalog.
/// Contains metadata, instantiation factories, and code generation hooks.
/// </summary>
public sealed record ControlShowcaseDescriptor(
    string Name,
    string Category,
    string Description,
    string TypeName,
    string RequiredPageName,
    Func<FrameworkElement> PreviewFactory,
    Action<FrameworkElement, TuningViewModel>? ConfigurePreview = null,
    Func<FrameworkElement>? ActionFactory = null,
    Func<TuningViewModel, string>? CustomXamlSnippet = null,
    Func<TuningViewModel, string>? CustomFluentSnippet = null);

using System.ComponentModel;
using System.Runtime.CompilerServices;
using SeaGlass.Core;
using SeaGlass.WinUI;

namespace SeaGlass.Sample.Common;

/// <summary>
/// Observable view model for real-time optical and M3 Expressive token tuning in the gallery showcase.
/// </summary>
public sealed class TuningViewModel : INotifyPropertyChanged
{
    private double _coarseness = 0.42;
    private double _thicknessDp = 16.0;
    private double _ior = 1.52;
    private double _rimChalkiness = 0.55;
    private double _tintDensity = 0.68;
    private SeaGlassTone _tone = SeaGlassTone.SeafoamMint;
    private SeaGlassVariant _variant = SeaGlassVariant.Primary;
    private SeaGlassControlSize _size = SeaGlassControlSize.Medium;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double Coarseness
    {
        get => _coarseness;
        set => SetField(ref _coarseness, value);
    }

    public double ThicknessDp
    {
        get => _thicknessDp;
        set => SetField(ref _thicknessDp, value);
    }

    public double Ior
    {
        get => _ior;
        set => SetField(ref _ior, value);
    }

    public double RimChalkiness
    {
        get => _rimChalkiness;
        set => SetField(ref _rimChalkiness, value);
    }

    public double TintDensity
    {
        get => _tintDensity;
        set => SetField(ref _tintDensity, value);
    }

    public SeaGlassTone Tone
    {
        get => _tone;
        set => SetField(ref _tone, value);
    }

    public SeaGlassVariant Variant
    {
        get => _variant;
        set => SetField(ref _variant, value);
    }

    public SeaGlassControlSize Size
    {
        get => _size;
        set => SetField(ref _size, value);
    }

    public void ResetToDefaults()
    {
        Coarseness = 0.42;
        ThicknessDp = 16.0;
        Ior = 1.52;
        RimChalkiness = 0.55;
        TintDensity = 0.68;
        Tone = SeaGlassTone.SeafoamMint;
        Variant = SeaGlassVariant.Primary;
        Size = SeaGlassControlSize.Medium;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

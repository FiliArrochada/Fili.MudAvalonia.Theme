using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Fili.MudAvalonia.Theme.Converters;

/// <summary>
/// Multiplies a bound <see cref="double"/> by a constant factor given as the converter parameter.
/// </summary>
///
/// <remarks>
/// <para>
/// This exists for one reason: MudBlazor sizes and positions several things as a PERCENTAGE of
/// the control they sit in, and Avalonia has no way to say that. Transform strings reject a `%`
/// unit outright (<c>FormatException: Invalid unit: %</c>, thrown at runtime — the XAML compiles
/// either way), and a binding cannot do arithmetic. So a percentage becomes a binding to a pixel
/// dimension the control already publishes, times a factor.
/// </para>
/// <para>
/// It is <c>internal</c> deliberately. This package ships resources, not an API: nothing here is
/// meant to be referenced from an app, and a consumer who needs a multiply converter should own
/// one rather than depend on this. Compiled XAML in the same assembly can construct an internal
/// type, so being internal costs nothing.
/// </para>
/// <para>
/// The parameter is parsed with <see cref="CultureInfo.InvariantCulture"/> on purpose: it is a
/// literal written in a XAML file, not user data, so <c>"-0.875"</c> must mean the same thing on
/// a machine whose decimal separator is a comma.
/// </para>
/// </remarks>
internal sealed class FactorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double input || double.IsNaN(input))
        {
            return AvaloniaProperty.UnsetValue;
        }

        return parameter switch
        {
            double factor => input * factor,
            string text when double.TryParse(
                text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                => input * parsed,
            _ => AvaloniaProperty.UnsetValue,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException(
            "FactorConverter is one-way: it feeds animation key frames, which never write back.");
}

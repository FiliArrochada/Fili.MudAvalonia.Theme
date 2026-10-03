using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Fili.Theme.MudAvalonia.Converters;

/// <summary>
/// Turns one entry of <c>DataValidationErrors.Errors</c> into the sentence a user should read.
/// </summary>
///
/// <remarks>
/// <para>
/// An entry is whatever the binding produced: usually an <see cref="Exception"/>, sometimes a
/// string from a validation attribute. Binding straight to it renders
/// <c>ToString()</c> — and for an exception that is
/// <c>"System.InvalidOperationException: Must be a valid email address"</c>, which is what the
/// field showed the first time this was wired up. MudBlazor's helper text is the message alone.
/// </para>
/// <para>
/// Internal, like <see cref="FactorConverter"/>: it exists because a template needs it, not
/// because a consumer does.
/// </para>
/// </remarks>
internal sealed class ErrorMessageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            null => null,
            Exception exception => exception.Message,
            string text => text,
            _ => value.ToString(),
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("ErrorMessageConverter is one-way: an error is never edited.");
}

// Copyright (c) v2rayWinUI Contributors. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Data;

namespace v2rayWinUI.UI.Xaml.Converter;

/// <summary>
/// Converter to negate a boolean value
/// Used in XAML bindings to inverse boolean properties
/// </summary>
public sealed class NegatedBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool boolValue && !boolValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is bool boolValue && !boolValue;
    }
}

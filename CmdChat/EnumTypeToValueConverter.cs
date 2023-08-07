using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CmdChat.ViewModels;
using CmdChatApi.Types;

namespace CmdChat;

public class OnlineStatusToColorConverter : EnumTypeToValueConverter<SolidColorBrush, ContactStatus>
{
}

public class StatusColorMap : EnumValueToValueMap<SolidColorBrush, ContactStatus>
{
}

public class EnumTypeToValueConverter<TValue, TEnum> : IValueConverter
    where TEnum : Enum
{
    private bool _isInitialized;
    private readonly Dictionary<TEnum, TValue> _enumValueToValueMap = new();

    private void VerifyInitialized()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        var enumValues = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
        foreach (var enumValue in enumValues)
        {
            _enumValueToValueMap[enumValue] = DefaultValue;
        }

        foreach (var enumValueToValueMap in EnumValueToValueMap)
        {
            _enumValueToValueMap[enumValueToValueMap.EnumValue] = enumValueToValueMap.Value;
        }
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TEnum e) return null;
        VerifyInitialized();
        return _enumValueToValueMap[e];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public TValue DefaultValue { get; set; }
    public Collection<EnumValueToValueMap<TValue, TEnum>> EnumValueToValueMap { get; set; } = new();
}

public class EnumValueToValueMap<TValue, TEnum>
    where TEnum : Enum
{
    public TEnum EnumValue { get; set; }
    public TValue Value { get; set; }
}
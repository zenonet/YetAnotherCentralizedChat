using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Common;

namespace ClientApp;

public class MessageToAlignmentConverter : IValueConverter
{
    public static readonly MessageToAlignmentConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Message msg && targetType.IsAssignableTo(typeof(HorizontalAlignment)))
        {
            if (App.Client == null)
            {
                // If this is a test environment, make every message of a user with an even id right aligned
                return msg.SenderId % 2 == 0 ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            return msg.SenderName == App.Client.Username ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }
        // converter used for the wrong type
        return new BindingNotification(new InvalidCastException(), 
            BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, 
        object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
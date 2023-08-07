using System.Runtime.CompilerServices;
using ReactiveUI;

namespace CmdChatApi.Types;

public class NotifyObject : ReactiveObject
{
    public bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        this.RaisePropertyChanged(propertyName);
        return true;
    }
}
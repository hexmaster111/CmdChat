using System.Collections.ObjectModel;

namespace CmdChatApi.Types;

public abstract class Contact : NotifyObject
{
    private string _name = string.Empty;
    private ContactStatus _status = ContactStatus.Offline;
    private ulong _userId;
    private bool _isFriend;

    public ObservableCollection<Message> DirectMessages { get; } = new();

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public ContactStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ulong UserId
    {
        get => _userId;
        set => SetField(ref _userId, value);
    }
    
    public bool IsFriend
    {
        get => _isFriend;
        set => SetField(ref _isFriend, value);
    }
    
    public override string ToString()
    {
        return $"{Name} ({UserId})";
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is Contact other)
        {
            return other.UserId == UserId;
        }

        return false;
    }
}
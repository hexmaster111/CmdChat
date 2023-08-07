namespace CmdChatApi.Types;

public abstract class Message : NotifyObject
{
    private string _messageText;
    private DateTime _timestamp;
    private Contact _sender;

    protected Message(string messageText, DateTime timestamp, Contact sender)
    {
        MessageText = messageText;
        Timestamp = timestamp;
        Sender = sender;
    }

    public string MessageText
    {
        get => _messageText;
        set => SetField(ref _messageText, value);
    }

    public DateTime Timestamp
    {
        get => _timestamp;
        set => SetField(ref _timestamp, value);
    }

    public Contact Sender
    {
        get => _sender;
        set => SetField(ref _sender, value);
    }
}
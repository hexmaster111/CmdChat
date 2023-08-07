using System.Collections.ObjectModel;
using System.Reactive;
using CmdChatApi;
using CmdChatApi.Types;
using ReactiveUI;

namespace CmdChat.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(ChatApi api)
    {
        _api = api;
        Contacts = _api.Contacts;
        AddUserCommand = ReactiveCommand.Create<string>(AddFriendUser);
        SendMessageCommand = ReactiveCommand.Create(SendMessage);
    }

    private readonly ChatApi _api;
    public ObservableCollection<Contact> Contacts { get; }
    public ReactiveCommand<string, Unit> AddUserCommand { get; }
    public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }


    private string _messageDraftText = string.Empty;

    public string MessageDraftText
    {
        get => _messageDraftText;
        set => this.RaiseAndSetIfChanged(ref _messageDraftText, value);
    }

    private Contact? _selectedUser;

    public Contact SelectedUser
    {
        get => _selectedUser;
        set
        {
            
            _api.SelectedUserId = value?.UserId??0;
            this.RaiseAndSetIfChanged(ref _selectedUser, value);
        }
    }

    void AddFriendUser(string id)
    {
        _api.AddFriend(ulong.Parse(id));
    }

    void SendMessage()
    {
        if (SelectedUser != null)
        {
            _api.SendDm(SelectedUser.UserId, MessageDraftText);
            MessageDraftText = string.Empty;
        }
    }
}
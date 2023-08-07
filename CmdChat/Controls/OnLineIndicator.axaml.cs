using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CmdChat.ViewModels;
using CmdChatApi.Types;

namespace CmdChat.Controls;

public partial class OnLineIndicator : UserControl
{
    private ContactStatus _status;

    public static readonly DirectProperty<OnLineIndicator, ContactStatus> StatusProperty =
        AvaloniaProperty.RegisterDirect<OnLineIndicator, ContactStatus>("Status", o => o.Status, (o, v) => o.Status = v);

    public OnLineIndicator() => InitializeComponent();

    public ContactStatus Status
    {
        get { return _status; }
        set { SetAndRaise(StatusProperty, ref _status, value); }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
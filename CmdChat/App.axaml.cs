using System.Net;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CmdChat.ViewModels;
using CmdChat.Views;
using CmdChatApi;

namespace CmdChat;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ChatApi api = new("https://localhost:7177", 5254415346834758247);
            api.StartUpdates();


            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            ExpressionObserver.DataValidators.RemoveAll(x => x is DataAnnotationsValidationPlugin);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(api)
            };
            base.OnFrameworkInitializationCompleted();
        }
    }
}

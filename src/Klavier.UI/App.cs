using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Klavier.UI.Views;

namespace Klavier.UI;

public class App(
    Func<MainWindow> mainWindowFactory)
    : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mainWindowFactory();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

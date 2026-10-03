using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Fili.Theme.MudAvalonia.Gallery;

/// <summary>
/// The gallery application. Its theme is the two includes in App.axaml - exactly what a consuming
/// app writes, with no code behind them.
/// </summary>
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // The browser build: no windows, one view filling the page.
            singleView.MainView = new MainView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

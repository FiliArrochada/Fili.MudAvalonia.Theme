using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Fili.MudAvalonia.Theme.Gallery;

/// <summary>The desktop host for <see cref="MainView"/>.</summary>
public partial class MainWindow : Window
{
    public MainWindow() => AvaloniaXamlLoader.Load(this);
}

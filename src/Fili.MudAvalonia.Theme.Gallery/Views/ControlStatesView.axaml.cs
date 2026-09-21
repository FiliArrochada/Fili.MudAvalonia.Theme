using System;
using Avalonia.Controls;

namespace Fili.MudAvalonia.Theme.Gallery.Views;

public partial class ControlStatesView : UserControl
{
    public ControlStatesView()
    {
        InitializeComponent();

        // A real validation failure, set the way a failing binding sets one. It is done here
        // rather than in markup because DataValidationErrors.Errors takes a live collection and
        // x:Array does not survive Avalonia's compiled XAML - and because a hand-set `.error`
        // class would prove nothing: the point of this sample is that the RULE turns red and the
        // MESSAGE appears without the app asking for either.
        DataValidationErrors.SetError(
            InvalidField,
            new InvalidOperationException("Must be a valid email address"));
    }
}

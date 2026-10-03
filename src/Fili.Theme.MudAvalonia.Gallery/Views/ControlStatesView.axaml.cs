using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Fili.Theme.MudAvalonia.Gallery.Views;

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

        // Keyboard focus, pinned so it shows up in a capture. NavigationMethod.Tab is what sets
        // :focus-visible; focusing with Pointer deliberately does not, which is the whole reason
        // the theme keys off :focus-visible rather than :focus.
        //
        // Only one element can hold focus, so the second sample is faked with the pseudo-class
        // directly. That is not something an app should do - it is a screenshot prop.
        Loaded += (_, _) =>
        {
            FocusedSample.Focus(NavigationMethod.Tab);
            SelectedNode.IsSelected = true;
            ((IPseudoClasses)FocusedText.Classes).Add(":focus-visible");
        };
    }
}

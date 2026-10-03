# Razor and AXAML

The same UI written twice: MudBlazor Razor first, then the AXAML that gives the same look with this
theme. For the full parameter-by-parameter list, see [MudBlazor parity](mudblazor-parity.md).

Two rules explain almost every pair below:

- **A class names a variant, and nothing else.** A MudBlazor parameter value such as
  `Variant.Outlined` or `Color.Error` becomes a lowercase class: `outlined`, `error`. Defaults
  need no class, because every control is themed by its type.
- **Behaviour stays Avalonia's.** Binding, events, validation and layout are ordinary Avalonia.
  The theme only supplies the look, so `@bind-Value` becomes a `{Binding}` and `OnClick` becomes
  `Command` or `Click`.

## Setup

```razor
@* MainLayout.razor *@
<MudThemeProvider @bind-IsDarkMode="_dark" />
```

```xml
<!-- App.axaml -->
<Application.Styles>
  <StyleInclude Source="avares://Fili.Theme.MudAvalonia/Themes/Base/FiliBaseTheme.axaml" />
  <StyleInclude Source="avares://Fili.Theme.MudAvalonia/FiliTheme.axaml" />
</Application.Styles>
```

```csharp
// IsDarkMode, and this theme's third variant
Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
Application.Current!.RequestedThemeVariant = FiliThemeVariants.HighContrast;
```

## Buttons

```razor
<MudButton>Learn more</MudButton>
<MudButton Color="Color.Primary">Learn more</MudButton>
<MudButton Variant="Variant.Outlined" Color="Color.Error">Delete</MudButton>
<MudButton Variant="Variant.Filled" Color="Color.Primary">Save</MudButton>
<MudButton Variant="Variant.Filled" Color="Color.Success" Size="Size.Small">OK</MudButton>
<MudButton Variant="Variant.Filled" Disabled="true">Saving…</MudButton>
<MudButton Color="Color.Inherit">Sign in</MudButton>
```

```xml
<Button Content="Learn more" />
<Button Classes="primary" Content="Learn more" />
<Button Classes="outlined error" Content="Delete" />
<Button Classes="filled primary" Content="Save" />
<Button Classes="filled success small" Content="OK" />
<Button Classes="filled" Content="Saving…" IsEnabled="False" />
<Button Classes="inherit" Content="Sign in" />
```

`primary` on its own is a primary *text* button, exactly as `Color="Color.Primary"` is without a
`Variant`. The fill comes from `filled`.

```razor
<MudButton Variant="Variant.Filled" Color="Color.Primary" DropShadow="false">Flat</MudButton>
<MudIconButton Icon="@Icons.Material.Filled.Menu" />
<MudIconButton Icon="@Icons.Material.Filled.Favorite" Color="Color.Primary" Size="Size.Small" />
<MudIconButton Icon="@Icons.Material.Filled.Add" Variant="Variant.Filled" Color="Color.Primary" />
```

```xml
<Button Classes="filled primary flat" Content="Flat" />
<Button Classes="icon"><PathIcon Data="{StaticResource MenuIcon}" /></Button>
<Button Classes="icon primary small"><PathIcon Data="{StaticResource FavoriteIcon}" /></Button>
<Button Classes="icon filled primary"><PathIcon Data="{StaticResource AddIcon}" /></Button>
```

The glyphs are the app's: copy the path from MudBlazor's `Icons/Material/Filled.cs` into a
`StreamGeometry`, without its `M0 0h24v24H0z` spacer.

## Chips

```razor
<MudChip T="string">Default</MudChip>
<MudChip T="string" Color="Color.Primary">Primary</MudChip>
<MudChip T="string" Variant="Variant.Outlined" Color="Color.Secondary">Secondary</MudChip>
<MudChip T="string" Variant="Variant.Text" Color="Color.Success" Size="Size.Small">Small</MudChip>

<MudChipSet T="string" SelectionMode="SelectionMode.SingleSelection">
    <MudChip Value="@("a")" Color="Color.Primary">Selected</MudChip>
</MudChipSet>
```

```xml
<Button Classes="chip" Content="Default" />
<Button Classes="chip primary" Content="Primary" />
<Button Classes="chip outlined secondary" Content="Secondary" />
<Button Classes="chip text success small" Content="Small" />

<ToggleButton Classes="chip primary" Content="Selected" IsChecked="{Binding IsA}" />
```

A selectable chip is a `ToggleButton`, and selected is `IsChecked`; the set's rules are bindings.

## Button groups

```razor
<MudButtonGroup Variant="Variant.Filled" Color="Color.Primary">
    <MudButton>Deploy</MudButton>
    <MudMenu Icon="@Icons.Material.Filled.ArrowDropDown">
        <MudMenuItem>Deploy to staging</MudMenuItem>
    </MudMenu>
</MudButtonGroup>
```

```xml
<SplitButton Classes="filled primary" Content="Deploy">
  <SplitButton.Flyout>
    <MenuFlyout>
      <MenuItem Header="Deploy to staging" />
    </MenuFlyout>
  </SplitButton.Flyout>
</SplitButton>
```

A split button takes the same classes as a button and they mean the same things:
`outlined primary`, `filled error small`, and so on.

## Typography

```razor
<MudText Typo="Typo.h6">Section</MudText>
<MudText Typo="Typo.caption">Supporting line</MudText>
<MudText Typo="Typo.overline">Label</MudText>
<MudText>Body text</MudText>
<MudText Typo="Typo.body2" Color="Color.Error">Could not reach the store</MudText>
<MudText Typo="Typo.body2" Class="mud-text-secondary">Last synced an hour ago</MudText>
```

```xml
<TextBlock Classes="h6" Text="Section" />
<TextBlock Classes="caption" Text="Supporting line" />
<TextBlock Classes="overline" Text="Label" />
<TextBlock Text="Body text" />
<TextBlock Classes="body2 error" Text="Could not reach the store" />
<TextBlock Classes="body2" Foreground="{DynamicResource FiliTextSecondaryBrush}" Text="Last synced an hour ago" />
```

## Surfaces

```razor
<MudPaper Elevation="4" Class="pa-4">
    <MudText Typo="Typo.h6">Section</MudText>
</MudPaper>

<MudPaper Outlined="true" Class="pa-4">…</MudPaper>
```

```xml
<Border Classes="surface elevation4" Padding="16">
  <TextBlock Classes="h6" Text="Section" />
</Border>

<Border Classes="surface outlined" Padding="16">…</Border>
```

`pa-4` is MudBlazor's 4 × 4px spacing step. Here it is a plain `Padding="16"`, or
`{DynamicResource FiliSpacing4}` where a resource is wanted.

## App bar

```razor
<MudAppBar>
    <MudText Typo="Typo.h6">Library</MudText>
    <MudSpacer />
    <MudButton Color="Color.Inherit">Sign in</MudButton>
</MudAppBar>
```

```xml
<Border Classes="appbar">
  <DockPanel>
    <Button DockPanel.Dock="Right" Classes="inherit" Content="Sign in" VerticalAlignment="Center" />
    <TextBlock Classes="h6" Text="Library" VerticalAlignment="Center" />
  </DockPanel>
</Border>
```

The 24px gutters are MudAppBar's `Gutters`, on by default; `Padding="0"` turns them off.

```razor
<MudAppBar Dense="true" Color="Color.Secondary">…</MudAppBar>
```

```xml
<Border Classes="appbar dense secondary">…</Border>
```

The bar's text colour is inherited. A control that sets its own colour, such as a select, keeps
it, just as a `MudSelect` keeps `text-primary` in an app bar. `inherit` is how a button asks for
the bar's colour instead.

## Text fields

```razor
<MudTextField @bind-Value="_email" Label="Email" />
<MudTextField @bind-Value="_email" Label="Email" Variant="Variant.Filled" />
<MudTextField @bind-Value="_email" Label="Email" Variant="Variant.Outlined" />
<MudTextField @bind-Value="_email" Label="Email" Variant="Variant.Outlined" Error="true" />
```

```xml
<TextBox Text="{Binding Email}" PlaceholderText="Email" />
<TextBox Text="{Binding Email}" PlaceholderText="Email" Classes="filled" />
<TextBox Text="{Binding Email}" PlaceholderText="Email" Classes="outlined" />
<TextBox Text="{Binding Email}" PlaceholderText="Email" Classes="outlined error" />
```

`PlaceholderText` is the floating label. A binding that fails validation shows its message under
the field without `error`, like MudBlazor's validation does.

```razor
<MudTextField @bind-Value="_email" Label="Email" HelperText="We never share it" />
<MudTextField @bind-Value="_title" Label="Title" Counter="40" MaxLength="40" />
<MudTextField @bind-Value="_email" Label="Email" Margin="Margin.Dense" Variant="Variant.Outlined" />
```

```xml
<TextBox Text="{Binding Email}" PlaceholderText="Email" AutomationProperties.HelpText="We never share it" />
<TextBox Text="{Binding Title}" PlaceholderText="Title" Classes="counter" MaxLength="40" />
<TextBox Text="{Binding Email}" PlaceholderText="Email" Classes="outlined dense" />
```

The helper text is the accessibility property, so a screen reader reads it as well. The counter
counts against `MaxLength`; without one it shows the length alone, as `Counter="0"` does.

## Selects and numbers

```razor
<MudSelect T="string" @bind-Value="_platform" Label="Platform">
    <MudSelectItem Value="@("PC")">PC</MudSelectItem>
    <MudSelectItem Value="@("Switch")">Switch</MudSelectItem>
</MudSelect>

<MudSelect T="string" @bind-Value="_library" Label="Library" Variant="Variant.Outlined"
           HelperText="Where new files go" Margin="Margin.Dense">…</MudSelect>

<MudNumericField @bind-Value="_quantity" Label="Quantity" />
```

```xml
<ComboBox SelectedItem="{Binding Platform}" PlaceholderText="Platform">
  <ComboBoxItem>PC</ComboBoxItem>
  <ComboBoxItem>Switch</ComboBoxItem>
</ComboBox>

<NumericUpDown Value="{Binding Quantity}" PlaceholderText="Quantity" />
```

```xml
<ComboBox SelectedItem="{Binding Library}" PlaceholderText="Library" Classes="outlined dense"
          AutomationProperties.HelpText="Where new files go">…</ComboBox>
```

## Selection controls

```razor
<MudCheckBox @bind-Value="_sync" Label="Enable sync" />
<MudRadioGroup @bind-Value="_cadence">
    <MudRadio Value="@("weekly")">Weekly</MudRadio>
    <MudRadio Value="@("monthly")">Monthly</MudRadio>
</MudRadioGroup>
<MudSwitch @bind-Value="_dark" Label="Dark mode" />
<MudSlider @bind-Value="_volume" />
```

```xml
<CheckBox IsChecked="{Binding Sync}" Content="Enable sync" />
<RadioButton GroupName="cadence" Content="Weekly" />
<RadioButton GroupName="cadence" Content="Monthly" />
<ToggleSwitch IsChecked="{Binding Dark}" Content="Dark mode" />
<Slider Value="{Binding Volume}" />
```

With no class these are MudBlazor's defaults: grey checkboxes, radios and switches, and a primary
slider. Colour and size are classes, exactly as `Color` and `Size` are parameters:

```razor
<MudCheckBox @bind-Value="_sync" Color="Color.Primary" Size="Size.Small" Label="Enable sync" />
<MudSwitch @bind-Value="_online" Color="Color.Success" Label="Online" />
<MudSlider @bind-Value="_volume" Color="Color.Secondary" Size="Size.Medium" />
```

```xml
<CheckBox IsChecked="{Binding Sync}" Classes="primary small" Content="Enable sync" />
<ToggleSwitch IsChecked="{Binding Online}" Classes="success" Content="Online" />
<Slider Value="{Binding Volume}" Classes="secondary medium" />
```

## Progress, dividers, links

```razor
<MudProgressLinear Value="65" />
<MudProgressLinear Color="Color.Primary" Size="Size.Large" Rounded="true" Value="65" />
<MudProgressLinear Color="Color.Primary" Indeterminate="true" />

<MudDivider />
<MudDivider DividerType="DividerType.Inset" />
<MudDivider Vertical="true" FlexItem="true" />
<MudDivider Light="true" />

<MudLink Href="/docs">Learn more</MudLink>
<MudLink Href="/docs" Underline="Underline.Always">Learn more</MudLink>
```

```xml
<ProgressBar Value="65" />
<ProgressBar Classes="primary large rounded" Value="65" />
<ProgressBar Classes="primary" IsIndeterminate="True" />

<Separator />
<Separator Classes="inset" />
<Separator Classes="vertical" />
<Separator Classes="light" />

<HyperlinkButton NavigateUri="https://example.com/docs" Content="Learn more" />
<HyperlinkButton Classes="underline" NavigateUri="https://example.com/docs" Content="Learn more" />
```

## Tabs, lists and panels

```razor
<MudTabs>
    <MudTabPanel Text="One">First tab</MudTabPanel>
    <MudTabPanel Text="Two">Second tab</MudTabPanel>
</MudTabs>

<MudList T="string" Dense="true">
    <MudListItem Text="First" />
    <MudListItem Text="Second" />
</MudList>

<MudExpansionPanels>
    <MudExpansionPanel Text="Details">…</MudExpansionPanel>
</MudExpansionPanels>
```

```xml
<TabControl>
  <TabItem Header="One"><TextBlock Text="First tab" /></TabItem>
  <TabItem Header="Two"><TextBlock Text="Second tab" /></TabItem>
</TabControl>

<!-- <MudTabs Color="Color.Primary" Centered="true" Rounded="true" Border="true"> -->
<TabControl Classes="primary centered rounded border">…</TabControl>

<ListBox Classes="surface">
  <ListBoxItem Classes="dense">First</ListBoxItem>
  <ListBoxItem Classes="dense">Second</ListBoxItem>
</ListBox>

<Expander Header="Details">…</Expander>
```

## Feedback

```razor
<MudAlert Severity="Severity.Info">Library scan finished</MudAlert>
<MudAlert Severity="Severity.Warning" Variant="Variant.Outlined">Two folders were skipped</MudAlert>
<MudAlert Severity="Severity.Error" Variant="Variant.Filled" Dense="true">Could not reach the store</MudAlert>

<MudSkeleton Width="200px" />
<MudSkeleton SkeletonType="SkeletonType.Circle" Width="40px" Height="40px" />
```

```xml
<Border Classes="alert info"><TextBlock Text="Library scan finished" /></Border>
<Border Classes="alert outlined warning"><TextBlock Text="Two folders were skipped" /></Border>
<Border Classes="alert filled error dense"><TextBlock Text="Could not reach the store" /></Border>

<Border Classes="skeleton" Width="200" />
<Border Classes="skeleton circle" Width="40" Height="40" />
```

MudAlert's severity icon is not drawn for you: put a `PathIcon` next to the text and it takes the
alert's colour.

```razor
<MudTooltip Text="Saved to the cloud">
    <MudButton>Sync</MudButton>
</MudTooltip>

@inject ISnackbar Snackbar
@code {
    void Done() => Snackbar.Add("Library scan finished", Severity.Success);
}
```

```xml
<Button Content="Sync" ToolTip.Tip="Saved to the cloud" />
```

```csharp
// once, for the window
var notifications = new WindowNotificationManager(TopLevel.GetTopLevel(this))
{
    Position = NotificationPosition.BottomRight,
};

// MudSnackbar's look comes with the theme
notifications.Show(new Notification(null, "Library scan finished", NotificationType.Success));
```

## Drawer

```razor
<MudLayout>
    <MudDrawer @bind-Open="_open" Variant="DrawerVariant.Persistent">
        <MudNavMenu>…</MudNavMenu>
    </MudDrawer>
    <MudMainContent>…</MudMainContent>
</MudLayout>
```

```xml
<SplitView IsPaneOpen="{Binding Open}" DisplayMode="Inline">
  <SplitView.Pane>…</SplitView.Pane>
  <!-- main content -->
</SplitView>
```

The drawer's 240px width and its background and text colours come from the theme. `DisplayMode`
covers MudBlazor's drawer variants: `Inline` (persistent), `Overlay` (temporary), and
`CompactInline` / `CompactOverlay` (mini).

# Fili.MudAvalonia.Theme

A design-token theme for Avalonia 12: palette, elevation, typography and geometry, in light, dark
and high contrast, with a gallery app to look at it in.

**[See the gallery live](https://filiarrochada.github.io/Fili.MudAvalonia.Theme/)** — the same app,
compiled to WebAssembly and served by GitHub Pages.

Coming from MudBlazor? [MudBlazor parity](docs/mudblazor-parity.md) maps every component
parameter and palette property to its class or token, and [Razor and AXAML](docs/razor-vs-axaml.md)
shows the same UI written both ways.

The token values are MudBlazor's defaults, transcribed from its source rather than eyeballed. The
aim is the *look* — this is not a component library: no new control types, no services, and one
public type. That type is `FiliThemeVariants`, whose only member is the high-contrast
`ThemeVariant`; it has to be public, because XAML can reach a custom variant only through
`x:Static`. Two `internal` converters exist where XAML cannot express a MudBlazor rule; see
*Two converters* below.

```
src/Fili.MudAvalonia.Theme                  the theme (the NuGet package)
src/Fili.MudAvalonia.Theme.Gallery          gallery UI, shared across heads
src/Fili.MudAvalonia.Theme.Gallery.Desktop  desktop head
src/Fili.MudAvalonia.Theme.Gallery.Browser  browser head (WebAssembly), deployed to GitHub Pages
tst/Fili.MudAvalonia.Theme.UnitTests        headless resource-resolution tests
tst/Fili.MudAvalonia.Theme.PixelTests       gallery frames diffed against committed PNGs
```

## Using it

**Standalone: no substrate theme.** Two includes, and order matters - base first, Fili second,
because later styles win for overlapping setters:

```xml
<Application.Styles>
  <StyleInclude Source="avares://Fili.MudAvalonia.Theme/Themes/Base/FiliBaseTheme.axaml" />
  <StyleInclude Source="avares://Fili.MudAvalonia.Theme/FiliTheme.axaml" />
</Application.Styles>
```

No `FluentTheme`, no `SimpleTheme`, no `Avalonia.Themes.*` package reference at all. The base is
this package's own fork of Avalonia Simple templates - see *The base* below.

Then use the tokens by key, and the type ramp by class:

```xml
<Border Classes="surface elevation4" Padding="16">
  <StackPanel Spacing="4">
    <TextBlock Classes="h6" Text="Section" />
    <TextBlock Classes="caption" Text="Supporting line" />
    <TextBlock Classes="body2 error" Text="Could not reach the store" />
  </StackPanel>
</Border>

<Border Background="{DynamicResource FiliPrimaryBrush}" />
```

A colour class on a `TextBlock` is `MudText`'s `Color`: the palette colour, so `secondary` is the
secondary colour. Grey supporting text is `text-secondary`, which MudBlazor reaches through its
`mud-text-secondary` utility class rather than a `Color`; here it is
`Foreground="{DynamicResource FiliTextSecondaryBrush}"`.

Always `DynamicResource`, never `StaticResource` — see **The one rule** below.

### Classes are variants, not opt-in

Every control this theme templates is keyed to its **type**, so it arrives themed with no markup
change at all. A class only ever names a MudBlazor **variant** — `Button.primary`,
`TextBox.filled`, `ProgressBar.large` — exactly as `Color.Primary` and `Variant.Filled` do there.

This was not always true. The package used to key every `ControlTheme` to a class, including a
literal `fili` marker meaning "please theme this one", because it layered over `FluentTheme` and
overriding a default `ControlTheme` would reach into controls composed out of that type. Once the
base became a fork of Simple that this repo owns, the hazard went with it: a `TextBox` inside a
`NumericUpDown` *should* look like this theme's text field. **The `fili` marker classes are gone**
— if you have `Classes="fili"` anywhere, delete it; it does nothing.

### Buttons

MudButton's whole Variant × Color × Size matrix, one class for each parameter:

```xml
<Button Content="Learn more" />                         text, default colour — MudButton's defaults
<Button Classes="primary" Content="Learn more" />       text, primary
<Button Classes="outlined error" Content="Delete" />    outlined, error
<Button Classes="filled primary" Content="Save" />      filled, primary, elevation 2/4/8
<Button Classes="filled success small" Content="OK" />  filled, success, small
<Button Classes="inherit" Content="Sign in" />          the surrounding text colour
```

- **Variant:** `text` (the default), `outlined`, `filled`. Each is a `ControlTheme`.
- **Colour:** `primary`, `secondary`, `tertiary`, `info`, `success`, `warning`, `error`, `dark`,
  `inherit`, or none for `Color.Default`.
- **Size:** `small`, `large`, or none for `Size.Medium`.

**A colour class is only a colour**, as `Color` is in MudBlazor: `primary` on its own is a primary
*text* button, and the fill comes from `filled`. With no colour, a button is `text-primary` with a
grey hover, which is MudBlazor's default and not Material's primary one.

Every value is from `_button.scss`. Text and outlined buttons tint with the colour at 6%
(`{color}-hover`) on hover, focus and press. Filled ones switch to `{color}-darken` and lift 2 → 4
→ 8. Outlined's line is the colour itself, because `BorderOpacity` is 1.0. The darker and lighter
shades are MudBlazor's own derivation, HSL lightness ∓ 0.075 with .NET's rounding, ported to C# in
`MudColorPort`; `PaletteDerivationTests` checks every shade token against it in all three variants,
and the port against the 14 values mudblazor.com publishes. `Button.axaml` is written by
`ThemeColourGenerator` from one list of colours, so no colour can differ from the others, and
`GeneratedThemeTests` fails if the file is edited by hand. `ButtonMatrixTests` checks every cell of
the matrix, at rest and on a real pointer hover. Both helpers live in the unit-test project, under
`Generation/`.

**The class names are MudBlazor's vocabulary, and they collide on purpose.** `primary`,
`secondary`, `outlined`, `text` and `filled` are `Color.Primary`, `Color.Secondary`,
`Variant.Outlined`, `Variant.Text` and `Variant.Filled` — API familiarity is the point of this
package, so they are not namespaced to `mud-primary` or hidden behind an attached property.

**All 59 of them, which is the list to grep an app against before adopting:**

| | |
|---|---|
| Type ramp (`TextBlock`) | `h1`–`h6`, `subtitle1`, `subtitle2`, `body1`, `body2`, `caption`, `overline` |
| Surfaces (`Border`) | `surface`, `appbar`, `elevation0/1/2/4/6/8/12/16/24` |
| Colour (`Color`) | `primary`, `secondary`, `tertiary`, `info`, `success`, `warning`, `error`, `dark`, `inherit` |
| Shape (`Variant`) | `text`, `filled`, `outlined`, `rounded`, `flat` |
| Size (`Size`) | `small`, `medium`, `large`, `dense` |
| Placement | `inset`, `middle`, `vertical`, `light`, `underline`, `no-underline`, `border`, `centered`, `hide-slider` |
| Component | `icon`, `chip`, `alert`, `skeleton`, `circle`, `rectangle`, `counter` |
| Behaviour | `helper-on-focus`, `no-animation` |

`ClassVocabularyTests` asserts that list is exactly what the theme uses, in both directions — an
undeclared class is a collision nobody signed off on, and a declared one no selector uses is a
lie to adopters. It walks the styles *and* the control themes, because the variant names live in
a resource dictionary rather than in the styles collection: walking only `Application.Styles`
finds barely half of them.

Note how ordinary those words are — `small`, `flat`, `middle`, `vertical`, `error`. That is the
hazard, and it is why the list is a published interface rather than an implementation detail.

The cost is real and adopters should expect it: **an app that already uses those words gets its
controls retemplated the moment it includes this theme.** Fili.PlaySphere collided on `primary`
in 20 places on its first day. Nothing broke visibly — its `/template/` selectors still matched
by name — but hover and press were being driven by the app's flat fill *and* the theme's state
layer at once. The reconciliation is to let the theme own shape and interaction and keep the app's
colour, which is two setters and a deletion. Grep for `Classes="` before adopting.

**An unclassed `<Button>` is the text button**, because `MudButton.Variant` defaults to
`Variant.Text`. The classes opt *up* from flat rather than opting in to being themed.

The raised variants step elevation 2 → 4 → 8 across rest, hover and press, and drop to 0 when
disabled. That step is the thing Fluent cannot express at all, because `Button` has no `BoxShadow`
property — only the `Border` inside a template does. `flat` is `DropShadow="false"`: a filled
button that stays at elevation 0 in every state.

**`icon` is MudIconButton, `chip` is MudChip** - classes on a `Button`, not new controls:

```xml
<Button Classes="icon"><PathIcon Data="{StaticResource MenuIcon}" /></Button>      grey
<Button Classes="icon primary small"><PathIcon Data="…" /></Button>
<Button Classes="chip" Content="Default" />                                        grey, filled
<ToggleButton Classes="chip primary" Content="Selected" IsChecked="True" />
```

An unclassed icon button is `action-default` grey rather than text-primary, because
`.mud-icon-button` comes after `.mud-button` in MudBlazor's bundle; a colour class beats it. The
`PathIcon` is 24px (18 small, 36 large) and the package ships no icon set - the gallery's glyphs
are its own. A chip is MudChip's `Variant.Filled` by default; on a `ToggleButton`, `IsChecked` is
`MudChip.GetVariant`'s swap, so a selected filled chip draws as a text chip and a selected text
chip as a filled one. Chip selection rules (`MudChipSet`) stay with the app.

**`alert` and `skeleton` are on a `Border`.** `<Border Classes="alert warning">` is a text-variant
MudAlert: the colour at 6% behind text in the colour's *darken* shade, which is what `_alert.scss`
does and is easy to mistake for the colour itself; a `PathIcon` inside takes the colour.
`outlined`, `filled` and `dense` are MudAlert's own. `<Border Classes="skeleton" Width="200" />`
pulses on MudSkeleton's timing, and `circle` / `rectangle` are its other two types;
`no-animation` is `Animation.False`. The pixel suite masks a pulsing skeleton for the same reason
it masks an indeterminate progress bar, and compares a still one like everything else.
`Animation.Wave` is not here: its band is an `::after` layer, which a `Border` does not have.

### Split and drop-down buttons

`SplitButton` is a two-segment `MudButtonGroup`; `DropDownButton` is a `MudMenu` whose activator
is a button.

```xml
<SplitButton Content="Save" />                         text group, text-primary
<SplitButton Classes="outlined primary" Content="Export" />
<SplitButton Classes="filled primary" Content="Deploy" />
```

**The split button takes Button's classes, meaning the same things**: `text` by default,
`outlined` and `filled`, a colour class that is only a colour, `small` and `large`. That is
MudButtonGroup's own Variant × Color × Size, and the same generator writes it
(`ThemeColourGenerator`, into a marked region of `SplitButton.axaml`). The line between the halves
follows the group's rule: text-primary or the colour for a text group, the colour's own border
for an outlined one, `divider` for a filled one with no colour, and the colour's lighten shade
between filled coloured segments.

`_buttongroup.scss` is almost entirely about joining segments into one shape: a non-last child
loses its right-hand corner radii, a non-first child loses its left-hand ones and pulls back by
`-1px` so two borders collapse into one, and a text group draws `border-left: 1px solid
rgba(text-primary, border-opacity)` between segments.

**`BorderOpacity` is 1.0 in `Palette.cs`**, so that divider is text-primary at full strength, not
a hairline. It looks like a mistake in a screenshot and is not one.

The `-1px` does not appear here: Avalonia's `SplitButton` already puts a dedicated separator
element between its halves, so there are no two borders to collapse. Same result, different
route — which is the usual shape of transcribing CSS onto a templated control.

### Text fields

```xml
<TextBox PlaceholderText="Email" />                              standard — the default
<TextBox Classes="filled"   PlaceholderText="Email" />
<TextBox Classes="outlined" PlaceholderText="Email" />
<TextBox Classes="outlined error" PlaceholderText="Email" Text="nope" />
```

**Standard is the base and the other two derive from it**, mirroring `_input.scss` where
`.mud-input` carries the box model — and matching `MudTextField.Variant`, which defaults to
`Variant.Text`. The three differ only in padding and how far the label travels:

| variant | content padding | label resting | label floated |
|---|---|---|---|
| standard | `6px 0 7px` | `translate(0, 24)` | `translate(0, 1.5) scale(.75)` |
| filled | `12px` horizontal | `translate(12, 20)` | `translate(12, 10) scale(.75)` |
| outlined | `14px` horizontal | `translate(14, 20)` | `translate(0, 1.5) scale(.75)` |

**`PlaceholderText` doubles as the floating label.** Avalonia's `TextBox` has no `Label` property,
and adding an attached one would be the first real API in a package whose point is not having any.
The cost: these variants have no separate placeholder — the label occupies that slot until it
floats. If both are ever needed, a `FiliTextField.Label` attached property is the follow-up, in a
package that depends on this one.

The label floats when the field is **focused or non-empty**, which is expressible as a selector
only because Avalonia gives `TextBox` an `:empty` pseudo-class.

**Two ways to be invalid, and both paint the same.** `Classes="error"` is the one you set by hand
(`MudTextField.Error`); the other fires on its own when a *binding* fails validation and Avalonia
sets `DataValidationErrors.HasErrors`. The template hosts a `DataValidationErrors`, so the message
renders **under the field** at 12px the way MudBlazor's helper text does — not as the forked
template's red circle with a tooltip off to the right. Before this was wired, a failing binding
recoloured nothing and showed nothing.

**Under the field is MudInputControl's helper line**, one row for three things:

```xml
<TextBox Classes="filled" PlaceholderText="Email" AutomationProperties.HelpText="We never share it" />
<TextBox Classes="outlined counter" PlaceholderText="Title" MaxLength="40" />     17 / 40
<TextBox Classes="dense" PlaceholderText="Email" />                              Margin.Dense
```

- **Helper text is `AutomationProperties.HelpText`**, Avalonia's own attached property, so it
  costs no API and a screen reader announces it, as MudBlazor wires `HelperText` into
  `aria-describedby`. An error takes its place, as MudBlazor's does.
- **`counter` is `MudTextField.Counter` over `MaxLength`**: `17 / 40` on the right, or the length
  alone when `MaxLength` is 0, which is `Counter="0"`. It turns red with the field.
- **`dense` is `Margin.Dense`**: 3px off a standard field, 8px off a filled one, 16px off an
  outlined one, from `.mud-input-root-margin-dense`, with the label resting and floating to match.

A `ComboBox` takes the same: `filled`, `outlined`, `dense`, `error`, and helper text. A select
used to have no error host at all, so a failing binding on one showed nothing.

**Filled and outlined fields had no side padding until 0.2.0**, though the themes declared 12px
and 14px. The template wrote the content margin inline, which binds at Template priority, and a
variant's plain `^ /template/` style cannot beat that; see the trap in `CLAUDE.md`.
`VariantsPadTheirContentFromTheSide` now pins it.

The outlined variant masks the border behind the floated label with an opaque patch rather than
cutting a real notch in the stroke — a notch needs a generated `Geometry`, and the mask is
indistinguishable when the field sits on `FiliSurface`.

### Selection controls

```xml
<CheckBox     Content="Enable sync" />
<RadioButton  Content="Weekly" GroupName="cadence" />
<ToggleSwitch Content="Dark mode" />
```

With no class they are MudBlazor's `Color.Default`, which is **grey, not primary**: the icon
button a checkbox or radio glyph sits in is `action-default`, checked or not, and a switch keeps
`_switch.scss`'s literal `#fafafa` thumb on and off. A colour class is `Color` — on a checkbox or
radio it paints the glyph in every state, on a switch the thumb and track only when on — and
`small` / `large` are MudIcon's 20 and 36px and the switch's own spans:

```xml
<CheckBox Classes="primary" Content="Enable sync" />
<ToggleSwitch Classes="success large" Content="Online" />
```

**MudBlazor does not draw them — it renders Material icons.** `MudCheckBox` picks between
`Icons.Material.Filled.CheckBox`, `CheckBoxOutlineBlank` and `IndeterminateCheckBox`; `MudRadio`
between `RadioButtonChecked` and `RadioButtonUnchecked`. So a hand-drawn box with a stroked tick
is not a close approximation, it is a *different artefact* — it cannot match the glyph's corner
geometry or the indeterminate bar. `Themes/Icons.axaml` carries those glyphs as `StreamGeometry`,
transcribed from `Icons/Material/Filled.cs`, and the templates swap between them.

- **CheckBox / RadioButton** — the real glyphs at 24px, swapped per state. Note the radio: checked
  grows an inner disc inside the ring rather than filling it; filling it is the usual mistake and
  the result reads as a round checkbox.
- **ToggleSwitch** — a 20px thumb that is *larger* than its 14px track, overhanging it above and
  below, with elevation under the thumb. The track is `action-default` at 48%, lifting to 50% when
  on, and only a colour class changes its colour.

All three centre a circular 40px state layer on the control rather than tinting the whole row.

**One trap in transcribing a Material glyph.** Each one carries an `M0 0h24v24H0z` viewbox spacer,
which is a transparent bounding rect in SVG and a *painted square* in a filled `StreamGeometry`.
It is dropped; bounds are preserved by drawing at `Width`/`Height` 24 with `Stretch="None"`, since
the data is already in 0–24 space.

### Slider and tabs

```xml
<Slider Value="40" />
<TabControl> <TabItem Header="One" /> </TabControl>
```

- **Slider** — `MudSlider` defaults to `Size.Small` and `Color.Primary`: a 2px rail and a 12px
  thumb, with the inactive half at 30% of the colour. `medium` and `large` are 4px / 20px and
  6px / 24px, and a colour class repaints it. The thumb does not grow: MudBlazor rings it, 1px of
  the colour at 24% on hover and 2px on focus and press. The two halves of the rail reach under the
  thumb so it reads as one continuous line, as MudBlazor's native range input does. Structure is dictated by Avalonia, not Material: `Slider` requires a `Track`
  named `PART_Track`, and the `Track`'s two `RepeatButton`s **are** the active and inactive halves
  of the rail — there is no separate fill element. Both orientations need their own `Template`; a
  horizontal one applied to a vertical slider renders sideways rather than degrading.
- **TabControl** — MudTabs' 48px bar with a 2px primary indicator. MudTabs' defaults decide the
  rest, and this theme used to differ on each: the bar is `surface` with **no** rule under it
  (`Border` defaults to false), tabs are text-primary rather than text-secondary, and
  `MinimumTabWidth` is 160px. `ItemContainerTheme` is what carries the header theme down to a bare
  `<TabItem>`; without it the control is themed and its headers are not, which looks like the
  theme half-applied.

```xml
<TabControl Classes="primary">…</TabControl>              MudTabs Color: the bar in the colour
<TabStrip Classes="border centered">…</TabStrip>          Border, Centered
<TabControl Classes="outlined rounded">…</TabControl>     Outlined, Rounded
```

A colour class is MudTabs' `Color`, the colour of the **bar** - the tabs and the indicator take
its contrast text, and the active tab's hover is `{color}-lighten`. A tab's theme cannot see its
parent's class, so those are `TabControl.primary > TabItem` child selectors in `FiliTheme.axaml`;
a descendant selector would also repaint a `TabControl` nested in the content.

The indicator does not slide between tabs — that needs to measure both headers and animate
between them, which means code-behind and a custom panel. A per-item indicator that fades is the
declarative 90%.

**`TabStrip` is the same thing without a content area.** Avalonia splits the header row and the
tab control into two types; MudBlazor does not, so both carry the same metrics. They are written
out twice rather than shared, because a `ControlTheme` cannot be `BasedOn` one whose `TargetType`
differs — if you change one, change the other, which is why the gallery shows them together.

**`PipsPager` is MudCarousel's bullets, not `MudPagination`.** Avalonia has no numbered pager, and
`MudCarousel.razor.cs` names the parts exactly: `CheckedIcon` is `RadioButtonChecked`,
`UncheckedIcon` is `RadioButtonUnchecked`, and the arrows are `NavigateBefore`/`NavigateNext`. So
the pips are **the same two glyphs the radio button uses** — selecting one swaps the glyph, a ring
gaining a disc, rather than a dot changing colour. The 0.75 opacity on the row is transcribed from
the inline style in that razor file. (`NavigateNext` turns out to be byte-identical to
`ChevronRight`, so `Icons.axaml` carries one path under the name it was first needed for.)

### Progress, dividers, panels and links

```xml
<ProgressBar Value="65" />                                4px, square — the MudBlazor defaults
<ProgressBar Classes="primary large rounded" Value="65" />
<Separator />                                             1px, no margin
<Separator Classes="inset" />                             72px indent, to clear an avatar column
<Expander Header="Details"> … </Expander>
<HyperlinkButton Content="Learn more" />
```

Four places where the MudBlazor default is not the one you would guess:

- **A bare `ProgressBar` is a 4px square hairline.** `MudProgressLinear.Size` defaults to
  `Size.Small` and `Rounded` to `false`; `medium` and `large` are 8px and 12px, `rounded` opts
  into the 4px radius. The track is always the bar's own colour at 20% — only `Color.Default`
  splits them (track `action-disabled`, bar `action-default`), which is why `Background` and
  `Foreground` are the two knobs.
- **A `Separator` has `margin: 0`.** The forked Simple template ships `Margin="29,1,0,1"`, a
  menu-shaped indent baked into every divider, which used to survive because the theme only
  recoloured it. `inset` (72px) and `middle` (16px) are MudBlazor's real variants.
- **An `Expander` header is 15px**, which is neither body1 (16) nor body2 (14) — MudBlazor sizes
  `.mud-expand-panel-header` on its own at `.9375rem`. The panel carries a surface and elevation 1
  because `MudExpansionPanels` is a `MudPaper`; `Classes="flat"` drops both when it already sits
  on a card.
- **A `HyperlinkButton` is not underlined at rest.** `MudLink.Underline` defaults to
  `Underline.Hover`. `Classes="underline"` is always-on, `no-underline` is never.

### The drawer, and a third way to theme a control

`SplitView` is MudDrawer, and it is the one control here treated with **styles rather than a
`ControlTheme`** — so it is not in the 34 above, and that is the point rather than an oversight.

Everything MudDrawer contributes is a *value*: `layout/_drawer.scss` (in `Styles/layout/`, not
`components/`, which is why it takes some finding) paints from the `drawer-background` and
`drawer-text` tokens, `LayoutProperties.cs` gives `DrawerWidthLeft = "240px"` and
`DrawerMiniWidthLeft = "56px"` — already `FiliDrawerWidth` and `FiliDrawerMiniWidth` — and
`MudDrawer.Elevation` defaults to 1. What the forked template carries is *structure*: pane
sliding, the four display modes, the light-dismiss layer. Get a part name wrong there and the
drawer lays out perfectly and never opens, which is the same reason `ScrollViewer` and `Window`
are left alone.

So there are three ways to change how a control looks here, and the third is new:

| | when |
|---|---|
| Hand-written `ControlTheme` | the shape is wrong — a floating label, a raised button |
| `Accents.axaml` | the control paints from one of Simple's ~96 shared keys |
| A `Style` in `FiliTheme.axaml` | the shape is right and only values are wrong |

The third works because **a `Style` outranks a `ControlTheme` setter** — normally the trap this
file warns about twice, relied on deliberately here. `DrawerTakesMudDrawerMetricsOverTheForkedTemplate`
pins it, because if that precedence ever reversed every drawer would quietly revert to Simple's
320px grey pane.

One caveat found while doing this: `Accents.axaml` **cannot** override a key a forked control
file declares for itself, such as `SplitViewOpenPaneThemeLength`. Accents is merged *before* the
control dictionaries, and in a merged `ResourceDictionary` the later entry wins.

Not carried over: the pane's elevation-1 shadow. The pane root is a `Panel`, and only a `Border`
can carry a `BoxShadow` — the same limitation that made `Button` need a template in the first
place. Adding it means owning the template.

### Snackbars

`WindowNotificationManager` and the `NotificationCard`s it builds are MudSnackbar: 6px/16px
padding, 288px minimum and 500px maximum width, 16px between stacked cards, and the manager 24px
from whichever edges it is anchored to.

Two things were read rather than chosen. The **elevation is 6** — `_snackbar.scss` spells out the
same three shadow layers `FiliElevation6` carries, character for character, so the level is
transcribed rather than picked. And the **severity colours come from `_alert.scss`**, because a
snackbar is a filled `MudAlert`: the palette colour as ground, its contrast text as foreground,
at **Medium** weight — the filled alert's 500 overrides the snackbar's own 400. Each severity has
its own contrast token (`FiliInfoContrastTextBrush` and so on). In light and dark all four are
MudBlazor's white; in high contrast they are black, because that variant's status colours are
pastels and white on them would be about 1.6:1.

**The enter and exit animations are copied from the forked template deliberately**, and one of
them is load-bearing: the key frame that sets `IsClosed` at 100% is what actually removes a card.
A theme that drops it renders beautifully and leaves notifications on screen forever.

### Right to left

Set `FlowDirection="RightToLeft"` and everything mirrors. No template in this package does
anything to make that happen, and that is the finding rather than an omission:

> **Avalonia mirrors an entire subtree with a single transform, applied where the flow direction
> CHANGES** — not per control.

So grid columns, dock sides and `Left`/`Right` alignment all flip on their own. The numeric
field's spin column moves to the left, the tree indents from the right, the split button's
rounded corners swap ends, and the inset divider indents from the right, none of which is coded
for anywhere. MudBlazor reaches the same place with logical CSS properties (`margin-inline-end`,
`padding-inline-start`), which is exactly why those have no counterpart here.

**The exception is a glyph that must not mirror.** A checkmark is not a directional symbol, so
Material's bidirectionality guidance leaves it alone — a reversed tick just looks broken. The
three checkbox glyphs therefore opt out with `FlowDirection="LeftToRight"`, which is the same
thing Avalonia's own Simple theme does to its check path and is how the case was spotted at all.

Arrows are deliberately **not** opted out. A tree's disclosure arrow *should* point left in RTL,
and the mirror gives that for free; pinning it left-to-right would leave it pointing away from
the content it opens. `RightToLeftTests` asserts both halves of that split.

Two things to know if you go looking:

- Measuring the mirror requires **crossing the boundary**. Translate a point to the mirrored
  panel and the coordinates are still left-to-right; translate it to the window and the flip
  appears. The first version of that test failed for exactly this reason.
- `HasMirrorTransform` is false on the controls *inside* an RTL subtree. It is true only on the
  element where the direction changed.
- **In a browser, RTL text needs a font for its script.** The mirroring works anywhere; the
  glyphs do not, because there are no system fonts to fall back on — see *Fonts*.

### High contrast

A third variant, and **not a MudBlazor one** — MudBlazor ships light and dark and nothing else, so
this one is derived rather than transcribed. Select it like any other variant:

```csharp
Application.Current.RequestedThemeVariant = FiliThemeVariants.HighContrast;
```

```xml
<Application xmlns:theme="using:Fili.MudAvalonia.Theme"
             RequestedThemeVariant="{x:Static theme:FiliThemeVariants.HighContrast}">
```

**`x:Key="HighContrast"` does not work**, and it does not fail quietly: Avalonia's `ThemeVariant`
type converter accepts the built-in variants only and throws `NotSupportedException` while the
merged dictionary is being built, which takes the rest of the theme down with it. A custom variant
is reachable from XAML only through `x:Static`, so `FiliThemeVariants.HighContrast` is the one
object every dictionary here — and every adopting app — has to name.

Three rules produced every value in it:

1. **Separation comes from lines, not fills.** `FiliSurfaceColor` sits one step off the page
   ground and is not doing the work; `#0D0D0D` against `#000000` is a difference nobody can see.
   What divides a card, a menu or a dialog from the page is its outline.
2. **Nothing is conveyed by alpha.** Light and dark lean on translucent blacks and whites — a
   divider is `#1FFFFFFF` — which composite against whatever is behind them. On a black ground a
   12% white line is not a faint line, it is no line. Alpha survives in six tokens, all of them
   overlays rather than content: the two state layers, the selection tint, the input fill and the
   two scrims.
3. **Disabled still has to be legible.** `#CFCFDA` on black is about 12:1, far too strong to read
   as disabled in a normal palette, which is the point. Disabled is signalled by being dimmer than
   enabled, not by being nearly gone.

**The shadow ladder becomes a hard 1px ring.** That is the whole reason this variant needed no
control template edits: every surface that already asked for a `FiliElevation*` gets its outline
for free. Two things follow — a surface painted with `FiliElevation0` has no outline here either,
and **the ring colour is fixed at `#E8E8EE`**, because a `BoxShadows` string cannot carry a
`DynamicResource`. An app that repaints `FiliLinesDefaultColor` for high contrast will find the
rings did not follow it.

#### What an adopting app has to do

**Declare the variant, not just the tokens.** `FiliThemeVariants.HighContrast` inherits from
`ThemeVariant.Dark`, which is load-bearing — it is what lets the eighty-one forked control themes,
which declare Light and Dark only, work here untouched. It is also the trap: an app that overrides
palette tokens in its `Light` and `Dark` dictionaries and stops there does **not** get a
high-contrast version of its own colours. It gets its *dark* ones, silently, because that is what
the variant falls back to.

```xml
<ResourceDictionary.ThemeDictionaries>
  <ResourceDictionary x:Key="Light">   <!-- … --> </ResourceDictionary>
  <ResourceDictionary x:Key="Dark">    <!-- … --> </ResourceDictionary>
  <ResourceDictionary x:Key="{x:Static theme:FiliThemeVariants.HighContrast}">
    <!-- the same Fili* keys again, in this variant's colours -->
  </ResourceDictionary>
</ResourceDictionary.ThemeDictionaries>
```

An app that swaps whole palette dictionaries at runtime rather than using `RequestedThemeVariant`
— Fili.PlaySphere does — needs nothing new: its high-contrast palette just has to carry the same
`Fili*` overrides as its other palettes.

### One rule for adopters: no blanket metric styles

The type ramp sets `LineHeight` and `LetterSpacing` on its own classes — `body2`, `h4`, `caption`
— and **not** on a blanket `TextBlock` selector. That is a scar, not a style preference.

It used to set body2's `LineHeight` on every `TextBlock`. Inside this package that is harmless,
because the gallery labels everything with a ramp class that supplies its own. In an adopting app
it is not: Fili.PlaySphere's headings set `FontSize` and nothing else, so a 28px title rendered in
a 20px line box with its descenders sliced off — on every page, silently, until somebody looked at
a screenshot.

The rule that falls out, and the reason this section exists at all:

> A blanket style is only safe when **everything it reaches also gets the matching value.**

Inside this package that can be arranged. An adopter's own type scale never will, because it does
not know the rule exists. So unclassed text keeps the font's natural line height, which cannot
clip, and `UnclassedTextKeepsItsNaturalLineHeight` keeps it that way.

### Keyboard focus

Every interactive control lights its state layer on `:focus-visible` — the same tint hovering
gives it. That is MudBlazor's own affordance rather than a choice made here: `_reset.scss` sets
`outline: none` and `outline: 0`, deliberately removing the browser's focus ring, and each
component maps `:focus-visible` to the hover colour:

```
_button.scss          &:focus-visible, &:active { background-color: action-default-hover }
_expansionpanel.scss  &:focus-visible           { background-color: action-default-hover }
_link.scss            &:focus-visible, &:active { text-decoration: underline }
_list.scss            &:focus:not(.mud-selected-item) { background-color: action-default-hover }
```

Three details worth keeping:

- **`:focus-visible`, not `:focus`.** Clicking a button should not leave it tinted once the
  pointer has gone; only keyboard navigation should mark it.
- **A selected list row is excluded**, per `_list.scss`. Arrow keys move focus and selection
  together, so tinting both would just be the selection tint twice.
- **Text fields keep `:focus`**, not `:focus-visible` — a field shows its accent rule however it
  was focused, because that rule says "this is where typing goes" rather than "this is where the
  keyboard is".

The affordance is a 4% tint, which is what MudBlazor ships and is on the subtle side. An app with
a stricter accessibility bar should add a ring of its own; the theme deliberately does not invent
one.

### Two converters

Besides `FiliThemeVariants`, the package's only code is two `internal` converters, each for
something XAML cannot express on its own:

- **`FactorConverter`** multiplies a bound pixel dimension by a constant. The indeterminate
  progress bar needs it: MudBlazor's keyframes animate `left`/`right` percentages, so each bar
  changes *width* as it travels (35% → 90%, and 200% → 1% for the second), and Avalonia's
  transform parser rejects a `%` unit outright — `FormatException: Invalid unit: %`, thrown at
  **runtime**, because a transform string in a key frame is parsed lazily and compiles either way.
- **`ErrorMessageConverter`** turns one `DataValidationErrors.Errors` entry into the sentence to
  show. An entry is usually an `Exception`, and binding straight to it renders
  `"System.InvalidOperationException: Must be a valid email address"`.

Compiled XAML in the same assembly can construct an `internal` type, so being internal costs
nothing and keeps the public surface at that one type. Reach for one only after establishing
that the XAML route does not exist.

### Lists, selects and the things built out of them

`MudSelect`, `MudAutocomplete` and `MudList` share their parts, and so do these:

```xml
<ComboBox PlaceholderText="Platform"> <ComboBoxItem>PC</ComboBoxItem> </ComboBox>
<ListBox Classes="surface"> <ListBoxItem>First</ListBoxItem> </ListBox>
<AutoCompleteBox PlaceholderText="Search" />
<NumericUpDown Value="42" PlaceholderText="Quantity" />
```

- **ComboBox** — the finding in `_select.scss` is a *negative* one: `.mud-select` has no box model
  at all, and every visual comes from `.mud-select-input`, which is a `MudInput`. So a select
  **is** a text field with a drop-down adornment, and it wears the standard field's underline,
  accent rule and floating label. Anything else would make selects and fields disagree in the
  same form.
- **A selected row is a tint, never a filled bar.** `MudList` marks it with
  `mud-selected-item mud-primary-text mud-primary-hover` — primary text over primary at 6% — so it
  stays as readable as the rows around it. `ListBox`, `ComboBox` and `ToggleButton` all use that
  one rule.
- **NumericUpDown** — a `MudInput` with a 24px spin column, which is not a guess:
  `_inputcontrol.scss` reserves exactly `padding-right: 24px` for it, with the comment *"This must
  be the same width of the spinners"*.
- **AutoCompleteBox** — both halves were already themed (a `TextBox` and a `ListBox`), so its
  theme exists only for the popup surface.
- **TreeView** — rows are 32px with 4px/8px padding, indented **17px per level**, and the arrow is
  `ChevronRight` turning **90°** rather than the expansion panel's `ExpandMore` turning 180°. A
  leaf keeps the 32px arrow column, for alignment, and hides the glyph. The indent is Avalonia's
  mechanism rather than MudBlazor's: a `TreeViewItem` multiplies its own `Level` by an indent
  resource through `TreeViewItemIndentConverter` instead of nesting margins, so the 17px is
  declared once and every level derives from it.

**The select's floating label needed two TextBlocks**, and the reason generalises. A select must
float its label when it holds a *value*, not only when focused, or the resting label sits on top
of the selected text — and Avalonia has no pseudo-class for "has a selection". `:empty` on an
`ItemsControl` means **no items**, which is why the first attempt floated every label permanently.
A `Style` selector is the only thing that can drive an animation, so the animated label exists
only while nothing is selected and a static copy takes over once something is. They share
position, size and colour, so the handover is invisible.

### Scrolling and menus

```xml
<ScrollViewer> … </ScrollViewer>
<Menu> <MenuItem Header="Library"> … </MenuItem> </Menu>
```

- **ScrollBar** — a thin buttonless rail with a rounded thumb that darkens on hover and press.
  Dropping the line buttons is safe (their lookups are null-safe, and wheel/drag/keyboard/touch
  are `ScrollViewer`'s job) but a `Track` named `PART_Track` is required or the thumb never moves.
- **ScrollViewer keeps its forked template**, deliberately. There was a hand-written one; it was
  deleted once the fork landed, because it did nothing the fork does not and it is the control
  where a mistake costs most: `PART_ContentPresenter` must be a `ScrollContentPresenter`
  specifically, alongside `PART_HorizontalScrollBar` and `PART_VerticalScrollBar`. Rename any of
  the three and you get a control that lays out perfectly and does not scroll. The themed
  `ScrollBar` is reached *through* it.
- **Menu** — **two** item themes, not one. A top-level strip item and a dropdown row look alike in
  markup and are different controls on screen: the strip item has no chevron, no icon column, and
  drops its popup downward. The first render of this menu showed `Library >  View >` across the
  top, because the dropdown template hides its chevron only for *leaf* items and a top-level item
  always has children. `Menu.ItemContainerTheme` is the strip item; that item's own
  `ItemContainerTheme` is the row.
- The reserved icon column means labels line up down a menu whether or not each item has an icon —
  the detail most hand-rolled menus miss.

### Overlays

`ToolTip`, `FlyoutPresenter` and `MenuFlyoutPresenter` are created by the framework rather than
placed in markup — `ToolTip.Tip="text"` builds the ToolTip internally, `<Flyout>` builds its
presenter — which is why they were keyed by type even back when everything else was keyed by
class. They now look like every other theme in the package.

The tooltip is deliberately **not theme-varying**: a Material tooltip is a dark chip in light and
dark alike, so it reads as an annotation floating above the UI rather than as another surface of
it. Its fill is transcribed rather than chosen — `_tooltip.scss` paints
`var(--mud-palette-gray-darker)`, which is `Palette.GrayDarker` = `Colors.Gray.Darken2` = `#616161`,
**solid**. An earlier version used 90% alpha, which is Material's spec and not MudBlazor's.

Flyout templates carry an 8px outer margin because `BoxShadow` draws *outside* the border and a
popup window is sized to its content — with no room to spill into, the shadow is clipped and the
flyout reads flat.

**`Window` is not themed**, and that is a deletion rather than an omission. There was a hand-written
`Window` theme, written while layering; it went when the fork landed, because two of its parts are
load-bearing and invisible until missing — `VisualLayerManager` (leave it out and every tooltip
and flyout in the app silently fails to appear) and `PART_TransparencyFallback` (without it, a
window asking for acrylic on a compositor that cannot provide it renders with no background at
all). The forked template already gets both right, and Material has no opinion about window
chrome.

### The base

**Avalonia ships no implicit default theme.** A `Button` with no `ControlTheme` in scope has no
template, so it measures to nothing and renders nothing — not an unstyled button, *no* button. Some
theme has to supply a template for every control type used.

This package supplies its own. `Themes/Base/` holds **79 templates forked verbatim from Avalonia's
Simple theme at 12.1.2**, rebased onto this package and repaletted by a single hand-written file,
`Themes/Base/Accents.axaml`, which redefines the ~96 resource keys those templates paint from in
terms of Fili tokens. One file repalettes all 79.

On top of that sit **hand-written Material control themes for 39 of the 89 templated control
types**, which win over their forked counterparts because `FiliTheme.axaml` is included second:

| | |
|---|---|
| Buttons | `Button`, `ToggleButton`, `RepeatButton`, `HyperlinkButton`, `SplitButton`, `DropDownButton` |
| Fields | `TextBox`, `ComboBox`, `ComboBoxItem`, `NumericUpDown`, `ButtonSpinner`, `AutoCompleteBox`, `Label`, `DataValidationErrors` |
| Selection | `CheckBox`, `RadioButton`, `ToggleSwitch` |
| Collections | `ListBox`, `ListBoxItem`, `TreeView`, `TreeViewItem`, `TabControl`, `TabItem`, `TabStrip`, `TabStripItem`, `Expander` |
| Indicators | `Slider`, `ProgressBar`, `PipsPager`, `Separator`, `ScrollBar` |
| Menus and overlays | `Menu`, `MenuItem`, `ContextMenu`, `ToolTip`, `FlyoutPresenter`, `MenuFlyoutPresenter` |
| Notifications | `NotificationCard`, `WindowNotificationManager` |

`StandaloneReadinessTests` pins that list — and the count — so neither can drift without a failing
build, and so an Avalonia version that adds control types shows up as a failure rather than as a
gap nobody noticed.

The other 50 keep the forked templates. Most of them are Avalonia plumbing MudBlazor has no
counterpart for — `PopupRoot`, `AdornerLayer`, `TextSelectionHandle`, `ManagedFileChooser`,
`CommandBar`, the `*Page` shell types — and keeping those byte-faithful is what makes an Avalonia
upgrade a re-download and a diff.

So the distinction is no longer themed-versus-invisible, it is **hand-written Material versus
forked-and-repaletted**. Nothing is missing, and nothing external is required.

`Themes/Base/FORK.md` records provenance, the two mechanical edits applied, and the upgrade
procedure. See also *How the fork earns its keep* below.

<details>
<summary>Historical: when this package layered over a substrate</summary>

This package used to be tokens plus control themes for **17** control types, with the other **72**
templated types (`ComboBox`, `ListBox`, `TreeView`, `DataGrid`, `DatePicker`, `Flyout`, `Window`
chrome, …) coming from `FluentTheme`. Everything below describes that arrangement and is kept for
the reasoning, not as instructions — the package has been standalone since the Simple fork.

Precisely what "invisible" means, because it is narrower than it sounds: **only *templated*
controls need a theme.** `TextBlock`, `Border`, `Image`, `Panel`, `Canvas` and the shapes draw
themselves and are unaffected. So a theme-less app is not a blank window — it is text and
rectangles laid out correctly with every interactive control missing, which is a worse failure
than blank because it looks half-working.

**The gallery let you switch substrate at runtime**, Fluent or Simple, from the app bar. The
themed controls did not change at all while it flipped, because they carry full templates;
everything else did. That difference was the honest measure of how much of the look was still
borrowed — and it was how the choice of substrate was settled, rather than by arguing it. (The
selector was removed once the base became a fork; see *Gallery*.)

**What Fluent is actually worth**, beyond "templates exist":

- **It is complete, first-party and versioned in lockstep with Avalonia.** It is regression-tested
  by the Avalonia team on every platform Avalonia targets. No community theme can promise that.
- **It covers the work nobody wants.** Not just "controls" — `DataGrid` (thousands of lines),
  `DatePicker`'s calendar flyout, `ColorPicker`, `NumericUpDown`'s spinner, IME/composition
  rendering inside `TextBox`, popup placement and flipping, window decorations. These are
  functional templates, not styling.
- **It carries accessibility and platform behaviour.** Focus adorners, keyboard affordances,
  hit-target sizing, high-contrast handling. Every `ControlTheme` written here re-earns all of
  that by hand, and it is easy to ship a beautiful theme with no visible focus ring.
- **It absorbs Avalonia's churn.** When a template part is renamed or a pseudo-class added in
  Avalonia 13, Fluent is updated with it. Every ControlTheme owned here is one that must be
  updated by hand — which is the argument that cuts hardest against going standalone in a small
  project.
- **It fails safe.** Forget a control while layered and it looks Fluent; forget one while
  standalone and it disappears.
- **One accent colour retints everything it templates** — a checked `CheckBox`, a `Slider` thumb,
  the selected `TabItem` underline — without this package knowing those controls exist.

The cost is equally real: shapes you do not control, a `:not()` guard on every blanket style, and
internal brush keys that move between versions. And the discomfort is not constant — **layering is
most comfortable at 0% coverage and at 100%, and worst in the middle**, because every control that
becomes properly Material increases the contrast with the ones that have not. At 13 of 89, that is
exactly where this repo sits.

That last point is the highest-leverage lever in the repo: one accent colour retints every
substrate-templated control at once — a checked `CheckBox`, a `Slider` thumb, the selected
`TabItem` underline — without this package knowing those controls exist.

**Set it through `FluentTheme.Palettes`, not through a `SystemAccentColor` resource.** Fluent
derives its accent ramp from its own `Palettes` collection, seeded from platform settings, and
never consults a resource of that name:

```csharp
var theme = new FluentTheme();
theme.Palettes[ThemeVariant.Light] = new ColorPaletteResources { Accent = Color.Parse("#594AE2") };
theme.Palettes[ThemeVariant.Dark]  = new ColorPaletteResources { Accent = Color.Parse("#776BE7") };
```

Doing it the other way fails **silently** — every unthemed control just stays the OS accent blue.
That is exactly what the first rendered capture of this gallery showed, after the wrong approach
had been confidently written down here. A test pinned it for as long as the gallery used Fluent.

`SimpleTheme` has no equivalent and ignores `Palettes` entirely; retinting it means overriding its
own `ThemeAccentBrush` family. That is not done, so the gallery's Simple substrate still renders
the OS blue — deliberately left visible rather than papered over.

**Order is load-bearing.** `FluentTheme` first, then `FiliTheme`. Styles are evaluated in order and
later ones win for overlapping setters, so reversing them leaves Fluent's colours on top.

What this arrangement costs: the app reads as **Fluent shapes wearing Material colours**, except
for the two controls with real templates. That seam is visible — Fluent separates surfaces with a
1px border where Material floats them on a shadow, and its controls are geometrically tighter. Each
`ControlTheme` added shrinks Fluent's role, and Fluent only disappears entirely if every control
gets one, which is the multi-year project this repo exists to avoid.

**Fluent versus Simple was settled by looking**, with `--capture`, and Fluent won clearly:

| | Fluent | Simple |
|---|---|---|
| Accent lever | `Palettes` / `ColorPaletteResources` — one colour retints everything | none; needs its own `ThemeAccentBrush` overrides |
| Metrics next to the themed controls | close in weight and spacing; mild seam | visibly tighter and smaller, so the seam is *worse* |
| Tab strip | a coloured underline — near-identical to Material's indicator | a filled box behind the active tab |
| Scrollbar | thin, unobtrusive | thick, with arrow buttons |
| ComboBox / NumericUpDown | clean chevrons | small triangles, cramped spinner |

The theory for Simple was that its plainness would be *neutral*, so retinted plain controls would
read as minimal Material while Fluent's leftovers would read as Windows 11 in purple. **Rendering
both killed that theory.** Simple does not read as neutral, it reads as Win32 circa 2003 — the tab
strip and scrollbar give it away instantly — and because its metrics are tighter than Material's,
the boundary between themed and unthemed controls is *more* obvious, not less.

Fluent also turned out to have an accidental advantage: its `TabItem` indicator is a coloured
underline, which is very close to what Material does anyway.

The evidence came from `--capture`, which rendered both substrates at the time. It renders the
shipping base only now.

`Material.Avalonia` would be a visually closer substrate, but consuming it means binding to *its*
slot names (`PrimaryHueMidBrush`, `MaterialCardBackgroundBrush`) — exactly the coupling
`Fili.MangaReader` has, which ties its Avalonia upgrades to Material.Avalonia shipping a matching
build.

</details>

### How the fork earns its keep

Going standalone was expected to mean writing 72 templates. It did not. It meant **downloading 79
and rewriting one file.**

The whole adaptation was:

1. Fetch `src/Avalonia.Themes.Simple/Controls/*.xaml` at tag 12.1.2.
2. `.xaml` → `.axaml`, and rebase `avares://Avalonia.Themes.Simple/...` onto this package. Both by
   script; no template was touched by hand.
3. Write `Accents.axaml`, mapping the ~96 keys those templates paint from onto Fili tokens.

Step 3 is the whole trick, and it is why **Simple** rather than Fluent: its key surface is small,
flat and stable. Fluent's equivalent is hundreds of layered keys that move between versions — the
same churn that made retinting it a documented caveat earlier in this file.

What this bought, beyond the palette: every `:not()` guard in this repo is now a transitional
artifact rather than a permanent tax, the accent needs no theme-specific API, and there is no
external theme package to track across Avalonia releases.

What it costs: the forked templates are this repo's problem now. `FORK.md` documents the upgrade
as re-download, re-apply the two edits, diff. `SimpleBridgeTests` asserts every contract key still
resolves in both variants, which is what turns "Avalonia 13 added a key" from a silent rendering
bug into a failing build.

**One bug found exactly that way, immediately.** The first standalone render looked correct until
you noticed the substrate-default `ToggleSwitch`es were labels with no switch. Eighteen `*Color`
keys had been missed while their `*Brush` twins were mapped — and nothing reported it. That is the
same silent-key failure this repo has now hit four separate times, and the reason the contract test
exists rather than a comment saying to be careful.

### Two different "use Simple" arguments — do not conflate them

Community guidance for writing a full Avalonia theme is to **copy the Simple theme's `.axaml`
files out of the Avalonia repo and edit them**, because there is less to strip than in Fluent.
That advice is real and good — and it is about a *different decision* from the Fluent-vs-Simple
comparison above.

| | Fork Simple's files | Reference Simple as substrate |
|---|---|---|
| What it is | copy ~89 templates into this repo and restyle them | `<SimpleTheme />` in `Application.Styles` |
| Runtime dependency | **none** — fully standalone | Simple, at runtime |
| The comparison above applies? | no | yes, and Fluent measured better |

The second is what the gallery switch compares, and Fluent won it. The first dissolves the
question entirely: fork the files and you depend on no substrate at all.

It also makes standalone **much cheaper than "write 89 templates"**, which is how the sequencing
below was originally framed. Forking Simple's templates and restyling them reuses all the part
names, states and keyboard handling — the tested structure — and changes only the visuals. That
is a far smaller job than Semi's several hundred files from scratch, and it is the route to take
if this repo ever goes standalone.

Note what the official docs do *not* say: they describe Simple as "a minimal and lightweight theme
with limited built-in styling" whose "low visual and structural complexity makes it a good choice
for applications running on embedded devices", and designate **neither** theme as the recommended
base. The fork-Simple advice is community practice, not documentation.

### Flowery.NET layers over Fluent too

Worth knowing, since it is the closest project to this one: [Flowery.NET](https://github.com/tobitege/Flowery.NET)
— 95 controls, a DaisyUI port — has exactly this shape in its gallery's `App.axaml`:

```xml
<Application.Styles>
    <FluentTheme />
    <daisy:DaisyUITheme />
</Application.Styles>
```

Its `DaisyUITheme.axaml` merges themes only for its own `Daisy*` types. Nothing for Avalonia's
built-ins — those stay Fluent's.

So there are **three** architectures, not two:

1. **Replace the substrate** — Semi.Avalonia, Material.Avalonia, Classic.Avalonia. Complete
   `ControlTheme` sets, no Fluent.
2. **Add new control types over a substrate** — Flowery.NET. New `Daisy*` controls with their own
   templates; Fluent still renders every built-in.
3. **Replace the substrate by forking one** — this repo, now. Avalonia's Simple templates taken
   wholesale and repaletted, with Material themes written over the controls that need them.

The second is the cheapest, and explains how one person shipped 95 controls in nine months: a
brand-new control type has no existing template to match, no states to preserve and nothing to
stay compatible with. Retemplating `TextBox` is harder than inventing `DaisyInput`.

The third — forking — turned out far cheaper than the first. Semi wrote several hundred files;
this repo downloaded 79 and wrote one. The difference is whether you author templates or inherit
them and change only what they paint from.

`Material.Avalonia` remains the closest *visual* match, but consuming it would mean binding to
*its* slot names (`PrimaryHueMidBrush`, `MaterialCardBackgroundBrush`) — exactly the coupling
`Fili.MangaReader` has, which ties its Avalonia upgrades to Material.Avalonia shipping a matching
build. Forking Simple avoids taking on anyone else's vocabulary.

## The one rule

**A missing or misspelt resource key is silent in Avalonia.** The lookup resolves to nothing and
whatever was there before simply stays, so a typo becomes a colour that quietly never changed
rather than an error. Likewise a `StaticResource` where a `DynamicResource` belonged: it freezes
the light value and that control stops following the theme at runtime, with no warning.

`tst/Fili.MudAvalonia.Theme.UnitTests/ResourceResolutionTests.cs` is what turns both into failures.
Every token is asserted to resolve under **both** theme variants. Add a token, add it there.

## Tokens

| Group | File | Notes |
|---|---|---|
| Palette | `Themes/Palette.axaml` | Light, dark and high contrast, as `ThemeDictionaries`. Colours and brushes. |
| Elevation | `Themes/Elevation.axaml` | Levels 0–24, three stacked shadow layers each — one hard ring each in high contrast. |
| Typography | `Themes/Typography.axaml` | Roboto, embedded; base size **14px**, not 16. |
| Controls | `Themes/ControlThemes.axaml` | Aggregator; one file per control under `Themes/Controls/`. Keyed by type. |
| Icons | `Themes/Icons.axaml` | The ten Material glyphs the templates cannot do without. Not an icon set. |
| Geometry | `Themes/Geometry.axaml` | 4px radius, 4px spacing scale, appbar and drawer sizes, and `FiliInputMinWidth` — the floor that keeps an unstretched empty field from measuring to nothing. |

Three things worth knowing before changing any of them:

- **The primary colour differs by variant.** `#594AE2` is the *light* primary; dark uses
  `#776BE7`. An app pinned to one variant that seeds from the other gets the brand colour wrong
  everywhere. `PrimaryDiffersBetweenVariants` pins this.
- **Base type is 14px.** That smaller baseline is much of why the look reads dense and tidy, and
  it is the first thing to check when a ported screen feels wrong.
- **Elevation does not vary between light and dark**, matching MudBlazor, which uses one shadow
  array for both — the two dictionaries hold the same ladder character for character, and
  `LightAndDarkShareTheSameLadder` keeps them that way. High contrast is the exception: there
  every raised level is a hard 1px ring instead. The ladder lives *inside* the theme dictionaries
  for that reason alone, and the reason is worth knowing on its own: **a dictionary's own entries
  are found before its `ThemeDictionaries`**, so a token declared outside them cannot be
  overridden by a variant. The outer value wins everywhere, silently.

## Fonts

Roboto is **embedded** (`src/Fili.MudAvalonia.Theme/Assets/Fonts`), in three static instances —
Light 300, Regular 400, Medium 500 — which are the weights this theme uses.

Two things to know before changing them:

- **Static instances, not the variable `Roboto[wdth,wght].ttf`.** Avalonia resolves a weight by
  picking a matching *face*, not by setting a variable axis, so a single variable file would
  render Light and Medium as Regular.
- **Their name tables are legacy.** `Roboto-Light` reports its family as `Roboto Light` and
  `Roboto-Medium` as `Roboto Medium`, each with subfamily `Regular`, rather than one `Roboto`
  family carrying three weights. Avalonia's embedded font collection groups them correctly
  anyway; `EveryUsedWeightHasItsOwnFace` is what keeps that true, and is why it asserts the
  family name *starts with* Roboto rather than equals it.

**Roboto covers Latin, Greek and Cyrillic, and nothing else.** On desktop that never shows: a
character Roboto lacks — Arabic, Hebrew, CJK — falls back to a font the operating system supplies.
**A browser build has no system fonts at all.** It can only draw with the faces it ships, so the
same text renders as nothing, silently. An app targeting the browser has to embed a face for every
script it displays and register it with `FontManagerOptions.FontFallbacks`. The theme does not do
this for you — which scripts an app needs is the app's decision, and each one is a few hundred
kilobytes. The gallery is the worked example: it ships static Noto Sans Arabic for the
right-to-left section, in `src/Fili.MudAvalonia.Theme.Gallery/Assets/Fonts`, and `GalleryFonts`
registers it for every head. Registering it on desktop too is deliberate even though Windows has
Arabic fonts of its own: an OS fallback is whatever that machine has installed, so text drawn with
it is not the same on Linux, on macOS, or on another Windows image — including the CI runner the
pixel baselines are compared on.

## Gallery

```powershell
dotnet run --project src/Fili.MudAvalonia.Theme.Gallery.Desktop
```

Five tabs: palette swatches with computed WCAG contrast ratios, the elevation ladder, the type
ramp, a control-state matrix, and a realistic sample screen. The variant selector in the app bar
switches between light, dark and high contrast at runtime — which is the fastest way to find a
token that was wired statically, or one that high contrast never got its own value for.

`--capture <dir>` renders every view in every variant to PNG without a display, for reviewing
without running the app. They are the same nine frames the pixel suite compares.

There used to be a substrate selector beside the variant one, flipping the base under the forked
controls between this package's Simple fork and Avalonia's Fluent. It mattered while the package
layered over Fluent; once the base was a fork it only showed a look this theme does not ship (and
drew Fluent's dark theme under the "High contrast" label), so it was removed along with the
gallery's Fluent reference. `StandaloneReadinessTests` is what says which controls are hand-written
and which are forked.

The sample screen matters more than the control matrix: a wall of buttons looks fine under any
theme, and only a real layout exposes flat hierarchy and wrong spacing.

### In the browser

The same gallery runs in a browser, from `src/Fili.MudAvalonia.Theme.Gallery.Browser`, and
`.github/workflows/pages.yml` publishes it to
[GitHub Pages](https://filiarrochada.github.io/Fili.MudAvalonia.Theme/) on every push to `master`.
Both heads share one `MainView`; the desktop's `MainWindow` only hosts it.

```powershell
dotnet workload install wasm-tools     # once: Avalonia.Browser links Skia and HarfBuzz natively
dotnet run --project src/Fili.MudAvalonia.Theme.Gallery.Browser
```

Building the solution needs the workload too, because the browser head is part of it.

Every URL the page loads is relative, so the published output works unchanged under the Pages
sub-path and at the root of a local server. The first time, Pages has to be switched to
**Settings → Pages → Source: GitHub Actions**; until then the deploy job fails.

The theme is loaded only by the two `StyleInclude`s in App.axaml, which compile to code. Keep it
that way: a `StyleInclude` constructed in C# loads its source by reflection at runtime, which the
browser build's trimming can strip, and the page would then render with no base theme at all.

## Pixel baselines

Nine frames — three views in all three variants — are rendered on every test run and
compared with PNGs committed under `tst/Fili.MudAvalonia.Theme.PixelTests/Baselines`.

```powershell
dotnet test tst/Fili.MudAvalonia.Theme.PixelTests

# after an intended change, and only after looking at the diff:
$env:FILI_PIXEL_BASELINES = "accept"; dotnet test tst/Fili.MudAvalonia.Theme.PixelTests
```

**This is the only suite that can notice a control theme restyling a screen nobody was looking
at.** Everything else asserts that a key resolves, a setter exists, a size is not zero — all of
which stay true while a frame changes completely. Control themes here are keyed by *type*, so one
edit reaches everything, and the two worst defects this package has shipped — headings clipped by
a blanket line height, a field measuring to nothing — were invisible to every assertion in the
suite and obvious in a picture.

A failure prints the rendered frame, the baseline, and a **diff image**: the layout ghosted in
grey with changed pixels in red. That picture is the point. Accepting a baseline without opening
it throws away the only thing the suite produces.

Three things to know before changing any of it:

- **The rendering is shared with the screenshot mode**, in `GalleryFrames`. Two copies of "make a
  window, measure it, take the frame" drift, and the day they drift the baselines stop describing
  what `--capture` produces.
- **The indeterminate `ProgressBar` is masked, by rectangle, located from the live tree.** Its
  band follows a wall-clock animation, so two runs of the same build differ by about eighty
  pixels there and nowhere else. A tolerance budget wide enough to absorb that would also absorb
  a redrawn glyph or a new one-pixel border, everywhere in the frame; masking hides it only where
  the animation is, keeps the rest strict, and still catches a change that *moves* the bar.
- **Baselines are platform-specific.** They were rendered on Windows with Skia, and text
  rasterisation differs across platforms — a Linux runner would fail every frame on glyph edges
  alone. A second committed set per platform is the answer if that day comes; a tolerance wide
  enough to cover it would be wide enough to hide real changes.

Fluent frames are captured but **not** baselined. What Fluent renders belongs to Avalonia, and
pinning it would turn every Avalonia upgrade into a failing test about someone else's theme.

## Continuous integration

`.github/workflows/build.yml`, on push and pull request to `master`, and on demand.
`.github/workflows/pages.yml` deploys the browser gallery separately; see *In the browser*.

| Runner | Builds | Runs |
|---|---|---|
| `ubuntu-latest` | the whole solution, browser gallery included | the 345 unit tests |
| `windows-latest` | the two test projects, which reference every project but the browser gallery | the 345 unit tests **and** the 9 pixel baselines |

**Only Linux installs the `wasm-tools` workload.** The browser gallery's build natively links Skia
and HarfBuzz into `dotnet.wasm`, so even restoring it needs the workload. Installing it on Windows
too would cost every run time for a project no Windows step uses, and the Linux build still
catches a change that breaks it.

**Linux is not there for symmetry.** This is a library other people will build on whatever they
have, and a Linux job is the only thing that catches a Windows-only assumption drifting into the
theme or the gallery. What it must not do is run the pixel suite — see *Pixel baselines* above for
why those PNGs are platform-specific.

A failing pixel frame uploads **`pixel-diffs`**: the rendered frame and the diff image, so the
change can be judged from the pull request instead of reproduced locally first. That artifact is
the reason the suite is worth running in CI at all; a red X with no picture would just be a
prompt to re-run it. The tests set `FILI_PIXEL_DIFF_DIR` to a path inside the workspace for
exactly this — the default is the system temp directory, which no artifact upload can reach.

Neither of those packs or publishes anything. Publishing to nuget.org is its own workflow and
happens only when you push a version tag; see *Releasing*.

**The hosted Windows runner renders these frames identically to a developer machine** — once the
locale is pinned. Its first runs failed on the three palette frames only: the runner is en-US and
the baselines were recorded on a pt-PT machine, so the contrast ratios read `6.00:1` against
`6,00:1`. `GalleryFrames.Render` now captures under the invariant culture, and every frame
matches. If a frame fails in CI and nowhere else, compare the uploaded artifact with the
committed baseline, and look for something that varies per machine before assuming the theme
changed.

## Releasing

`.github/workflows/release.yml` publishes the package to nuget.org, and only when a version tag is
pushed:

```powershell
# 1. bump <Version> in Directory.Build.props, commit, push
# 2. tag that exact version and push the tag
git tag v0.1.0
git push origin v0.1.0
```

The workflow runs the unit and pixel suites on Windows first, because a tag can point at a commit
`build.yml` never saw pass. It then checks that the tag matches `<Version>` exactly and fails if it
does not, packs the `.nupkg` and its `.snupkg` symbols, pushes both, and creates a GitHub release
with the packages attached and generated notes. A version with a suffix (`0.2.0-preview.1`) is
marked as a prerelease.

Publishing uses **nuget.org trusted publishing**: the workflow swaps GitHub's OIDC token for an API
key that expires within the hour, so no key is stored anywhere. Two one-time steps:

1. On nuget.org, under *Trusted Publishing*, add a policy for owner `FiliArrochada`, repository
   `Fili.MudAvalonia.Theme`, workflow `release.yml`.
2. In this repository's *Settings → Secrets and variables → Actions → Variables*, set
   `NUGET_USER` to the nuget.org username that owns that policy. It is a name, not a secret; the
   workflow fails early with a clear message if it is missing.

Packages built on a runner set `ContinuousIntegrationBuild`, so the PDBs carry `/_/` paths
instead of the runner's directory layout, and Source Link points each file at the tagged commit.

A version cannot be replaced on nuget.org, only unlisted. Re-running a release that failed after
the push is safe (`--skip-duplicate`), but fixing a bad package means a new version.

## Known gaps

**39 of the 89 templated control types are hand-written**; the other 50 wear forked Simple
templates repaletted onto these tokens. Nothing is invisible and nothing external is required —
the remaining gap is Material *shape*, not colour.

**The control list is closed, and "fifty still forked" is not a backlog.** Every one of those
fifty is classified with a reason in `StandaloneReadinessTests.Unthemed`, and the test asserts
that inventory is exhaustive and says each type once — so a future Avalonia that adds a control
fails by name asking to be classified, rather than quietly making the coverage number mean less.
What is left is framework plumbing with no design opinion to express, a bespoke MudBlazor
component that would be a rewrite rather than a restyle, or a control MudBlazor does not have.

Two of those reasons were surprises worth recording:

- **`MaskedTextBox` and `ToggleSplitButton` already get the themed templates.** Their
  `StyleKeyOverride` points the theme lookup at `TextBox` and `SplitButton`, so they render
  identically — same desired size, same descendant count. A theme of their own would be a second
  copy of one that already applies.
- **`GridSplitter` is left alone on purpose.** MudBlazor's counterpart exists, and transcribing
  it faithfully would make the control *invisible*: `_splitpanel.scss` gives the divider a
  cursor, a 12px invisible hit area and a focus outline, and nothing else — the separation comes
  from the two panels having different backgrounds, which Avalonia does not guarantee. Simple's
  visible hairline is kept deliberately. This is the same failure this package has already
  shipped twice in other costumes, caught before rather than after.

Controls MudBlazor has a counterpart for and this theme does not, roughly by how often an app
hits them:

- **`Carousel`, `CarouselPage` and `GroupBox` are deliberately left forked**, which closes the
  list rather than leaving it open. `.mud-carousel` is `display: flex; position: relative;
  overflow: hidden` plus transition keyframes — its visible chrome is icon buttons and bullets,
  which are themed elsewhere, and the forked template is already a clipped `ScrollViewer` with no
  decoration. `CarouselPage` is one of Avalonia's shell page types, like `NavigationPage` and
  `TabbedPage`, and was never in scope. `GroupBox` has no MudBlazor counterpart at all: the
  nearest things are `MudCard` and `MudPaper`, which are different shapes, so theming it would be
  invention rather than transcription.
- **The date and time pickers** (`Calendar` and its four helpers, `DatePicker`, `TimePicker`) and
  **`TableView`**. These are not restyling jobs: `MudDatePicker` and `MudTable` are bespoke
  components, so matching them means a rewrite per template with no shortcut. A real data grid is
  explicitly out of scope for this package.

And the things that are not controls:

- **No ripple.** Avalonia has no primitive for it; faking it means an animated, clipped ellipse
  driven from pointer position. Everything interactive carries the static half — a tinted state
  layer on hover, press and keyboard focus.
- **No uppercase button text.** Avalonia has no text-transform; the tracking and weight are
  applied, the casing is not.
- **No separate placeholder on a text field** — `PlaceholderText` is the floating label; see
  *Text fields* above.
- **The `dark` colour barely shows as text or a line in dark mode, and not at all in high
  contrast**: `#27272F` on a `#32333D` page, and black on black. MudBlazor's dark theme behaves the
  same way. A filled dark button stays visible.
- **Colour is on every control MudBlazor gives a `Color` that has an Avalonia counterpart here**,
  the tab bar and the app bar included. See [MudBlazor parity](docs/mudblazor-parity.md) for every remaining gap.
- **RTL works, with one deliberate exception** — see *Right to left* above. The remaining gap is
  narrow: no control here has a *bidi-aware* behaviour beyond mirroring, so if one ever needs to
  keep a numeral or a code fragment left-to-right inside otherwise-RTL content, that is the app's
  `FlowDirection` to set, not the theme's.
- **No spacing utility classes.** The 4px scale exists as values; `pa-4`-style generated classes
  do not.
- **The tab indicator does not slide** between tabs — see *Slider and tabs*.
- **High contrast does not reach the ring colour.** Every raised surface is outlined there rather
  than shadowed, but the outline is a `BoxShadows` string and those cannot carry a
  `DynamicResource`, so `#E8E8EE` is fixed. An app whose high-contrast palette uses a different
  line colour gets that colour on its borders and this one on its rings.
- **Elevation reads faintly in dark mode.** Material's shadows are black at low alpha, which is
  nearly invisible on a dark ground; MudBlazor has the same problem and this theme reproduces it
  rather than inventing a lighter shadow. Use `FiliSurfaceBrush` against `FiliBackgroundGrayBrush`
  to separate surfaces in dark, not elevation alone.

## Prior art in this workspace

`Fili.MangaReader/src/Fili.MangaReader.Views/Themes/MudBlazorPalette.axaml` already applies this
palette, over **Material.Avalonia** rather than Fluent, on Avalonia 12, **dark only**. It is worth
reading before changing anything here: it documents the light/dark primary trap and the silent-key
problem from experience, and its `TestAppFidelityTests` is the same idea as the tests here.

The gap it names as unfinished — "MudBlazor's light palette leans on internal swatch constants
that were not read back" — is closed here. Those constants are `Colors.cs` in MudBlazor:
`Pink.Accent2` = `#FF4081`, `Blue.Default` = `#2196F3`, `Green.Accent4` = `#00C853`,
`Orange.Default` = `#FF9800`, `Red.Default` = `#F44336`, `Gray.Darken3` = `#424242`.

## Licence and attribution

This package is MIT licensed — see [`LICENSE`](LICENSE). The licence covers the code, the XAML
and the gallery; the third-party material below keeps its own terms.

The token *values* come from [MudBlazor](https://github.com/MudBlazor/MudBlazor) (MIT). Colours,
sizes and shadow definitions are data, not code, and no MudBlazor code is used or derived here.

**This project is not affiliated with, endorsed by, or connected to MudBlazor.** The name is
descriptive — an Avalonia theme in MudBlazor's visual idiom — and the disclaimer matters more
now that the package name carries it, not less. Keep this section.

Roboto is © 2011 The Roboto Project Authors, under the **SIL Open Font License 1.1**; the licence
travels with the fonts in `src/Fili.MudAvalonia.Theme/Assets/Fonts/OFL.txt` and is packed into the
NuGet package. OFL requires that the licence stays with the font files and that they are not sold
on their own — neither constrains this use, but the file must not be removed.

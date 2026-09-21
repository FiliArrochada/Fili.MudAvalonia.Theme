# Fili.MudAvalonia.Theme

A design-token theme for Avalonia 12: palette, elevation, typography and geometry, in light and
dark, with a gallery app to look at it in.

The token values are MudBlazor's defaults, transcribed from its source rather than eyeballed. The
aim is the *look* — this is not a component library: no new control types, no services, and no
public API surface. (Two `internal` converters exist where XAML cannot express a MudBlazor rule;
see *Two converters* below. Nothing in the assembly is public.)

```
src/Fili.MudAvalonia.Theme                  the theme (the NuGet package)
src/Fili.MudAvalonia.Theme.Gallery          gallery UI, shared across heads
src/Fili.MudAvalonia.Theme.Gallery.Desktop  desktop head
tst/Fili.MudAvalonia.Theme.UnitTests        headless resource-resolution tests
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
  </StackPanel>
</Border>

<Border Background="{DynamicResource FiliPrimaryBrush}" />
```

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

Four variant classes, each backed by a `ControlTheme`:

```xml
<Button Classes="primary"   Content="Save" />        raised, primary fill, elevation 2/4/8
<Button Classes="secondary" Content="Share" />       raised, secondary fill
<Button Classes="outlined"  Content="Cancel" />      1px line, no fill
<Button Classes="text"      Content="Learn more" />  flat — Material's actual default
```

**The class names are MudBlazor's vocabulary, and they collide on purpose.** `primary`,
`secondary`, `outlined`, `text` and `filled` are `Color.Primary`, `Color.Secondary`,
`Variant.Outlined`, `Variant.Text` and `Variant.Filled` — API familiarity is the point of this
package, so they are not namespaced to `mud-primary` or hidden behind an attached property.

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
property — only the `Border` inside a template does.

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

The outlined variant masks the border behind the floated label with an opaque patch rather than
cutting a real notch in the stroke — a notch needs a generated `Geometry`, and the mask is
indistinguishable when the field sits on `FiliSurface`.

### Selection controls

```xml
<CheckBox     Content="Enable sync" />
<RadioButton  Content="Weekly" GroupName="cadence" />
<ToggleSwitch Content="Dark mode" />
```

These have **one** Material shape each rather than a family of variants, so they carry no class at
all.

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
  below, with elevation under the thumb. The off track is `action-default` at 48% in **both**
  states, not a separate grey.

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

- **Slider** — a 4px rail with a 12px knob that *grows* on hover and press (1.3× / 1.5×) rather
  than only tinting. Structure is dictated by Avalonia, not Material: `Slider` requires a `Track`
  named `PART_Track`, and the `Track`'s two `RepeatButton`s **are** the active and inactive halves
  of the rail — there is no separate fill element. Both orientations need their own `Template`; a
  horizontal one applied to a vertical slider renders sideways rather than degrading.
- **TabControl** — a flat 48px strip with a 2px primary indicator and a hairline under it.
  `ItemContainerTheme` is what carries the header theme down to a bare `<TabItem>`; without it the
  control is themed and its headers are not, which looks like the theme half-applied.

The indicator does not slide between tabs — that needs to measure both headers and animate
between them, which means code-behind and a custom panel. A per-item indicator that fades is the
declarative 90%.

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

### Two converters

The package ships two `internal` classes and nothing else. Both exist because MudBlazor expresses
something as a percentage of a container and Avalonia has no way to say that:

- **`FactorConverter`** multiplies a bound pixel dimension by a constant. The indeterminate
  progress bar needs it: MudBlazor's keyframes animate `left`/`right` percentages, so each bar
  changes *width* as it travels (35% → 90%, and 200% → 1% for the second), and Avalonia's
  transform parser rejects a `%` unit outright — `FormatException: Invalid unit: %`, thrown at
  **runtime**, because a transform string in a key frame is parsed lazily and compiles either way.
- **`ErrorMessageConverter`** turns one `DataValidationErrors.Errors` entry into the sentence to
  show. An entry is usually an `Exception`, and binding straight to it renders
  `"System.InvalidOperationException: Must be a valid email address"`.

Compiled XAML in the same assembly can construct an `internal` type, so being internal costs
nothing and keeps the no-public-API claim literally true. Reach for one only after establishing
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

On top of that sit **hand-written Material control themes for 30 of the 89 templated control
types**, which win over their forked counterparts because `FiliTheme.axaml` is included second:

| | |
|---|---|
| Buttons | `Button`, `ToggleButton`, `RepeatButton`, `HyperlinkButton` |
| Fields | `TextBox`, `ComboBox`, `ComboBoxItem`, `NumericUpDown`, `ButtonSpinner`, `AutoCompleteBox`, `Label`, `DataValidationErrors` |
| Selection | `CheckBox`, `RadioButton`, `ToggleSwitch` |
| Collections | `ListBox`, `ListBoxItem`, `TabControl`, `TabItem`, `Expander` |
| Indicators | `Slider`, `ProgressBar`, `Separator`, `ScrollBar` |
| Menus and overlays | `Menu`, `MenuItem`, `ContextMenu`, `ToolTip`, `FlyoutPresenter`, `MenuFlyoutPresenter` |

`StandaloneReadinessTests` pins that list — and the count — so neither can drift without a failing
build, and so an Avalonia version that adds control types shows up as a failure rather than as a
gap nobody noticed.

The other 59 keep the forked templates. Most of them are Avalonia plumbing MudBlazor has no
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

**The gallery lets you switch substrate at runtime**, Fluent or Simple, from the app bar. Watch the
themed controls while it flips: they do not change at all, because they carry full templates.
Everything else does. That difference is the honest measure of how much of the look is still
borrowed — and it is the right way to settle which substrate to stand on, rather than arguing it.

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
had been confidently written down here. `FluentAccentIsTheMudPrimary` now pins it.

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

Regenerate the evidence any time:

```powershell
dotnet run --project src/Fili.MudAvalonia.Theme.Gallery.Desktop -- --capture screenshots
```

`Material.Avalonia` would be a visually closer substrate, but consuming it means binding to *its*
slot names (`PrimaryHueMidBrush`, `MaterialCardBackgroundBrush`) — exactly the coupling
`Fili.MangaReader` has, and what keeps it on Avalonia 11.

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
`Fili.MangaReader` has, and what keeps it on Avalonia 11. Forking Simple avoids taking on anyone
else's vocabulary.

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
| Palette | `Themes/Palette.axaml` | Light and dark, as `ThemeDictionaries`. Colours and brushes. |
| Elevation | `Themes/Elevation.axaml` | Levels 0–24, three stacked shadow layers each. |
| Typography | `Themes/Typography.axaml` | Roboto, embedded; base size **14px**, not 16. |
| Controls | `Themes/ControlThemes.axaml` | Aggregator; one file per control under `Themes/Controls/`. Keyed by type. |
| Icons | `Themes/Icons.axaml` | The eight Material glyphs the templates cannot do without. Not an icon set. |
| Geometry | `Themes/Geometry.axaml` | 4px radius, 4px spacing scale, appbar and drawer sizes. |

Three things worth knowing before changing any of them:

- **The primary colour differs by variant.** `#594AE2` is the *light* primary; dark uses
  `#776BE7`. An app pinned to one variant that seeds from the other gets the brand colour wrong
  everywhere. `PrimaryDiffersBetweenVariants` pins this.
- **Base type is 14px.** That smaller baseline is much of why the look reads dense and tidy, and
  it is the first thing to check when a ported screen feels wrong.
- **Elevation is not theme-varying**, matching MudBlazor, which uses one shadow array for both.

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

## Gallery

```powershell
dotnet run --project src/Fili.MudAvalonia.Theme.Gallery.Desktop
```

Five tabs: palette swatches with computed WCAG contrast ratios, the elevation ladder, the type
ramp, a control-state matrix, and a realistic sample screen. The theme toggle in the app bar flips
the variant at runtime — which is the fastest way to find a token that was wired statically.

The sample screen matters more than the control matrix: a wall of buttons looks fine under any
theme, and only a real layout exposes flat hierarchy and wrong spacing.

## Known gaps

**30 of the 89 templated control types are hand-written**; the other 59 wear forked Simple
templates repaletted onto these tokens. Nothing is invisible and nothing external is required —
the remaining gap is Material *shape*, not colour.

Controls MudBlazor has a counterpart for and this theme does not, roughly by how often an app
hits them:

- **`TreeView`, `SplitButton`/`DropDownButton`, `NotificationCard` (MudSnackbar), `SplitView`
  (MudDrawer), `Carousel`, `TabStrip`, `PipsPager` (MudPagination), `GroupBox`** — one SCSS file
  and one template each.
- **The date and time pickers** (`Calendar` and its four helpers, `DatePicker`, `TimePicker`) and
  **`TableView`**. These are not restyling jobs: `MudDatePicker` and `MudTable` are bespoke
  components, so matching them means a rewrite per template with no shortcut. A real data grid is
  explicitly out of scope for this package.

And the things that are not controls:

- **No ripple.** Avalonia has no primitive for it; faking it means an animated, clipped ellipse
  driven from pointer position. Everything interactive carries the static half — a tinted state
  layer on hover and press.
- **No uppercase button text.** Avalonia has no text-transform; the tracking and weight are
  applied, the casing is not.
- **No separate placeholder on a text field** — `PlaceholderText` is the floating label; see
  *Text fields* above.
- **No counter under a field.** The error/helper line exists; `MudTextField`'s character counter
  would need an attached property.
- **No `divider-light` token**, so `MudDivider`'s `light` variant is not implemented.
- **No spacing utility classes.** The 4px scale exists as values; `pa-4`-style generated classes
  do not.
- **The tab indicator does not slide** between tabs — see *Slider and tabs*.
- **Elevation reads faintly in dark mode.** Material's shadows are black at low alpha, which is
  nearly invisible on a dark ground; MudBlazor has the same problem and this theme reproduces it
  rather than inventing a lighter shadow. Use `FiliSurfaceBrush` against `FiliBackgroundGrayBrush`
  to separate surfaces in dark, not elevation alone.

## Prior art in this workspace

`Fili.MangaReader/src/Fili.MangaReader.Views/Themes/MudBlazorPalette.axaml` already applies this
palette, over **Material.Avalonia** rather than Fluent, on Avalonia 11, **dark only**. It is worth
reading before changing anything here: it documents the light/dark primary trap and the silent-key
problem from experience, and its `TestAppFidelityTests` is the same idea as the tests here.

The gap it names as unfinished — "MudBlazor's light palette leans on internal swatch constants
that were not read back" — is closed here. Those constants are `Colors.cs` in MudBlazor:
`Pink.Accent2` = `#FF4081`, `Blue.Default` = `#2196F3`, `Green.Accent4` = `#00C853`,
`Orange.Default` = `#FF9800`, `Red.Default` = `#F44336`, `Gray.Darken3` = `#424242`.

## Licence and attribution

The token *values* come from [MudBlazor](https://github.com/MudBlazor/MudBlazor) (MIT). Colours,
sizes and shadow definitions are data, not code, and no MudBlazor code is used or derived here.

**This project is not affiliated with, endorsed by, or connected to MudBlazor.** The name is
descriptive — an Avalonia theme in MudBlazor's visual idiom — and the disclaimer matters more
now that the package name carries it, not less. Keep this section.

Roboto is © 2011 The Roboto Project Authors, under the **SIL Open Font License 1.1**; the licence
travels with the fonts in `src/Fili.MudAvalonia.Theme/Assets/Fonts/OFL.txt` and is packed into the
NuGet package. OFL requires that the licence stays with the font files and that they are not sold
on their own — neither constrains this use, but the file must not be removed.

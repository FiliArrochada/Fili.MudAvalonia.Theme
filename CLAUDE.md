# CLAUDE.md

Guidance for Claude Code working in this repository. The workspace `CLAUDE.md` one level up
applies too; this file wins where they differ.

## What this is

A **design-token theme for Avalonia 12**, not a component library. Resource dictionaries, a set of
style classes, `ControlTheme`s for the controls a Material look genuinely cannot be faked without,
and a gallery to look at it all in. No new control *types*, no services, **no PUBLIC API
surface**. Read `README.md` first; it carries the usage, the token tables and the known gaps.

`internal` helpers are allowed where XAML genuinely cannot express something — compiled XAML in
the same assembly can construct an internal type, so nothing leaks to a consumer. There is one
today: `Converters/FactorConverter`, which multiplies a bound pixel dimension by a constant,
because MudBlazor sizes several things as a percentage of their container and Avalonia has no way
to say that (its transform parser rejects `%` outright, at runtime). Reach for one only after
establishing that the XAML route does not exist; a public type still needs a separate package.

Scope discipline matters here, and the line is between **retemplating an Avalonia control** and
**inventing a new one**. The first is in scope: `Button` needed it because `Button` has no
`BoxShadow`, and `TextBox` needed it for the floating label. The second is not — a `Card`, a
`Chip`, a `DataGrid` or a dialog service turns this into a different, much larger project, the
kind that takes years, and there are already several (Semi.Avalonia, SukiUI, Ursa,
Material.Avalonia, Flowery.NET). If a new control genuinely needs to exist, it belongs in a
separate `Fili.MudAvalonia.*` package that depends on this one.

## The one rule

**A missing or misspelt resource key is silent.** Avalonia resolves it to nothing and leaves the
previous value in place, so a typo becomes a colour that quietly never changed. The same is true
of a `StaticResource` used where a `DynamicResource` belonged: it freezes the light value and that
control silently stops following the theme.

Consequently:

- Every reference to a token uses `{DynamicResource}`. No exceptions in this repo.
- **Every new token gets an entry in `ResourceResolutionTests`**, in the same change. That suite
  asserts every key resolves under *both* theme variants, and it is the only thing standing
  between a typo and a silent no-op.

## Token provenance

Values are MudBlazor's defaults, transcribed from `src/MudBlazor/Themes/Models/` — `Palette.cs`
(light), `PaletteDark.cs`, `Shadow.cs`, `Typography.cs`, `LayoutProperties.cs` — with the
`Colors.*` references resolved through MudBlazor's `src/MudBlazor/Colors/Colors.cs`.

**Control METRICS come from `src/MudBlazor/Styles/components/*.scss`**, and the component's
`.razor.cs` for its parameter DEFAULTS — which are as load-bearing as the CSS and easier to get
wrong. `MudProgressLinear.Size` defaults to `Size.Small` (4px, not 8) and `Rounded` to false;
`MudButton.Variant` and `MudTextField.Variant` default to `Variant.Text`; `MudLink.Underline`
defaults to `Underline.Hover`. Read both files, not just the SCSS.

**Icon GLYPHS come from `src/MudBlazor/Icons/Material/Filled.cs`**, because MudBlazor renders
Material icons rather than drawing shapes — a hand-drawn tick is a different artefact, not an
approximation. Drop each glyph's `M0 0h24v24H0z` viewbox spacer: it is transparent in SVG and a
painted square in a filled `StreamGeometry`. Keep bounds with `Width`/`Height` 24 and
`Stretch="None"`.

**Transcribe, never eyeball.** If a value needs checking, read it from that source again rather
than sampling a screenshot. When adding one, say in a comment which file it came from.

Two conversions are already applied and should stay applied consistently:

- CSS `rgba()` alpha folds into Avalonia `#AARRGGBB` (`0.54` -> `8A`, `0.12` -> `1F`, and so on;
  the table is in `Themes/Palette.axaml`).
- CSS `line-height` ratios and `em` letter-spacing become absolute device-independent pixels,
  because that is what Avalonia's `LineHeight` and `LetterSpacing` take. `1.43` on 14px is
  `LineHeight="20"`.

## Traps worth knowing

- **The primary colour differs between variants.** `#594AE2` is the *light* primary; dark is
  `#776BE7`. Seeding from the wrong one renders the wrong brand colour everywhere in the only
  variant users see. Pinned by `PrimaryDiffersBetweenVariants`.
- **Base type is 14px, not 16.** First thing to check when a ported screen feels off.
- **`Button` has no `BoxShadow`.** Only `Border` does, which is why the raised button is a
  `ControlTheme` with a `Border` in its template rather than a handful of setters.
- **Control themes are keyed by TYPE. A class only ever names a variant.** Every theme is declared
  under a `Fili*` name for the inventory, then aliased at the bottom of its file:

  ```xml
  <ControlTheme x:Key="{x:Type TextBox}" TargetType="TextBox" BasedOn="{StaticResource FiliStandardTextBox}" />
  ```

  This was the opposite rule while the package layered over Fluent, because overriding a default
  `ControlTheme` reaches into every control composed out of that type — a `TextBox` inside
  `NumericUpDown`, a `Button` inside `ButtonSpinner`. Once the base became a fork this repo owns,
  that stopped being a hazard and became the point: those embedded controls *should* look like
  this theme. The `fili` marker classes were deleted with it. **Do not reintroduce a class that
  means "please theme me".**

  **Class names are MudBlazor's vocabulary** (`primary` = Color.Primary, `outlined`/`filled`/`text`
  = Variant.*, `small`/`medium`/`large` = Size.*), deliberately NOT namespaced — API familiarity is
  the point. So they collide with any app already using those words, and the app's own setters win
  for the properties it declares while every property it does *not* declare leaks through from the
  theme. Grep an adopter for `Classes="` first.

- **When a control gets a ControlTheme, delete it from the blanket styles in `FiliTheme.axaml`.**
  A `Style` outranks a `ControlTheme` setter, so a leftover blanket `CornerRadius` silently
  overrides the template's. The remaining blanket selector is the pickers, which are still forked.
- **A Style always outranks a ControlTheme setter.** This has bitten twice, in different
  costumes, and it is the single most likely way to break this package:
  - A blanket `Selector="TextBlock"` setting `Foreground` also matched the `TextBlock` a
    `ContentPresenter` makes for button content, repainting white-on-primary text body-grey.
    Fixed by putting inheritable defaults (`FontFamily`, `FontSize`, `Foreground`) on
    `Window, UserControl` and leaving only `LineHeight` and `LetterSpacing` on `TextBlock`.
  - A blanket input style setting `CornerRadius` flattened the filled field's top-only
    `4,4,0,0`. Fixed with `:not(.filled):not(.outlined)` on that selector.

  The rule that falls out: **any blanket style that sets a property a ControlTheme also sets needs
  a `:not()` guard, or it must move to the container as an inherited value.**
  `ButtonContentKeepsItsContrastForeground` and `FilledTextFieldKeepsTopOnlyRounding` pin both.
- **`TextPresenter` has no `Foreground` AvaloniaProperty.** It reads the inherited
  `TextElement.Foreground` from its parent, so a setter targeting `PART_TextPresenter` fails to
  compile with `AVLN3000`. Set `Foreground` on the `TextBox` itself instead.
- **`DynamicResource` is not type-checked.** Binding a `Background` to a `*Color` token instead of
  a `*Brush` compiles cleanly and renders nothing at runtime. Every colour token therefore has a
  paired brush, and `ResourceResolutionTests` asserts the brush list resolves to `IBrush` — which
  is what catches the paired brush being forgotten. This bit once, on the switch tokens.
- **Every test body runs inside `UiThread.RunAsync`, with no exceptions.** Constructing almost any
  Avalonia object — a `FluentTheme` included — touches the compositor, and doing that from the
  xunit thread throws *"The calling thread cannot access this object"* and then **poisons the
  shared headless session**, so every later test in the assembly fails too. A plain `[Fact]` that
  forgets the wrapper does not fail alone; it fails the suite.
- **Transitioned properties cannot be asserted synchronously.** A value carrying a
  `TransformOperationsTransition` or `BrushTransition` is still interpolating when a test reads
  it, and compares equal to its resting value. Assert a non-transitioned property of the same
  state instead — `CheckedToggleSwitchTintsItsTrack` uses track opacity for exactly this reason.
- **Roboto's static instances use legacy name tables.** `Roboto-Light` reports family
  `Roboto Light`, not weight 300 of `Roboto`. Avalonia groups them correctly; assert family names
  with `StartsWith`, not `Equal`. And do not swap in the variable `Roboto[wdth,wght].ttf` —
  Avalonia picks a face per weight rather than setting an axis, so Light and Medium would render
  as Regular.
- **Avalonia's transform parser has no `%` unit.** `translateX(-35%)` throws
  `FormatException: Invalid unit: %` — **at runtime**, when the template is instantiated, because
  a transform string in a key frame is parsed lazily and compiles cleanly either way. MudBlazor
  sizes several things as a percentage of their container, so the conversion is a binding to a
  pixel dimension the control publishes, times a constant, through `FactorConverter`.
- **`:empty` on an `ItemsControl` means NO ITEMS, not "no selection".** `ComboBox:not(:empty)`
  reads like "has a selection" and is true for every populated select, which floated every label
  permanently. There is no pseudo-class for a selection; use `ObjectConverters.IsNull` on
  `SelectionBoxItem` and swap two elements.
- **A `DataValidationErrors.Errors` entry is usually an `Exception`**, so binding straight to it
  renders `"System.InvalidOperationException: the message"`. `ErrorMessageConverter` exists for
  exactly that.
- **A Style setter DOES override a value set inline on a template child.** This is the flip side
  of the trap above and it is relied on throughout — the floating labels set a resting
  `RenderTransform` inline and the `:focus` styles override it. Both facts are true at once:
  styles outrank ControlTheme *setters* and inline *template* values alike.
- **Fluent's internal brush keys move between Avalonia versions.** Only the gallery's Fluent
  comparison mode touches them now; the library has no `Avalonia.Themes.*` reference. If that mode
  is ever edited, read the keys from Avalonia's `Themes/Fluent/Accents/*.axaml` for the version in
  use rather than guessing.
- **Elevation is deliberately not theme-varying**, matching MudBlazor.

## The base is forked, not depended on

Avalonia has no implicit default theme: a control with no `ControlTheme` in scope has no template
and renders nothing at all. `Themes/Base/` holds 79 templates forked verbatim from Avalonia Simple
at tag 12.1.2, rebased onto this package and repaletted by `Themes/Base/Accents.axaml`. There is
no `Avalonia.Themes.*` reference anywhere in the library.

**Do not restyle a forked template.** Keeping them byte-faithful is what makes an Avalonia upgrade
a re-download and a diff rather than a merge. To change how a control looks, either redefine what
it paints from in `Accents.axaml`, or write a hand-written ControlTheme in `Themes/Controls/`
which wins by include order. `FORK.md` has the procedure.

Two consequences to keep in mind when editing:

- **Order is load-bearing.** `FiliBaseTheme` first, then `FiliTheme`; later styles win. That is
  the only reason the hand-written control themes beat their forked counterparts.
- **The accent is now just a token.** Standalone, `FiliPrimaryColor` flows into the forked
  templates through `ThemeAccentBrush`/`ThemeAccentColor` in `Accents.axaml` — no theme-specific
  API. (Historically it had to go through `FluentTheme.Palettes`, and setting a `SystemAccentColor`
  resource instead failed silently; the gallery keeps a Fluent comparison mode that still does it
  the correct way.)

**Coverage is pinned, not reported.** `StandaloneReadinessTests` asserts the exact set of
hand-written themes — **30 of the 89 templated types** — plus the count of both. The other 59 wear
forked templates, and `SimpleBridgeTests` asserts the ~96 contract keys those paint from still
resolve in both variants. Adding a theme means adding its `Fili*` key AND its target type to the
first test in the same change; that is what keeps the number honest and turns an Avalonia version
that adds control types into a failing build rather than a silent gap.

**Do not add a hand-written theme for a control MudBlazor has no counterpart for.** `PopupRoot`,
`AdornerLayer`, `TextSelectionHandle`, `ManagedFileChooser`, `CommandBar`, the `*Page` shell types
and roughly 25 others exist because Avalonia needs them, not because a design system has an
opinion about them. They stay forked permanently, and that is the fork earning its keep rather
than a gap to close.

The two deliberate deletions worth not re-adding: **`ScrollViewer`** (structural — get a part name
wrong and it lays out perfectly and does not scroll) and **`Window`** (`VisualLayerManager` and
`PART_TransparencyFallback` are load-bearing and invisible until missing). Both were written while
layering and both went when the fork made them redundant.

## Layout and conventions

Standard workspace shape: one `Directory.Build.props` opening with the shared block, Central
Package Management in `Directory.Packages.props` (no `Version=` on a `PackageReference`), classic
`.sln`, `src/` + `tst/`, `net10.0`, Avalonia **12.1.2** matching Fili.Plex.

`global.json` pins the SDK and opts `dotnet test` into Microsoft.Testing.Platform, which xunit v3
needs on SDK 10.

## Commands

```powershell
dotnet build Fili.MudAvalonia.Theme.sln
dotnet test  Fili.MudAvalonia.Theme.sln
dotnet run --project src/Fili.MudAvalonia.Theme.Gallery.Desktop
```

The gallery is the development loop, not a deliverable added at the end. A theme has no surface of
its own, so tune against the gallery rather than against an app.

## Related work in this workspace

`Fili.MangaReader/src/Fili.MangaReader.Views/Themes/MudBlazorPalette.axaml` applies the same
palette over Material.Avalonia, on Avalonia 11, dark only. Read it before changing anything here —
its header documents both traps above from experience. The two are not yet reconciled; doing so is
a deliberate decision, not a drive-by refactor.

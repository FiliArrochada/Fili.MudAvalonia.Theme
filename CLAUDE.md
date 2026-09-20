# CLAUDE.md

Guidance for Claude Code working in this repository. The workspace `CLAUDE.md` one level up
applies too; this file wins where they differ.

## What this is

A **design-token theme for Avalonia 12**, not a component library. Resource dictionaries, a set of
style classes, `ControlTheme`s for the controls a Material look genuinely cannot be faked without,
and a gallery to look at it all in. No new control *types*, no services, no API surface. Read
`README.md` first; it carries the usage, the token tables and the known gaps.

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
- **Control themes are opt-in by class, never by overriding the default.** Avalonia composes
  `ButtonSpinner`, the `DatePicker` presenter and flyout affordances out of plain `Button`s, and
  `AutoCompleteBox`/`NumericUpDown`/`DatePicker`/editable `ComboBox` all embed a `TextBox`, so
  keying a theme to `{x:Type ...}` silently retemplates all of them. Keep new control themes keyed
  and wired through a class. The class is a **variant name** where the control has variants
  (`primary`, `outlined`, `filled`) and the literal marker **`fili`** where it has only one shape
  (`CheckBox`, `RadioButton`, `ToggleSwitch`, `Slider`, `TabControl`, `ScrollViewer`, `Menu`,
  `Window`).

  **The one exception, and it is principled:** key by TYPE when the framework instantiates the
  control and there is no markup site to carry a class - `ToolTip`, `FlyoutPresenter`,
  `MenuFlyoutPresenter`. The risk the rule guards against is absent there too, since nothing
  composes a leaf presentation surface into its own template. Each is declared under a name for
  the inventory, then aliased to its type key in Overlays.axaml.
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
- **Fluent's internal brush keys move between Avalonia versions.** Read them from Avalonia's
  `Themes/Fluent/Accents/*.axaml` for the version in use; do not guess, and record what is found.
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
  the only reason the 17 hand-written control themes beat their forked counterparts.
- **The accent is now just a token.** Standalone, `FiliPrimaryColor` flows into the forked
  templates through `ThemeAccentBrush`/`ThemeAccentColor` in `Accents.axaml` — no theme-specific
  API. (Historically it had to go through `FluentTheme.Palettes`, and setting a `SystemAccentColor`
  resource instead failed silently; the gallery keeps a Fluent comparison mode that still does it
  the correct way.)

**Fluent is the chosen substrate, decided by rendering both** (`--capture`). It stays until
**Standalone since the Simple fork.** `StandaloneReadinessTests` pins the 17 HAND-WRITTEN control
themes; the other 72 types are covered by the forked templates, which `SimpleBridgeTests` checks
are reachable. Adding a hand-written theme means adding its key to the first test. Simple looked
plausible in theory — plainness reads as neutral — and looked like Win32 circa 2003 in practice,
with tighter metrics that make the themed/unthemed seam worse rather than better.

Each `ControlTheme` added here shrinks the substrate's role. Note where that leads: **no mature
Avalonia theme layers over a substrate.** Semi.Avalonia, Material.Avalonia and Classic.Avalonia
all ship complete `ControlTheme` sets and replace Fluent outright. Layering is scaffolding, and
going standalone is a scope decision rather than a technical one — Semi is several hundred axaml
files.

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

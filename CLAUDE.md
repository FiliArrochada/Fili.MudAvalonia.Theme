# CLAUDE.md

Guidance for Claude Code working in this repository. The workspace `CLAUDE.md` one level up
applies too; this file wins where they differ.

## What this is

A **design-token theme for Avalonia 12**, not a component library. Resource dictionaries, a set of
style classes, `ControlTheme`s for the controls a Material look genuinely cannot be faked without,
and a gallery to look at it all in. No new control *types* and no services. Read `README.md`
first; it carries the usage, the token tables and the known gaps.

`docs/mudblazor-parity.md` maps every MudBlazor parameter and palette property to its class or
token, and `docs/razor-vs-axaml.md` shows the same UI in both. **They are part of the change, not
a follow-up:** adding, renaming or dropping a class or token means updating the parity row in the
same commit, or the page starts lying to the people most likely to read it.

**The public API is ONE TYPE and is meant to stay that way:** `FiliThemeVariants`, whose single
member is the high-contrast `ThemeVariant`. It cannot be internal, and not for convenience — a
custom variant is unreachable from XAML except through `x:Static`, so this package's own
dictionaries and every adopting app both have to be able to name that object. Anything else that
wants to be public is a design change, not an addition.

`internal` helpers are allowed where XAML genuinely cannot express something — compiled XAML in
the same assembly can construct an internal type, so nothing leaks to a consumer. There are two
today: `Converters/FactorConverter`, which multiplies a bound pixel dimension by a constant,
because MudBlazor sizes several things as a percentage of their container and Avalonia has no way
to say that (its transform parser rejects `%` outright, at runtime); and
`Converters/ErrorMessageConverter`, which turns a validation-error entry (usually an `Exception`)
into the sentence to show. Reach for one only after establishing that the XAML route does not
exist; a public type still needs a separate package.

Scope discipline matters here, and the line is between **retemplating an Avalonia control** and
**inventing a new one**. The first is in scope: `Button` needed it because `Button` has no
`BoxShadow`, and `TextBox` needed it for the floating label. The second is not — a `Card`, a
`DataGrid`, a date picker or a dialog service turns this into a different, much larger project, the
kind that takes years, and there are already several (Semi.Avalonia, SukiUI, Ursa,
Material.Avalonia, Flowery.NET). A MudBlazor component that is a *look* on a control Avalonia
already has is a class, not a new control: MudIconButton (`Button.icon`), MudChip (`Button.chip`
/ `ToggleButton.chip`, themed by the keyed `FiliChip`), MudAlert (`Border.alert`) and MudSkeleton
(`Border.skeleton`) are all in scope, and `ComponentClassTests` pins them. If a new control
genuinely needs to exist, it belongs in a separate `Fili.MudAvalonia.*` package that depends on
this one.

## The one rule

**A missing or misspelt resource key is silent.** Avalonia resolves it to nothing and leaves the
previous value in place, so a typo becomes a colour that quietly never changed. The same is true
of a `StaticResource` used where a `DynamicResource` belonged: it freezes the light value and that
control silently stops following the theme.

Consequently:

- Every reference to a token uses `{DynamicResource}`. No exceptions in this repo.
- **Every new token gets an entry in `ResourceResolutionTests`**, in the same change. That suite
  asserts every key resolves under light and dark, `HighContrastTests` does the same for the third
  variant AND asserts each token differs from dark, and together they are the only thing standing
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
- **A colour class is only a colour, exactly as `Color` is in MudBlazor - on every control.**
  `primary` alone is a primary TEXT button; `filled primary` is the filled one. `SplitButton` takes
  the same classes (MudButtonGroup's Variant x Color x Size), and on a `TextBlock` a colour class
  is MudText's `Color` - so `secondary` is the PINK secondary colour, and grey supporting text is
  `Foreground="{DynamicResource FiliTextSecondaryBrush}"`. `text`/`outlined`/`filled` switch the
  ControlTheme (FiliTheme.axaml); colour and size are nested `^.primary` / `^.small` styles inside
  each theme.
- **Button.axaml, and marked regions in SplitButton.axaml (split and drop-down), Chip, CheckBox,
  RadioButton, ToggleSwitch, Slider, ProgressBar and FiliTheme.axaml (TextBlock, alert, app bar
  and tab bar colours), are GENERATED** by `ThemeColourGenerator` (unit-test project,
  `Generation/`) from one list of eight colours. Change the generator, never one colour's block by
  hand: `GeneratedThemeTests` fails on any difference, and rewrites the files when
  `FILI_REGENERATE=1` is set. Each colour needs five
  tokens - `Fili{C}Color`, `…ContrastTextColor`, `…HoverColor` (the colour at 6%, 35% in high
  contrast), `…DarkenColor` and `…LightenColor`. The last two are MudBlazor's derivation, ported
  as `MudColorPort`, and `PaletteDerivationTests` asserts every one in every variant against it, so
  changing a colour without its shades fails the build. The port must use .NET's own
  `Math.Round`, which scales by 100 in double arithmetic before rounding half-to-even (0.465 ->
  0.46, 0.575 -> 0.57); it is checked against the 14 values mudblazor.com publishes.
  `ButtonMatrixTests` and `SplitButtonMatrixTests` check every cell under a real headless
  `MouseMove`.
- **Most MudBlazor components default to `Color.Default`, and Color.Default is NOT primary.**
  MudButton, MudMenu, MudButtonGroup, MudCheckBox, MudRadio and MudSwitch all default to it, and
  it means text-primary text or the grey `action-default` icon colour - a checked checkbox with no
  class is grey. Only MudSlider and MudLink default to primary. Primary
  everywhere is MATERIAL's default and this theme carried it until each control was read against
  its `.razor.cs`; check the component's parameter default before assuming one.
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
- **A Style always outranks a ControlTheme setter.** This has bitten three times, in different
  costumes, and it is the single most likely way to break this package:
  - A blanket `Selector="TextBlock"` setting `Foreground` also matched the `TextBlock` a
    `ContentPresenter` makes for button content, repainting white-on-primary text body-grey.
    Fixed by putting inheritable defaults (`FontFamily`, `FontSize`, `Foreground`) on
    `Window, UserControl` and leaving only `LineHeight` and `LetterSpacing` on `TextBlock`.
  - A blanket input style setting `CornerRadius` flattened the filled field's top-only
    `4,4,0,0`. Fixed with `:not(.filled):not(.outlined)` on that selector.
  - `Border.appbar TextBlock` painted app-bar text white - and, because a descendant selector
    walks the LOGICAL tree, also every item of a ComboBox on the bar, which renders in a popup on
    a white surface. White on white. Fixed by setting `TextElement.Foreground` on the bar
    instead, so the colour is inherited and loses to any control's own.

  The rule that falls out: **any blanket style that sets a property a ControlTheme also sets needs
  a `:not()` guard, or it must move to the container as an inherited value.**
  `ButtonContentKeepsItsContrastForeground`, `FilledTextFieldKeepsTopOnlyRounding` and
  `AppBarTests` pin all three. A descendant selector reaching into popups is the specific thing
  to suspect whenever a container's style shows up somewhere it should not.
- **`TextPresenter` has no `Foreground` AvaloniaProperty.** It reads the inherited
  `TextElement.Foreground` from its parent, so a setter targeting `PART_TextPresenter` fails to
  compile with `AVLN3000`. Set `Foreground` on the `TextBox` itself instead.
- **`DynamicResource` is not type-checked.** Binding a `Background` to a `*Color` token instead of
  a `*Brush` compiles cleanly and renders nothing at runtime. Every colour token therefore has a
  paired brush, and `ResourceResolutionTests` asserts the brush list resolves to `IBrush` — which
  is what catches the paired brush being forgotten. This bit once, on the switch tokens.
- **Re-focusing an already-focused control does not revisit `:focus-visible`.** A test that
  focuses one control with `NavigationMethod.Pointer` and then with `Tab` sees no tint and passes
  for the wrong reason. Use two controls, one per navigation method.
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
- **A browser build has no system fonts.** Text in any script Roboto and Inter lack — the
  gallery's Arabic, first — renders as nothing there while looking fine on desktop, where the OS
  falls back silently. The gallery embeds static Noto Sans Arabic in its own `Assets/Fonts` and
  `GalleryFonts.WithGalleryFonts()` registers it as a `FontManagerOptions.FontFallbacks` entry for
  the desktop head, the browser head AND `GalleryFrames.Configure` - so the pixel baselines do not
  depend on whichever Arabic face the machine has installed either. New non-Latin text in the
  gallery needs a face added there too; on desktop nothing fails without one, so the checks are
  the published page and a baseline that changes when it should not.
- **RTL needs nothing from a template, and one thing from a glyph.** Avalonia mirrors a whole
  subtree with a single transform where the flow direction CHANGES, so hardcoded `Left`/`Right`
  alignment, dock sides and grid columns flip on their own — do not "fix" them. What does need
  saying is the opposite: a glyph that must NOT mirror (a checkmark) opts out with
  `FlowDirection="LeftToRight"`, while a directional one (a disclosure arrow) is left alone.
  Note also that `HasMirrorTransform` is false *inside* the subtree, and that measuring the flip
  requires translating a point across the boundary, not to the mirrored panel.
- **A selector may cross only ONE `/template/` boundary.** Two hops throw
  `InvalidOperationException: ControlTemplate styles cannot contain multiple template selectors`
  when the theme is first instantiated — it compiles fine, and because the throw happens while the
  merged dictionary is being built, it takes *other* themes down with it. That is worth knowing on
  its own: a single bad selector in `Slider.axaml` made unrelated themes fail to resolve and a
  Button style silently stop applying, which looked like three separate bugs. When a parent must
  reach a grandchild part, set a property on the child and have the child's template bind to it —
  `Slider` lights the `Thumb`'s halo through `BorderBrush` for exactly this reason.
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
- **A Style overrides a value set inline in a template ONLY IF the selector has an activator.**
  The priority order is `Animation < LocalValue < StyleTrigger < Template < Style`, lower binding
  harder. A value written inside a `ControlTemplate` binds at **Template**, which OUTRANKS a
  plain `Style` — so `SplitView /template/ Rectangle#HCPaneBorder { Fill: ... }` does nothing at
  all, silently, and reads exactly like a selector that failed to match. Add any pseudo-class and
  the same setter binds at **StyleTrigger**, which wins. This is why the floating labels work:
  they set a resting `RenderTransform` inline in the template and every style that moves it is on
  a `:focus`. It is also why the high-contrast drawer edge is selected through the four
  `DisplayMode` pseudo-classes rather than on the bare type. Pinned by
  `TheDrawerGainsAnEdgeInHighContrastOnly`.

  **It also applies between a theme and the themes `BasedOn` it.** A variant theme's plain
  `^ /template/ X#PART_Y` style is not an activator either, so it cannot move anything the base
  template wrote inline. Filled and outlined text fields shipped with NONE of their 12px / 14px
  side padding for exactly this reason, while their `.dense` styles, which do carry an
  activator, applied fine. The rule: **a value a variant theme changes is set by a style in the base
  theme, never inline in the template.** `VariantsPadTheirContentFromTheSide` pins it.

  And a `{Binding}` in a template is stronger still: it binds at **LocalValue**, which beats
  every style, activator or not. So a part whose `IsVisible` a style must be able to switch off
  cannot also bind it - put the binding on a wrapping `Panel` instead, as the helper line does.
- **A dictionary's own entries are found BEFORE its `ThemeDictionaries`.** So a token declared
  once outside the variant dictionaries cannot be given a per-variant value by adding one - the
  outer value wins in every variant, silently. This is why the elevation ladder had to move
  *into* Light and Dark as two identical copies rather than gaining a third dictionary beside it.
  Pinned by `OwnEntriesWinOverThemeDictionaries`.
- **`x:Key="HighContrast"` is not a theme variant.** Avalonia's `ThemeVariant` type converter
  accepts the built-in variants only and throws `NotSupportedException` while the merged
  dictionary is being built - which takes unrelated themes down with it, like the two-hop selector
  above. A custom variant is reachable from XAML only through
  `x:Key="{x:Static theme:FiliThemeVariants.HighContrast}"`.
- **High contrast inherits from Dark, and the fallback is silent.** A token the high-contrast
  dictionary does not declare resolves to the dark one, so it neither fails nor goes missing - it
  comes back subtly wrong, usually as a translucent line over a black ground, which is to say no
  line. Resolution tests therefore prove nothing here: `EveryPaletteTokenHasItsOwnValue` asserts
  each token DIFFERS from dark, which is the only evidence the key was actually written.
- **Every brush in this package is one shared application-level object** whose `Color` is a
  `DynamicResource`. A per-WINDOW `RequestedThemeVariant` therefore does not work: the window gets
  the same brush instance and so the same colour. Set the variant on the `Application`.
- **`UseHeadlessDrawing` defaults to TRUE, and it is a no-op renderer.** Every capture comes back
  blank, nothing throws, and a pixel suite built on it compares two blank images and passes
  forever. Skia plus `UseHeadlessDrawing = false` is the only configuration that produces a
  frame; `GalleryFrames.Configure` is the single place that says so.
- **An indeterminate `ProgressBar` is not reproducible.** Its band follows a wall-clock animation
  clock, so two runs of the SAME BUILD differ by about eighty pixels. The pixel suite masks that
  rectangle, located from the live tree rather than remembered, and compares everything else
  strictly. Do not answer a flaky frame with a wider tolerance: a budget big enough to absorb an
  animation is big enough to hide a redrawn glyph, everywhere in the frame.
- **A one-shot animation is not reproducible either, until it finishes.** The same wall clock
  drives the snackbar's 0.45s enter fade, and a frame captured at 99% of it fails strictly on
  one machine and passes on another. `GalleryFrames.Render` therefore waits out `SettleTime`
  before capturing. Raise that constant if a longer animation starts on load; do not mask a
  region whose colours the suite is meant to be watching.
- **A frame must not depend on the machine's locale.** The palette tab formats contrast ratios
  with the current culture, so baselines recorded on a pt-PT machine said `6,00:1` and the en-US
  CI runner rendered `6.00:1` - three palette frames failed in CI while every other frame passed,
  which is the signature to recognise. `GalleryFrames.Render` pins the invariant culture for the
  capture. Anything else that varies per machine (time zone, current date) needs the same
  treatment before it appears in a baselined view.
- **Elevation does not vary between light and dark**, matching MudBlazor - but it DOES vary
  in high contrast, where every raised level becomes a hard 1px ring instead of a shadow. That is
  what gives the variant its separation without a single control template knowing it exists.

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
  resource instead failed silently.)

**Nothing in this repo references `Avalonia.Themes.Fluent` any more.** The gallery's substrate
selector, which flipped the forked controls to Fluent for comparison, was removed: it showed a look
the package does not ship and drew Fluent's dark theme under the High contrast label. Do not bring
Fluent back for a comparison - `StandaloneReadinessTests` is the record of what is hand-written.

**Coverage is pinned, not reported.** `StandaloneReadinessTests` asserts the exact set of
hand-written themes — **39 of the 89 templated types** — plus the count of both. The other 50 wear
forked templates, and `SimpleBridgeTests` asserts the ~96 contract keys those paint from still
resolve in both variants. Adding a theme means adding its `Fili*` key AND its target type to the
first test in the same change; that is what keeps the number honest and turns an Avalonia version
that adds control types into a failing build rather than a silent gap.

**There are THREE ways to change how a control looks, and reaching for the heaviest one by
default is the mistake.** In order of increasing cost: a `Style` in `FiliTheme.axaml` when the
shape is right and only values are wrong (`SplitView`/MudDrawer is the worked example — a Style
outranks a ControlTheme setter, which is the trap above used deliberately); `Accents.axaml` when
the control paints from one of Simple's shared keys; a hand-written `ControlTheme` when the shape
itself is wrong.

**`Accents.axaml` cannot override a key a forked control file declares for itself.** It is merged
BEFORE the control dictionaries, and in a merged `ResourceDictionary` the later entry wins. So
`SplitViewOpenPaneThemeLength`, declared inside `Base/Controls/SplitView.axaml`, is out of its
reach — a Style setting the property directly is the way.

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
dotnet workload install wasm-tools   # once - the solution includes the browser gallery
dotnet build Fili.MudAvalonia.Theme.sln
dotnet test  Fili.MudAvalonia.Theme.sln
dotnet run --project src/Fili.MudAvalonia.Theme.Gallery.Desktop
dotnet run --project src/Fili.MudAvalonia.Theme.Gallery.Browser     # needs the wasm-tools workload

# Accept new pixel baselines, after looking at the diff image the failure printed:
$env:FILI_PIXEL_BASELINES = "accept"; dotnet test tst/Fili.MudAvalonia.Theme.PixelTests

# Button.axaml and the marked regions are generated - edit ThemeColourGenerator, then rewrite them:
$env:FILI_REGENERATE = "1"; dotnet test tst/Fili.MudAvalonia.Theme.UnitTests --filter-class "*GeneratedThemeTests"
```

**Releasing is a tag, and the tag is the human's to push.** `.github/workflows/release.yml` runs on
`v*` tags only: it reruns both suites on Windows, refuses a tag that does not equal `<Version>` in
`Directory.Build.props`, then packs, pushes to nuget.org through trusted publishing (no stored
key; the `NUGET_USER` repository variable names the account) and creates the GitHub release. A
nuget.org version can never be replaced, so a release is outward-facing and permanent: bump
`<Version>` in a reviewed commit and leave creating and pushing the tag to the human, like every
other git write here. The same commit adds the version's row to `docs/versions.md`, and an
"Upgrading" section there when it changes what existing markup looks like; the README links to it
rather than carrying the history itself.

**There are two test projects.** `tst/{Name}.UnitTests` is the fast one and renders nothing;
`tst/{Name}.PixelTests` renders nine gallery frames with Skia and diffs them against committed
PNGs. The split is deliberate - the pixel suite needs headless drawing turned OFF, which changes
text measurement, and the unit suite should not silently start measuring differently because the
pixel suite needed a different platform.

**CI runs both, on two runners, and the pixel suite on only one of them.** Linux builds the
whole solution and runs the unit tests; Windows builds only the two test projects (everything but
the browser gallery, so it skips the `wasm-tools` install) and also runs the baselines, because
those PNGs were rendered on Windows with Skia. A failing frame uploads the rendered image and the
diff as the `pixel-diffs` artifact - which is why the diff directory is overridable through
`FILI_PIXEL_DIFF_DIR`, the default being a system temp path no artifact upload can reach.

**The hosted Windows runner rasterises identically to a developer machine.** Its only failures so
far were the locale (see the trap above). If a frame fails in CI and nowhere else, compare the
uploaded artifact with the committed baseline and look for something machine-dependent first; if
the runner ever genuinely rasterises differently, the fix is a second committed set of baselines
per environment, never a wider tolerance.

**The gallery has two heads and one view.** `MainView` is the whole UI; `MainWindow` hosts it on
desktop and the browser head sets it as the single view. Put gallery UI in `MainView`, never in
the window, or the GitHub Pages build silently loses it. And never construct a `StyleInclude` in
gallery code: the two in App.axaml compile to code, while one built in C# loads by reflection,
which the browser build's trimming can strip - leaving the page with no base theme at all.

The gallery is the development loop, not a deliverable added at the end. A theme has no surface of
its own, so tune against the gallery rather than against an app.

## Related work in this workspace

`Fili.MangaReader/src/Fili.MangaReader.Views/Themes/MudBlazorPalette.axaml` applies the same
palette over Material.Avalonia, on Avalonia 12, dark only. Read it before changing anything here —
its header documents both traps above from experience. The two are not yet reconciled; doing so is
a deliberate decision, not a drive-by refactor.

# Testing

What the tests check, how CI runs them, and why each one exists. The short version: **almost
every way a theme breaks is silent.** A misspelt resource key resolves to nothing, a token the
high-contrast variant forgets falls back to dark without a word, a style that loses a priority
contest just does not apply. Nothing throws and the app still starts. The tests are there to turn
those silent failures into red builds, because looking at the app cannot be relied on to find them.

## Two test projects

| Project | What it does | Speed |
|---|---|---|
| `tst/Fili.MudAvalonia.Theme.UnitTests` | 345 tests on a headless Avalonia session. They resolve resources, template controls, move a real pointer over them, and read back the values the theme applied. Nothing is rendered. | seconds |
| `tst/Fili.MudAvalonia.Theme.PixelTests` | 9 tests. Each renders one gallery frame with Skia and compares it pixel by pixel with a PNG committed in `Baselines/`. | ~20 seconds |

They are separate on purpose. The pixel suite needs Skia with headless drawing turned **off** -
Avalonia's default headless renderer draws nothing at all, and a pixel suite built on it would
compare two blank images and pass forever. Turning real drawing on also changes how text is
measured, and the unit suite should not start measuring differently because the pixel suite
needed a different platform.

## The unit tests

345 tests in 23 classes, grouped here by the failure each one exists to catch.

### Tokens: does every value exist, in every variant?

| Class | Tests | What it checks | Why it matters |
|---|---|---|---|
| `ResourceResolutionTests` | 5 | Every colour, brush and elevation token resolves in light and dark, every brush really is an `IBrush`, primary differs between the variants, and the dark surface sits above the dark background. | A missing or misspelt key is silent in Avalonia: the control keeps whatever it had and the colour quietly never changes. A `*Color` bound where a `*Brush` belonged compiles and paints nothing. |
| `HighContrastTests` | 15 | Every token resolves in high contrast **and differs from dark**, and the variant's own rules (hard rings for elevation, the drawer edge) apply only there. | High contrast inherits from Dark, so a forgotten token comes back with the dark value - usually a translucent line over black, which is to say no line. Only "differs from dark" proves the key was written. |
| `PaletteDerivationTests` | 23 | Every `*DarkenColor` and `*LightenColor` equals MudBlazor's own derivation (`MudColorPort`, HSL lightness ∓ 0.075 with .NET rounding), and the port matches the 14 values mudblazor.com publishes. | Hover and filled-button shades are derived, not chosen. Changing a colour without its shades, or rounding the port differently, shows up here rather than as a slightly wrong hover. |
| `SimpleBridgeTests` | 6 | The ~96 keys the forked Avalonia Simple templates paint from still resolve in both variants. | The first standalone render had switches with no switch: eighteen `*Color` keys were missing while their brushes were mapped. A new Avalonia version that adds a key fails here instead of on screen. |

### Coverage: is every control themed?

| Class | Tests | What it checks | Why it matters |
|---|---|---|---|
| `StandaloneReadinessTests` | 3 | The exact set of hand-written control themes (39 of the 89 templated types) and that every one resolves; every other type is classified with the reason it stays on its forked template. | Avalonia has no implicit default theme. A control with no theme does not look wrong - it renders **nothing**. An Avalonia upgrade that adds a control type fails here asking to be classified. |
| `ClassVocabularyTests` | 1 | The classes the theme's selectors use are exactly the 59 the README publishes, in both directions. | Class names are MudBlazor's ordinary words (`small`, `error`, `vertical`) and they collide with an app's own. An undeclared class is a collision nobody signed off on; a declared one nothing uses is a lie to adopters. |
| `GeneratedThemeTests` | 1 | `Button.axaml` and the marked regions in eight other files are exactly what `ThemeColourGenerator` writes from the one list of eight colours. | Hundreds of colour styles are generated so that no colour can differ from the others. Editing one block by hand fails the build. `FILI_REGENERATE=1` rewrites the files. |

### Controls: does each one look and behave as MudBlazor's does?

These template real controls, hover them with a real headless pointer move, focus them, select
them, and assert the brushes, sizes and visibility the theme produced - each value transcribed
from MudBlazor's SCSS or the component's parameter defaults.

| Class | Tests | What it covers |
|---|---|---|
| `ThemeCompositionTests` | 49 | The cross-cutting rules: Roboto loads with a face per weight, button content keeps its contrast colour, the filled field keeps its top-only corners, the select floats its label only with a selection, validation shows the message rather than the exception type, keyboard focus lights the state layer and pointer focus does not, tree indent, drawer metrics. |
| `ButtonMatrixTests` | 45 | `Button`: three variants × eight colours × three sizes, at rest and under a real pointer hover, plus contrast ratios. |
| `ComponentClassTests` | 38 | Icon buttons, chips, alerts, skeletons, the light divider and `flat`. |
| `TabsAndAppBarTests` | 31 | MudTabs' defaults, colours, `border` / `outlined` / `rounded` / `centered` / `hide-slider`, and that a bar's colour never reaches a nested `TabControl`; the app bar's gutters, `dense` and colours. |
| `SplitButtonMatrixTests` | 29 | `SplitButton` as MudButtonGroup, including the separator rule and the hit-testing of each half. |
| `FieldDetailTests` | 21 | Helper text, the error in its place, the counter, `helper-on-focus`, `dense`, select variants, and that filled and outlined fields pad their content. |
| `SliderMatrixTests` | 13 | `Slider` sizes and colours, and the rings on hover and focus. |
| `DropDownButtonMatrixTests` | 12 | `DropDownButton` as MudMenu's button activator. |
| `SelectionGlyphMatrixTests` | 12 | Checkbox and radio glyph colours and sizes. |
| `SwitchMatrixTests` | 11 | Toggle switch thumb and track colours and sizes. |
| `TextColourTests` | 9 | `TextBlock` colours, MudText's `Color`. |
| `ControlSizingTests` | 8 | The minimum sizes a control needs to exist when nothing stretches it - a number MudBlazor never has to give, because CSS sizes by container. |
| `ProgressColourTests` | 8 | `ProgressBar` colours. |
| `RightToLeftTests` | 2 | Avalonia mirrors a subtree with one transform, so layout flips on its own; a checkmark must opt out and does. |
| `AppBarTests` | 2 | The app bar's text colour is inherited, so a select on the bar keeps text-primary in its popup instead of white on white. |
| `LinkInheritTests` | 1 | `HyperlinkButton.inherit` really takes the surrounding text colour. |

Several of these exist because the bug they pin actually shipped once: tabs and selects reached
by a descendant selector, a select with no error host, filled fields with no side padding because
a style lost a priority contest to a value written in the template. The test names say what was
wrong, so a failure reads as the sentence that broke.

## The pixel tests

Nine frames: the gallery's **controls**, **sample screen** and **palette** views, each in
**light**, **dark** and **high contrast**. Each is rendered by `GalleryFrames.Render`, the same
routine the gallery's `--capture` mode uses, and compared with `tst/.../Baselines/{view}-{variant}.png`.

**Why they matter:** the unit tests check values, and values can all be right while the picture
is wrong - a part laid out on top of another, a mask on the wrong ground, a glyph clipped by
its container. The frames are also the review: a theme change arrives with the PNGs it changed,
so the diff in a pull request is a picture rather than a description.

Four things keep a frame reproducible, and each was learned from a failure:

- **Animations that never end are masked.** An indeterminate `ProgressBar` and a pulsing skeleton
  follow a wall-clock animation, so two runs of the same build differ there. Their rectangles
  are found from the live tree and left out of the comparison; everything else stays strict. A
  `no-animation` skeleton holds still and is compared like everything else.
- **One-shot animations are waited out.** The capture waits until the longest animation that
  starts on load (the snackbar's 0.45s enter) has finished.
- **The culture is pinned.** The palette view formats contrast ratios with the current culture,
  so baselines recorded on a pt-PT machine (`6,00:1`) failed on an en-US runner (`6.00:1`).
  Frames render under the invariant culture.
- **The frame is drawn in one pass.** The capture used to read the window's own rendered frame,
  which is the sum of every partial redraw since it opened - and how many there were depends on
  timing. On a loaded machine, and on CI's slower runners, the antialiased ends of a large slider's
  rail and knob and a large switch's track came out up to 36 levels off on about one run in three,
  failing a build and then a release. Drawing the whole tree with `RenderTargetBitmap` has no
  history: fifteen runs out of fifteen were identical under full load. To check a fix like this,
  run the pixel suite while every core is busy; an idle machine hides it.

Each channel of each pixel may differ by 1/255, as headroom for a Skia release rounding an edge
differently. That is the only slack, and it should stay that small: a pixel that keeps failing by
a few levels means a capture that is not deterministic, which is how this one was found.

What is deliberately **not** done: there is no budget of differing pixels. A budget wide enough
to absorb an animation is wide enough to hide a redrawn glyph anywhere in the frame. And the
baselines are Windows/Skia only - text rasterises differently on Linux, so Linux does not run
them; a second committed set per platform is the answer if that is ever needed.

When a frame fails, the test prints how many pixels differ, by how much, and where, and writes
the rendered frame and a diff image (the baseline in grey, changed pixels in red, masked regions
in orange) to `FILI_PIXEL_DIFF_DIR`, or the system temp directory by default.

## In CI

Three workflows, in `.github/workflows/`.

### `build.yml` - every push and pull request to `master`

| Runner | Builds | Runs |
|---|---|---|
| `ubuntu-latest` | the whole solution, the WebAssembly gallery included | the unit tests |
| `windows-latest` | the two test projects, which reference every other project | the unit tests **and** the pixel tests |

- **Linux** is there to catch a Windows-only assumption in a library other people will build on
  whatever they have, and to build the browser gallery, which needs the `wasm-tools` workload.
  It does not run the pixel tests, for the rasterisation reason above.
- **Windows** runs the pixel tests because the baselines were rendered there.
- Every run uploads the test results (`.trx`) as `test-results-{os}`. A failing pixel run also
  uploads **`pixel-diffs`**, the rendered frames and diff images, kept for 14 days - so a change
  can be judged from the run page instead of reproduced locally first.
- `fail-fast` is off, so one runner failing does not cancel the other's answer.
- Nothing in it packs or publishes; the token is read-only.

### `release.yml` - a `v*` tag

Before anything is published, it **reruns both suites on Windows**. A tag on a commit that does
not pass does not reach nuget.org - and a nuget.org version can never be replaced, only unlisted,
so this is the last check that matters. It then refuses a tag that differs from `<Version>`,
packs, pushes through trusted publishing and creates the GitHub release.

### `pages.yml` - every push to `master`

Publishes the browser gallery to GitHub Pages. It runs no tests, but it is the check that the
WebAssembly build still works *as a page* - a font that only exists on desktop, or a style
include that trimming strips, shows up there and nowhere else.

## Running them

```bash
dotnet test tst/Fili.MudAvalonia.Theme.UnitTests                     # everything, seconds
dotnet test tst/Fili.MudAvalonia.Theme.UnitTests --filter-class "*FieldDetailTests"
dotnet test tst/Fili.MudAvalonia.Theme.PixelTests                    # the nine frames
```

```bash
# After changing ThemeColourGenerator: rewrite the generated files.
FILI_REGENERATE=1 dotnet test tst/Fili.MudAvalonia.Theme.UnitTests --filter-class "*GeneratedThemeTests"

# After an intended visual change: look at the diff first, then accept the new frames.
FILI_PIXEL_BASELINES=accept dotnet test tst/Fili.MudAvalonia.Theme.PixelTests
git restore $(git status --porcelain | grep png | grep -v "<the frames you meant to change>" | cut -c4-)
```

Accepting rewrites every frame, so restore the ones the change should not have touched, then run
the suite twice more: a frame that differs between two runs of the same build is an animation
that needs masking, not a baseline to accept. PowerShell sets the variables with
`$env:FILI_REGENERATE = "1"` and `$env:FILI_PIXEL_BASELINES = "accept"`.

## When you change the theme

| Change | Test that must change with it |
|---|---|
| a new token | an entry in `ResourceResolutionTests` and in `HighContrastTests`, with a high-contrast value that differs from dark |
| a new colour shade | `PaletteDerivationTests` checks it against `MudColorPort` |
| a new class | `ClassVocabularyTests`, and the README's class table |
| a colour rule on a control | the generator, never a hand edit; then `FILI_REGENERATE=1` |
| a new control theme | its key and target type in `StandaloneReadinessTests` |
| anything visible | the pixel baselines, reviewed and accepted in the same commit |

Every test body runs inside `UiThread.RunAsync`. Constructing almost any Avalonia object from the
test thread throws, and doing it once poisons the shared headless session, so every later test
in the run fails too - a missing wrapper fails the suite, not just its own test.

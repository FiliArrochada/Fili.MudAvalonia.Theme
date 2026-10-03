# Versions

What changed in each release of `Fili.Theme.MudAvalonia`, newest first. The package is on
[nuget.org](https://www.nuget.org/packages/Fili.MudAvalonia.Theme) under its original id,
`Fili.MudAvalonia.Theme`; install steps are in the [README](../README.md#install).

| Version | |
|---|---|
| *Unreleased* | The project is renamed `Fili.Theme.MudAvalonia`: the assembly, namespaces, `avares://` paths and repository. The NuGet id stays `Fili.MudAvalonia.Theme`. See [Upgrading to the renamed project](#upgrading-to-the-renamed-project). |
| **0.2.0** | MudBlazor parity. A colour class is now only a colour, as `Color` is in MudBlazor - on buttons, split and drop-down buttons, checkboxes, radios, switches, sliders, progress bars, text, tabs and the app bar - with Button's full Variant × Color × Size matrix. Icon buttons, chips, alerts and skeletons as classes; helper text, a counter and dense fields; select variants; MudTabs and MudAppBar options. See [Upgrading from 0.1 to 0.2](#upgrading-from-01-to-02). |
| 0.1.1 | The app bar's text colour is inherited rather than set on every `TextBlock`, which had turned an app-bar select's drop-down white on white. The gallery loses its Fluent comparison switch. |
| 0.1.0 | First release: the palette, elevation, typography and geometry tokens, the hand-written control themes over a forked Avalonia Simple base, light, dark and high contrast. |

Every version and its release notes are on the
[releases page](https://github.com/FiliArrochada/Fili.Theme.MudAvalonia/releases); a version on
nuget.org can be unlisted but never replaced.

## Upgrading to the renamed project

The package id does not change, so `dotnet add package Fili.MudAvalonia.Theme` and every
`PackageReference` keep working. What changes is the assembly name inside it, which is part of
every resource URI and namespace:

- **The two includes:** `avares://Fili.MudAvalonia.Theme/...` becomes
  `avares://Fili.Theme.MudAvalonia/...` - in `Themes/Base/FiliBaseTheme.axaml` and
  `FiliTheme.axaml` alike. The old URI does not fail to compile; it fails at startup.
- **`FiliThemeVariants`:** `xmlns:theme="using:Fili.MudAvalonia.Theme"` becomes
  `using:Fili.Theme.MudAvalonia`, and `using Fili.MudAvalonia.Theme;` in C# likewise.

## Upgrading from 0.1 to 0.2

0.2.0 changes what some existing markup looks like:

- **`Classes="primary"` on a button is a primary *text* button.** It used to mean filled; add
  `filled` for that. The same holds on `SplitButton`, and an unclassed `DropDownButton` is no
  longer primary.
- **`secondary` on a `TextBlock` is the pink secondary colour**, MudText's `Color`. Grey
  supporting text is `Foreground="{DynamicResource FiliTextSecondaryBrush}"`.
- **Tabs follow MudTabs' defaults:** a surface bar with no rule under it (add `border` for one),
  tabs at least 160px wide, labels in text-primary.
- **An app bar has 24px side padding**, MudAppBar's `Gutters`; `Padding="0"` removes it.
- **Filled and outlined fields gain their 12px / 14px side padding**, which 0.1 never applied.

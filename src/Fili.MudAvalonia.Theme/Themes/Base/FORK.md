# Forked from Avalonia's Simple theme

Everything under `Controls/`, plus `InvariantResources.axaml`, is taken **verbatim** from the
Avalonia repository at tag **12.1.2** and is under Avalonia's MIT licence:

- `Controls/*.axaml` — `src/Avalonia.Themes.Simple/Controls/*.xaml`
- `InvariantResources.axaml` — `src/Avalonia.Themes.Fluent/Strings/InvariantResources.xaml`
  (Simple references it by a relative path but does not carry its own copy)

Copyright (c) .NET Foundation and Contributors. MIT licence:
<https://github.com/AvaloniaUI/Avalonia/blob/master/licence.md>

## What was changed

Only two mechanical edits, applied by script:

1. `.xaml` → `.axaml`, matching this repo's convention.
2. Every `avares://Avalonia.Themes.Simple/Controls/X.xaml` rebased to
   `avares://Fili.MudAvalonia.Theme/Themes/Base/Controls/X.axaml`.

**No template was restyled.** Keeping them byte-faithful is deliberate: it is what makes the
upgrade path a re-download plus a diff rather than a merge.

## How they become Material

`Accents.axaml` — which is *not* forked, and is the only hand-written file here — redefines the
~75 resource keys the Simple templates paint from, binding each to a Fili token. One file
repalettes all 79 templates. That small, stable key surface is the reason to fork Simple rather
than Fluent, whose equivalent is hundreds of layered keys that move between versions.

Controls with a hand-written theme in `Themes/Controls/` override their forked counterpart,
because `FiliTheme.axaml` is included after `FiliBaseTheme.axaml`.

## Upgrading Avalonia

Re-run the fetch against the new tag, re-apply the two edits, and diff. Anything that changed in
a template is an upstream fix worth taking; anything that changed in the key surface needs a line
in `Accents.axaml`. `StandaloneReadinessTests` pins the templated-control count, so a version that
adds controls fails the build rather than leaving them unstyled.

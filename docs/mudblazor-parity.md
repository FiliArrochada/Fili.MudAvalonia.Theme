# MudBlazor parity

What each MudBlazor component parameter, palette property and token becomes in this theme, and
where the two differ. For side-by-side markup, see [Razor and AXAML](razor-vs-axaml.md).

The rule behind every row: **a class only ever names a MudBlazor variant.** Every control the
theme templates is keyed to its type, so an unclassed control is already the MudBlazor default.
Class names are MudBlazor's own words (`primary`, `outlined`, `small`), deliberately not
namespaced. `ClassVocabularyTests` pins the full list of 46.

| Mark | Meaning |
|---|---|
| ✅ | Same values, transcribed from MudBlazor's source |
| ≈ | Same look, reached differently, or close with a stated difference |
| ❌ | Not available |

## Components

### MudButton → `Button`

Variant, colour and size are one class each and combine freely:
`<Button Classes="outlined error small" />`.

| MudBlazor | Here | |
|---|---|---|
| `Variant.Text` (default) | no class, or `text` | ✅ |
| `Variant.Outlined` | `outlined` | ✅ |
| `Variant.Filled` | `filled` | ✅ |
| `Color.Default` (default) | no class | ✅ text-primary; grey fill when filled |
| `Color.Primary` … `Color.Dark` | `primary` `secondary` `tertiary` `info` `success` `warning` `error` `dark` | ✅ |
| `Color.Inherit` | `inherit` | ✅ text and line follow the surrounding text colour |
| `Size.Small` / `Medium` / `Large` | `small` / no class / `large` | ✅ per-variant padding, 13 / 14 / 15px |
| `Disabled` | `IsEnabled="False"` | ✅ |
| hover, `:focus-visible`, `:active` | the same state, as in `_button.scss` | ✅ `{color}-hover` for text and outlined, `{color}-darken` for filled |
| elevation 2 → 4 → 8 | the same | ✅ |
| `DropShadow="false"` | — | ❌ |
| `FullWidth` | `HorizontalAlignment="Stretch"` | ≈ |
| `StartIcon` / `EndIcon` | put an icon in `Content` | ≈ no icon margins applied |
| ripple | — | ❌ no Avalonia primitive; the state tint is its static half |
| uppercase label | — | ❌ Avalonia has no text-transform |

### MudIconButton, MudFab, MudChip, MudBadge, MudAvatar

❌ None yet. An icon button and a chip are restyles of `Button` and `ToggleButton` and are the
likely next additions. Badge needs an adorner, so it would be a new control, not a class.

### MudText → `TextBlock`

| MudBlazor | Here | |
|---|---|---|
| `Typo.h1` … `Typo.h6` | `h1` … `h6` | ✅ size, weight, line height and letter spacing from `Typography.cs` |
| `Typo.subtitle1` / `subtitle2` | `subtitle1` / `subtitle2` | ✅ |
| `Typo.body1` / `body2` | `body1` / `body2` | ✅ body1 is also the default text |
| `Typo.caption` / `overline` | `caption` / `overline` | ✅ |
| `Typo.button` | `FiliButton*` tokens | ≈ no class; a button applies them itself |
| `Color.Primary` … `Color.Dark` | `primary` `secondary` `tertiary` `info` `success` `warning` `error` `dark` | ✅ the palette colour; beats a type style's own colour |
| `mud-text-secondary` (grey) | `Foreground="{DynamicResource FiliTextSecondaryBrush}"` | ≈ a utility class in MudBlazor, not a `Color` |
| `Align`, `GutterBottom`, `Inline` | `TextAlignment`, `Margin` | ≈ ordinary Avalonia properties |

### MudPaper, MudCard → `Border`

| MudBlazor | Here | |
|---|---|---|
| `MudPaper` | `Border.surface` | ✅ surface fill and the default radius |
| `Elevation="0…24"` | `elevation0` `1` `2` `4` `6` `8` `12` `16` `24` | ✅ the ladder levels apps use; three shadow layers each, from `Shadow.cs` |
| `Outlined` | `outlined` | ✅ 1px `lines-default` |
| `Square` | `CornerRadius="0"` | ≈ |
| `MudCard`, `MudCardHeader`, `MudCardContent` | a `Border.surface` you lay out yourself | ≈ no card parts |

### MudAppBar → `Border.appbar`

| MudBlazor | Here | |
|---|---|---|
| background, text colour | `appbar` | ✅ text colour is inherited, so a control's own colour wins |
| height | `FiliAppbarHeight` | ✅ 64px |
| elevation | the same | ✅ 4 |
| `Dense`, `Bottom`, `Fixed` | — | ❌ |

### MudTextField → `TextBox`

| MudBlazor | Here | |
|---|---|---|
| `Variant.Text` (default) | no class | ✅ |
| `Variant.Filled` / `Outlined` | `filled` / `outlined` | ✅ |
| `Label` | `PlaceholderText` | ≈ **the placeholder is the floating label**, so there is no separate placeholder |
| `Error="true"` | `error` | ✅ |
| validation | a binding's own validation error | ✅ message under the field at 12px |
| `HelperText` | — | ❌ |
| `Counter` | — | ❌ |
| `Margin.Dense` | — | ❌ |
| `Adornment` | `InnerLeftContent` / `InnerRightContent` | ≈ ordinary Avalonia properties |

### MudSelect, MudAutocomplete, MudNumericField

| MudBlazor | Here | |
|---|---|---|
| `MudSelect` | `ComboBox` | ✅ a standard text field with a drop-down adornment, as `_select.scss` makes it |
| `Label` | `PlaceholderText` | ≈ the floating label |
| `Error` | `error` | ✅ |
| `Variant` on a select | — | ❌ standard only |
| `MudAutocomplete` | `AutoCompleteBox` | ✅ |
| `MudNumericField` | `NumericUpDown` | ✅ 24px spin column, as `_inputcontrol.scss` reserves |

### MudCheckBox, MudRadio, MudSwitch

| MudBlazor | Here | |
|---|---|---|
| `MudCheckBox` | `CheckBox` | ✅ the real Material glyphs, swapped per state |
| `MudRadio` | `RadioButton` | ✅ checked grows an inner disc |
| `MudSwitch` | `ToggleSwitch` | ✅ 20px thumb over a 14px track |
| `TriState` | `IsThreeState` | ✅ the indeterminate glyph |
| `Color` | — | ❌ primary only |
| `Size` | — | ❌ |

### MudSlider → `Slider`

| MudBlazor | Here | |
|---|---|---|
| rail, knob, hover and press growth | the same | ✅ 4px rail, 12px knob, 1.3× / 1.5× |
| vertical | `Orientation="Vertical"` | ✅ |
| `Color`, `Size` | — | ❌ |

### MudProgressLinear → `ProgressBar`

| MudBlazor | Here | |
|---|---|---|
| `Size.Small` (default) | no class | ✅ a 4px square hairline |
| `Size.Medium` / `Large` | `medium` / `large` | ✅ 8 / 12px |
| `Rounded` | `rounded` | ✅ |
| `Indeterminate` | `IsIndeterminate` | ✅ MudBlazor's two-bar animation |
| `Color` | `primary` `secondary` `info` `success` `warning` `error` | ✅ track at 20% of the colour |
| `Color.Tertiary`, `Color.Dark` | — | ❌ |
| `Buffer`, `Striped` | — | ❌ |
| `MudProgressCircular` | — | ❌ no circular progress control in Avalonia |

### MudDivider → `Separator`

| MudBlazor | Here | |
|---|---|---|
| default | no class | ✅ 1px, no margin |
| `DividerType.Inset` / `Middle` | `inset` / `middle` | ✅ 72px / 16px |
| `Vertical` | `vertical` | ✅ |
| `Light` | — | ❌ no `divider-light` token yet |

### MudLink → `HyperlinkButton`

| MudBlazor | Here | |
|---|---|---|
| `Underline.Hover` (default) | no class | ✅ |
| `Underline.Always` / `None` | `underline` / `no-underline` | ✅ |
| `Color.Primary` (default) | no class | ✅ |
| `Color.Secondary` / `Inherit` | `secondary` / `inherit` | ≈ `inherit` paints text-primary |

### MudTabs → `TabControl`, `TabStrip`

| MudBlazor | Here | |
|---|---|---|
| 48px strip, 2px indicator | the same | ✅ |
| sliding indicator | — | ❌ it fades per tab instead |
| `Color`, `Centered`, `Rounded`, `Border` | — | ❌ |

### MudList → `ListBox`

| MudBlazor | Here | |
|---|---|---|
| selected item | the same | ✅ primary text over primary at 6%, never a filled bar |
| `Dense` | `dense` on the item | ✅ |
| on a surface | `ListBox.surface` | ✅ |

### MudExpansionPanels → `Expander`

| MudBlazor | Here | |
|---|---|---|
| panel on a surface at elevation 1 | the same | ✅ |
| 15px header | the same | ✅ `.9375rem`, sized on its own |
| `Elevation="0"`, sitting on a card | `flat` | ≈ drops both surface and shadow |

### MudToggleGroup, MudButtonGroup, MudMenu

| MudBlazor | Here | |
|---|---|---|
| `MudToggleGroup` item | `ToggleButton` | ✅ |
| `Outlined`, `Size.Small` / `Large` | `outlined`, `small` / `large` | ✅ |
| a two-part `MudButtonGroup` | `SplitButton` | ✅ Button's classes: `outlined` / `filled`, any colour, `small` / `large` |
| group separator | the same | ✅ text-primary or the colour; `divider` when filled; `{color}-lighten` between filled coloured segments |
| `Vertical`, `DropShadow="false"` | — | ❌ |
| `MudMenu` with a button activator | `DropDownButton` | ≈ defaults to primary text, where `MudMenu` defaults to `Color.Default`; `outlined` only |
| menu items | `Menu`, `MenuItem` | ✅ strip items and dropdown rows are separate themes |

### MudTooltip, MudSnackbar, MudDrawer, MudTreeView

| MudBlazor | Here | |
|---|---|---|
| `MudTooltip` | `ToolTip.Tip` | ✅ solid `gray-darker` chip in every variant |
| `ISnackbar.Add(msg, Severity.X)` | `WindowNotificationManager.Show(…)` with `NotificationType.X` | ✅ a filled alert at elevation 6 |
| `MudDrawer` | `SplitView` | ✅ 240px open, 56px mini |
| `MudTreeView` | `TreeView` | ✅ 32px rows, 17px per level |

### Not here

These need a whole new control, or are not visual: MudDataGrid, MudTable, MudDatePicker,
MudTimePicker, MudColorPicker, MudChart, MudRating, MudPagination, MudStepper, MudTimeline,
MudBreadcrumbs, MudCarousel (left on the forked template), MudFileUpload, MudDialog (no dialog
service), MudSkeleton, MudAlert, MudOverlay, MudHidden, MudFocusTrap, MudHotkey, MudForm, MudGrid,
MudStack.

## Theme

### Palette → `Fili…Color` / `Fili…Brush`

Every colour token is a `Color` with a paired `Brush`, declared for light, dark and high contrast.
Light and dark are MudBlazor's `Palette.cs` and `PaletteDark.cs`; high contrast is derived.

| `Palette.cs` | Token | |
|---|---|---|
| `Primary` … `Dark` | `FiliPrimaryColor` … `FiliDarkColor` | ✅ |
| `PrimaryContrastText` … `DarkContrastText` | `Fili…ContrastTextColor` | ✅ |
| `PrimaryDarken` … `DarkDarken` | `Fili…DarkenColor` | ✅ MudBlazor's own algorithm, checked on every build by `PaletteDerivationTests` |
| `{color}-hover` (theme provider) | `Fili…HoverColor` | ✅ the colour at `HoverOpacity` |
| `PrimaryLighten` … `DarkLighten` | `Fili…LightenColor` | ✅ the same algorithm; the 14 darken and lighten values of the default palette match mudblazor.com |
| `Black`, `White` | `FiliBlackColor`, `FiliWhiteColor` | ✅ |
| `TextPrimary` / `TextSecondary` / `TextDisabled` | `FiliText…Color` | ✅ |
| `ActionDefault` / `ActionDisabled` / `ActionDisabledBackground` | `FiliAction…Color` | ✅ |
| `action-default-hover` (theme provider) | `FiliActionDefaultHoverColor` | ✅ |
| `Background` / `BackgroundGray` / `Surface` | `FiliBackground…Color`, `FiliSurfaceColor` | ✅ |
| `DrawerBackground` / `DrawerText` / `DrawerIcon` | `FiliDrawer…Color` | ✅ |
| `AppbarBackground` / `AppbarText` | `FiliAppbar…Color` | ✅ |
| `LinesDefault` / `LinesInputs` / `Divider` | `FiliLines…Color`, `FiliDividerColor` | ✅ |
| `TableLines` / `TableStriped` / `TableHover` | `FiliTable…Color` | ✅ |
| `Skeleton` | `FiliSkeletonColor` | ✅ token only; no skeleton class |
| `OverlayDark` / `OverlayLight` | `FiliOverlayDark/LightColor` | ✅ |
| `DividerLight` | — | ❌ |
| `GrayDefault` … `GrayDarker` | — | ❌ only `GrayDarker` is used, inlined in the tooltip |
| `HoverOpacity` / `BorderOpacity` | — | ≈ folded into the hover and line tokens rather than exposed |
| `RippleOpacity` / `RippleOpacitySecondary` | — | ❌ no ripple |

### Typography, shadows, layout

| MudBlazor | Here | |
|---|---|---|
| `Typography.cs` | `Fili{Style}FontSize`, `…LineHeight`, `…LetterSpacing` per style | ✅ line height and letter spacing converted to absolute pixels |
| `Shadow.cs` | `FiliElevation0` … `FiliElevation24` | ✅ three layers each; a 1px ring in high contrast |
| `DefaultBorderRadius` | `FiliCornerRadius` | ✅ 4px |
| `AppbarHeight`, `DrawerWidthLeft`, `DrawerMiniWidthLeft` | `FiliAppbarHeight`, `FiliDrawerWidth`, `FiliDrawerMiniWidth` | ✅ |
| spacing scale (`pa-4` and friends) | `FiliSpacing1` … `FiliSpacing8` | ≈ values only, no utility classes |
| font | Roboto, embedded | ✅ Light, Regular and Medium as static faces |

### Theme variants

| MudBlazor | Here |
|---|---|
| `IsDarkMode` | `Application.RequestedThemeVariant = ThemeVariant.Dark` |
| — | `FiliThemeVariants.HighContrast`, this package's own third variant |

## Known divergences

- **`DropDownButton` defaults to primary text.** `MudMenu`'s activator defaults to
  `Color.Default`, and the drop-down button has no colour or `filled` classes yet.
- **The `dark` colour barely shows in dark mode, and not at all in high contrast** when used as text
  or a line: `#27272F` on a `#32333D` page, and black on black. MudBlazor's dark theme has the same
  property; a filled dark button is still visible.
- **No ripple and no uppercase button text.** Neither has an Avalonia primitive.

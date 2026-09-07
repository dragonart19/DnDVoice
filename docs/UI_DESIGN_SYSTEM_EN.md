# V2 UI design system — English

This document defines the visual language and interaction rules for D&D
Proximity Voice V2. The current implementation uses **Unity IMGUI** because the
project's interface is already generated at runtime from C#. This avoids a
risky migration and preserves scenes, networking, and voice logic. Components
are centralized in `AppUiTheme` and can later move to UI Toolkit without
changing the acoustic model.

## Principles

1. **The map is the primary content.** Advanced controls stay in collapsible
   drawers and block input from reaching the map underneath.
2. **Restrained fantasy.** Parchment, brass, and dark surfaces evoke a tabletop
   without covering every control in stone, wood, or manuscript decoration.
3. **State never relies on color alone.** Symbols, labels, and segmented bars
   distinguish speaking, mute, connection, and audibility.
4. **One primary action per region.** Bright gold is reserved for important
   decisions and confirmations; secondary actions stay neutral.
5. **Immediate feedback.** Selection, hover, focus, errors, and audio operations
   expose a recognizable state without invasive modal dialogs.

## Color tokens

| Token | Value | Use |
| --- | --- | --- |
| `Background` | `#07090A` | backdrop and vignette |
| `Surface` | `#121312` | primary panels |
| `SurfaceRaised` | `#1D1C18` | drawers and foreground panels |
| `SurfaceSoft` | `#26231D` | rows, control groups, fields |
| `Stroke` | `#8B6A39` | borders and dividers |
| `Text` | `#F7EFDA` | primary text |
| `Muted` | `#C1B294` | descriptions and metadata |
| `Accent` | `#AA742B` | primary actions |
| `AccentBright` | `#E8BE63` | focus, detail, selection |
| `Success` | `#53BE89` | connected, speaking, clear voice |
| `Warning` | `#E6A443` | reconnecting and attenuation |
| `Danger` | `#D25248` | errors, mute/deafen, out of range |
| `Info` | `#67A4CD` | neutral information |

Contrast primarily comes from luminance, text, and shape. Green, amber, and
red must never be the only way to understand a state.

## Typography and hierarchy

The application uses Unity's available dynamic system font, keeping builds free
of external font dependencies. The scale is:

- display: 30 px, screen titles;
- title: 22 px, panel titles;
- compact title: 18 px, HUD heading;
- heading: 15 px, states and sections;
- body: 14 px, operational copy;
- caption: 12 px, secondary detail;
- eyebrow: 11 px uppercase, section labels.

Long copy wraps. Player and device names use single-line clipping so they never
distort panel geometry.

## Spacing and size

The base spacing grid is `4 / 8 / 12 / 20 / 32`. Primary controls are 44 px
high and the minimum interaction target is 40 px. Cards use an approximately
8–9 px visual radius, a thin border, a top highlight, and restrained corner
ornaments. Shadows are reserved for panels that overlap the map.

## Shared components

- `DrawCard`: primary or raised surface with a restrained frame;
- `DrawStatusBadge`: symbol, heading, and detail for connection/audio;
- `DrawPill`: role, short state, or shortcut;
- `DrawSegmentedMeter`: intensity that remains readable without color;
- `DrawKeyHint`: key plus associated action;
- `DrawTooltip`: contextual help near the pointer;
- `AppUiControls.IconButton`: consistent button using local Heroicons assets;
- `AppUiControls.BeginScrollView`: clipped scrolling area with correct hover and tooltip bounds;
- `AppUiPointer`: shared interactive cursor, restored when the application loses focus;
- `PrimaryButton`, `SecondaryButton`, `DangerButton`, `IconButton` styles;
- display, title, body, caption, code, and token text styles.

Hover, pressed, selected, focused, and disabled states use surface and text
differences. Focus receives the same high contrast as hover.

## Voice states

The side panel explicitly communicates:

- connected, starting, reconnecting, stopped, or failed voice;
- active/muted microphone;
- microphone disabled by the DM, distinct from user-requested mute;
- active/deafened output;
- inactive, waiting, or transmitting push-to-talk;
- Discord participant count;
- audio-device success and error messages.

The active speaker temporarily moves to the top of the player list and gets
animated bars, an **IN PAROLA** label, and a token halo. Selection remains a
separate gold-accent state.

### Audibility

The segmented meter summarizes the same gain used by voice: distance,
Whisper/Normal/Shout mode, wall occlusion, and private group. Labels are:

- `NITIDA`: strength at or above 56%;
- `ATTENUATA`: 18% to 56%;
- `DEBOLE`: under 18% but still audible;
- `FUORI PORTATA`: zero gain due to distance;
- `PRIVATA`: incompatible private voice group.

These labels present the existing calculation and do not alter mixing or
network behavior.

## Audio settings

The **Audio settings** drawer allows users to:

- cycle through microphone and output devices reported by Discord Social SDK;
- set microphone volume from `0–100%`;
- set listening volume from `0–200%`;
- use automatic voice detection or a manual `-100–0 dB` threshold;
- enable hold-to-talk with the `V` key;
- fully deafen incoming call audio.

The drawer is enabled only while the call is connected. Settings are applied by
the Discord manager; UI code never manipulates PCM packets, audio queues,
attenuation, or networking.

## DM toolbar and contextual menus

Frequent map tools use monochrome icons with tooltips, a selected state, and
supporting text. Actions for the selected element appear next to the token or
wall instead of occupying a permanent panel. Delete, kick, and leave
confirmations block the underlying interface. Popups consume pointer and wheel
events, so no action passes visually through the menu.

## Input and accessibility

- mouse: all controls and tokens;
- keyboard: `1/2/3` for voice mode, `V` for push-to-talk, `Enter` in the
  session-code field, `Escape` to close settings or cancel construction, and
  `Delete/Backspace` for the selected construction item;
- zoom: `Ctrl + mouse wheel`, centered on the pointer;
- controller: current IMGUI controls expose focus, but a complete directional
  navigation and remapping layer is not implemented yet.

Voice shortcuts are ignored while editing a saved-map name. While a drawer is
open, map dragging, construction, zoom, and scrollbars are suspended.

## Resolution, safe area, and performance

`BeginResponsive` derives scale from the actual safe area. Maximum scale is
`2×` and minimum scale is `0.72×`. Tactical layouts have automated checks for
`1366×768`, `1920×1080`, and `2560×1440`; all three retain approximately
`1280×720` or more logical workspace.

Textures, gradients, frames, and styles are generated once and reused. The
player panel reuses one list and insertion sorting, avoiding LINQ queries and
new collections inside `OnGUI`. The audibility presentation reuses one static
attenuation curve.

## Verification and future replacements

EditMode tests cover audibility, ordering, scale, and missing scripts in project
scenes. Real connected screens require a Discord session, so release QA must
manually check:

1. login and mode selection;
2. creating and joining a session;
3. player drawer with at least two accounts;
4. mute, deafen, push-to-talk, and device switching;
5. readability at all three supported resolutions;
6. no click-through to the map.

No external fonts, icons, or images were added. Text symbols and procedural
shapes are production-safe placeholders; a future commercial release should
bundle an open-source font and a documented-license vector icon set.

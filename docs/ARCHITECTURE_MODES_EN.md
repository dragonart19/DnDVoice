# Mode architecture — issue #3

[← README](../README.md) · [Italiano](ARCHITECTURE_MODES_IT.md) ·
[V2 roadmap](ROADMAP_2_0_EN.md) ·
[Issue #3](https://github.com/dragonart19/DnDVoice/issues/3)

## Goal

This first foundation explicitly separates three concepts:

- **Stable Build 1.0:** remains on `main` and does not receive unvalidated V2 work;
- **2D Tabletop:** uses the existing map, session, Relay, and voice systems;
- **3D World Builder:** has a separate identity but remains disabled until its
  camera, data, and construction tools are implemented in dedicated issues.

No 3D scene is created yet, and the 2D systems are not moved. The purpose is to
prevent future work from accidentally activating the wrong flow or appearing
ready before it is usable.

## Application flow

```text
Startup
  ↓
Discord login
  ↓
Mode selection
  ├── 2D Tabletop ──→ Create/join ──→ Build 1.0 map and voice
  └── 3D World Builder ──→ Disabled: V2 roadmap
```

`ProductModeManager` is the single source of mode state. It starts at `None`,
accepts only modes declared available by `ProductModeCatalog`, and publishes
mode changes. Only `Tabletop2D` is currently available.

## Introduced boundaries

| Area | Responsibility |
| --- | --- |
| `ProductMode` | Stable `None`, `Tabletop2D`, and `WorldBuilder3D` identifiers |
| `ProductModeCatalog` | Mode availability and human-readable names |
| `ProductModeManager` | Central selection and return to the mode chooser |
| `ProductModeOverlay` | Visible choice after Discord login |
| `BuildInfo` | Identifies V2 branches as `2.0-dev` / `V2 Preview` |
| Windows build | Uses a V2 Preview name and ZIP, separate from `main`'s Build 1.0 command |
| `DiscordSessionManager` | Rejects create/join unless 2D Tabletop is selected |
| `DiscordSessionOverlay` | Appears only in 2D and can return to mode selection |
| `ProximityMapOverlay` | Appears only after joining a 2D session |

The future 3D module must not depend on `ProximityMapOverlay` or `RoomManager`.
It may share Discord authentication, the UI theme, and general services, while
keeping its own data, view, and tools.

## Issue #3 acceptance criteria

- `main` continues to identify the stable Build 1.0;
- a package produced from the V2 branch is labelled V2 Preview and cannot be
  mistaken for the Build 1.0 package;
- exactly one mode-selection screen appears after login;
- 2D Tabletop reaches the previous create/join flow;
- 3D World Builder is visible but cannot be selected;
- “Change mode” returns to the chooser before joining a session;
- create/join cannot start without 2D mode;
- the Build 1.0 map, Relay, and voice behavior remain unchanged in 2D;
- Italian and English documentation remain aligned.

## Tests to be run by the user

1. Open `feature/3-mode-boundaries` with Unity `6000.3.8f1`.
2. Wait for compilation and confirm that the Console has no red errors.
3. Enter Play Mode and complete Discord login.
4. Confirm that `2D Tabletop` and `3D World Builder` are shown.
5. Confirm that the 3D `IN DEVELOPMENT` action is disabled.
6. Select `CONTINUE IN 2D`, then `CHANGE MODE`, and confirm the chooser returns.
7. Select 2D again, create a session, and check map, token, and voice behavior.
8. Join with a second account and confirm that Build 1.0 behavior is unchanged.
9. Run EditMode tests from the Test Runner. The four new `ProductModeTests` join
   the previous 47 tests; the expected result is **51 passing**.
10. Confirm that Unity exposes **Build Windows V2 Preview** and does not produce
    a package named Build 1.0 from this branch.

Codex did not run these tests, as agreed. The issue moves to review only after
the user reports the results.

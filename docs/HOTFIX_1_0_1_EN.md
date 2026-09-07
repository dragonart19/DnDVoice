# Build 1.0.1 Hotfix — occlusion and audio stability

[← README](../README.md) · [Italiano](HOTFIX_1_0_1_IT.md) ·
[Playtest outcome](PLAYTEST_2026_09_06_EN.md) ·
[Kanban](https://github.com/users/dragonart19/projects/1/views/1)

## Origin

On September 6, 2026, the Build 1.0 candidate was checked shortly before a
seven-participant session. Two problems prevented its use for the full game:

1. a maximum-thickness wall still left speech too audible;
2. voice and session disconnects occurred after an unrecorded interval,
   reportedly including the DM.

The group completed the game with an earlier build and overall product feedback
was very positive. The candidate's local log contains `JoinTimeout`, a Relay
disconnect, and an unavailable Discord lobby. It has no absolute timestamp on
each line and does not prove one shared cause for every event.

## Build 1.0.1 changes

- a `0.2 m` wall applies clearer minimum occlusion while leaving conversation
  distinguishable, with roughly `0.66` gain before distance multiplication;
- the maximum `2 m` thickness reduces wall gain to roughly `0.02`, making
  speech almost inaudible;
- multiple obstacles continue to accumulate occlusion;
- an already joined session preserves its lobby and Relay during temporary
  Discord client `Connecting`/`Reconnecting` states instead of clearing them;
- transient call failures, including `JoinTimeout`, schedule up to three
  automatic attempts after `2`, `4`, and `6` seconds;
- `Forbidden` remains terminal and requires user action;
- logs include Discord state and elapsed runtime for comparable future reports;
- the build, folder, and UI are identified as `1.0.1 Hotfix`.

## Not solved by this hotfix

- no automatic migration of the DM role to another participant;
- no recovery guarantee if Discord actually deletes the lobby;
- no automatic recreation of the DM's Relay allocation;
- no low-pass filter on the Discord Direct path;
- automated tests cannot certify Internet voice quality, latency, or continuity.

## User verification

Do not publish 1.0.1 before completing at least A–D.

### A. Compilation and local suite

1. Open the inner `DnDVoice` Unity project with Unity `6000.3.8f1`.
2. Wait until the Console has no red compilation errors.
3. Open **Window > General > Test Runner > EditMode**.
4. Run every test: the expected total is **53**, all green.

### B. Wall profile with two clients

Use headphones, two PCs/accounts, **Normal** mode, disabled private groups, and
the same token distance for each comparison.

1. No wall: voice is the full-volume reference.
2. `0.2 m` wall: reduced but still intelligible.
3. Roughly `1 m` wall: strongly attenuated.
4. `2 m` wall: almost inaudible without disappearing because of routing errors.
5. Open door: voice returns as though there were no obstacle.
6. Closed door: clear attenuation, less extreme than the thickest wall.
7. Move a token back and forth across the same wall and verify the correct
   volume change on both clients.

### C. Call recovery

1. Start voice on both clients.
2. Interrupt the guest network for 5–10 seconds, then restore it.
3. The UI may display reconnection; it must not immediately return to the
   create/join screen.
4. Verify voice returns. If all three attempts fail, the UI must show an error
   and keep **Retry voice** available.
5. Repeat once on the DM only after saving the map. Session preservation covers
   short transitions, not permanent host loss.

### D. Duration

Keep two clients connected for at least **30 minutes**, periodically speaking
and moving tokens. Record start time, failure time if any, machine role, and
the message visible in the UI.

### E. Package

Use this Unity menu command:

```text
D&D Proximity Voice > Build Windows 1.0.1 Hotfix
```

Distribute the complete ZIP generated at
`Builds/DnDProximityVoice-Windows-BUILD-1.0.1-HOTFIX.zip`. After testing,
record the commit, size, and SHA-256; do not overwrite the fallback package.

## Git

Work remains on `hotfix/1.0-playtest-audio-stability`, derived from `main`.
The user runs tests, commits, and pushes. After validation, the fix can enter
`main`; only then should compatible changes be integrated into `develop/v2`
before resuming the V2 roadmap.

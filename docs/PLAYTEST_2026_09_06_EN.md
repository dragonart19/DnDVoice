# Playtest — Sunday, September 6, 2026

[← README](../README.md) · [Italiano](PLAYTEST_2026_09_06_IT.md) ·
[V2 roadmap](ROADMAP_2_0_EN.md) ·
[Kanban](https://github.com/users/dragonart19/projects/1/views/1)

## Goal and deadline

**Start: Sunday, September 6, 10:30, Europe/Rome. Seven participants in total,
including the DM.** The priority is completing a session on the 2D Build 1.0
Discord Direct foundation, with uninterrupted voice and a synchronized map.

This plan was prepared on September 5. Automated checks do not certify call
quality: a test with all seven real participants is required. The session's
outcome has not yet been verified.

## Inspected baseline

- Repository: `dragonart19/DnDVoice`, branch `main`, commit `86e05a8`.
- Unity project: the repository's inner `DnDVoice/` directory.
- Editor: `6000.3.8f1`; network protocol: `6`.
- Relay: seven connections plus the host, eight total participants. The
  planned group uses six connections plus the host.
- `BuildInfo.SupportedPlayers` still says `20`; it does not increase the actual
  Relay and packet limit of eight. Do not treat it as a verified capacity claim.
- Voice: Discord Direct playback, with PCM callbacks for panning when the
  format supports it. Avoid buffer, latency, or filter changes for this test.
- Recovery: synchronization loss is reported, but full automatic recovery and
  host migration are not implemented.
- Saves contain dimensions, walls, and doors. They do not restore the session,
  connected accounts, or token positions.
- The local build folder contains older artifacts. A `Build 1.0` label alone
  does not prove that an executable matches the inspected sources.

## Feasible work before the playtest

Estimates are indicative, not guaranteed deadlines. Reproduced bugs take
priority over additions. The IDs below can become Kanban cards; this document
does not imply that online cards have already been created or moved.

| ID | Priority | Task | Approximate time | Completion criterion |
| --- | --- | --- | --- | --- |
| SUN-01 | P0 | Compile and run EditMode tests from the GitHub copy | 20–45 min, plus any fixes | Report lists discovered/executed tests, all passing; no compilation errors |
| SUN-02 | P0 | One identifiable Windows package | 20–45 min after SUN-01 | Complete ZIP, recorded SHA-256, launch from the extracted archive on another PC |
| SUN-03 | P0 | Prepared map and separate backup | 15–30 min | Save/load verified, separate JSON copy, correct walls and doors |
| SUN-04 | P0 | Extended test with seven accounts | 45–60 min with the group | Checklist passes without recurring voice cuts or map synchronization loss |
| SUN-05 | P0 | Leave/rejoin and recovery procedure | 10–15 min | One player rejoins; everyone knows what to do if the DM disconnects |
| SUN-06 | P0 | IT/EN documentation and results | Throughout each task | Actual status, checks, and limits recorded; user performs commits and pushes |
| SUN-07 | P1, optional | Copy session code button | 30–60 min including UI check | Copies the exact code; click does not interact with the map beneath the menu |
| SUN-08 | P1, optional | Quick access to logs and saved maps | 30–90 min including UI check | Opens only the intended local folders; handles folders that do not exist yet |

The 3D builder, map/asset import, NPC impersonation, community, visual overhaul,
new audio architecture, full automatic reconnection, and host migration remain
on the V2 roadmap for after this deadline. Even a small addition requires a new
build and checks of the behavior it can affect.

## Proposed schedule

1. **Saturday:** inspect sources and tests, fix only reproduced issues that
   block the playtest, and prepare the map and candidate package.
2. **Saturday evening, around 21:00:** freeze new features and distribute the
   same ZIP to all six players. Keep any previously verified working package
   and the map backup separately.
3. **Sunday 09:15–10:00, if the group is available:** run the seven-person
   checklist, including at least 30 minutes of continuous conversation and
   token movement.
4. **Sunday 10:00:** decide which build to use. If a critical issue remains,
   use a previously verified package or the fallback procedure. Avoid an
   unverified last-minute change.
5. **10:30:** start the playtest and note the exact time of any issue.

The group's availability before 10:30 has not been confirmed. If SUN-04 cannot
be completed earlier, the 10:30 session will be the first seven-person test,
not an already validated seven-user release.

## Candidate package checklist

Use seven distinct Discord accounts on the actual computers. The DM remains
host throughout the test; everyone wears headphones and leaves any separate
Discord call. The limit of eight includes extra instances: avoid unnecessary
second clients during the test.

| Test | Procedure | Required result | Status |
| --- | --- | --- | --- |
| T01 — Package | Everyone extracts the same ZIP and launches the EXE | No missing files; matching package identity | Pending |
| T02 — Join | DM creates; six players join | Seven participants, voice and synchronization ready on every client | Pending |
| T03 — Movement | DM moves every token; players attempt dragging | Smooth, consistent positions; only the DM can modify them | Pending |
| T04 — Map | Change walls, a door, and map size; let one player join late | Matching state everywhere, including the late arrival | Pending |
| T05 — Voice | Take turns and overlap speech while moving tokens for at least 30 min | No recurring cuts, permanent stalls, or duplicate-listening echo | Pending |
| T06 — Range | Use 1/2/3 and move tokens nearer/farther | Consistent 3/12/24 m ranges; voices return when moving closer | Pending |
| T07 — Obstacles | Compare thin/thick walls and open/closed doors | Consistent attenuation for each listener | Pending |
| T08 — Groups | Enable/disable A/B/C isolation | Correct group audibility; voices return after isolation is disabled | Pending |
| T09 — UI | Use menus, sliders, zoom, and scrollbars over tokens/walls | No clicks pass through to the map; no stalls during speech | Pending |
| T10 — Saves | Save a test map under a new name, reload, and restart the app | Dimensions, walls, and doors preserved; DM can reposition tokens | Pending |
| T11 — Rejoin | One player leaves, rejoins, and activates voice | Matching map, no duplicate token, working voice | Pending |
| T12 — Recovery | Before the session, briefly interrupt one guest's network and restore it | Clear failure state; manual rejoin verified or limitation recorded | Pending |

Record a problem if map state still differs after multiple reliable snapshots
(roughly five seconds in normal conditions). This is a proposed test threshold,
not a reconnection-time guarantee.

## Identify and preserve the build

- Generate the candidate from the repository's inner project using
  **D&D Proximity Voice > Build Windows 1.0**, after SUN-01.
- The build script reuses its output path. First copy any fallback ZIP to a
  separate folder so it is not overwritten.
- Distribute the complete ZIP, never only the EXE. Give the shared copy a
  distinctive name, such as `DnDVoice-1.0-playtest-20260906-rc1.zip`.
- Record the source commit, any included local modifications, build date, ZIP
  name, and SHA-256. The hash identifies the package; it is not an author
  signature. Do not reuse `rc1` for different contents.
- Copy the JSON files from `Application.persistentDataPath/SavedMaps` into the
  DM's backup folder. Do not automatically include personal maps in the ZIP.

Optional checksum command, from the directory containing the ZIP:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath '.\DnDVoice-1.0-playtest-20260906-rc1.zip'
```

## If something fails during the session

1. **Voice only:** check mute, distance, and group isolation. Stop and restart
   voice using the existing controls. If necessary, leave and rejoin the session.
2. **Map only:** check Relay status, then have the affected player leave and
   rejoin. Working voice does not prove that the map is synchronized.
3. **DM disconnects:** the DM restarts the app, creates a new session, loads the
   saved map, and shares the new code. Tokens must be repositioned because the
   current JSON format does not store their state.
4. **Persistent issue:** stop the app's voice on every computer before using
   a normal Discord call. This fallback gives up proximity voice; if needed,
   the DM shares the map screen.

## Record results and update GitHub

For each issue, record the test ID, timestamp and timezone, DM/guest role,
participant count, ZIP identity, steps, expected and actual results, and any
workaround. Preserve logs from affected computers before repeatedly restarting
the app. Review logs before publishing them: they may contain session codes,
identifiers, and personal paths.

The map code uses `Application.persistentDataPath`. In Windows builds the
standard log is `Player.log` in the product's persistent directory. In the
local run Unity resolved that directory to
`%USERPROFILE%/AppData/LocalLow/DnD Proximity Voice/D_D Proximity Voice`;
`SavedMaps` is inside it.

| Check | Result as of September 5 |
| --- | --- |
| Repository and baseline sources located | Complete, `86e05a8` |
| Actual capacity for the group of seven | Code inspected: maximum eight; real load test pending |
| Baseline EditMode tests | **42/42 passed**, zero failed or skipped; September 5, 16:49:59 Europe/Rome |
| New candidate Windows build | Must be generated and tested |
| Real seven-person test and voice continuity | Pending |
| Commit and push of this preparation | Performed by the user |

Verification ran in Unity `6000.3.8f1`, batch EditMode. Unity exited with code
`0`, with no compilation errors. The local report is
`DnDVoice/Logs/preflight-2026-09-05/editmode-results.xml`; `unity-tests.log` is
in the same directory. Both are excluded from Git.

| Suite | Passing tests |
| --- | ---: |
| SessionCodeTests | 3 |
| VoiceModeProfileTests | 7 |
| VoiceRangeCalculatorTests | 10 |
| WallAcousticsTests (including snapping, doors, rooms, and saves) | 13 |
| Pcm16ConverterTests | 2 |
| PcmRingBufferTests | 5 |
| RemotePcmDiagnosticTests | 1 |
| RemotePcmStreamTests | 1 |

The nine tests in the four PCM suites cover local components, including the
previous experimental playback path. They do not measure Discord Direct call
quality or latency. The log contains a non-blocking Unity licensing message
about refreshing its own access token; it is not a Discord voice error and did
not prevent the tests from completing.

Update both language versions whenever a result changes, and update the README
and functional documentation when behavior changes. Keep fixes small and
verify their effects. Local logic tests do not replace T02–T12 on real clients.
Logs, Unity caches, and ZIPs remain excluded from commits. Online content is
updated after the user's commit/push and any separate Kanban card updates.

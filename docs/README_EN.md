# D&D Proximity Voice — English documentation

[← Main README](../README.md) · [Documentazione italiana](README_IT.md) ·
[Product Roadmap 2.0](ROADMAP_2_0_EN.md) ·
[Mode architecture](ARCHITECTURE_MODES_EN.md) ·
[GitHub Kanban](https://github.com/users/dragonart19/projects/1/views/1)

> Scheduled playtest: **September 6, 2026, 10:30 Europe/Rome, seven participants
> including the DM**. The [playtest plan](PLAYTEST_2026_09_06_EN.md) records
> priorities, the checklist, fallback procedure, and verification results.
> The user performs commits and pushes.

## 1. Project vision

D&D Proximity Voice is a desktop companion for online Dungeons & Dragons
sessions. Its purpose is to make conversation feel as if the characters were
actually sharing a place: voices change with distance, position, and obstacles
drawn by the Dungeon Master.

Discord provides identity, lobby membership, and voice transport. Unity renders
the tactical table, synchronizes its state, and computes a different mix for
each listener. The application is not intended to replace a complete VTT. It
focuses on position and voice and can run alongside the group's existing tools.

Design principles:

- join a game in under one minute whenever possible;
- the DM controls the scene while players only need to join and speak;
- acoustic rules should be understandable and visible on the map;
- use a modern fantasy interface without developer panels during play;
- never store private credentials in the client or repository;
- prioritize uninterrupted speech over the lowest theoretical latency.

## 2. Build 1.0 status

Build 1.0 is a playable Windows prototype. The full primary flow exists:
Discord login, coded sessions, shared map, tokens, proximity voice, walls,
doors, rooms, groups, and local saves.

This is not a final production release. Every networking or audio-path change
must be validated with at least two PCs and two Discord accounts.

| Area | Status | Details |
| --- | :---: | --- |
| Discord OAuth | ✅ | PKCE, Public Client, and local redirect |
| Sessions | ✅ | Create/join with a six-character code |
| Discord Direct voice | ✅ | Native call playback without a Unity audio queue |
| Distance attenuation | ✅ | Computed locally for every participant |
| Stereo direction | 🟡 | Available only when the callback supplies at least two PCM channels |
| Map and tokens | ✅ | DM-authoritative state with client interpolation |
| Walls, doors, and rooms | ✅ | Drawing, snapping, thickness, door states, and room detection |
| Occlusion | 🟡 | Volume attenuation works; low-pass filtering is not active |
| Private groups | ✅ | Groups A/B/C applied as a local application mixing rule |
| Saved maps | ✅ | Local JSON persistence |
| Reconnection | 🟡 | Basic error handling exists; full recovery needs hardening |
| Advanced immersive features | ⬜ | Telepathy, ambient sound, reverb, and presets |

Legend: ✅ available · 🟡 partial or needs validation · ⬜ planned.

## 3. Requirements

### To run a build

- Windows 10 or 11 x64;
- Discord desktop installed, running, and signed in;
- headphones recommended to prevent speaker-to-microphone echo;
- Internet access for Discord and Unity Relay.

### To develop

- Unity `6000.3.8f1`;
- a **Windows Build Support** module compatible with the x64 configuration;
- Git if contributing through the repository;
- a Discord Developer application enabled for the Social SDK.

Main dependencies:

- Discord Social SDK `1.10.18687`, included as a local package;
- Netcode for GameObjects `2.7.0`;
- Unity Multiplayer Services `1.2.0`;
- Universal Render Pipeline `17.0.1`;
- Input System `1.18.0`;
- Test Framework `1.4.2`.

## 4. Discord configuration

The current project is configured for Discord Application ID
`1541099026571722772` and this OAuth redirect:

```text
http://127.0.0.1/callback
```

In the Discord Developer Portal:

1. enable the Social SDK for the application;
2. configure the exact redirect URI shown above;
3. enable **Public Client** for the OAuth2 PKCE flow;
4. enable the communication scopes required by the Social SDK;
5. never copy a client secret into the Unity project.

Login opens Discord for user confirmation. Access and refresh tokens are not
written to logs, save files, or project files. A new authorization may be
required after restarting the application. That is an intentional conservative
choice in the current version, not a failure.

To use a different Discord application, update
`Assets/_Project/Runtime/Discord/DiscordConfiguration.cs` and keep its redirect
URI synchronized with the Developer Portal.

## 5. Opening and running the project

1. Clone or download the repository.
2. Open Unity Hub and choose **Add project from disk**.
3. Select the inner `DnDVoice` folder containing `Assets`, `Packages`, and
   `ProjectSettings`.
4. Select Unity `6000.3.8f1`.
5. Wait for Unity to rebuild `Library` and restore packages.
6. Open the main scene and enter Play Mode.
7. Allow microphone access and complete Discord authorization.

Do not select the repository root in Unity Hub. The actual Unity project is the
inner `DnDVoice` directory.

## 6. DM and player flow

### Creating a session

1. The DM starts the application and selects **Continue with Discord**.
2. After login, the DM selects **2D Tabletop**.
3. The DM creates a session.
4. The application generates a six-character code without ambiguous symbols.
4. The DM shares only this code with the party.
5. As members join, their tokens appear on the shared map.

### Joining a session

1. A player starts the same build and authorizes Discord.
2. The player chooses to join a session.
3. The player enters the code received from the DM.
4. The player waits until both Discord and Relay report a ready state.

The code identifies the Discord lobby, and a deterministic lobby secret is
derived from it. Lobby metadata records the application, code, host, and
protocol version. The current protocol version is `6`.

If the DM leaves, map authority is lost. Automatic host migration has not been
implemented yet.

## 7. Interface controls

| Action | Control |
| --- | --- |
| Whisper | `1` |
| Normal voice | `2` |
| Shout | `3` |
| Map zoom | `Ctrl + mouse wheel` |
| Vertical scroll | Mouse wheel |
| Horizontal scroll | `Shift + mouse wheel` |
| Select a token | Click the token |
| Move a token | Drag it, with DM authority |
| Cancel current construction | `Esc` |
| Delete selected construction item | `Delete` or `Backspace` |

The top-left burger menu contains construction tools and a collapsible connected
players list, keeping the map clear. UI panels consume pointer events so a menu
click should not move a token or draw a wall underneath it.

### Copy the code and open local files

- In the burger menu, **COPIA** next to the session code copies it without
  spaces. **COPIATO** confirms success for three seconds. Both DM and players
  can use it after joining a session.
- **UTILITÀ** opens a side panel, replacing the Players or Maps drawer.
- **APRI CARTELLA LOG** opens the current instance's log directory. In a
  Windows build the log is normally `Player.log`; in the Editor it is
  `Editor.log`. Custom log locations are respected using
  [`Application.consoleLogPath`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-consoleLogPath.html).
- The DM also sees **APRI CARTELLA MAPPE**, which opens the same `SavedMaps`
  directory used by map storage, creating an empty folder if necessary.
  Saving the current map still requires **SALVA MAPPA** in the Maps drawer.
- Players see a reminder that session maps are saved on the DM's computer.

These commands open local folders and do not upload files. Access failures
display a message in the panel. While the menu is open, map dragging, zoom,
wheel scrolling, and map scrollbars are suspended. The wheel remains available
for side-panel lists. Closing the menu restores map navigation.

## 8. Tactical map

The map starts at `48 × 48 m`. Each grid square represents one meter. The DM can
change width and height in eight-meter increments, within the current bounds of
`32 × 32 m` and `96 × 96 m`.

The viewport includes vertical and horizontal scrollbars. Zoom is centered on
the cursor and ranges approximately from 43% to 300% of the base scale.
Selecting a token displays that character's voice radius.

The DM is authoritative for map state and token movement. Clients receive
snapshots through Relay and interpolate their visual position toward each
target, reducing visible stutter.

## 9. Walls, doors, and rooms

### Drawing walls

- select **Walls** from the menu;
- the first click sets the start and the second sets the end;
- endpoints snap to the one-meter grid;
- near existing construction, snapping also uses existing segments and endpoints;
- the minimum segment length is `0.5 m`;
- the thickness slider ranges from `0.2 m` to `2 m`;
- the current configuration supports up to 44 segments;
- `Esc` cancels only the construction currently in progress;
- select an older item and press `Delete` or `Backspace` to remove it.

Snapping keeps vertical and horizontal segments consistent and lets corners
close precisely, which is required for room detection.

### Rooms

A room is not a predefined rectangle. It is reconstructed as a polygon from a
closed graph of wall segments. The minimum recognized area is `1 m²`. The
**Close room** action helps connect the final segment back to the start of the
current chain.

### Doors

A door is inserted into an existing wall by splitting its segment. Its reference
length is `2 m`. Available states are:

- **Open**: no occlusion;
- **Closed**: voice attenuation;
- **Locked**: closed and marked as non-traversable for future gameplay logic.

The DM can click a door to cycle or change its state through the available
controls. A closed door currently uses an occlusion factor of approximately
`0.58`.

### Acoustic effect

Every segment crossing the line between speaker and listener reduces volume.
Thicker walls produce more attenuation. Open doors do not obstruct voice;
closed and locked doors do.

The original design also called for a low-pass filter to make obstructed voices
sound muffled. In Discord Direct mode, participant volume is applied directly
to the native call, but that filter is not inserted into the current audio path.
It is therefore planned rather than complete.

## 10. Voice model

Each client calculates the other participants' local volume using:

1. distance between listener and speaker tokens;
2. the attenuation curve;
3. Whisper/Normal/Shout mode;
4. intersected walls and doors;
5. active private group rules;
6. the final gain applied to the Discord participant.

| Mode | Minimum distance | Maximum range | Base gain |
| --- | ---: | ---: | ---: |
| Whisper | 0.75 m | 3 m | 0.72 |
| Normal | 2 m | 12 m | 1.00 |
| Shout | 3 m | 24 m | 1.00 |

The normalized distance curve uses these reference points: 100% at zero
distance, 80% at 20% of range, 55% at 40%, 30% at 60%, 10% at 80%, and 0% at
the maximum range.

### Why “Discord Direct”

An earlier version copied PCM into a Unity queue to gain full control over pan
and filters. When that queue was made too short, speech started cutting out due
to underflow. Build 1.0 delegates continuity and jitter buffering to Discord's
native path and applies per-participant volume.

This removes the fragile custom playback queue and prioritizes stability. An
end-to-end latency of `1 ms` is not realistic over the Internet: capture,
encoding, network transport, jitter buffering, and playback all take time. The
goal is stable conversation with the lowest practical delay, not an impossible
headline number.

Stereo pan can be computed when Discord supplies at least two PCM channels. A
mono callback remains centered. This behavior must be validated with two real
clients after SDK or audio changes.

### Echo

Echo can come from open speakers, a sensitive microphone, listening through two
calls, or device processing. For a reliable test:

- both users wear headphones;
- neither stays connected to a separate Discord voice channel;
- only one microphone is active at each station;
- compare behavior with Discord echo suppression enabled and disabled.

## 11. Private groups

Each player can belong to no group, group A, B, or C. When private isolation is
enabled, a member only hears other participants in the same non-empty group.
This supports split parties, narrative rooms, and DM-managed side conversations.

This is an important security boundary: everyone remains in the same call and
the client enforces the mix. It is not cryptographic isolation and should not be
presented as protection against a modified client.

## 12. Networking and synchronization

Discord handles authentication, lobbies, and voice. Unity Relay over DTLS
transports map state through Netcode for GameObjects.

```text
Discord OAuth ──> identity ──> lobby/call
                                   │
DM ──> authoritative state ──> Unity Relay ──> clients
       tokens, modes, map, walls, doors
                                   │
client ──> distance/occlusion ──> local Discord volume
```

Current behavior:

- position and map snapshots run at up to 15 Hz while state is dirty;
- a reliable snapshot is sent every two seconds and to newly joined clients;
- frequent packets are unreliable to reduce latency and traffic;
- periodic reliable state realigns clients;
- clients visually interpolate movement;
- the host is authoritative;
- Relay allows seven connections beyond the host: eight total participants.

Data models carry a design target of 20 players, but that does not override the
real Build 1.0 Relay limit. Declaring support for 20 requires a higher allocation,
bandwidth tests, voice tests, and UI validation.

## 13. Save data

The DM can save, list, load, and delete local maps. Files use JSON schema version
`1` and live under:

```text
Application.persistentDataPath/SavedMaps
```

A save includes:

- name, up to 32 characters;
- map width and height;
- wall identifiers, endpoints, and thickness;
- segment type;
- door state.

Rooms are recalculated from walls after loading. Files remain local to the DM's
computer. Online clients receive the loaded state through Relay but do not gain
a persistent copy. Backup, export, and cloud storage are not implemented yet.

## 14. Code architecture

```text
Assets/_Project/Runtime/
├── Bootstrap/   startup and service composition
├── Core/        application state and build information
├── Discord/     configuration, OAuth, and Discord user state
├── Session/     lobby, session codes, and membership
├── Realtime/    Unity Relay, snapshots, and synchronization
├── Players/     player model and token registry
├── Map/         UI rendering, input, walls, doors, rooms, and saves
├── Voice/       modes, distance, occlusion, and call integration
└── UI/          theme and shared visual components
```

Main responsibilities:

- `DiscordAuthManager`: SDK initialization and PKCE login;
- `ProductModeManager`: central selection between 2D Tabletop and the future 3D World Builder;
- `ProductModeOverlay`: mode selection, with 3D visible but disabled;
- `DiscordSessionManager`: lobbies, session code, and membership;
- `PositionSyncManager`: Relay and authoritative snapshots;
- `PlayerManager`: participant state and interpolated movement;
- `ProximityMapOverlay`: tactical view, menus, and construction tools;
- `RoomManager`: segments, doors, room detection, and persistence;
- `DiscordVoiceManager`: call state, per-user volume, and acoustic rules;
- `VoiceRangeCalculator`: distance curve and range calculation.

The boundaries and acceptance tests are documented in
[Mode architecture](ARCHITECTURE_MODES_EN.md).

`PcmRingBuffer`, `RemotePcmStream`, and `VoiceAudioSource` remain as experimental
infrastructure and tests for the previous PCM path. They are not the main
playback queue in Discord Direct mode.

## 15. Windows build

The command depends on the branch. On `main`, use:

```text
D&D Proximity Voice > Build Windows 1.0
```

which creates the stable build under:

```text
Builds/DnDProximityVoice-Windows-BUILD-1.0
```

On `develop/v2` and `feature/*` branches, use instead:

```text
D&D Proximity Voice > Build Windows V2 Preview
```

The resulting package is named `Builds/DnDProximityVoice-Windows-V2-PREVIEW`
and must not be distributed as Build 1.0.

The script also prepares a shareable ZIP archive. Distribute the full folder or ZIP,
not only the `.exe`, because Unity needs its `*_Data` folder and libraries.

The executable is unsigned, so Windows SmartScreen can display a warning. A
public distribution will require code signing, a release page, checksums, and a
repeatable publishing process.

## 16. Tests and manual verification

For September 6, use the [seven-person checklist](PLAYTEST_2026_09_06_EN.md).
The two-client checklist below is a baseline check, not certification of
stability with seven or eight users.

In Unity, open **Window > General > Test Runner**, select **EditMode**, and run
all tests. The suite covers core areas including:

- session-code generation and normalization;
- voice-mode ranges and attenuation curves;
- obstacle intersections and attenuation;
- PCM conversion and bounded behavior of the previous audio queue;
- map and room data logic where currently covered.
- menu regressions: plain, `Ctrl`, and `Shift` wheel input do not change the
  underlying map; closing the menu restores scrolling; a menu click cancels map
  dragging and remains available to the button.

Minimum checklist before sharing a build:

1. the project compiles without errors;
2. all EditMode tests pass;
3. two Discord accounts can log in;
4. the host creates and the guest joins with a code;
5. tokens move smoothly and synchronize in both instances;
6. modes `1/2/3` are visible and audible;
7. volume changes inside and outside the current range;
8. a thick wall attenuates more than a thin wall;
9. opening/closing a door changes sound and synchronizes;
10. save, load, and delete a map;
11. leave cleanly and join again without restarting Discord;
12. maintain a 10–15 minute conversation without recurring audio cuts.

## 17. Troubleshooting

### Unity Hub says “No projects found”

Select the inner `DnDVoice` folder containing `Assets`, `Packages`, and
`ProjectSettings`.

### Errors under `Library/PackageCache`

Close Unity. Delete only the generated `Library`, `Temp`, and `obj` folders from
the project, then reopen it with the correct Unity version. Never delete
`Assets`, `Packages`, or `ProjectSettings`.

### A player cannot find the session

- verify all six characters of the code;
- confirm that both users run the same build and protocol;
- keep Discord open and signed in;
- check firewall and Internet connectivity;
- have the DM recreate the session if the previous lobby became inconsistent.

### The map does not update

- check that Relay is ready on both instances;
- confirm that the DM remains connected;
- do not mix builds with different protocol versions;
- first try a small move and wait for the reliable snapshot.

### Voice cuts out

- use Build 1.0 Discord Direct, not an experimental Unity-queue build;
- check network stability and CPU load;
- use headphones and close duplicate voice channels;
- preserve complete logs from both computers with the interruption timestamp.

### Audio is not directional

Volume attenuation can work even when the SDK supplies mono data. Stereo pan
requires a compatible multichannel PCM path and remains an area for validation
and improvement.

## 18. Known limitations

- Windows is the only assumed target for the first release;
- practical current maximum: eight total participants;
- no automatic host migration;
- no full recovery after network or audio-device changes;
- stereo pan depends on the available PCM format;
- no low-pass filter or reverb in Discord Direct mode;
- no in-app input/output device picker;
- no master or per-user volume UI;
- save files are local only;
- no complete Discord avatars; tokens primarily use initials and colors;
- interface copy is primarily Italian;
- private groups are not cryptographic;
- the application is unsigned and has no installer;
- the Discord Application ID is currently embedded in client configuration.

## 19. Full roadmap

### Priority 1 — reliability

- repeatable automated and manual multi-client testing;
- lobby, Relay, and call recovery after a network interruption;
- DM disconnection handling and possible host migration;
- safe local diagnostics for dropouts, jitter, and SDK state without tokens;
- confirm player limits and load-test four to eight users;
- regression tests for vertical/horizontal walls, snapping, and deletion.

### Priority 2 — audio controls

- microphone and output-device selection;
- microphone test and level meter;
- master volume, per-player volume, and manual mute;
- configurable spatial-audio intensity;
- low-pass filtering through walls and doors;
- anti-echo profiles and duplicate-listening diagnostics;
- research stable stereo panning without restoring a fragile queue.

### Priority 3 — DM tools

- multi-selection and group movement;
- teleport and token locking;
- per-player mute/isolate controls;
- richer door, room-name, and acoustic-property editors;
- clear green/yellow/red “who hears whom” visualization;
- undo/redo and edit history;
- map import/export.

### Priority 4 — D&D features

- telepathy and distance-independent magical channels;
- private DM-to-player communication;
- positional ambient sound sources, music, and scene effects;
- reverb and acoustic profiles for tavern, dungeon, cave, outdoors, and temple;
- range and environment presets;
- campaigns with multiple maps and saved sessions.

### Priority 5 — experience and publishing

- Discord avatars, speaking animation, and UI transitions;
- tooltips, onboarding, and remappable shortcuts;
- UI scaling, contrast, color-blind modes, and keyboard navigation;
- complete Italian/English localization;
- macOS/Linux builds after SDK support validation;
- installer, code signing, updates, and automated GitHub releases;
- an appropriate backend/confidential OAuth design for public distribution;
- an explicit open-source or commercial license.

## 20. Ideas preserved beyond Build 1.0

The original concept also included animated lines and arcs between speakers and
listeners, pulsing tokens, a volume-colored halo, a developer audio visualizer,
dynamic ambient effects, environment profiles, diagnostic overlays, and campaign
management. These remain valid ideas but must not be confused with currently
available functionality.

Single-player testing controls were removed from the public UI to keep Build 1.0
clean. Development checks remain in the EditMode suite and can grow without
returning technical buttons to the normal player flow.

## 21. Contributing

Before making a change:

1. create a dedicated branch;
2. never add `Library`, `Temp`, `Logs`, builds, or credentials;
3. keep Discord, networking, map, and voice responsibilities separate;
4. update tests and documentation when behavior changes;
5. test with two clients when touching sessions, synchronization, or audio;
6. describe the player-visible outcome in the commit, not only changed files.

Before publishing the repository, add a `LICENSE` file appropriate to the
intended distribution model.

# D&D Proximity Voice

D&D Proximity Voice is a desktop companion for online tabletop sessions. Discord handles authentication, lobby membership and voice transport; Unity renders the map and produces a different spatial voice mix for every listener.

## Current status

Gate 0 is ready for its live Discord login test:

- Unity 6.3 LTS project created from the matching official Unity template;
- modular runtime and test assemblies created;
- application bootstrap created;
- bounded PCM ring buffer and PCM16-to-mono conversion implemented with Edit Mode tests;
- official Discord Social SDK Unity plugin `1.10.18687` installed as a local package;
- Discord application configured as a development Public Client with the local OAuth redirect;
- OAuth2 PKCE login implemented with Discord's default communication scopes;
- Discord connection/status monitoring and current-user retrieval implemented;
- lightweight login screen added for the Gate 0 verification.

No Discord API is simulated. The runtime compiles directly against the installed SDK. Access and refresh tokens are never written to logs or project files; the development build asks the user to authorize again after restarting.

## Requirements

- Unity `6000.3.8f1`;
- Windows 10/11 x64 for the first target;
- Discord desktop app installed and running;
- a Discord application with Social SDK and the required communication scopes enabled.

## Open the project

1. Open Unity Hub.
2. Select **Add project from disk**.
3. Select this directory.
4. Open it with Unity `6000.3.8f1`.
5. Allow Unity to restore the packages listed in `Packages/manifest.json`.

## Tests

Open **Window > General > Test Runner**, select **EditMode**, and run all tests.

The Gate 0 tests verify:

- FIFO PCM playback order;
- deterministic silence on buffer underflow;
- bounded latency through drop-oldest overflow behavior;
- PCM16 normalization and stereo-to-mono conversion.

## Discord configuration

The project is configured for Discord Application ID `1541099026571722772`. In the application's **OAuth2** page in the Discord Developer Portal:

1. keep the configured redirect URI `http://127.0.0.1/callback`;
2. keep **Public Client** enabled for this development-only PKCE flow;
3. never add the client secret, access tokens or refresh tokens to the project;
4. review a confidential backend flow before production.

The local package reference automatically enables `DND_DISCORD_SDK` for the runtime assembly through an assembly version define.

## Architecture

```text
Discord Social SDK                 Unity session transport
  auth / lobby / voice               positions / voice modes
           |                                  |
           v                                  v
     PCM per user  ---> participant registry / scene state
                              |
                              v
                       per-user ring buffer
                              |
                              v
                  AudioSource + acoustic rules
                              |
                              v
                     local AudioListener
```

Runtime code lives under `Assets/_Project/Runtime`; Edit Mode tests live under `Assets/_Project/Tests/EditMode`.

## Known limitations

- The live OAuth login still needs to be verified once from Unity Play Mode.
- The first development gate intentionally does not persist OAuth tokens between launches.
- Realtime coordinate networking and Relay are scheduled after the raw per-user PCM gate succeeds.
- Private-group muting inside one shared call is an application-level privacy boundary, not a cryptographic boundary.

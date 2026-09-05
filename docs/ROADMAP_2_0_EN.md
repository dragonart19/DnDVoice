# D&D Proximity Voice — Product Roadmap 2.0

[← Main README](../README.md) · [Versione italiana](ROADMAP_2_0_IT.md) ·
[GitHub Kanban](https://github.com/users/dragonart19/projects/1/views/1)

## Immediate priority — September 6 playtest

Before adding V2 features, validate the 2D foundation for **seven participants
on Sunday, September 6, 2026, at 10:30 Europe/Rome**. Tasks SUN-01–SUN-08,
acceptance criteria, and results are in the [playtest plan](PLAYTEST_2026_09_06_EN.md).
The V2 P0 priorities below belong to the following phase; they are not promises
to deliver before Sunday. The user performs commits and pushes.

## Goal

Version 2.0 evolves D&D Proximity Voice from a 2D voice companion into a hybrid
2D/3D virtual tabletop designed around the Dungeon Master's needs. Its defining
feature is not merely building a scene: the DM can **become every voice** in
that scene by choosing the character or location from which they speak.

> **Product promise:** Build the scene. Become every voice.

Build 1.0 remains the stable, playable foundation. New capabilities are built
as separate modules and integrated only after tests with two or more clients.

## Decisions already made

- The product has separate **2D** tabletop and **3D** world modes.
- Imported paper, image, and digital maps remain 2D; there is no automatic 3D
  conversion.
- A 2D builder and a separate 3D world builder live in the same product.
- Only the DM moves tokens, characters, and NPCs.
- NPCs are not AI agents. They are identities the DM can possess and use as a
  voice origin.
- The DM uses the full edition; players use a free client to join, view, and
  speak.
- Initial distribution stays outside Steam and requires no upfront spending.
- Community assets arrive in stages: official catalog, moderated submissions,
  and only later a possible public hub.

## Intended experience

### 2D tabletop

- import paper drawings, images, and digital maps;
- use an internal grid, wall, door, room, and token builder;
- apply proximity voice across distances, obstacles, and floors;
- save campaigns made of multiple scenes.

### 3D world builder

- load finite scenes rather than one infinite open world;
- build modular floors, walls, ceilings, doors, and lights;
- place objects from a controlled catalog;
- safely import local GLB/glTF assets;
- attach behavior to assets: doors, acoustic materials, lights, sounds, and
  floor links;
- let players navigate while keeping editing authority with the DM.

### DM voice direction

Where the DM speaks from and where the DM listens from are separate concepts.
Available modes should include:

- **Narrator:** global voice with no token origin;
- **Environmental DM:** voice from the DM token or location;
- **Possess NPC:** voice from the selected NPC;
- **Private whisper:** direct speech to selected players;
- **Magical or divine voice:** special range and effect;
- **Off-screen voice:** a narrative origin hidden from the map.

While possessing an NPC, the interface must display an unmistakable banner such
as **“Speaking as: Innkeeper”**. The DM can listen omnisciently or from the
possessed NPC's position.

### NPC data

Each NPC may include name, portrait or model, color, position, floor,
visibility, private DM notes, faction, voice range, acoustic profile, optional
voice effect, associated sounds, and hidden/visible state.

## Product architecture

```text
Campaign
├── 2D scenes
│   ├── image or drawing
│   ├── walls, doors, and zones
│   └── tokens and sound sources
└── 3D scenes
    ├── modular structure
    ├── assets and lights
    ├── NPCs and sound sources
    └── acoustic materials and floor links
```

Relay synchronizes only lightweight scene state: transforms, doors, tokens,
voice modes, and actions. It must not carry 3D model files. Every asset has an
ID, version, and checksum. Clients download or read it from cache before joining
and show a safe placeholder if it is unavailable.

## Assets and safety

The first phase uses a curated catalog of 20–30 CC0 assets and safe local
GLB/glTF imports. Assets cannot contain scripts, DLLs, custom shaders, or other
executable code. Validation enforces size, polygon, texture, and material limits.
A manifest records author, license, version, preview, collider, and acoustic
properties.

Community progression:

1. local packages and an official catalog;
2. manually reviewed submissions with verified licenses;
3. an online catalog with downloads, caching, and reports;
4. a public hub only after demand, moderation, and operating costs are proven.

A paid marketplace is outside the MVP because payments, taxes, refunds,
moderation, and creator rights make it a separate product problem.

## Zero-upfront-cost commercial model

- **Player Client:** free;
- **DM Edition:** one-time purchase;
- no subscription in the first commercial version;
- a free official starter asset pack;
- optional premium packs only after product validation;
- initial distribution through itch.io or GitHub downloads, not Steam;
- Unity Personal, Blender, GitHub, and CC0 assets to avoid upfront costs;
- no paid domain, signed installer, or paid infrastructure before users or
  revenue justify them.

“Zero cost” means no mandatory spending before validation. Code signing, larger
hosting, legal advice, and commercial services can become real costs only if
the project grows.

## Vertical slice

The first 3D demo must be small but complete:

- one modular 3D tavern;
- 20–30 verified free assets;
- a working door, lights, and a fireplace with positional sound;
- two connected clients;
- tokens moved only by the DM;
- a possessable innkeeper NPC;
- Narrator mode;
- 3D voice attenuated by distance, walls, doors, and floors;
- local scene save and load.

If this experience is not fun, stable, and understandable, the community
platform does not move forward.

## Phases and exit criteria

| Phase | Outcome | Exit criterion |
| --- | --- | --- |
| Foundations | V2 boundaries, data, and pipeline | Build 1.0 remains stable and formats are documented |
| 3D vertical slice | complete playable tavern | two-client session with no critical blocker |
| Closed alpha | campaigns, import, and recovery | external testers finish a session without assistance |
| Commercial MVP | DM Edition + Player Client | distributable build with verified licenses and privacy |
| Community | moderated catalog | sustainable submission, review, and takedown workflow |

## Ordered backlog

### P0 — essential

- define boundaries between Build 1.0, 2D mode, and 3D mode;
- design versioned campaign and 3D scene data;
- implement 3D camera, selection, and transform tools;
- build the modular 3D room editor;
- prepare the initial CC0 catalog;
- implement NPC data, editing, and placement;
- add NPC possession with an always-visible voice identity;
- extend spatial voice to X/Y/Z, walls, doors, and floors;
- synchronize scene, doors, tokens, and NPCs through Relay;
- separate DM Edition from the free Player Client;
- complete and test the tavern vertical slice.

### P1 — required for alpha

- Narrator, private, magical, and off-screen voice modes;
- positional ambient sound sources;
- multi-scene campaigns, autosave, and recovery;
- validated local GLB/glTF importer;
- asset manifests, versions, checksums, cache, and placeholders;
- asset dependency preflight before joining a session;
- UI rebuild, accessibility, and quality settings;
- performance budgets, LOD, culling, and load tests;
- reconnect and host/session recovery;
- automated and manual multi-client QA plan;
- zero-upfront-cost distribution pipeline;
- privacy, Discord, asset-license, and commercial-terms review.

### P2 — after validation

- code-signing strategy;
- public asset-package specification;
- manual community submission and review;
- online catalog with download and cache;
- reports, moderation, and takedown process.

### P3 — only with real traction

- public user-generated content hub;
- community package monetization;
- scalable backend and dedicated commercial services;
- marketplace payments and revenue sharing.

## Development rules

- Every task lives on GitHub with priority, area, milestone, and acceptance
  criteria.
- `main` remains the stable Build 1.x line. V2 development is integrated into
  `develop/v2`, created from `main` at commit `6d2304a`.
- Each selected Kanban card is first converted from a draft into a repository
  issue, then developed in a `feature/<number>-<short-name>` branch created
  from `develop/v2`. Use one issue per branch and keep changes small and
  verifiable.
- The user runs the tests and reports their outcome. Codex provides a checklist
  and the required commands with each delivery, but does not run suites or
  builds unless the user explicitly requests it.
- Close an issue only after its acceptance criteria are met, documentation is
  current, the user has confirmed the tests, and the change is integrated into
  `develop/v2`. Moving a card to `Done` does not replace closing its linked issue.
- Networking and voice changes always require at least two real clients.
- No asset enters the project without recorded origin and license.
- New work must not break Build 1.0.
- Validate the DM experience before expanding the platform.

## Intentionally outside the MVP

- automatic conversion of a 2D photo into a 3D world;
- AI-controlled NPCs;
- an infinite open world;
- a public paid marketplace;
- assets containing scripts or executable code;
- a mandatory subscription;
- initial Steam distribution.

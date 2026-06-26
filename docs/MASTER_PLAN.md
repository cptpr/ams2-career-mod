# AMS2 Career Mod Master Plan

## Product Goal

Build the best external career layer for `Automobilista 2`:

- fully focused on `official AMS2 base game + all DLC` first
- optimized for `automation`, `clarity`, `low friction`, and `long-term replayability`
- better than ApexRivals in:
  - event preparation flow
  - UX
  - result reliability
  - progression depth
  - save/profile handling
  - official-content coverage
  - long-term maintainability

This is not an in-game mod. It is a `Windows companion platform` that:

- prepares the next race as automatically as AMS2 realistically allows
- launches and monitors AMS2
- reconstructs and validates race results
- runs a persistent career simulation with leagues, unlocks, rivals, economy, prestige, and long-form progression

## Reality Check

The plan must respect what AMS2 and ApexRivals actually prove.

### What AMS2 Clearly Allows

- read-only live monitoring through `Project Cars 2` shared memory
- external app launch and restart orchestration
- external manipulation of some saved/editor state before launch
- external result reconstruction from telemetry and session-state transitions

### What ApexRivals Proves

From the reverse-engineering evidence:

- it reads AMS2 live telemetry and standings
- it has its own structured preset/content system under `LocalAppData`
- it appears to generate race/config data externally
- it requires AMS2 restart after creating certain custom AI/config data
- it still asks the user to set up or confirm the race manually
- it explicitly states there is currently no way to automatically configure a race fully

### Implication

We should not design around fake "one click straight to green flag" promises.

We should design around a `maximum-realism automation ladder`:

1. prepare as much as possible outside the game
2. reduce in-game setup to the smallest possible set of confirmations
3. make post-race capture fully automatic and reliable
4. make the overall race-to-race career loop feel seamless even if AMS2 still requires some manual confirmation

That is the path to beating ApexRivals honestly.

## Product Positioning

The correct vision is not "Forza Horizon in AMS2."

The correct vision is:

`A polished motorsport career platform for AMS2 with modern progression, persistent rivals, event preparation, and low-friction session automation.`

The feel should be:

- approachable
- rewarding
- aspirational
- systemic
- simulation-friendly

The core differentiator is not story first. It is `quality of loop`:

- choose or receive next event
- prepare event quickly
- launch AMS2
- race
- auto-capture outcome
- update progression, rivals, standings, economy, and recommendations
- surface the next meaningful choice immediately

## Design Principles

### 1. Official Content First

V1 should cover:

- base game cars
- all official DLC cars
- base game tracks
- all official DLC tracks/layouts

Do not start with arbitrary mods. Mod support comes later through a separate extension layer.

### 2. No Fake Automation

Do not use:

- UI macros
- brittle menu-driving hacks
- memory injection
- input playback

Prefer:

- file/state generation
- restart orchestration
- launch presets
- telemetry-driven automation
- strong user-facing checklists when full automation is impossible

### 3. Content as Data

Do not hardcode the career in C# classes.

Move to a content pipeline with versioned data files and generated indexes.

### 4. One Durable Career Platform

The app should support:

- multiple career profiles
- strong profile identity
- stable migrations
- ledger-style event history
- long-term balancing without schema resets

### 5. VR/Wheel Friendly

Assume the user often:

- launches from the rig
- uses an ultrawide or second screen
- minimizes alt-tab time
- wants large readable layouts

## ApexRivals-Informed Strategy

The most important ApexRivals lesson is not the UI. It is the `content and preset architecture`.

It appears to use:

- its own preset databases
- class/tier metadata
- track metadata
- settings metadata
- external export to AMS2-adjacent race state/config

We should adopt that architectural direction, but make it cleaner and more extensible.

### Our Replacement for the Current Static Content Catalog

Create a first-party official-content database with these domains:

- `cars`
- `car_classes`
- `manufacturers`
- `tracks`
- `track_layouts`
- `series`
- `league_tiers`
- `event_templates`
- `opponent_pools`
- `reward_curves`
- `challenge_templates`
- `rival_archetypes`
- `titles`
- `achievements`
- `unlock_graph`
- `career_presets`

This content must be:

- human-readable
- versioned
- schema-validated
- safe to patch without code changes

Recommended format:

- `JSON` for now
- optional generated indexes/caches later

## The Automation Ladder

This is the core product strategy.

### Tier A: Reliable Post-Race Automation

Already partially working. Must become rock-solid.

Responsibilities:

- detect AMS2 running
- detect session lifecycle
- reconstruct player result
- auto-commit high-confidence races
- queue manual review only when needed

Ship target:

- single-class and multiclass support
- DNF/DQ/quit/restart handling
- duplicate-proof persistence
- clear evidence logs

### Tier B: Prepared Event Launch

Current `.sav` preset swapping is only a stopgap.

Improve it into:

- official built-in presets
- league-linked presets
- event queue integration
- "apply next event and launch" flow
- restart-aware workflow

User experience:

- choose event in the app
- app applies event state
- app launches AMS2
- app tells the user exactly where to confirm/start the race

### Tier C: Generated Event Packages

This is the real medium-term win.

Instead of storing only captured editor saves, generate event packages from structured data:

- track/layout
- player car/class
- opponent pool
- AI levels/aggression
- rolling/grid settings
- session lengths
- weather/time
- assists/rules
- pit/race options

The app then exports this into the best known AMS2-compatible state format.

This should support:

- league-specific fields
- handcrafted marquee events
- dynamic difficulty scaling
- multiclass odds and grids

### Tier D: Restart-Orchestrated Race Loop

Because AMS2 may require restart for some externally written state:

- detect whether the selected next event requires restart
- offer one-click `Apply + Restart + Launch`
- warn only when truly necessary
- never make the user guess whether a restart is needed

This is where we can beat ApexRivals on UX even if the underlying game limitation remains.

### Tier E: Race Desk

This is the "revolutionary" user-facing layer.

The app should have a dedicated `Race Desk` screen that becomes the central workflow:

- current career context
- next event card
- launch/apply state
- required restart state
- event instructions
- rival spotlight
- active challenges
- last result summary
- one obvious primary action

The goal is that the user always knows:

- what the next race is
- what the app already prepared
- what still must be done in AMS2
- what they get for completing it

## Career Design

### Career Pillars

The core career should use four independent progression systems:

- `XP / Level`
- `Credits`
- `Driver Rating`
- `Reputation`

### XP / Level

Purpose:

- long-term profile progression
- regular small rewards
- unlock pacing for non-critical systems

Use for:

- titles
- profile cosmetics
- challenge slots
- certain access gates

### Credits

Purpose:

- resource pressure
- progression decisions

Use for:

- entry fees
- series buy-ins
- class access
- repairs/incidents abstraction
- special invitations

### Driver Rating

Purpose:

- performance skill measurement
- scaling AI and opportunity quality

Use for:

- recommended difficulty
- league eligibility
- rival seeding
- marquee event qualification

### Reputation

Purpose:

- prestige and career narrative status

Use for:

- title track
- elite invitations
- sponsor/contract systems later
- prestige branches

### League Structure

The league tree should be curated, not random.

Recommended initial structure:

- `Tier 0: Rookie`
- `Tier 1: Club`
- `Tier 2: Regional`
- `Tier 3: National`
- `Tier 4: International`
- `Tier 5: Prestige`

Each league node should define:

- official AMS2 class access
- track pool
- event count
- session format
- weather/time policy
- opponent model
- reward baseline
- prerequisite leagues
- level/rating/reputation requirements
- championship completion criteria

### Starter Experience

Start with `3 rookie starter cars`, but make them official-content-aware and intentionally different:

- one forgiving momentum choice
- one tin-top choice
- one formula or lightweight performance choice

Starter choice should affect:

- early event access
- first rival pool
- some challenge flavor

It should not permanently lock the player into one branch.

### Event Cadence

The career needs three layers of event rhythm:

- `main career events`
- `rotating challenges`
- `special invitations`

#### Main Career Events

- the backbone of progression
- handcrafted league structure
- deterministic unlock path

#### Rotating Challenges

- daily
- weekly
- monthly

These should be template-driven, not hand-authored one by one.

#### Special Invitations

- reputation-based
- event-style variety
- short, high-reward, high-identity moments

Examples:

- one-make challenge
- weather showcase
- multiclass endurance short
- manufacturer spotlight

### Rival System

Rivals should be persistent, not generic names on a board.

Each rival should have:

- identity
- archetype
- class affinity
- aggression
- consistency
- growth bias
- relationship state to player
- current league trajectory

Rival states:

- neutral
- emerging
- active rivalry
- title rival
- fallen rival

Triggers:

- repeated close finishes
- upset wins/losses
- class promotion overlap
- challenge interactions
- streak-breaking moments

Do not simulate the whole world every frame.

Simulate:

- only current-league and nearby future rivals
- only on event boundaries
- deterministic advancement with trait weighting

### Story Strategy

Do not start with branching story scenes.

V1 story should be `systemic narrative`:

- rivals
- titles
- progression headlines
- milestone recaps
- special invitations
- streaks and collapses

V2 can add authored narrative wrappers if the systems already feel strong.

## UX Plan

### Core Screens

#### 1. Home

- active career
- current rating, credits, reputation, level
- next recommended action
- recent unlocks

#### 2. Race Desk

- next event
- preset/export status
- restart requirement
- launch controls
- setup instructions
- challenge preview
- rival spotlight

#### 3. Career

- league tree
- progression lanes
- unlocked classes/series
- current season/championship state

#### 4. History

- race log
- season log
- best finishes
- win/podium/clean-race summaries

#### 5. Rivals

- major rivals
- recent head-to-heads
- rivalry intensity
- projected next overlap

#### 6. Settings

- AMS2 path
- shared memory health
- profile data paths
- automation settings
- display/VR modes

### UX Rules

- one primary action per state
- no developer tools in the normal path
- strong active-profile visibility
- readable large-format layout
- no default Windows-looking tabs in final UI
- explicit success/failure state after every automation step

## Technical Architecture

The current repo split is good:

- `App`
- `Core`
- `Infrastructure`

But the next evolution should add clearer subdomains.

### Proposed Internal Architecture

#### Core

- `CareerDomain`
- `ProgressionEngine`
- `LeagueEngine`
- `ChallengeEngine`
- `RivalEngine`
- `ResultEngine`
- `EconomyEngine`
- `UnlockEngine`

#### Infrastructure

- `Telemetry`
- `Persistence`
- `Launch`
- `PresetExport`
- `OfficialContent`
- `Diagnostics`

#### App

- `Shell`
- `RaceDesk`
- `Career`
- `History`
- `Rivals`
- `Settings`

### New Subsystems to Add

#### 1. Official Content Catalog

This is the foundation.

Create a real data package for official AMS2 content:

- cars and classes
- tracks and layouts
- DLC ownership flags
- grid limits
- event suitability
- branch mapping
- weather suitability
- multiclass compatibility

This should be curated manually first, then optionally augmented with scanners later.

#### 2. Event Generation Engine

Input:

- league definition
- active career state
- selected player car/class
- current difficulty/rating targets

Output:

- race setup package
- opponent pool
- AI settings
- event metadata
- export target

#### 3. Export Adapter Layer

Do not lock the system to one export mechanism.

Use adapters:

- `ChampionshipEditorSaveAdapter`
- future `CustomAiXmlAdapter`
- future `RaceConfigAdapter`
- future `UnknownFormatResearchAdapter`

Each adapter reports:

- what it can generate
- whether restart is required
- what user confirmation is still needed

#### 4. Career State Ledger

Persist all important transitions as append-only records:

- race result commits
- XP changes
- credit changes
- rating changes
- reputation changes
- unlock grants
- challenge completions

This enables:

- balancing
- debugging
- migration safety
- retrospective summaries

#### 5. Diagnostics Layer

We need a first-class diagnostics system, not scattered log files.

Required logs:

- telemetry health
- event export/apply
- launch/restart actions
- result reconstruction confidence
- persistence/migration

Expose user-readable status in the UI.

## Data Model Direction

Replace the tiny static `CareerContentCatalog` with schema-backed data.

Recommended directory structure:

```text
content/
  official/
    cars.json
    car-classes.json
    tracks.json
    track-layouts.json
    series.json
    leagues.json
    event-templates.json
    rival-archetypes.json
    challenges.json
    achievements.json
    titles.json
    unlock-graph.json
    reward-curves.json
    difficulty-curves.json
  generated/
  schemas/
```

Longer term:

- mod content packs go in a separate root
- official content remains the canonical first-party baseline

## Build Order

### Phase 1: Harden the Current Core

Goal:

- make current telemetry/result/profile/history flow production-stable

Tasks:

- finish settings persistence
- fix remaining UI state clarity gaps
- improve DNF/DQ/quit handling
- improve diagnostics surfacing
- finish race history and season summaries
- hide dev tools behind explicit dev mode

Exit criteria:

- reliable race capture for normal AMS2 sessions
- stable multi-profile handling
- no confusing active-profile state

### Phase 2: Official Content Database

Goal:

- create the curated canonical AMS2 content layer

Tasks:

- define schemas
- populate official cars/classes/tracks/layouts
- map DLC ownership
- define league branches and track suitability
- define curated starter paths

Exit criteria:

- no more hardcoded starter cars/leagues in C#
- the app can bootstrap from data files alone

### Phase 3: Event Generation Engine

Goal:

- generate race definitions from career data instead of static presets only

Tasks:

- create event-template model
- create opponent pool model
- create AI scaling model
- create weather/time presets
- create export-ready race package objects

Exit criteria:

- "next event" is generated from career state and content data

### Phase 4: Export Adapter System

Goal:

- move beyond raw captured `.sav` files

Tasks:

- formalize adapter interfaces
- keep current championship-editor adapter
- research and add better AMS2 export targets where proven
- add restart requirement reporting

Exit criteria:

- each event shows exactly how it will be applied
- the app can explain whether restart is needed

### Phase 5: Race Desk UX

Goal:

- turn the app into a proper race-control surface

Tasks:

- redesign navigation
- large-format race desk
- launch/apply/restart state cards
- pre-race checklist
- post-race reward summary polish
- second-screen friendliness

Exit criteria:

- a player can run the whole loop from the rig with minimal confusion

### Phase 6: Full Career Depth

Goal:

- make the career truly special

Tasks:

- bigger league tree
- richer rivals
- invitations
- monthly challenge layer
- title and prestige tracks
- championship recaps

Exit criteria:

- strong long-term replay loop

### Phase 7: Apex-Beating Automation

Goal:

- beat ApexRivals on practical usability

Tasks:

- tighter restart orchestration
- auto-selection of correct export adapter
- stronger pre-race guidance
- fewer manual steps per event
- richer official preset/event library

Exit criteria:

- the user spends dramatically less time managing event setup than in ApexRivals

## What Not to Do Early

- do not build mod-content support before official content is excellent
- do not build authored story scenes before the systemic loop works
- do not chase fragile "full auto-start race" hacks
- do not overcomplicate garage/car ownership fantasies early
- do not bury important state inside unstructured code constants

## Success Criteria

The mod is winning when a player can:

1. create multiple careers cleanly
2. choose a starter path quickly
3. receive a meaningful next event automatically
4. prepare and launch AMS2 with minimal setup pain
5. finish a race and have the result captured reliably
6. feel persistent progression across leagues, rivals, unlocks, and titles
7. keep playing because the next step always feels valuable

## Immediate Next Work

The next implementation sequence should be:

1. finish app-state polish and settings persistence
2. replace hardcoded content with schema-backed official content files
3. define league tree and event-template model
4. build the event generation engine
5. formalize export adapters around the current preset flow
6. redesign the `Race` workflow into a true `Race Desk`

## Final Strategic Call

To build the best AMS2 career mod, we should stop thinking in terms of:

- "How do we mimic ApexRivals?"

and think in terms of:

- `How do we build a proper AMS2 career operating system?`

That means:

- official-content-first
- data-driven content
- telemetry-solid
- restart-aware automation
- race-desk workflow
- persistent rivals and progression
- no fake promises about impossible automation

If we execute this well, the result will be better than ApexRivals not because it breaks AMS2 harder, but because it turns AMS2's real constraints into a dramatically better career loop.

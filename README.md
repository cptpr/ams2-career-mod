# AMS2 Career Mod

External career companion app for `Automobilista 2`.

## Projects

- `Ams2CareerCompanion.App`: WPF desktop app
- `Ams2CareerCompanion.Core`: career domain, progression, result reconstruction
- `Ams2CareerCompanion.Infrastructure`: SQLite persistence, AMS2 launch, shared-memory telemetry

## Current Scope

- Career profiles with starter car selection
- AMS2 launcher with normal and VR launch buttons
- Built-in and user-captured AMS2 event presets for non-macro event preparation
- Shared-memory telemetry ingestion using AMS2 `Project Cars 2` mode
- Race result capture, manual review fallback, and recent race history
- Progression, rivals, challenges, and unlock tracking

## Build

```powershell
dotnet build .\Ams2CareerCompanion.sln
```

## Runtime Note

In AMS2, enable shared memory with:

- `Options -> System -> Shared Memory -> Project Cars 2`

## Master Plan

- See [docs/MASTER_PLAN.md](docs/MASTER_PLAN.md) for the product, architecture, automation, and delivery plan.

## Event Presets

- The app ships with a built-in AMS2 event preset in `Ams2CareerCompanion.App/PresetLibrary`
- Built-in presets can be applied directly from the `Race` tab
- User presets can still be captured from the current AMS2 championship/custom-event editor state
- Built-in presets are read-only in the UI; user-captured presets can be deleted

# Changelog

## Animation Profile System (2026-10-22)

### Changed
- **Requires `AnimationProfileSystem`** (`HAS_ANIMATION_PROFILE_SYSTEM` gates every Occupation assembly; the dependency guard lists it). The default body `NavMeshOccupationBody` animates the NPC with the `ProfileAnimator` on it (added when missing): the idle, walking and running of the character's Animation Profile, the walk or run shown whole at the occupation's speeds, and the step animation on the profile's activity layer (`PlayActivity` / `StopActivity`).
- `OccupationData.Idle`, `Walk` and `Run` are removed (a saved value is ignored), and so is the private graph `OccupationAnimation`. The Movement tab keeps the walk and run speeds.

## [Unreleased] - Studio editor look

### Runtime
- `OccupationAgent.Hold()` / `ReleaseHold()` / `IsHeld`: another system (the Dialogue System while the player talks to the NPC) holds the plan
  still: a walk stops, a stay does not end, the step's animation keeps playing; holds count.

### Editor
- The **Occupation Agent** and **Occupation Clock Setup** Inspectors are now drawn from **default Veneer layouts** too (`OccupationAgentDefaultLayout`, `OccupationClockSetupDefaultLayout`; system items in `OccupationAgentItems`, `OccupationClockSetupItems`); the editors derive from `VeneerLayoutDrivenInspector<T>` and are customisable in the Inspector Designer. The pills, warnings, create button, place checks, hour widget and the Play Mode "Right now" controls are system items. Differences: the Play Mode card is hidden by its own item (a layout card cannot be hidden by Play Mode). Without Veneer nothing changes. Not run in Unity yet.
- The **Occupation** and **Occupation Place** Inspectors are now drawn from **default Veneer layouts** (`OccupationDataDefaultLayout`, `OccupationPlaceDefaultLayout`; system items and widgets in `OccupationDataItems`, `OccupationPlaceItems`). The editors derive from `VeneerLayoutDrivenInspector<T>`; customisable in the Inspector Designer (Customize / Reset to system default). Differences: the Occupation header title is the fixed text "Occupation Data" instead of the asset name (the asset name is still used when several are selected); the Plans tab content sits in an untitled card. Without Veneer nothing changes. Not run in Unity yet.
- All Inspectors moved onto the shared Veneer (UI Toolkit, gold accent `#FFB224`, logo and banner in `Editor/Brand`): **Occupation** (tabs Plans / Days / Movement; plan tabs with add and remove, a card per step with move and delete buttons, hours as clock-time sliders snapped to five minutes, place picker menu), **Occupation Agent** (occupation slot with a "Create an occupation" button, own places list, warnings, Play Mode "Right now" card), **Occupation Clock Setup** (settings plus a Play Mode card to set the hour and pause) and **Occupation Place** (new Inspector: name, warnings).
- New assembly `OccupationSystem.Editor.Studio` (folder `Editor/Studio`) holds the studio Inspectors, gated by `HAS_VENEER`. `OccupationSystem.Editor` is unchanged and keeps compiling without the kit: it holds the gizmos (now in `OccupationGizmos.cs`), `OccupationChecks` (now public) and, while the kit is missing, plain fallback Inspectors for the Agent and the Clock Setup.
- `DependencyGuard` lists the Veneer as an optional own-system dependency (`HAS_VENEER`).
- Removed the IMGUI `OccupationDataEditor` and `OccupationGUI`; without the kit the Occupation asset uses Unity's default Inspector. Saved data is unchanged.

## [1.0.0] - 2026-10-09

First release. It grew out of the Combat System's occupations (the `Life...` classes) and now stands on its own.

### Added
- `OccupationData` asset (`Assets > Create > Occupation System > Occupation`): day plans (`OccupationPlan`) of steps (`OccupationStep`: place, walk or run, set-off hour, stay for nothing / minutes / until an hour, animation looped or held), a random or sequential order, `NoRepeat`, a day-start hour.
- `OccupationAgent` component: gives an NPC an occupation, its own place bindings (`OccupationBinding`), and runs the day plan as a state machine (wait for the start hour, walk, stay). An NPC that joins a day late starts with the first step that is not over; a plan put on hold carries on with the same step.
- `OccupationPlace` component: a named spot in the scene; an NPC goes to the nearest one with the name its step says unless it has its own binding. `Home` falls back to where the NPC started.
- `OccupationClock` (a built-in game clock worked out from Unity's time, or your own through `Provider`) and `OccupationClockSetup`; pure `OccupationRules` for the arithmetic of days, plans and steps.
- `IOccupationBody` and `OccupationBodies` (register a factory): another system can provide the walking and the animation, without a component being added to the character.
- A default body, `NavMeshOccupationBody`: used when nothing else provides one. The character's NavMeshAgent walks it, and its Animator shows an idle / walk / run base plus the step's animation in a playable graph of its own (`OccupationAnimation`). `OccupationAgent` requires a NavMeshAgent and an Animator. The occupation (`OccupationData`) holds the Idle, Walk and Run clips and the walk and run speeds in its **Movement** card; another system that moves the NPC ignores them (the Combat System's animation profile animates its NPCs). With no clips the NPC shows the Animator's own controller.
- The first step is the starting point: the NPC is put at its place (`IOccupationBody.Teleport`) when it is enabled, instead of walking there. The Occupation Inspector marks the first step "(start)".
- Exact places: an NPC walks right up to the place (within 10 cm) and is put exactly on it at the start, using the world position of the object. A binding that points at the parent of an Occupation Place uses the place under it. The Occupation Place no longer has an *Arrival Radius*.
- Gait **Idle** next to Walk and Run: the NPC does not travel and does the step where it stands.
- `OccupationStepEvent` through the Event System: an NPC began or finished a step.
- `OccupationManager`: one UniTask loop for every agent, each thinking every 0.25 s on scaled time, spread over time.
- Editor: Occupation Inspector (plan tabs, step cards, hours as clock times), Occupation Agent and Occupation Clock Setup Inspectors (live state and a way to set the hour while playing), gizmos for places and plans, a check that every place the steps name can be found.
- `Editor/Setup/DependencyGuard.cs`: sets `HAS_UNITASK`, `HAS_EVENT_SYSTEM` and `HAS_OCCUPATION_SYSTEM`, and offers to install UniTask and Event System; without them the system is left out of compilation.

### Changed (from the Combat System's occupations)
- Everything is in the namespace `OccupationSystem` and no longer needs the Combat System: `LifeData`, `LifeStep`, `LifePlan`, `LifePlace`, `LifeAgent`, `LifeClock`, `LifeClockSetup`, `LifeRules` became `OccupationData`, `OccupationStep`, `OccupationPlan`, `OccupationPlace`, `OccupationAgent`, `OccupationClock`, `OccupationClockSetup`, `OccupationRules`. Saved assets and components keep working: the scripts kept their GUIDs and the serialized fields kept their names.
- The plan is no longer run by the Combat System's Brain: the agent runs it, and asks its body to walk and animate. The agent no longer requires a `Brain`; it requires a NavMeshAgent and an Animator instead.
- `LifeStepEvent` is `OccupationStepEvent` (it names the agent instead of the fighter); `LifeActivityEvent` is the Combat System's own `FighterActivityEvent`.
- The clock and the walk timeout run on Unity's scaled time instead of the combat clock.

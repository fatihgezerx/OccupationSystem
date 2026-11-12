# OccupationSystem

Occupation System gives NPCs a daily life: they go to a place, stay there, play an animation, and move on to the next step of their day plan.

## Overview

You describe a job once, as an **Occupation** asset: a list of day plans, each a list of steps ("go to the forge, stay until 18:00, hammer; go home, sleep until the morning"). Add an **Occupation Agent** to an NPC, give it the occupation, and the NPC lives it: it walks from place to place by the game clock, stays as long as the step says, and plays the step's animation. A new day begins at the hour you choose and brings a new plan, picked at random or in order.

The agent plans; the walking and the animation are done by the NPC's **body**. Out of the box that is the character's own **NavMeshAgent** and **Animator**: the agent adds both, and the occupation holds the idle / walk / run clips. Another system can provide the body instead (the [Combat System](https://github.com/fatihgezerx/CombatSystem) does for its NPCs), so the same occupation works with any character system. When the body is busy (a fight, a search) the plan waits and carries on with the same step afterwards.

## Features

- **Occupation** asset: day plans of steps, a random or sequential order (never yesterday's plan, if wanted), a day-start hour. One asset serves every NPC with that job
- **Steps**: a place (by name), walk or run, an optional hour to set off at, how long to stay (not at all, game minutes, or until an hour), an optional animation (looped, or once and held)
- **Places by name**: an NPC uses its own binding of the name (his *Home*, his *Forge*) first, else the nearest **Occupation Place** in the scene with that name; *Home* with nothing called that is where the NPC started
- **The first step is the starting point**: when the game starts the NPC is put at the place of its first step (the step the time of day says, when it starts late) instead of walking there from wherever it stands in the scene
- **Joins the day late**: an NPC that appears at 20:00 starts with the first step that is not over yet, so it does not walk to work at night
- **Game clock**: a built-in clock (start hour, real minutes per day) or your own day and night system through `OccupationClock.Provider`
- **Events**: `OccupationStepEvent` when an NPC begins or finishes a step (open the shop when the baker arrives), sent through the Event System
- No `Update` anywhere: one UniTask loop runs every agent, each thinking four times a second, spread over time
- Inspectors with plan tabs and step cards, the hours drawn as clock times, a check that every place the steps name can be found, and Scene view gizmos (places, plans drawn as lines through their places)

## Setup

### Requirements

- Unity 2022.3 LTS or newer (made with Unity 6)
- [UniTask](https://github.com/Cysharp/UniTask) and [EventSystem](https://github.com/fatihgezerx/EventSystem): the setup guard offers to install them
- A character with a humanoid (or generic) rig and its Animator **on the same object as the agent**, with its Avatar assigned, and a baked NavMesh. Or a system that provides the body (see *Plugging in another system*): with the Combat System, an NPC needs its Brain

### Installation

Copy the repository into your project's `Assets/` (e.g. `Assets/Scripts/OccupationSystem`). `Editor/Setup/DependencyGuard.cs` runs once the scripts load: it sets the `HAS_UNITASK`, `HAS_EVENT_SYSTEM` and `HAS_OCCUPATION_SYSTEM` scripting symbols and offers to install what is missing. Until it is installed, Occupation System is left out of compilation, so the project keeps compiling.

## Quick Start

1. **Assets > Create > Occupation System > Occupation**. Open it: it has one plan. Add steps; give each a *Place* name (the ▾ button lists the names the scene knows), how long it stays, and an animation.
2. Put **Occupation Place** components on objects in the scene, one per spot, and name them like the steps do ("Forge", "Town Hall"). The NPC walks to the exact world position of that object (a child of another object is fine) and is put there when it starts. If you bind a place on the agent to a parent object, the Occupation Place under it is used.
3. Select the NPC and **Add Component > Occupation Agent**: it adds the NavMeshAgent and the Animator if they are missing (assign the Avatar yourself). Drag the occupation onto the agent and bind the NPC's own places (his "Home"); a name that is not bound goes to the nearest place of that name.
   Set the **Idle**, **Walk** and **Run** clips (and the speeds) in the occupation's **Movement** card. With another system moving the NPC (the Combat System's Brain) they are ignored: that system's own animation settings are used.
4. Optional: **Add Component > Occupation Clock Setup** on any object to set the start hour and the length of a day (20 real minutes by default; the day starts at 08:00).
5. Press Play. While playing, the agent's Inspector shows the plan, the step and the time; the Clock Setup's Inspector lets you set the hour to test a whole day.

From code:

```csharp
using OccupationSystem;

agent.SetData(otherOccupation);                 // another job, starting a new day with it
agent.Bind("Home", houseDoor);                  // this NPC's own "Home"
OccupationClock.SetTime(21.5f);                 // 21:30
OccupationClock.DayLengthMinutes = 10f;
OccupationClock.Provider = () => dayNight.TotalHours; // or drive it from your own clock

EventManager.Register<OccupationStepEvent>(evt =>
{
    if (evt.Place == "Bakery") shop.SetOpen(evt.Started);
});
```

## In the editor

With the [Veneer](https://github.com/fatihgezerx/Veneer) in the project (the setup guard offers to install it; it is optional) every Occupation System component and asset has the studio look, in the system's gold accent:

- **Occupation** asset: a header with a pill counting plans and steps, and three tabs. **Plans**: one tab per plan (a plus button adds a plan), the plan's name with a remove button, and a card per step with its name, place (type it or pick from the names the scene knows), gait (Idle / Walk / Run), an optional "wait for an hour" with a clock-time slider, how long it stays (nothing, game minutes, or until an hour) and the animation; the first step carries a *Start* pill, and every step has move-up, move-down and delete buttons. **Days**: how the day's plan is picked (random or in order, never the same plan twice) and the day-start hour. **Movement**: the idle, walk and run clips and the walk and run speeds.
- **Occupation Agent**: the occupation (with a button that creates and assigns a new one), the NPC's own place bindings, warnings for a missing occupation, an occupation without steps, and place names nothing provides, and, while playing, a "Right now" card with the plan, step and time.
- **Occupation Clock Setup**: start hour as a clock time, day length, start paused; while playing, a card with the time of day, a slider to set the hour and a Paused switch.
- **Occupation Place**: its name, with a warning when it is empty and a note when other places share the name.

- **Customise it.** All four Inspectors (Occupation, Occupation Agent, Occupation Clock Setup, Occupation Place) are drawn from default layouts that Occupation System ships. With Veneer installed, open `Tools > Veneer > Inspector Designer`, drop the `OccupationData`, `OccupationAgent`, `OccupationClockSetup` or `OccupationPlace` script into it and press **Customize** to make your own copy; **Reset to system default** returns to the shipped design. The pills, the checks, the create-occupation button, the Play Mode "Right now" controls and the plans list, plan order, no-repeat, day-start and start-hour widgets are *system items*: you can move or hide them, but what they do stays in the system's code. Keep a "Right now" item in a card of its own: that card is hidden outside Play Mode.

Without the kit the system keeps working with Unity's default Inspectors (the Occupation asset is shown as plain arrays; the Agent and Clock Setup Inspectors keep their checks and their Play Mode time controls). Scene view gizmos work either way.

## Settings

| Setting | What it does |
|---|---|
| **Order** | *Random*: a different plan every day. *Sequential*: the plans in order, one per day |
| **No Repeat** | Random order only: never the plan of the day before |
| **Day Start** | The hour a new day, and a new plan, begins (6:00). A plan's steps run from here |
| **Place** (step) | The name of the place to go to. Empty = stay where it stands |
| **Gait** (step) | Walk or Run to the place, or Idle: it does not go anywhere and does the step where it stands (the starting step still puts the NPC at its place first) |
| **Animations** (the character) | How the NPC looks while it stands, walks and runs is the character's Animation Profile (Animation Profile System): put a `Profile Animator` with a profile on the NPC (the default body adds one when missing). The occupation has no idle / walk / run clips of its own |
| **Walk Speed / Run Speed** (occupation, Movement card) | Meters per second given to the NavMeshAgent (1.4 and 4); the profile's walking and running clips show at them |
| **Set off at** (step) | The hour before which the NPC does not leave for the step |
| **Stay** (step) | *Nothing*: moves on at once. *Minutes*: game minutes. *Until Hour*: until 18:00 (an hour before the day start means the next morning, the day start itself means the end of the day). An *Until Hour* step whose hour has passed is skipped |
| **Animation** (step) | Played at the place at its own speed, looped or once and held. Empty = the NPC just stands there |

A blacksmith: plan *Work day*: (1) *Forge*, until 18:00, Hammering; (2) *Home*, until 06:00, Sleeping. Plan *Town hall day*: (1) *Town Hall*, until 14:00, Sitting; (2) *Forge*, 2 hours; (3) *Home*, until the morning. With *Random* he works at the forge one day and goes to the town hall another.

## Plugging in another system

The agent asks an `IOccupationBody` to do everything physical. By default that is the NavMeshAgent and Animator of its own object (`NavMeshOccupationBody`), used only when no other body is found. To move NPCs another way, implement it on a component of the same object, or register a factory so every agent that belongs to your system gets a body without a component being added:

```csharp
using OccupationSystem;
using UnityEngine;

sealed class CrowdBody : IOccupationBody
{
    public Vector3 Home => start;                         // what "Home" is when nothing is called that
    public bool IsFree => !panicking;                     // false: the plan waits, the animation stops
    public bool TryReach(Vector3 position, out Vector3 destination) { ... }
    public void Teleport(Vector3 position) { ... }        // where the day begins
    public void MoveTo(Vector3 destination, OccupationGait gait) { ... }
    public bool HasArrived(float radius) { ... }
    public void StopMoving() { ... }
    public void PlayActivity(AnimationClip clip, bool loop) { ... }
    public void StopActivity() { ... }
    public void Release() { ... }                          // the agent was disabled
}

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void Register() => OccupationBodies.Register(agent => agent.GetComponent<Crowd>() != null ? new CrowdBody() : null);
```

If your system needs to know what the agent is doing (to keep an NPC from walking back to its own routine, to return it to its place after a fight), the body can ask the agent: `IsActive`, `IsWalking`, `TryGetPoint(out point)`, `Describe()`, and tell it: `Interrupt()` when the character is taken over, `Restart()` when it starts over.

The [Combat System](https://github.com/fatihgezerx/CombatSystem) does exactly this for its NPCs (its `CombatSystem.Occupation` assemblies): the body is the Brain, which is free while it is Idle; the step's animation is played by the fighter animator.

## Notes

- The default body plays the clips in a **playable graph of its own** on the Animator: a base (idle / walk / run, else the Animator's controller) and one layer on top for the step's animation. It ticks only while something is fading or a clip has to be wrapped by hand, so a standing NPC costs nothing. Leave Loop Time on in the clips' import settings, or the agent starts them over by hand every frame.
- The agent thinks every 0.25 s on Unity's scaled time (`OccupationManager`, one UniTask loop that starts with the first agent and ends with the last); a paused game stops the day.
- A walk that has not arrived after 180 seconds counts as arrived, so a stuck NPC does not freeze its day. A place the body cannot reach (`TryReach` false) skips the step, with one warning in the Console.
- A place no binding and no Occupation Place provides is done where the NPC stands, with one warning; the agent's Inspector lists such names while editing.
- The built-in clock is worked out from Unity's time whenever it is asked for; nothing ticks it. Setting `Provider` makes it ignore its own speed and pause.

## License

[MIT License](LICENSE)

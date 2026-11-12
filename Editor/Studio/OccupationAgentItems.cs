using System;
using System.Collections.Generic;
using Veneer;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace OccupationSystem
{
    /// <summary>
    /// The pieces of the <see cref="OccupationAgent"/> Inspector that are computed or imperative, registered with Veneer so the default
    /// layout (and a user's copy of it) can place them: the state pill, the no-occupation warning, the create button, the no-steps
    /// warning, the place checks and the Play Mode "Right now" strip. They can be moved or hidden in the Inspector Designer, not changed.
    /// </summary>
    internal static class OccupationAgentItems
    {
        /// <summary>Id of the header pill (No occupation, No steps, Places missing, Ready; or the phase while playing).</summary>
        public const string StateId = "occupation.agent-state";

        /// <summary>Id of the warning shown while no occupation is assigned (card item).</summary>
        public const string NoDataId = "occupation.agent-no-data";

        /// <summary>Id of the "Create an occupation" button (card item).</summary>
        public const string CreateId = "occupation.agent-create";

        /// <summary>Id of the warning shown while the occupation has no step (card item).</summary>
        public const string NoStepsId = "occupation.agent-no-steps";

        /// <summary>Id of the two place checks: names nothing provides, and the all-found note (card item).</summary>
        public const string PlacesId = "occupation.agent-places";

        /// <summary>Id of the Play Mode strip with the plan, step and time (card item; hides its card outside Play Mode).</summary>
        public const string LiveId = "occupation.agent-live";

        private const string SetupMessage = "Create one with Assets > Create > Occupation System > Occupation, or with the button below.";

        /// <summary>Registers the custom items of <see cref="OccupationAgent"/>. Safe to call again (items are replaced).</summary>
        public static void Register()
        {
            Type type = typeof(OccupationAgent);

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = StateId,
                Label = "State pill",
                Icon = "check-circle",
                Description = "A pill in the header: the phase of the day while playing; No occupation, No steps, Places missing or Ready while editing.",
                Slot = VeneerCustomItemSlot.HeaderBadge,
                Build = BuildState,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = NoDataId,
                Label = "No occupation warning",
                Icon = "warning",
                Description = "A warning while no occupation is assigned (editing only).",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildNoData,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = CreateId,
                Label = "Create occupation button",
                Icon = "plus",
                Description = "A button, shown while no occupation is assigned, that asks where to save a new Occupation asset and assigns it.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildCreate,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = NoStepsId,
                Label = "No steps warning",
                Icon = "warning",
                Description = "A warning while the assigned occupation has no plan with a step (editing only).",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildNoSteps,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = PlacesId,
                Label = "Place checks",
                Icon = "map",
                Description = "A warning listing the place names nothing provides, or a note that every place is found (editing only).",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildPlaces,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = LiveId,
                Label = "Right now strip",
                Icon = "play",
                Description = "While playing, where the NPC is in its day. Keep it in a card of its own: the card is hidden outside Play Mode.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildLive,
            });
        }

        private static OccupationAgent AgentOf(VeneerCustomItemContext context)
        {
            return context.SerializedObject.targetObject as OccupationAgent;
        }

        private static VisualElement BuildState(VeneerCustomItemContext context)
        {
            var status = new OccupationAgentStatus(AgentOf(context), context.Targets.Length == 1);
            Label pill = VeneerStyle.Pill(string.Empty, "idle");
            Action refresh = () =>
            {
                (string text, string kind) state = status.PillState();
                VeneerStyle.SetPill(pill, state.kind, state.text);
            };

            // The first run of Poll is on the first tick: show the right state at once.
            refresh();
            context.Poll(refresh);
            return pill;
        }

        private static VisualElement BuildNoData(VeneerCustomItemContext context)
        {
            SerializedProperty data = context.SerializedObject.FindProperty("_data");
            context.Builder.ConditionalHint(
                () => !Application.isPlaying && data.objectReferenceValue == null ? "No occupation assigned: the NPC lives no daily life. " + SetupMessage : null,
                "warn",
                true);
            return null;
        }

        private static VisualElement BuildCreate(VeneerCustomItemContext context)
        {
            SerializedObject so = context.SerializedObject;
            SerializedProperty data = so.FindProperty("_data");
            Button create = context.Builder.Button(
                "Create an occupation",
                "plus",
                () => CreateOccupation(so),
                "Asks where to save a new Occupation asset and assigns it to this agent.");
            context.Builder.VisibleWhen(create, () => data.objectReferenceValue == null && !data.hasMultipleDifferentValues);
            return null;
        }

        private static VisualElement BuildNoSteps(VeneerCustomItemContext context)
        {
            var status = new OccupationAgentStatus(AgentOf(context), context.Targets.Length == 1);
            context.Builder.ConditionalHint(status.NoStepsText, "warn", true);
            return null;
        }

        private static VisualElement BuildPlaces(VeneerCustomItemContext context)
        {
            var status = new OccupationAgentStatus(AgentOf(context), context.Targets.Length == 1);
            context.Builder.ConditionalHint(status.MissingText, "warn", true);
            context.Builder.ConditionalHint(status.FoundAllText, "info", true);
            return null;
        }

        private static VisualElement BuildLive(VeneerCustomItemContext context)
        {
            var status = new OccupationAgentStatus(AgentOf(context), context.Targets.Length == 1);
            VisualElement strip = context.Builder.Live(status.LiveText);
            OccupationControls.HideCardOutsidePlay(context.Builder, strip);
            return null;
        }

        private static void CreateOccupation(SerializedObject so)
        {
            string path = EditorUtility.SaveFilePanelInProject("Create occupation", "New Occupation", "asset", "Where to save the new occupation.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var created = ScriptableObject.CreateInstance<OccupationData>();
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            so.Update();
            so.FindProperty("_data").objectReferenceValue = created;
            so.ApplyModifiedProperties();
            EditorGUIUtility.PingObject(created);
        }
    }

    /// <summary>
    /// The state of one <see cref="OccupationAgent"/> Inspector: the pill, the warnings and the live text. The scene scan for the place
    /// names is throttled and its text is rebuilt only when the list of names changed, so the 200 ms poll allocates nothing in between.
    /// </summary>
    internal sealed class OccupationAgentStatus
    {
        private const double CheckSeconds = 0.5;

        private readonly OccupationAgent agent;
        private readonly bool single;
        private readonly List<string> missing = new List<string>(4);
        private readonly List<string> previousMissing = new List<string>(4);
        private double nextCheck;
        private string missingText;

        /// <summary>Makes the status of <paramref name="agent"/>.</summary>
        /// <param name="agent">The agent to describe; may be null.</param>
        /// <param name="single">True when exactly one object is selected (the warnings are for a single agent only).</param>
        public OccupationAgentStatus(OccupationAgent agent, bool single)
        {
            this.agent = agent;
            this.single = single;
        }

        /// <summary>The text of the "no step" warning, or null while there is nothing to warn about.</summary>
        public string NoStepsText()
        {
            return !Application.isPlaying && single && agent != null && agent.Data != null && !agent.Data.HasSteps
                ? "The occupation has no plan with a step, so the NPC stands still."
                : null;
        }

        /// <summary>The text of the "places nothing provides" warning, or null.</summary>
        public string MissingText()
        {
            if (Application.isPlaying || !single || agent == null || agent.Data == null || !agent.Data.HasSteps)
            {
                return null;
            }

            RefreshMissing();
            return missing.Count > 0 ? missingText : null;
        }

        /// <summary>The note that every place is found, or null.</summary>
        public string FoundAllText()
        {
            if (Application.isPlaying || !single || agent == null || agent.Data == null || !agent.Data.HasSteps)
            {
                return null;
            }

            RefreshMissing();
            return missing.Count == 0
                ? "Every place of the occupation is found. Select the NPC to see its plans drawn in the Scene view."
                : null;
        }

        /// <summary>The Play Mode text: where the NPC is in its day.</summary>
        public string LiveText()
        {
            if (agent == null)
            {
                return string.Empty;
            }

            if (agent.isActiveAndEnabled && agent.Body == null)
            {
                return "The agent has no body to live with: nothing provides an IOccupationBody for this object.";
            }

            string text = agent.Describe();
            return text.Length > 0 ? text : "No plan is running (the occupation has no steps).";
        }

        /// <summary>The pill text and kind.</summary>
        public (string text, string kind) PillState()
        {
            if (agent == null)
            {
                return (string.Empty, "idle");
            }

            if (Application.isPlaying)
            {
                if (agent.isActiveAndEnabled && agent.Body == null)
                {
                    return ("No body", "error");
                }

                switch (agent.Phase)
                {
                    case OccupationPhase.WaitStart:
                        return ("Waiting", "info");
                    case OccupationPhase.Walk:
                        return ("Walking", "info");
                    case OccupationPhase.Stay:
                        return ("Staying", "ok");
                    case OccupationPhase.Done:
                        return ("Day done", "idle");
                    default:
                        return ("Idle", "idle");
                }
            }

            if (agent.Data == null)
            {
                return ("No occupation", "warn");
            }

            if (!agent.Data.HasSteps)
            {
                return ("No steps", "warn");
            }

            RefreshMissing();
            return missing.Count > 0 ? ("Places missing", "warn") : ("Ready", "ok");
        }

        // The scene scan is throttled; the text is rebuilt only when the list of names changed.
        private void RefreshMissing()
        {
            if (EditorApplication.timeSinceStartup < nextCheck)
            {
                return;
            }

            nextCheck = EditorApplication.timeSinceStartup + CheckSeconds;
            if (agent == null)
            {
                return;
            }

            OccupationChecks.Unresolved(agent, missing);
            bool same = missing.Count == previousMissing.Count;
            for (int i = 0; same && i < missing.Count; i++)
            {
                same = string.Equals(missing[i], previousMissing[i], StringComparison.Ordinal);
            }

            if (same)
            {
                return;
            }

            previousMissing.Clear();
            previousMissing.AddRange(missing);
            missingText = missing.Count == 0
                ? null
                : "Nothing provides the place(s): " + string.Join(", ", missing) +
                  ". Bind the name above, or put an Occupation Place with that name in the scene (add an Occupation Place component to an object). Until then those steps are done where the NPC stands.";
        }
    }
}

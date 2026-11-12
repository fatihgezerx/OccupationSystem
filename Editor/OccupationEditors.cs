#if !HAS_VENEER
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OccupationSystem
{
    // These are the plain fallbacks used while the Veneer is not in the project (the studio look lives in Editor/Studio and
    // replaces them as soon as the kit is installed, so exactly one Inspector is ever active per type). The Occupation asset has no
    // fallback: Unity's default Inspector already shows its plans and steps.

    /// <summary>
    /// Plain Occupation Agent Inspector: the default fields, and (while editing) a check that every place the steps name can be found; (while
    /// playing) where the NPC is in its day.
    /// </summary>
    [CustomEditor(typeof(OccupationAgent))]
    internal sealed class OccupationAgentEditor : UnityEditor.Editor
    {
        private static readonly List<string> Missing = new List<string>();

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            var agent = (OccupationAgent)target;

            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Right now", EditorStyles.boldLabel);
                var text = agent.Describe();
                if (agent.isActiveAndEnabled && agent.Body == null)
                {
                    text = "The agent has no body to live with: nothing provides an IOccupationBody for this object.";
                }

                EditorGUILayout.HelpBox(text.Length > 0 ? text : "No plan is running (the occupation has no steps).", MessageType.None);
                return;
            }

            if (agent.Data == null)
            {
                EditorGUILayout.HelpBox("No occupation assigned: the NPC lives no daily life. Create one with Assets > Create > Occupation System > Occupation.", MessageType.Warning);
                return;
            }

            if (!agent.Data.HasSteps)
            {
                EditorGUILayout.HelpBox("The occupation has no plan with a step, so the NPC stands still.", MessageType.Warning);
                return;
            }

            OccupationChecks.Unresolved(agent, Missing);
            if (Missing.Count > 0)
            {
                EditorGUILayout.HelpBox("Nothing provides the place(s): " + string.Join(", ", Missing) +
                                        ". Bind the name above, or put an Occupation Place with that name in the scene (add an Occupation Place component to an object). Until then those steps are done where the NPC stands.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Every place of the occupation is found. Select the NPC to see its plans drawn in the Scene view.", MessageType.None);
            }
        }
    }

    /// <summary>Plain Occupation Clock Setup Inspector: the settings, and while playing the time of day with a way to change it for testing.</summary>
    [CustomEditor(typeof(OccupationClockSetup))]
    internal sealed class OccupationClockSetupEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Optional. Without it the game starts at 08:00 and a day lasts 20 real minutes. Change the time while playing to test a day.", MessageType.None);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Right now", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Time", $"{OccupationClock.Format(OccupationClock.Hour)}   (day {OccupationClock.Day + 1})");

            EditorGUI.BeginChangeCheck();
            var hour = EditorGUILayout.Slider("Set the hour", OccupationClock.Hour, 0f, 24f);
            if (EditorGUI.EndChangeCheck())
            {
                OccupationClock.SetTime(hour);
            }

            OccupationClock.Paused = EditorGUILayout.Toggle("Paused", OccupationClock.Paused);
        }
    }
}
#endif

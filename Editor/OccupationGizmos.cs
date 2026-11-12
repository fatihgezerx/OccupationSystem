using UnityEditor;
using UnityEngine;

namespace OccupationSystem
{
    /// <summary>
    /// Scene view helpers: an Occupation Place is a disc with an arrow for the way it faces and its name; a selected NPC with an occupation
    /// shows each of its plans as a line through its places, numbered by step, one colour per plan. They work with or without the studio
    /// Inspectors.
    /// </summary>
    internal static class OccupationGizmos
    {
        private const float DiscRadius = 0.25f;
        private static readonly Color PlaceColor = new Color(0.62f, 0.78f, 0.30f, 0.9f);
        private static readonly Color[] PlanColors =
        {
            new Color(0.95f, 0.78f, 0.30f), new Color(0.35f, 0.72f, 0.95f), new Color(0.90f, 0.45f, 0.65f),
            new Color(0.55f, 0.85f, 0.55f), new Color(0.75f, 0.55f, 0.95f), new Color(0.95f, 0.60f, 0.35f)
        };

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawPlace(OccupationPlace place, GizmoType type)
        {
            var position = place.transform.position;
            Handles.color = PlaceColor;
            Handles.DrawWireDisc(position, Vector3.up, DiscRadius);
            var forward = place.transform.forward;
            Handles.color = Color.blue;
            Handles.DrawLine(position, position + forward * (DiscRadius + 0.5f));
            Handles.ConeHandleCap(0, position + forward * (DiscRadius + 0.5f), Quaternion.LookRotation(forward), 0.15f, EventType.Repaint);
            Handles.Label(position + Vector3.up * 0.3f, string.IsNullOrEmpty(place.PlaceName) ? "(no name)" : place.PlaceName);
        }

        [DrawGizmo(GizmoType.Selected)]
        private static void DrawAgent(OccupationAgent agent, GizmoType type)
        {
            var data = agent.Data;
            if (data == null || data.Plans == null)
            {
                return;
            }

            var scene = OccupationChecks.ScenePlaces();
            for (var p = 0; p < data.Plans.Length; p++)
            {
                var plan = data.Plans[p];
                if (plan == null || plan.Steps == null)
                {
                    continue;
                }

                Handles.color = PlanColors[p % PlanColors.Length];
                var previous = agent.transform.position;
                for (var s = 0; s < plan.Steps.Length; s++)
                {
                    var step = plan.Steps[s];
                    if (step == null || !OccupationChecks.TryFind(agent, step.Place, scene, out var position))
                    {
                        continue;
                    }

                    Handles.DrawDottedLine(previous + Vector3.up * 0.1f * (p + 1), position + Vector3.up * 0.1f * (p + 1), 4f);
                    Handles.Label(position + Vector3.up * (0.6f + 0.25f * p), $"{plan.Name} {s + 1}: {step.Name}");
                    previous = position;
                }
            }
        }
    }
}

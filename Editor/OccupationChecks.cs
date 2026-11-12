using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OccupationSystem
{
    /// <summary>
    /// What the editor can say about an occupation outside play mode (the runtime lists of places are only filled while playing): is a
    /// step's place a binding of the NPC, an Occupation Place in the open scenes, or "Home"? Shared by the Inspectors (the studio look and the
    /// plain fallback) and the gizmos; public so the studio Inspector assembly can use it.
    /// </summary>
    public static class OccupationChecks
    {
        /// <summary>Every Occupation Place in the open scenes, inactive ones included.</summary>
        public static OccupationPlace[] ScenePlaces() =>
            Object.FindObjectsByType<OccupationPlace>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        /// <summary>
        /// Finds a place by name the way the NPC will: its own binding, else the nearest Occupation Place, else (for "Home") where the NPC
        /// stands now. False when the name is empty or nothing has it.
        /// </summary>
        public static bool TryFind(OccupationAgent agent, string place, OccupationPlace[] scene, out Vector3 position)
        {
            position = default;
            if (string.IsNullOrEmpty(place))
            {
                return false;
            }

            foreach (var binding in agent.Bindings)
            {
                if (binding.Target != null && string.Equals(binding.Place, place, System.StringComparison.OrdinalIgnoreCase))
                {
                    position = OccupationAgent.PlaceOf(binding.Target).position;
                    return true;
                }
            }

            OccupationPlace best = null;
            var bestDistance = float.MaxValue;
            foreach (var candidate in scene)
            {
                if (candidate == null || !string.Equals(candidate.PlaceName, place, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var distance = OccupationRules.PlanarDistance(agent.transform.position, candidate.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            if (best != null)
            {
                position = best.transform.position;
                return true;
            }

            if (string.Equals(place, OccupationAgent.HomeName, System.StringComparison.OrdinalIgnoreCase))
            {
                position = agent.transform.position;
                return true;
            }

            return false;
        }

        /// <summary>The distinct place names of the occupation's steps that nothing provides for this agent, into <paramref name="names"/>.</summary>
        public static void Unresolved(OccupationAgent agent, List<string> names)
        {
            names.Clear();
            var data = agent.Data;
            if (data == null || data.Plans == null)
            {
                return;
            }

            var scene = ScenePlaces();
            foreach (var plan in data.Plans)
            {
                if (plan == null || plan.Steps == null)
                {
                    continue;
                }

                foreach (var step in plan.Steps)
                {
                    if (step != null && !string.IsNullOrEmpty(step.Place) && !TryFind(agent, step.Place, scene, out _) && !names.Contains(step.Place))
                    {
                        names.Add(step.Place);
                    }
                }
            }
        }
    }
}

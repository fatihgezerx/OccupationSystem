using System;
using System.Collections.Generic;
using UnityEngine;

namespace OccupationSystem
{
    /// <summary>
    /// A named place in the scene that the steps of an occupation send NPCs to: the forge, the town hall, the bakery. Put it where an
    /// NPC should stand (add an Occupation Place component to an object); the NPC walks to the exact spot of the object's position. An NPC uses the
    /// nearest one with the name its step says, unless its own Occupation Agent binds that name to something else.
    /// </summary>
    [AddComponentMenu("Occupation System/Occupation Place")]
    public sealed class OccupationPlace : MonoBehaviour
    {
        private static readonly List<OccupationPlace> Places = new List<OccupationPlace>();

        [SerializeField] [Tooltip("What the steps call this place (\"Forge\"). Several places can share a name: an NPC goes to the nearest.")]
        private string _placeName = "Place";

        /// <summary>The name the steps use.</summary>
        public string PlaceName => _placeName;

        /// <summary>The enabled places. Do not modify; index with <c>for</c>.</summary>
        public static IReadOnlyList<OccupationPlace> All => Places;

        private void OnEnable()
        {
            if (!Places.Contains(this))
            {
                Places.Add(this);
            }
        }

        private void OnDisable() => Places.Remove(this);

        /// <summary>The nearest enabled place called <paramref name="placeName"/> (any capitals) to <paramref name="from"/> on the ground, or null.</summary>
        public static OccupationPlace Nearest(string placeName, Vector3 from)
        {
            OccupationPlace best = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < Places.Count; i++)
            {
                var place = Places[i];
                if (place == null || !string.Equals(place._placeName, placeName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var distance = OccupationRules.PlanarDistance(from, place.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = place;
                }
            }

            return best;
        }

        // Keeps a stale list out when "Enter Play Mode Options" skips the domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Places.Clear();
    }
}

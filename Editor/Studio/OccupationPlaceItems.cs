using System;
using Veneer;
using UnityEngine.UIElements;

namespace OccupationSystem
{
    /// <summary>
    /// The pieces of the <see cref="OccupationPlace"/> Inspector that are computed, registered with Veneer so the default layout (and a
    /// user's copy of it) can place them: the Named / No name pill and the two name hints. They can be moved or hidden in the Inspector
    /// Designer, not changed.
    /// </summary>
    internal static class OccupationPlaceItems
    {
        /// <summary>Id of the header pill (Named or No name; header badge).</summary>
        public const string StateId = "occupation.place-state";

        /// <summary>Id of the two name hints under the name field: empty name, and other places sharing it (card item).</summary>
        public const string ChecksId = "occupation.place-checks";

        /// <summary>Registers the custom items of <see cref="OccupationPlace"/>. Safe to call again (items are replaced).</summary>
        public static void Register()
        {
            Type type = typeof(OccupationPlace);

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = StateId,
                Label = "Name pill",
                Icon = "check-circle",
                Description = "A pill in the header: Named, or No name in warning colour.",
                Slot = VeneerCustomItemSlot.HeaderBadge,
                Build = BuildState,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = ChecksId,
                Label = "Name checks",
                Icon = "warning",
                Description = "Hints under the name: a warning while it is empty, a note while other places share it.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildChecks,
            });
        }

        private static VisualElement BuildState(VeneerCustomItemContext context)
        {
            var place = context.SerializedObject.targetObject as OccupationPlace;
            Label pill = VeneerStyle.Pill(string.Empty, "idle");
            Action refresh = () =>
            {
                if (place == null)
                {
                    return;
                }

                bool named = !string.IsNullOrWhiteSpace(place.PlaceName);
                VeneerStyle.SetPill(pill, named ? "ok" : "warn", named ? "Named" : "No name");
            };

            // The first run of Poll is on the first tick: show the right state at once.
            refresh();
            context.Poll(refresh);
            return pill;
        }

        private static VisualElement BuildChecks(VeneerCustomItemContext context)
        {
            var place = context.SerializedObject.targetObject as OccupationPlace;
            context.Builder.ConditionalHint(
                () => place != null && string.IsNullOrWhiteSpace(place.PlaceName) ? "This place has no name, so no step can find it." : null,
                "warn");
            context.Builder.ConditionalHint(() => SharedNameText(place), "info");
            return null;
        }

        private static string SharedNameText(OccupationPlace place)
        {
            if (place == null || string.IsNullOrWhiteSpace(place.PlaceName))
            {
                return null;
            }

            int others = 0;
            foreach (OccupationPlace other in OccupationChecks.ScenePlaces())
            {
                if (other != null && other != place && string.Equals(other.PlaceName, place.PlaceName, StringComparison.OrdinalIgnoreCase))
                {
                    others++;
                }
            }

            return others == 0
                ? null
                : others + (others == 1 ? " other place shares" : " other places share") + " this name: an NPC goes to the nearest one (unless its own bindings say otherwise).";
        }
    }
}

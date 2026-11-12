using System;
using Veneer;
using UnityEditor;
using UnityEngine.UIElements;

namespace OccupationSystem
{
    /// <summary>
    /// The pieces of the <see cref="OccupationData"/> Inspector that are not plain fields, registered with Veneer so the default layout
    /// (and a user's copy of it) can place them: the plans / steps pill, the no-steps hint, and the widgets of the plans list, the plan
    /// order (with a tooltip per value), the no-repeat chip (shown only for several random plans) and the day start (a clock-time slider).
    /// The items can be moved or hidden in the Inspector Designer, not changed.
    /// </summary>
    internal static class OccupationDataItems
    {
        /// <summary>Id of the header pill that counts plans and steps (header badge).</summary>
        public const string StateId = "occupation.state";

        /// <summary>Id of the "no plan has a step yet" hint (card item).</summary>
        public const string NoStepsId = "occupation.no-steps";

        private static readonly string[] OrderTooltips = { "Random", "Sequential" };

        /// <summary>Registers the custom items and widgets of <see cref="OccupationData"/>. Safe to call again (items are replaced).</summary>
        public static void Register()
        {
            Type type = typeof(OccupationData);

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = StateId,
                Label = "Plans and steps pill",
                Icon = "list",
                Description = "A pill in the header: how many plans and steps there are, in warning colour while there is no step.",
                Slot = VeneerCustomItemSlot.HeaderBadge,
                Build = BuildState,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = NoStepsId,
                Label = "No steps check",
                Icon = "warning",
                Description = "A warning while no plan has a step (an NPC with this occupation would stand still).",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildNoSteps,
            });

            VeneerCustomItems.RegisterWidget(type, nameof(OccupationData.Plans), (context, property) => new OccupationPlansView(property, context.Brand));

            VeneerCustomItems.RegisterWidget(type, nameof(OccupationData.Order), (context, property) =>
            {
                VeneerLayoutEntry entry = OccupationControls.EntryOf(context, nameof(OccupationData.Order));
                string label = entry != null && !string.IsNullOrEmpty(entry.Label) ? entry.Label : "Plan order";
                return context.Builder.Enum(nameof(OccupationData.Order), null, OrderTooltips, label);
            });

            VeneerCustomItems.RegisterWidget(type, nameof(OccupationData.NoRepeat), BuildNoRepeat);

            VeneerCustomItems.RegisterWidget(type, nameof(OccupationData.DayStart), (context, property) =>
            {
                VeneerLayoutEntry entry = OccupationControls.EntryOf(context, nameof(OccupationData.DayStart));
                string label = entry != null && !string.IsNullOrEmpty(entry.Label) ? entry.Label : "Day start";
                return OccupationControls.HourRow(property, label);
            });
        }

        private static VisualElement BuildState(VeneerCustomItemContext context)
        {
            var status = new OccupationDataStatus(context.SerializedObject.targetObject as OccupationData);
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

        private static VisualElement BuildNoSteps(VeneerCustomItemContext context)
        {
            var data = context.SerializedObject.targetObject as OccupationData;
            context.Builder.ConditionalHint(
                () => data != null && !data.HasSteps ? "No plan has a step yet, so an NPC with this occupation stands still. Add a step to a plan." : null,
                "warn");
            return null;
        }

        // The chip only matters for a random order with several plans, so it is shown only then (or while the selection differs).
        private static VisualElement BuildNoRepeat(VeneerCustomItemContext context, SerializedProperty property)
        {
            SerializedObject so = context.SerializedObject;
            SerializedProperty order = so.FindProperty(nameof(OccupationData.Order));
            SerializedProperty plans = so.FindProperty(nameof(OccupationData.Plans));
            VeneerLayoutEntry entry = OccupationControls.EntryOf(context, nameof(OccupationData.NoRepeat));
            string label = entry != null && !string.IsNullOrEmpty(entry.Label) ? entry.Label : "Never the same plan twice in a row";
            string icon = entry != null && !string.IsNullOrEmpty(entry.Icon) ? entry.Icon : "refresh";
            VisualElement chip = context.Builder.Chip(nameof(OccupationData.NoRepeat), label, icon);
            context.Builder.VisibleWhen(chip, () => order.hasMultipleDifferentValues
                || (order.enumValueIndex == (int)OccupationPlanOrder.Random && plans.arraySize > 1));
            return chip;
        }
    }

    /// <summary>
    /// The state of the pill of an <see cref="OccupationData"/>: how many plans and steps it has. The text is rebuilt only when a count
    /// changes, so the 200 ms poll allocates nothing in between.
    /// </summary>
    internal sealed class OccupationDataStatus
    {
        private readonly OccupationData data;
        private int pillKey = -1;
        private string pillText = string.Empty;

        /// <summary>Makes the status of <paramref name="data"/>.</summary>
        /// <param name="data">The occupation to describe; may be null.</param>
        public OccupationDataStatus(OccupationData data)
        {
            this.data = data;
        }

        /// <summary>The pill text and kind (<c>ok</c> with a step, <c>warn</c> without; <c>idle</c> when there is nothing to read).</summary>
        public (string text, string kind) PillState()
        {
            if (data == null || data.Plans == null)
            {
                return (string.Empty, "idle");
            }

            int steps = 0;
            for (int i = 0; i < data.Plans.Length; i++)
            {
                OccupationPlan plan = data.Plans[i];
                steps += plan != null && plan.Steps != null ? plan.Steps.Length : 0;
            }

            int key = (data.Plans.Length * 4096) + steps;
            if (key != pillKey)
            {
                pillKey = key;
                int plans = data.Plans.Length;
                pillText = plans + (plans == 1 ? " plan, " : " plans, ") + steps + (steps == 1 ? " step" : " steps");
            }

            return (pillText, steps > 0 ? "ok" : "warn");
        }
    }
}

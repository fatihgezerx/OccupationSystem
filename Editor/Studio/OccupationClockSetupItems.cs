using System;
using Veneer;
using UnityEngine;
using UnityEngine.UIElements;

namespace OccupationSystem
{
    /// <summary>
    /// The pieces of the <see cref="OccupationClockSetup"/> Inspector that are computed or imperative, registered with Veneer so the
    /// default layout (and a user's copy of it) can place them: the state pill, the "optional" note, the clock-time widget of the start
    /// hour and the Play Mode strip that sets the hour and pauses the clock. They can be moved or hidden in the Inspector Designer, not
    /// changed.
    /// </summary>
    internal static class OccupationClockSetupItems
    {
        /// <summary>Id of the header pill (Optional while editing; the time of day while playing; header badge).</summary>
        public const string StateId = "occupation.clock-state";

        /// <summary>Id of the note shown while editing: the setup is optional and what the defaults are (card item).</summary>
        public const string OptionalId = "occupation.clock-optional";

        /// <summary>Id of the Play Mode controls: the time, an hour slider and a Paused chip (card item; hides its card outside Play Mode).</summary>
        public const string LiveId = "occupation.clock-live";

        private const string SetHourTooltip = "Sets the hour of the built-in clock (it keeps running from there).";

        /// <summary>Registers the custom items and widgets of <see cref="OccupationClockSetup"/>. Safe to call again (items are replaced).</summary>
        public static void Register()
        {
            Type type = typeof(OccupationClockSetup);

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = StateId,
                Label = "Clock pill",
                Icon = "clock",
                Description = "A pill in the header: Optional while editing; the time and day while playing, in warning colour while the clock is paused.",
                Slot = VeneerCustomItemSlot.HeaderBadge,
                Build = BuildState,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = OptionalId,
                Label = "Optional note",
                Icon = "info",
                Description = "A note shown while editing: the setup is optional and what the clock does without it.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildOptional,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = LiveId,
                Label = "Right now controls",
                Icon = "play",
                Description = "While playing: the time of day, a slider to set the hour and a Paused switch. Keep it in a card of its own: the card is hidden outside Play Mode.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildLive,
            });

            VeneerCustomItems.RegisterWidget(type, "_startHour", (context, property) =>
            {
                VeneerLayoutEntry entry = context.Entry != null ? context.Entry : OccupationControls.EntryOf(context, "_startHour");
                string label = entry != null && !string.IsNullOrEmpty(entry.Label) ? entry.Label : "Start hour";
                return OccupationControls.HourRow(property, label);
            });
        }

        private static VisualElement BuildState(VeneerCustomItemContext context)
        {
            var status = new OccupationClockStatus();
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

        private static VisualElement BuildOptional(VeneerCustomItemContext context)
        {
            context.Builder.ConditionalHint(
                () => Application.isPlaying
                    ? null
                    : "Optional. Without it the game starts at 08:00 and a day lasts 20 real minutes. Change the time while playing to test a day.",
                "info",
                true);
            return null;
        }

        // The Play Mode controls: not bound to any serialized data, they read and set the static clock and refresh on the builder's poll.
        private static VisualElement BuildLive(VeneerCustomItemContext context)
        {
            VeneerInspectorBuilder b = context.Builder;
            var status = new OccupationClockStatus();
            bool dragging = false;

            var panel = new VisualElement();
            Label time = VeneerStyle.MutedLabel(string.Empty);
            panel.Add(time);

            var slider = new Slider(0f, 24f);
            slider.style.flexGrow = 1f;
            slider.style.flexShrink = 1f;
            slider.style.marginLeft = 0f;
            slider.style.marginRight = 0f;
            slider.tooltip = SetHourTooltip;
            slider.RegisterCallback<PointerDownEvent>(evt => dragging = true, TrickleDown.TrickleDown);
            slider.RegisterCallback<PointerUpEvent>(evt => dragging = false, TrickleDown.TrickleDown);
            slider.RegisterCallback<PointerCaptureOutEvent>(evt => dragging = false);
            slider.RegisterValueChangedCallback(evt =>
            {
                if (Application.isPlaying)
                {
                    OccupationClock.SetTime(evt.newValue);
                }
            });
            panel.Add(VeneerStyle.FieldRow("Set the hour", slider, SetHourTooltip, out _));

            VisualElement chips = VeneerStyle.Row(true);
            VisualElement paused = VeneerStyle.Chip("Paused", "pause", false, on =>
            {
                if (Application.isPlaying)
                {
                    OccupationClock.Paused = on;
                }
            }, context.Brand);
            paused.tooltip = "Stops the built-in clock.";
            chips.Add(paused);
            panel.Add(chips);

            string shownTime = null;
            b.Add(panel);
            OccupationControls.HideCardOutsidePlay(b, panel);
            context.Poll(() =>
            {
                if (!Application.isPlaying)
                {
                    return;
                }

                string text = status.TimeText();
                if (!ReferenceEquals(text, shownTime))
                {
                    shownTime = text;
                    time.text = text;
                }

                if (!dragging)
                {
                    slider.SetValueWithoutNotify(OccupationClock.Hour);
                }

                if (VeneerStyle.IsChipOn(paused) != OccupationClock.Paused)
                {
                    VeneerStyle.SetChipOn(paused, OccupationClock.Paused);
                }
            });
            return null;
        }
    }

    /// <summary>
    /// The clock texts of one Occupation Clock Setup Inspector: the pill and the time line. Each text is rebuilt only when the minute
    /// or the day changes, so the polls allocate nothing in between.
    /// </summary>
    internal sealed class OccupationClockStatus
    {
        private int pillKey = -1;
        private string pillText = string.Empty;
        private int timeKey = -1;
        private string timeText = string.Empty;

        /// <summary>The pill text and kind: Optional while editing; the time and day while playing (warning colour while paused).</summary>
        public (string text, string kind) PillState()
        {
            if (!Application.isPlaying)
            {
                return ("Optional", "idle");
            }

            float hour = OccupationClock.Hour;
            int key = Key(hour);
            if (key != pillKey)
            {
                pillKey = key;
                pillText = OccupationClock.Format(hour) + " - day " + (OccupationClock.Day + 1);
            }

            return (pillText, OccupationClock.Paused ? "warn" : "ok");
        }

        /// <summary>The line of the Play Mode strip: the time and the day. The same string instance is returned until the minute changes.</summary>
        public string TimeText()
        {
            float hour = OccupationClock.Hour;
            int key = Key(hour);
            if (key != timeKey)
            {
                timeKey = key;
                timeText = "Time " + OccupationClock.Format(hour) + "   (day " + (OccupationClock.Day + 1) + ")";
            }

            return timeText;
        }

        private static int Key(float hour)
        {
            return (OccupationClock.Day * 2000) + Mathf.FloorToInt(hour * 60f);
        }
    }
}

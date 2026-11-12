using System;
using UnityEngine;

namespace OccupationSystem
{
    /// <summary>
    /// The arithmetic of a day plan, in plain functions: which day it is, which plan a day gets, which step an NPC that joins the
    /// day late starts with, and when a step ends. Times are total game hours (see <see cref="OccupationClock.TotalHours"/>); a day runs
    /// from the occupation's day start for 24 hours. Pure (no Unity objects), so it can be checked by driving it with numbers.
    /// </summary>
    public static class OccupationRules
    {
        /// <summary>The number of the plan day that <paramref name="totalHours"/> falls in: a day begins at <paramref name="dayStart"/> o'clock.</summary>
        public static int DayIndex(double totalHours, float dayStart) => (int)Math.Floor((totalHours - dayStart) / 24.0);

        /// <summary>Total hours at which plan day <paramref name="day"/> begins.</summary>
        public static double DayBegin(int day, float dayStart) => day * 24.0 + dayStart;

        /// <summary>Hours from the start of the day to <paramref name="hour"/> o'clock, 0 up to 24 (an hour before the day start is the next morning).</summary>
        public static double OffsetInDay(float hour, float dayStart)
        {
            var offset = (hour - dayStart) % 24.0;
            return offset < 0.0 ? offset + 24.0 : offset;
        }

        /// <summary>Total hours of <paramref name="hour"/> o'clock within plan day <paramref name="day"/>.</summary>
        public static double HourTotal(int day, float hour, float dayStart) => DayBegin(day, dayStart) + OffsetInDay(hour, dayStart);

        /// <summary>
        /// Total hours at which a step that stays until <paramref name="hour"/> o'clock ends, within plan day <paramref name="day"/>.
        /// The day's start hour itself means the end of the day (a night's sleep "until 06:00" of a day that starts at 06:00).
        /// </summary>
        public static double UntilTotal(int day, float hour, float dayStart)
        {
            var offset = OffsetInDay(hour, dayStart);
            if (offset < 1e-4)
            {
                offset = 24.0;
            }

            return DayBegin(day, dayStart) + offset;
        }

        /// <summary>
        /// Which plan the next day follows. Sequential: the one after <paramref name="previous"/> (the first when there was none).
        /// Random: a random one, never <paramref name="previous"/> when <paramref name="noRepeat"/> and there are several.
        /// </summary>
        /// <param name="roll">A random number in [0, 1]; ignored for the sequential order.</param>
        public static int PickPlan(int count, OccupationPlanOrder order, int previous, bool noRepeat, float roll)
        {
            if (count <= 1)
            {
                return 0;
            }

            if (order == OccupationPlanOrder.Sequential)
            {
                return previous < 0 ? 0 : (previous + 1) % count;
            }

            var skip = noRepeat && previous >= 0 && previous < count;
            var candidates = skip ? count - 1 : count;
            var pick = Math.Min(candidates - 1, (int)(roll * candidates));
            return skip && pick >= previous ? pick + 1 : pick;
        }

        /// <summary>Whether the step's end hour of plan day <paramref name="day"/> has already passed (only a step that stays until an hour can).</summary>
        public static bool IsExpired(OccupationStep step, int day, double now, float dayStart) =>
            step.Stay == OccupationStay.UntilHour && UntilTotal(day, step.UntilHour, dayStart) <= now;

        /// <summary>
        /// The step an NPC begins with when it joins plan day <paramref name="day"/> at <paramref name="now"/>: the first one that is not
        /// over yet, so an NPC that wakes up at 20:00 does not walk to work. <c>steps.Length</c> when every step is over.
        /// </summary>
        public static int FirstStep(OccupationStep[] steps, int day, double now, float dayStart)
        {
            var index = 0;
            while (index < steps.Length && (steps[index] == null || IsExpired(steps[index], day, now, dayStart)))
            {
                index++;
            }

            return index;
        }

        /// <summary>Total hours at which the NPC may set off for the step (<paramref name="now"/> when the step has no start hour).</summary>
        public static double StartTotal(OccupationStep step, int day, double now, float dayStart) =>
            step.UseStartAt ? Math.Max(now, HourTotal(day, step.StartAt, dayStart)) : now;

        /// <summary>Total hours at which the NPC, arriving at <paramref name="now"/>, stops staying.</summary>
        public static double StayEnd(OccupationStep step, int day, double now, float dayStart)
        {
            switch (step.Stay)
            {
                case OccupationStay.Minutes:
                    return now + step.Minutes / 60.0;
                case OccupationStay.UntilHour:
                    return UntilTotal(day, step.UntilHour, dayStart);
                default:
                    return now;
            }
        }

        /// <summary>The distance between two points on the ground (height ignored).</summary>
        public static float PlanarDistance(Vector3 a, Vector3 b)
        {
            var x = a.x - b.x;
            var z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}

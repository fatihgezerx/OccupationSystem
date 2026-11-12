using System;
using UnityEngine;

namespace OccupationSystem
{
    /// <summary>
    /// The game clock the occupations run on: hours of a day that pass at the speed you set. Nothing ticks it: the time is worked
    /// out from Unity's scaled time (so a paused game stops it) whenever it is asked for. Set it up with an
    /// <see cref="OccupationClockSetup"/> in the scene, from code, or drive it from your own day and night system with
    /// <see cref="Provider"/>.
    /// </summary>
    public static class OccupationClock
    {
        private const double DefaultStartHour = 8.0;
        private const float DefaultDayMinutes = 20f;

        private static double _anchorSeconds;
        private static double _anchorHours = DefaultStartHour;
        private static float _dayMinutes = DefaultDayMinutes;
        private static bool _paused;

        /// <summary>
        /// Your own source of time: the total game hours since day 0 at 00:00 (so day 3 at 14:30 is 3 * 24 + 14.5). While it is set
        /// the clock ignores its own speed and pause. Set it to null to use the built-in clock again.
        /// </summary>
        public static Func<double> Provider { get; set; }

        /// <summary>Total game hours since day 0 at 00:00.</summary>
        public static double TotalHours
        {
            get
            {
                var provider = Provider;
                if (provider != null)
                {
                    return provider();
                }

                return _paused ? _anchorHours : _anchorHours + (Time.timeAsDouble - _anchorSeconds) * HoursPerSecond;
            }
        }

        /// <summary>The hour of the day, 0 to 24 (14.5 = 14:30).</summary>
        public static float Hour
        {
            get
            {
                var hour = TotalHours % 24.0;
                return (float)(hour < 0.0 ? hour + 24.0 : hour);
            }
        }

        /// <summary>The number of the day, from 0.</summary>
        public static int Day => (int)Math.Floor(TotalHours / 24.0);

        /// <summary>Real minutes one game day lasts (default 20). Changing it keeps the time of day.</summary>
        public static float DayLengthMinutes
        {
            get => _dayMinutes;
            set
            {
                Reanchor();
                _dayMinutes = Mathf.Max(0.05f, value);
            }
        }

        /// <summary>Whether the built-in clock stands still.</summary>
        public static bool Paused
        {
            get => _paused;
            set
            {
                if (_paused != value)
                {
                    Reanchor();
                    _paused = value;
                }
            }
        }

        private static double HoursPerSecond => 24.0 / (_dayMinutes * 60.0);

        /// <summary>Sets the hour of the day (0 to 24) of the built-in clock, keeping the day number.</summary>
        public static void SetTime(float hour)
        {
            var day = Math.Floor(TotalHours / 24.0);
            _anchorSeconds = Time.timeAsDouble;
            _anchorHours = day * 24.0 + Mathf.Repeat(hour, 24f);
        }

        /// <summary>The built-in clock back to its start: 08:00 on day 0, a 20 minute day, running.</summary>
        public static void Reset()
        {
            Provider = null;
            _dayMinutes = DefaultDayMinutes;
            _paused = false;
            _anchorSeconds = Time.timeAsDouble;
            _anchorHours = DefaultStartHour;
        }

        /// <summary>An hour as text: 14.5 becomes "14:30".</summary>
        public static string Format(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            var minutes = Mathf.Min(59, Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f + 0.001f));
            return $"{(int)hour:00}:{minutes:00}";
        }

        // The time as it is now becomes the anchor, so a change of speed or pause does not jump the clock.
        private static void Reanchor()
        {
            _anchorHours = Provider == null ? TotalHours : _anchorHours;
            _anchorSeconds = Time.timeAsDouble;
        }

        // Keeps a stale clock out when "Enter Play Mode Options" skips the domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Provider = null;
            _dayMinutes = DefaultDayMinutes;
            _paused = false;
            _anchorSeconds = 0.0;
            _anchorHours = DefaultStartHour;
        }
    }
}

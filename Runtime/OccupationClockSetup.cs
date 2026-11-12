using UnityEngine;

namespace OccupationSystem
{
    /// <summary>
    /// Sets the <see cref="OccupationClock"/> up from the scene: put one anywhere (add an Occupation Clock Setup component to an object).
    /// Optional: without it the clock starts at 08:00 and a game day lasts 20 real minutes.
    /// </summary>
    [AddComponentMenu("Occupation System/Occupation Clock Setup")]
    [DefaultExecutionOrder(-200)]
    public sealed class OccupationClockSetup : MonoBehaviour
    {
        [SerializeField] [Range(0f, 24f)] [Tooltip("The hour the game starts at (14.5 = 14:30).")]
        private float _startHour = 8f;

        [SerializeField] [Min(0.05f)] [Tooltip("Real minutes one game day lasts.")]
        private float _dayLengthMinutes = 20f;

        [SerializeField] [Tooltip("Start with the clock stopped (set the time with OccupationClock.SetTime, or in this Inspector while playing).")]
        private bool _startPaused;

        private void Awake()
        {
            OccupationClock.Reset();
            OccupationClock.DayLengthMinutes = _dayLengthMinutes;
            OccupationClock.SetTime(_startHour);
            OccupationClock.Paused = _startPaused;
        }
    }
}

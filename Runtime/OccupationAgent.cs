using System;
using System.Text;
using EventSystem;
using UnityEngine;
using UnityEngine.AI;

namespace OccupationSystem
{
    /// <summary>One of an NPC's own places: the name a step uses, and what it means for this NPC.</summary>
    [Serializable]
    public struct OccupationBinding
    {
        [Tooltip("The name the steps use (\"Home\", \"Work\").")]
        public string Place;

        [Tooltip("Where it is for this NPC: an object in the scene (an Occupation Place, or any object).")]
        public Transform Target;
    }

    /// <summary>Where an NPC is in the step of its day plan.</summary>
    public enum OccupationPhase : byte
    {
        /// <summary>No plan yet (not started, or no occupation).</summary>
        None,

        /// <summary>The step has a start hour that has not come: the NPC waits where it is.</summary>
        WaitStart,

        /// <summary>Walking to the place of the step.</summary>
        Walk,

        /// <summary>At the place, staying (and playing the step's animation).</summary>
        Stay,

        /// <summary>Every step of the day is done: it stands until the next day.</summary>
        Done
    }

    /// <summary>
    /// Gives an NPC an occupation (<see cref="OccupationData"/>): while its body is free it follows the plan of the day, walking from
    /// place to place and playing the animation of each step. The agent only plans; the walking and the animation are done by the
    /// NPC's <see cref="IOccupationBody"/>, which another system provides (the Combat System's bridge for its NPCs). When the body is
    /// busy (a fight) the plan waits and carries on with the same step afterwards. The places of the steps are found by name: first in
    /// this agent's own <see cref="OccupationBinding"/>s (so every blacksmith has his own home), then the nearest <see cref="OccupationPlace"/>.
    /// An NPC that no other system moves is walked by its own NavMeshAgent and animated by its own Animator (its idle, walk and run clips come from the <see cref="OccupationData"/>).
    /// </summary>
    [AddComponentMenu("Occupation System/Occupation Agent")]
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    [DisallowMultipleComponent]
    public sealed class OccupationAgent : MonoBehaviour
    {
        /// <summary>The name that falls back to where the NPC started when nothing else is called that.</summary>
        public const string HomeName = "Home";

        // Seconds after which a walk to a place that has not got there counts as arrived (a stuck NPC does not freeze its day).
        private const double WalkTimeout = 180.0;

        // Meters from the place within which the NPC counts as standing on it: it walks right up to the spot, not just near it.
        private const float ArrivalTolerance = 0.1f;

        [SerializeField] [Tooltip("The occupation to follow: its day plans and how one is chosen each day. Create one with Assets > Create > Occupation System > Occupation.")]
        private OccupationData _data;

        [SerializeField] [Tooltip("This NPC's own places, by the names the steps use. A name that is not listed here goes to the nearest Occupation Place with that name in the scene.")]
        private OccupationBinding[] _bindings = Array.Empty<OccupationBinding>();

        private IOccupationBody _body;
        private OccupationPhase _phase;
        private double _nextThinkAt;
        private int _day = int.MinValue;
        private int _plan = -1;
        private int _lastPlan = -1;
        private int _step;
        private double _startAt;
        private double _until;
        private bool _keepUntil;
        private bool _announced;
        private bool _needsRestart;
        private bool _hasDest;
        private Vector3 _dest;
        private double _walkBeganAt;
        private bool _activityOn;
        private bool _warnedPlace;
        private bool _warnedBody;
        private bool _placePending;
        private int _holds;

        /// <summary>The occupation, or null.</summary>
        public OccupationData Data => _data;

        /// <summary>The NPC's own places.</summary>
        public OccupationBinding[] Bindings => _bindings;

        /// <summary>Whether there is an occupation with something to do.</summary>
        public bool IsActive => _data != null && _data.HasSteps;

        /// <summary>Where the NPC is in the step it is at.</summary>
        public OccupationPhase Phase => _phase;

        /// <summary>Whether the NPC is walking to the place of its step.</summary>
        public bool IsWalking => _phase == OccupationPhase.Walk;

        /// <summary>The body the agent steers, or null (nothing provides one for this object).</summary>
        public IOccupationBody Body => _body;

        /// <summary>Whether something holds the plan still (see <see cref="Hold"/>).</summary>
        public bool IsHeld => _holds > 0;

        private void OnEnable()
        {
            if (_body == null && !TryGetComponent(out _body))
            {
                // Another system's body first; else the agent walks and animates the NPC itself.
                _body = OccupationBodies.Create(this) ?? NavMeshOccupationBody.Create(this);
            }

            if (_body == null)
            {
                WarnBody();
                return;
            }

            Restart();
            _placePending = true;
            _nextThinkAt = Time.timeAsDouble + UnityEngine.Random.value * OccupationManager.ThinkSeconds;
            OccupationManager.Add(this);
        }

        private void OnDisable()
        {
            _holds = 0;
            OccupationManager.Remove(this);
            if (_body != null)
            {
                EndActivity();
                _body.Release();
                _body = null;
            }
        }

        /// <summary>Gives the NPC another occupation (or none) at run time; it starts a new day with it.</summary>
        public void SetData(OccupationData data)
        {
            _data = data;
            Restart();
        }

        /// <summary>Binds (or rebinds) a place name to an object for this NPC.</summary>
        public void Bind(string place, Transform target)
        {
            for (var i = 0; i < _bindings.Length; i++)
            {
                if (string.Equals(_bindings[i].Place, place, StringComparison.OrdinalIgnoreCase))
                {
                    _bindings[i].Target = target;
                    return;
                }
            }

            Array.Resize(ref _bindings, _bindings.Length + 1);
            _bindings[_bindings.Length - 1] = new OccupationBinding { Place = place, Target = target };
        }

        /// <summary>
        /// The object a binding really points at: <paramref name="target"/> itself when it is (or has no) Occupation Place, else the Occupation
        /// Place under it. Binding the parent of a place (the "Home" object that holds the "Point" with the Occupation Place) therefore
        /// still sends the NPC to the exact spot of the place, not to the parent's pivot.
        /// </summary>
        public static Transform PlaceOf(Transform target)
        {
            if (target == null || target.TryGetComponent<OccupationPlace>(out _))
            {
                return target;
            }

            var child = target.GetComponentInChildren<OccupationPlace>();
            return child != null ? child.transform : target;
        }

        /// <summary>
        /// Finds a place by name for this NPC standing at <paramref name="from"/>: a binding, else the nearest Occupation Place, else (for
        /// "Home") <paramref name="home"/>. <paramref name="position"/> is a world position (an object's own position, whatever it is a
        /// child of).
        /// </summary>
        public bool TryResolve(string place, Vector3 from, Vector3 home, out Vector3 position)
        {
            position = default;
            if (string.IsNullOrEmpty(place))
            {
                return false;
            }

            for (var i = 0; i < _bindings.Length; i++)
            {
                if (_bindings[i].Target != null && string.Equals(_bindings[i].Place, place, StringComparison.OrdinalIgnoreCase))
                {
                    position = PlaceOf(_bindings[i].Target).position;
                    return true;
                }
            }

            var found = OccupationPlace.Nearest(place, from);
            if (found != null)
            {
                position = found.transform.position;
                return true;
            }

            if (string.Equals(place, HomeName, StringComparison.OrdinalIgnoreCase))
            {
                position = home;
                return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="place"/> could be found: a binding, an Occupation Place of that name in the scene, or "Home".</summary>
        public bool CanResolve(string place)
        {
            if (string.IsNullOrEmpty(place) || string.Equals(place, HomeName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            for (var i = 0; i < _bindings.Length; i++)
            {
                if (_bindings[i].Target != null && string.Equals(_bindings[i].Place, place, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var places = OccupationPlace.All;
            for (var i = 0; i < places.Count; i++)
            {
                if (places[i] != null && string.Equals(places[i].PlaceName, place, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        #region Day

        /// <summary>Forgets the day: the NPC picks a plan again and begins it from the step the time of day says. For a new occupation, or after a revive.</summary>
        public void Restart()
        {
            EndActivity();
            _day = int.MinValue;
            _phase = OccupationPhase.None;
            _needsRestart = false;
            _announced = false;
            _hasDest = false;
            _keepUntil = false;
        }

        /// <summary>
        /// The body is busy (a fight, a search, death): the step's animation stops and the plan waits for the body to be free again,
        /// then carries on with the same step. Safe to call at any time, as often as you like.
        /// </summary>
        public void Interrupt()
        {
            EndActivity();
            if (_phase == OccupationPhase.Walk || _phase == OccupationPhase.Stay)
            {
                _needsRestart = true;
            }
        }

        /// <summary>
        /// Holds the plan still while another system has a moment with the NPC (a conversation, a cutscene): a walk stops where it is, a
        /// stay does not end, and the step's animation keeps playing (a blacksmith goes on hammering while he talks). Holds count: every
        /// <see cref="Hold"/> needs its <see cref="ReleaseHold"/>, so two systems can hold the same NPC. When the last one lets go the
        /// plan carries on (a walk sets off again; a stay whose hour passed meanwhile ends).
        /// </summary>
        public void Hold()
        {
            if (_holds++ == 0 && _phase == OccupationPhase.Walk && _body != null)
            {
                _body.StopMoving();
            }
        }

        /// <summary>Lets go of a <see cref="Hold"/>. Safe to call when not held.</summary>
        public void ReleaseHold()
        {
            if (_holds == 0 || --_holds > 0)
            {
                return;
            }

            if (_phase == OccupationPhase.Walk && _hasDest && _body != null && _body.IsFree)
            {
                var step = CurrentStep();
                if (step != null)
                {
                    _walkBeganAt = Time.timeAsDouble;
                    _body.MoveTo(_dest, step.Gait);
                }
            }
        }

        /// <summary>
        /// The place the NPC is meant to be at (where its plan has it, while it has a place to be): what a system that makes it return
        /// after a fight walks to, and measures its leash from.
        /// </summary>
        public bool TryGetPoint(out Vector3 point)
        {
            point = _dest;
            return IsActive && _hasDest && (_phase == OccupationPhase.Walk || _phase == OccupationPhase.Stay);
        }

        // Called by the manager every wake; thinks when the agent's turn has come.
        internal void Step(double now)
        {
            if (now < _nextThinkAt)
            {
                return;
            }

            _nextThinkAt = now + OccupationManager.ThinkSeconds;
            if (_body == null || !IsActive || _holds > 0)
            {
                return;
            }

            if (!_body.IsFree)
            {
                Interrupt();
                return;
            }

            Think(now);
        }

        // The body is free: one step of the day.
        private void Think(double now)
        {
            var hours = OccupationClock.TotalHours;
            var day = OccupationRules.DayIndex(hours, _data.DayStart);
            if (day != _day)
            {
                BeginDay(day, now, hours);
            }
            else if (_needsRestart)
            {
                _needsRestart = false;
                Resume(now, hours);
            }

            var step = CurrentStep();
            if (step == null)
            {
                return;
            }

            switch (_phase)
            {
                case OccupationPhase.WaitStart:
                    if (hours >= _startAt)
                    {
                        BeginWalk(now, hours, step);
                    }

                    break;

                case OccupationPhase.Walk:
                    ThinkWalk(now, hours, step);
                    break;

                case OccupationPhase.Stay:
                    if (hours >= _until)
                    {
                        EndStep(now, hours);
                    }

                    break;
            }
        }

        private OccupationStep CurrentStep()
        {
            var plans = _data.Plans;
            if (_plan < 0 || _plan >= plans.Length || plans[_plan] == null)
            {
                return null;
            }

            var steps = plans[_plan].Steps;
            return steps != null && _step >= 0 && _step < steps.Length ? steps[_step] : null;
        }

        // A new day: a plan is picked and the NPC joins it at the first step that is not over (so a spawn at 20:00 does not walk to work).
        private void BeginDay(int day, double now, double hours)
        {
            EndActivity();
            _day = day;
            _needsRestart = false;
            _announced = false;
            _plan = OccupationRules.PickPlan(_data.Plans.Length, _data.Order, _lastPlan, _data.NoRepeat, UnityEngine.Random.value);
            _lastPlan = _plan;

            var plan = _data.Plans[_plan];
            _step = plan != null && plan.Steps != null ? OccupationRules.FirstStep(plan.Steps, day, hours, _data.DayStart) : 0;
            if (_placePending)
            {
                _placePending = false;
                PlaceAtStart();
            }

            EnterStep(now, hours);
        }

        // The very first day of the NPC begins at the place of the step it joins (the first step of the plan when the day is young): it is
        // put there instead of walking from wherever it was placed in the scene.
        private void PlaceAtStart()
        {
            var step = CurrentStep();
            if (step != null && TryResolve(step.Place, transform.position, _body.Home, out var position) &&
                _body.TryReach(position, out var destination))
            {
                _body.Teleport(destination);
            }
        }

        // Starts the step the NPC is at: waits for its start hour, or goes.
        private void EnterStep(double now, double hours)
        {
            var step = CurrentStep();
            if (step == null)
            {
                _phase = OccupationPhase.Done;
                _hasDest = false;
                return;
            }

            _keepUntil = false;
            _announced = false;
            var start = OccupationRules.StartTotal(step, _day, hours, _data.DayStart);
            if (start > hours)
            {
                _phase = OccupationPhase.WaitStart;
                _startAt = start;
                return;
            }

            BeginWalk(now, hours, step);
        }

        // Sets off for the place of the step. A step with no place (or none found), or with the Idle gait, is done where the NPC stands.
        private void BeginWalk(double now, double hours, OccupationStep step)
        {
            if (step.Gait == OccupationGait.Idle)
            {
                _hasDest = false;
                BeginStay(now, hours, step);
                return;
            }

            if (!TryResolve(step.Place, transform.position, _body.Home, out var position))
            {
                if (!string.IsNullOrEmpty(step.Place))
                {
                    WarnPlace(step.Place);
                }

                _hasDest = false;
                BeginStay(now, hours, step);
                return;
            }

            if (!_body.TryReach(position, out var destination))
            {
                // Out of reach: the step cannot be done, the day goes on.
                if (!_warnedPlace)
                {
                    _warnedPlace = true;
                    Debug.LogWarning($"[Occupation] The place '{step.Place}' of '{name}' is too far from anywhere it can stand, so the step '{step.Name}' is skipped.", this);
                }

                _step++;
                EnterStep(now, hours);
                return;
            }

            _dest = destination;
            _hasDest = true;
            if (OccupationRules.PlanarDistance(transform.position, destination) <= ArrivalTolerance)
            {
                BeginStay(now, hours, step);
                return;
            }

            _phase = OccupationPhase.Walk;
            _walkBeganAt = now;
            _body.MoveTo(destination, step.Gait);
        }

        private void ThinkWalk(double now, double hours, OccupationStep step)
        {
            if (OccupationRules.PlanarDistance(transform.position, _dest) <= ArrivalTolerance ||
                _body.HasArrived(ArrivalTolerance) || now - _walkBeganAt > WalkTimeout)
            {
                _body.StopMoving();
                BeginStay(now, hours, step);
            }
        }

        // Arrived (or standing where the step is done): how long it stays, and the animation. A step whose hour has
        // already passed is over at once, without the animation.
        private void BeginStay(double now, double hours, OccupationStep step)
        {
            _phase = OccupationPhase.Stay;
            if (!_keepUntil)
            {
                _until = OccupationRules.StayEnd(step, _day, hours, _data.DayStart);
            }

            _keepUntil = false;
            if (hours >= _until)
            {
                EndStep(now, hours);
                return;
            }

            if (step.Clip != null)
            {
                _activityOn = true;
                _body.PlayActivity(step.Clip, step.LoopClip);
            }

            if (!_announced)
            {
                _announced = true;
                RaiseStep(step, true);
            }
        }

        private void EndStep(double now, double hours)
        {
            var step = CurrentStep();
            EndActivity();
            if (_announced && step != null)
            {
                RaiseStep(step, false);
            }

            _announced = false;
            _step++;
            EnterStep(now, hours);
        }

        // The body is free again after something put the plan on hold: back to the place of the step (usually it is already there).
        private void Resume(double now, double hours)
        {
            var step = CurrentStep();
            if (step == null)
            {
                return;
            }

            if (_phase == OccupationPhase.Walk || _phase == OccupationPhase.Stay)
            {
                if (OccupationRules.IsExpired(step, _day, hours, _data.DayStart))
                {
                    EndStep(now, hours);
                    return;
                }

                _keepUntil = _phase == OccupationPhase.Stay;
                BeginWalk(now, hours, step);
            }
        }

        private void EndActivity()
        {
            if (_activityOn)
            {
                _activityOn = false;
                _body?.StopActivity();
            }
        }

        private void RaiseStep(OccupationStep step, bool started)
        {
            var plan = _data.Plans[_plan];
            EventManager.Invoke(new OccupationStepEvent(this, plan != null ? plan.Name : string.Empty, step.Name, step.Place, _step, started));
        }

        private void WarnPlace(string place)
        {
            if (!_warnedPlace)
            {
                _warnedPlace = true;
                Debug.LogWarning($"[Occupation] '{name}' cannot find the place '{place}': give its Occupation Agent a binding with that name, or put an Occupation Place called '{place}' in the scene. The step is done where the NPC stands.", this);
            }
        }

        private void WarnBody()
        {
            if (!_warnedBody)
            {
                _warnedBody = true;
                Debug.LogWarning($"[Occupation] '{name}' has no body to live its occupation with: it needs a NavMeshAgent and an Animator on the same object, or a system that provides one (the Combat System does for an object with a Brain), or a component that implements IOccupationBody.", this);
            }
        }

        #endregion

        /// <summary>The state of the NPC's day for the Inspector and the monitors: the plan, the step, the clock. Allocates: editor use only.</summary>
        public string Describe()
        {
            if (!IsActive)
            {
                return string.Empty;
            }

            var text = new StringBuilder();
            text.Append("Time: ").Append(OccupationClock.Format(OccupationClock.Hour)).Append(", day ").Append(OccupationClock.Day + 1);
            var plans = _data.Plans;
            if (_plan < 0 || _plan >= plans.Length || plans[_plan] == null)
            {
                return text.Append("\nNo plan yet").ToString();
            }

            text.Append("\nPlan: ").Append(plans[_plan].Name);
            var step = CurrentStep();
            if (step == null)
            {
                return text.Append(" (the day is done)").ToString();
            }

            text.Append("\nStep ").Append(_step + 1).Append(": ").Append(step.Name).Append(" (").Append(string.IsNullOrEmpty(step.Place) ? "where it stands" : step.Place).Append("), ").Append(_phase);
            if (_phase == OccupationPhase.Stay && step.Stay != OccupationStay.Nothing)
            {
                text.Append(", until ").Append(OccupationClock.Format((float)(_until % 24.0)));
            }
            else if (_phase == OccupationPhase.WaitStart)
            {
                text.Append(", sets off at ").Append(OccupationClock.Format((float)(_startAt % 24.0)));
            }

            return text.ToString();
        }
    }
}

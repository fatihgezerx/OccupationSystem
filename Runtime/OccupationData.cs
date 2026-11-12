using System;
using UnityEngine;

namespace OccupationSystem
{
    /// <summary>How long an NPC stays at a step's place.</summary>
    public enum OccupationStay : byte
    {
        /// <summary>It does not stay: it arrives and moves on to the next step at once.</summary>
        Nothing,

        /// <summary>For a number of game minutes after it arrives.</summary>
        Minutes,

        /// <summary>Until an hour of the game clock (a step whose hour has already passed is skipped).</summary>
        UntilHour
    }

    /// <summary>How an NPC gets to a step's place.</summary>
    public enum OccupationGait : byte
    {
        Walk = 0,
        Run = 1,

        /// <summary>It does not go anywhere: the step is done where the NPC stands (the starting step still puts it at its place first).</summary>
        Idle = 2
    }

    /// <summary>How an occupation picks the plan of each day.</summary>
    public enum OccupationPlanOrder : byte
    {
        /// <summary>A random plan every day.</summary>
        Random,

        /// <summary>The plans one after the other, day after day, then from the first again.</summary>
        Sequential
    }

    /// <summary>One step of a day plan: go to a place, then stay there and (optionally) play an animation.</summary>
    [Serializable]
    public sealed class OccupationStep
    {
        [Tooltip("A name for yourself (\"Work at the forge\"). Shown in the Inspector and in events.")]
        public string Name = "Step";

        [Tooltip("The place to go to, by name: the NPC's own binding of that name (its Occupation Agent), else the nearest Occupation Place with that name in the scene. \"Home\" is where the NPC starts when nothing else has that name. Empty = stay where it stands.")]
        public string Place = "Home";

        [Tooltip("How it gets there. Idle: it does not go anywhere and does the step where it stands (right for the starting step, where the NPC is put at the place).")]
        public OccupationGait Gait = OccupationGait.Walk;

        [Tooltip("On: it does not set off before the hour below. Off: it goes as soon as the step before it is done.")]
        public bool UseStartAt;

        [Range(0f, 24f)] [Tooltip("The hour of the day before which it does not set off (8.5 = 08:30).")]
        public float StartAt = 8f;

        [Tooltip("Played at the place at its own speed (looped, or once and held). Empty = the NPC just stands there.")]
        public AnimationClip Clip;

        [Tooltip("Loop the animation for as long as the NPC stays. Off: it plays once and its last pose is held.")]
        public bool LoopClip = true;

        [Tooltip("How long it stays after it arrives.")]
        public OccupationStay Stay = OccupationStay.Minutes;

        [Min(0f)] [Tooltip("Game minutes it stays (Stay: Minutes).")]
        public float Minutes = 30f;

        [Range(0f, 24f)] [Tooltip("The hour it stays until (Stay: Until Hour): 18 = 18:00. An hour before the day's start counts as the next morning.")]
        public float UntilHour = 18f;
    }

    /// <summary>A day: steps done one after the other, from the start of the day.</summary>
    [Serializable]
    public sealed class OccupationPlan
    {
        [Tooltip("A name for yourself (\"Work day\", \"Town hall day\").")]
        public string Name = "Plan";

        [Tooltip("The steps, in order.")]
        public OccupationStep[] Steps = new OccupationStep[0];
    }

    /// <summary>
    /// An occupation: the daily life of an NPC. A set of day plans (each a list of steps: go here, stay there, play this
    /// animation) and how one is chosen each day. Share one asset between all the NPCs with the same job; each NPC says where
    /// "Home" and "Work" are on its own Occupation Agent. Give an NPC an occupation with an <see cref="OccupationAgent"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "New Occupation", menuName = "Occupation System/Occupation")]
    public sealed class OccupationData : ScriptableObject
    {
        /// <summary>Meters per second for a walk when nothing says otherwise.</summary>
        public const float DefaultWalkSpeed = 1.4f;

        /// <summary>Meters per second for a run when nothing says otherwise.</summary>
        public const float DefaultRunSpeed = 4f;

        [Tooltip("The day plans. Each day the NPC follows one of them.")]
        public OccupationPlan[] Plans = { new OccupationPlan() };

        [Tooltip("Random: a different plan every day, picked by chance. Sequential: the plans in order, one per day.")]
        public OccupationPlanOrder Order = OccupationPlanOrder.Random;

        [Tooltip("Random order only: never the plan of the day before (when there are several).")]
        public bool NoRepeat = true;

        [Range(0f, 24f)] [Tooltip("The hour a new day, and with it a new plan, begins (6 = 06:00). A plan's steps run from here; an Until Hour before it means the next morning.")]
        public float DayStart = 6f;

        // How the NPC looks while it stands, walks and runs is not here: those clips belong to the character's Animation Profile (the
        // Animation Profile System), played by its Profile Animator. Only the speeds are the occupation's.
        [Min(0.1f)] [Tooltip("Meters per second when a step says Walk. The walking clips of the character's Animation Profile show at this speed.")]
        public float WalkSpeed = DefaultWalkSpeed;

        [Min(0.1f)] [Tooltip("Meters per second when a step says Run. The running clips of the character's Animation Profile show at this speed.")]
        public float RunSpeed = DefaultRunSpeed;

        /// <summary>Whether there is at least one plan with at least one step.</summary>
        public bool HasSteps
        {
            get
            {
                if (Plans == null)
                {
                    return false;
                }

                for (var i = 0; i < Plans.Length; i++)
                {
                    if (Plans[i] != null && Plans[i].Steps != null && Plans[i].Steps.Length > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace OccupationSystem
{
    /// <summary>
    /// The body an <see cref="OccupationAgent"/> lives its day with: it knows nothing about walking or animating, so it asks the body.
    /// Another system (a combat or AI system, a crowd system) provides it. Implement it on a component of the same object, or register a
    /// factory with <see cref="OccupationBodies"/> so every character of that system gets a body without a component being added.
    /// </summary>
    public interface IOccupationBody
    {
        /// <summary>Where the character started: the place a step calls "Home" is here when nothing else has that name.</summary>
        Vector3 Home { get; }

        /// <summary>
        /// Whether the character may follow its plan right now. False while something with priority has it (a fight, a search, being
        /// dead): the agent then stops the step's animation and waits. Asked every tick.
        /// </summary>
        bool IsFree { get; }

        /// <summary>The point the character can stand on nearest to <paramref name="position"/>; false when there is none close enough (the step is then skipped).</summary>
        bool TryReach(Vector3 position, out Vector3 destination);

        /// <summary>Puts the character at <paramref name="position"/> (a point <see cref="TryReach"/> gave) at once, without walking: where the day begins, the place of the first step.</summary>
        void Teleport(Vector3 position);

        /// <summary>Sets off for <paramref name="destination"/> (a point <see cref="TryReach"/> gave), turning on its own while it goes.</summary>
        void MoveTo(Vector3 destination, OccupationGait gait);

        /// <summary>Whether the character, going to the point it was last sent to, is within <paramref name="radius"/> meters of it.</summary>
        bool HasArrived(float radius);

        /// <summary>Stops walking.</summary>
        void StopMoving();

        /// <summary>Plays <paramref name="clip"/> on the character at its place (looped, or once and held).</summary>
        void PlayActivity(AnimationClip clip, bool loop);

        /// <summary>Stops the clip started by <see cref="PlayActivity"/>.</summary>
        void StopActivity();

        /// <summary>The agent is disabled and lets go of the body: stop whatever it started.</summary>
        void Release();
    }

    /// <summary>The factories that make an <see cref="IOccupationBody"/> for an agent that has none on its object.</summary>
    public static class OccupationBodies
    {
        /// <summary>Makes a body for <paramref name="agent"/>, or returns null when this factory has none for it.</summary>
        public delegate IOccupationBody Factory(OccupationAgent agent);

        private static readonly List<Factory> Factories = new List<Factory>();

        /// <summary>Adds a factory (a second call with the same one does nothing). Call it early, e.g. before the first scene loads.</summary>
        public static void Register(Factory factory)
        {
            if (factory != null && !Factories.Contains(factory))
            {
                Factories.Add(factory);
            }
        }

        /// <summary>Removes a factory.</summary>
        public static void Unregister(Factory factory) => Factories.Remove(factory);

        internal static IOccupationBody Create(OccupationAgent agent)
        {
            for (var i = 0; i < Factories.Count; i++)
            {
                var body = Factories[i](agent);
                if (body != null)
                {
                    return body;
                }
            }

            return null;
        }
    }
}

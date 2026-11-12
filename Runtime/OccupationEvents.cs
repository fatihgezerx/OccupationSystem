namespace OccupationSystem
{
    /// <summary>
    /// An NPC living its occupation arrived at the place of a step and started it (<see cref="Started"/>), or finished it. For the game
    /// to react: open the shop when the baker arrives, close it when he leaves. Sent through the Event System.
    /// </summary>
    public readonly struct OccupationStepEvent
    {
        /// <summary>The agent of the NPC.</summary>
        public readonly OccupationAgent Agent;

        /// <summary>The name of the day plan.</summary>
        public readonly string Plan;

        /// <summary>The name of the step.</summary>
        public readonly string Step;

        /// <summary>The place name of the step.</summary>
        public readonly string Place;

        /// <summary>The step's number in its plan, from 0.</summary>
        public readonly int Index;

        /// <summary>True when the NPC arrived and began the step, false when it finished it.</summary>
        public readonly bool Started;

        public OccupationStepEvent(OccupationAgent agent, string plan, string step, string place, int index, bool started)
        {
            Agent = agent;
            Plan = plan;
            Step = step;
            Place = place;
            Index = index;
            Started = started;
        }
    }
}

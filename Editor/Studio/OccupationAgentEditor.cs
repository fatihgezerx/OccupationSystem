using Veneer;
using UnityEditor;

namespace OccupationSystem
{
    /// <summary>
    /// Studio-look Inspector for <see cref="OccupationAgent"/>, drawn from a Veneer layout: the project's own copy made in the Inspector
    /// Designer when there is one, otherwise <see cref="OccupationAgentDefaultLayout"/> (the occupation with a button that creates one, the
    /// NPC's own places, warnings while editing and, while playing, where the NPC is in its day). The pill, the warnings, the button and
    /// the live strip are custom items (<see cref="OccupationAgentItems"/>).
    /// </summary>
    [CustomEditor(typeof(OccupationAgent))]
    internal sealed class OccupationAgentEditor : VeneerLayoutDrivenInspector<OccupationAgent>
    {
        /// <inheritdoc />
        protected override VeneerBrand Brand => OccupationBrand.Value;
    }
}

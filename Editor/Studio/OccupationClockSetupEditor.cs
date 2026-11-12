using Veneer;
using UnityEditor;

namespace OccupationSystem
{
    /// <summary>
    /// Studio-look Inspector for <see cref="OccupationClockSetup"/>, drawn from a Veneer layout: the project's own copy made in the
    /// Inspector Designer when there is one, otherwise <see cref="OccupationClockSetupDefaultLayout"/> (the start hour as a clock time, the
    /// length of a day and whether the clock starts stopped; while playing, a card to set the hour or pause the clock). The pill, the
    /// note, the hour widget and the Play Mode controls are custom items (<see cref="OccupationClockSetupItems"/>).
    /// </summary>
    [CustomEditor(typeof(OccupationClockSetup))]
    internal sealed class OccupationClockSetupEditor : VeneerLayoutDrivenInspector<OccupationClockSetup>
    {
        /// <inheritdoc />
        protected override VeneerBrand Brand => OccupationBrand.Value;
    }
}

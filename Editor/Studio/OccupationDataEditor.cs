using Veneer;
using UnityEditor;

namespace OccupationSystem
{
    /// <summary>
    /// Studio-look Inspector for <see cref="OccupationData"/>, drawn from a Veneer layout: the project's own copy made in the Inspector
    /// Designer when there is one, otherwise <see cref="OccupationDataDefaultLayout"/> (tabs Plans, Days and Movement). The header pill,
    /// the no-steps hint and the widgets of the plans, the order, the no-repeat chip and the day start are custom items
    /// (<see cref="OccupationDataItems"/>). Only a selection of several assets is built here: a header, the pill and a hint.
    /// </summary>
    [CustomEditor(typeof(OccupationData))]
    internal sealed class OccupationDataEditor : VeneerLayoutDrivenInspector<OccupationData>
    {
        /// <inheritdoc />
        protected override VeneerBrand Brand => OccupationBrand.Value;

        /// <inheritdoc />
        protected override bool BuildMultiSelection(VeneerInspectorBuilder b)
        {
            b.Header(Target.name, OccupationDataDefaultLayout.Subtitle);
            b.StatePill(new OccupationDataStatus(Target).PillState);
            b.Hint("Several occupations are selected. Select one to edit its plans.");
            return true;
        }
    }
}

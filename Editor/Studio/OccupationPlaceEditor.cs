using Veneer;
using UnityEditor;

namespace OccupationSystem
{
    /// <summary>
    /// Studio-look Inspector for <see cref="OccupationPlace"/>, drawn from a Veneer layout: the project's own copy made in the Inspector
    /// Designer when there is one, otherwise <see cref="OccupationPlaceDefaultLayout"/> (the name the steps use, with its warnings).
    /// </summary>
    [CustomEditor(typeof(OccupationPlace))]
    internal sealed class OccupationPlaceEditor : VeneerLayoutDrivenInspector<OccupationPlace>
    {
        /// <inheritdoc />
        protected override VeneerBrand Brand => OccupationBrand.Value;
    }
}

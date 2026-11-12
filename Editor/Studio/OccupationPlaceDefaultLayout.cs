using System;
using Veneer;

namespace OccupationSystem
{
    /// <summary>
    /// The design of the <see cref="OccupationPlace"/> Inspector that Occupation System ships: one Place card with the name, the name
    /// checks and a note. With Veneer installed a project can copy it in the Inspector Designer and change the copy; this default is
    /// never saved as an asset.
    /// </summary>
    internal sealed class OccupationPlaceDefaultLayout : IVeneerDefaultLayoutProvider
    {
        /// <inheritdoc />
        public Type TargetType => typeof(OccupationPlace);

        /// <inheritdoc />
        public VeneerLayout CreateLayout()
        {
            VeneerLayout layout = new VeneerLayoutBuilder(typeof(OccupationPlace), "Occupation Place", "A named spot NPCs go to")
                .Card("Place", "map")
                .Custom(OccupationPlaceItems.StateId)
                .Field("_placeName", "Name")
                .Custom(OccupationPlaceItems.ChecksId)
                .Note("The NPC walks to this object's exact position. The place is drawn as a disc with its name in the Scene view.")
                .Build();

            // The look of the system (accent, style sheet). UseThemeLogo stays off on purpose: the Inspector's builder already carries the
            // brand and shows its logo in the header.
            layout.ThemeBrand = OccupationBrand.Value.Name;
            return layout;
        }

        /// <inheritdoc />
        public void RegisterItems()
        {
            OccupationPlaceItems.Register();
        }
    }
}

using System;
using Veneer;

namespace OccupationSystem
{
    /// <summary>
    /// The design of the <see cref="OccupationAgent"/> Inspector that Occupation System ships: the Occupation card (the asset slot, a
    /// button that creates one, two warnings), the Own places card (a note, the bindings list, the place checks) and a Play Mode
    /// "Right now" card. The state pill is a header item. With Veneer installed a project can copy it in the Inspector Designer and
    /// change the copy; this default is never saved as an asset.
    /// </summary>
    internal sealed class OccupationAgentDefaultLayout : IVeneerDefaultLayoutProvider
    {
        /// <inheritdoc />
        public Type TargetType => typeof(OccupationAgent);

        /// <inheritdoc />
        public VeneerLayout CreateLayout()
        {
            VeneerLayout layout = new VeneerLayoutBuilder(typeof(OccupationAgent), "Occupation Agent", "Gives this NPC a daily life")
                .Card("Occupation", "calendar")
                .Custom(OccupationAgentItems.StateId)
                .Field("_data", "Occupation", widget: VeneerWidget.Object)
                .Custom(OccupationAgentItems.NoDataId)
                .Custom(OccupationAgentItems.CreateId)
                .Custom(OccupationAgentItems.NoStepsId)
                .Card("Own places", "map")
                .Note("The NPC's own places, by the names the steps use (his Home, his Forge). A name that is not listed goes to the nearest Occupation Place with that name in the scene.")
                .Field("_bindings", "Place bindings", widget: VeneerWidget.List)
                .Custom(OccupationAgentItems.PlacesId)
                .Card("Right now", "play")
                .Custom(OccupationAgentItems.LiveId)
                .Build();

            // The look of the system (accent, style sheet). UseThemeLogo stays off on purpose: the Inspector's builder already carries the
            // brand and shows its logo in the header.
            layout.ThemeBrand = OccupationBrand.Value.Name;
            return layout;
        }

        /// <inheritdoc />
        public void RegisterItems()
        {
            OccupationAgentItems.Register();
        }
    }
}

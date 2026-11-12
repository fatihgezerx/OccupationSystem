using System;
using Veneer;

namespace OccupationSystem
{
    /// <summary>
    /// The design of the <see cref="OccupationData"/> Inspector that Occupation System ships: three tabs (Plans, Days, Movement) with
    /// their cards, fields and notes, and the system's own items (the plans / steps pill, the no-steps hint and the widgets of the plans,
    /// the order, the no-repeat chip and the day start). With Veneer installed a project can copy it in the Inspector Designer and change
    /// the copy; this default is never saved as an asset.
    /// </summary>
    internal sealed class OccupationDataDefaultLayout : IVeneerDefaultLayoutProvider
    {
        /// <summary>The muted line under the header title.</summary>
        internal const string Subtitle = "Occupation: the daily life of an NPC";

        /// <inheritdoc />
        public Type TargetType => typeof(OccupationData);

        /// <inheritdoc />
        public VeneerLayout CreateLayout()
        {
            VeneerLayout layout = new VeneerLayoutBuilder(typeof(OccupationData), "Occupation Data", Subtitle)
                .Page("Plans", "list")
                .Card("Plans", "list", false)
                .Custom(OccupationDataItems.StateId)
                .Custom(OccupationDataItems.NoStepsId)
                .Field(nameof(OccupationData.Plans), "Plans", widget: VeneerWidget.List)
                .Page("Days", "calendar")
                .Card("Days", "calendar")
                .Note("Each day the NPC follows one plan. A new day (and a new plan) begins at the day start. An NPC that appears in the middle of a day joins its plan at the first step that is not over yet.")
                .Field(nameof(OccupationData.Order), "Plan order", widget: VeneerWidget.Enum)
                .Field(nameof(OccupationData.NoRepeat), "Never the same plan twice in a row", widget: VeneerWidget.Toggle)
                .Icon("refresh")
                .Field(nameof(OccupationData.DayStart), "Day start", widget: VeneerWidget.Slider)
                .Page("Movement", "move")
                .Card("Speed", "speed")
                .Note("How the NPC looks while it stands, walks and runs comes from the Animation Profile of its Profile Animator. These are the speeds it moves at; the walking and running clips show at them.")
                .Field(nameof(OccupationData.WalkSpeed), "Walk speed", unit: "m/s", widget: VeneerWidget.Number)
                .Field(nameof(OccupationData.RunSpeed), "Run speed", unit: "m/s", widget: VeneerWidget.Number)
                .Build();

            // The look of the system (accent, style sheet). UseThemeLogo stays off on purpose: the Inspector's builder already carries the
            // brand and shows its logo in the header.
            layout.ThemeBrand = OccupationBrand.Value.Name;
            return layout;
        }

        /// <inheritdoc />
        public void RegisterItems()
        {
            OccupationDataItems.Register();
        }
    }
}

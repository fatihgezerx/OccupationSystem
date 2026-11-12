using System;
using Veneer;

namespace OccupationSystem
{
    /// <summary>
    /// The design of the <see cref="OccupationClockSetup"/> Inspector that Occupation System ships: a Start card (a note while editing,
    /// the start hour as a clock time, the day length and the start-paused chip) and a Play Mode "Right now" card to set the hour and
    /// pause the clock. The state pill is a header item. With Veneer installed a project can copy it in the Inspector Designer and
    /// change the copy; this default is never saved as an asset.
    /// </summary>
    internal sealed class OccupationClockSetupDefaultLayout : IVeneerDefaultLayoutProvider
    {
        /// <inheritdoc />
        public Type TargetType => typeof(OccupationClockSetup);

        /// <inheritdoc />
        public VeneerLayout CreateLayout()
        {
            VeneerLayout layout = new VeneerLayoutBuilder(typeof(OccupationClockSetup), "Occupation Clock", "The game's time of day")
                .Card("Start", "clock")
                .Custom(OccupationClockSetupItems.StateId)
                .Custom(OccupationClockSetupItems.OptionalId)
                .Field("_startHour", "Start hour", widget: VeneerWidget.Slider)
                .Field("_dayLengthMinutes", "Day length", unit: "real min", widget: VeneerWidget.Number)
                .Field("_startPaused", "Start with the clock stopped", widget: VeneerWidget.Toggle)
                .Icon("pause")
                .Card("Right now", "play")
                .Custom(OccupationClockSetupItems.LiveId)
                .Build();

            // The look of the system (accent, style sheet). UseThemeLogo stays off on purpose: the Inspector's builder already carries the
            // brand and shows its logo in the header.
            layout.ThemeBrand = OccupationBrand.Value.Name;
            return layout;
        }

        /// <inheritdoc />
        public void RegisterItems()
        {
            OccupationClockSetupItems.Register();
        }
    }
}

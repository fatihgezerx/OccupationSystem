using Veneer;
using UnityEngine;

namespace OccupationSystem
{
    /// <summary>The identity of Occupation System in the shared editor look: name, gold accent, logo and banner.</summary>
    internal static class OccupationBrand
    {
        private const string BrandFolder = "Assets/Scripts/OccupationSystem/Editor/Brand/";

        /// <summary>The cached brand; its pictures load lazily from <c>Editor/Brand</c>.</summary>
        public static readonly VeneerBrand Value = VeneerBrand.FromAssets(
            name: "Occupation System",
            subtitle: "Daily life for NPCs",
            accent: new Color32(0xFF, 0xB2, 0x24, 255),
            logoPath: BrandFolder + "logo.png",
            bannerPath: BrandFolder + "banner.png",
            iconFolder: null,
            styleSheetPath: BrandFolder + "OccupationBrand.uss",
            accentClass: "vn-accent--amber");
    }
}

using System.Collections.Generic;
using Colossal;

namespace AdvancedGridTool
{
    public class LocaleEN : IDictionarySource
    {
        private readonly ModSettings m_Setting;

        public LocaleEN(ModSettings setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Advanced Grid Tool" },
                { m_Setting.GetOptionTabLocaleID(ModSettings.kSection), "Main" },

                // Grid Configuration Group
                { m_Setting.GetOptionGroupLocaleID(ModSettings.kGridGroup), "Grid Configuration" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.DefaultGridMode)), "Default Grid Mode" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.DefaultGridMode)), "Select the default grid generation pattern" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.DefaultSpacing)), "Default Spacing" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.DefaultSpacing)), "Default distance between grid points (meters)" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.DefaultGridWidth)), "Default Grid Width" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.DefaultGridWidth)), "Default number of grid columns" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.DefaultGridHeight)), "Default Grid Height" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.DefaultGridHeight)), "Default number of grid rows" },

                // Placement Options Group
                { m_Setting.GetOptionGroupLocaleID(ModSettings.kPlacementGroup), "Placement Options" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.SnapToTerrain)), "Snap to Terrain" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.SnapToTerrain)), "Automatically adjust grid height to follow terrain" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.SnapToExistingRoads)), "Snap to Existing Roads" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.SnapToExistingRoads)), "Align grid with nearby existing roads" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.AutoCreateZones)), "Auto-Create Zones" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.AutoCreateZones)), "Automatically create zoning along grid roads" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.AvoidWater)), "Avoid Water" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.AvoidWater)), "Prevent grid placement in water areas" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.MaximumSlope)), "Maximum Slope" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.MaximumSlope)), "Maximum terrain slope angle for grid placement (degrees)" },

                // Visual Settings Group
                { m_Setting.GetOptionGroupLocaleID(ModSettings.kVisualsGroup), "Visual Settings" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.GridLineTransparency)), "Grid Line Transparency" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.GridLineTransparency)), "Transparency of preview grid lines (0-100%)" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.ShowGridNumbers)), "Show Grid Numbers" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.ShowGridNumbers)), "Display row and column numbers on grid preview" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.ShowMeasurements)), "Show Measurements" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.ShowMeasurements)), "Display distance and area measurements" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.AnimatePreview)), "Animate Preview" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.AnimatePreview)), "Enable animated effects for grid preview" },

                // Key Bindings Group
                { m_Setting.GetOptionGroupLocaleID(ModSettings.kKeybindingGroup), "Key Bindings" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.ToolActivationKey)), "Activate Tool" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.ToolActivationKey)), "Key to activate/deactivate the Advanced Grid Tool" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.GridModeKey)), "Change Grid Mode" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.GridModeKey)), "Key to cycle through grid generation modes" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.DecreaseSpacingKey)), "Decrease Spacing" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.DecreaseSpacingKey)), "Key to decrease grid spacing" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.IncreaseSpacingKey)), "Increase Spacing" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.IncreaseSpacingKey)), "Key to increase grid spacing" },
                { m_Setting.GetOptionLabelLocaleID(nameof(ModSettings.ResetBindings)), "Reset Key Bindings" },
                { m_Setting.GetOptionDescLocaleID(nameof(ModSettings.ResetBindings)), "Reset all key bindings to default values" },

                // Key binding names
                { m_Setting.GetBindingKeyLocaleID(Mod.kToolActivationActionName), "Tool Activation" },
                { m_Setting.GetBindingKeyLocaleID("GridModeSelection"), "Grid Mode" },
                { m_Setting.GetBindingKeyLocaleID("SpacingAdjustment", Game.Input.AxisComponent.Negative), "Decrease Spacing" },
                { m_Setting.GetBindingKeyLocaleID("SpacingAdjustment", Game.Input.AxisComponent.Positive), "Increase Spacing" },

                { m_Setting.GetBindingMapLocaleID(), "Advanced Grid Tool" },

                // Grid Mode Names (for dropdown)
                { "AdvancedGridTool.GridMode.Standard", "Standard Grid" },
                { "AdvancedGridTool.GridMode.Organic", "Organic Grid" },
                { "AdvancedGridTool.GridMode.Suburban", "Suburban Pattern" },
                { "AdvancedGridTool.GridMode.Hexagonal", "Hexagonal Grid" },
                { "AdvancedGridTool.GridMode.Curved", "Curved Grid" },
                { "AdvancedGridTool.GridMode.TerrainAdaptive", "Terrain Adaptive" },

                // Tool Tooltips
                { "AdvancedGridTool.TOOLTIP_Mode", "Mode" },
                { "AdvancedGridTool.TOOLTIP_Spacing", "Spacing" },
                { "AdvancedGridTool.TOOLTIP_Dimensions", "Dimensions" },
                { "AdvancedGridTool.TOOLTIP_TotalSize", "Total Size" },
                { "AdvancedGridTool.TOOLTIP_Area", "Area" },
            };
        }

        public void Unload()
        {
        }
    }
}
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;

namespace AdvancedGridTool
{
    [FileLocation(nameof(AdvancedGridTool))]
    [SettingsUIGroupOrder(kGridGroup, kPlacementGroup, kVisualsGroup, kKeybindingGroup)]
    [SettingsUIShowGroupName(kGridGroup, kPlacementGroup, kVisualsGroup, kKeybindingGroup)]

    // Tool Activation Action
    [SettingsUIKeyboardAction(
        name: Mod.kToolActivationActionName,
        type: ActionType.Button,
        rebindOptions: RebindOptions.All,
        modifierOptions: ModifierOptions.Allow,
        canBeEmpty: false,
        developerOnly: false,
        mode: Mode.DigitalNormalized,
        usages: new string[] { Usages.kToolUsage },
        interactions: new string[] { "Press" }
    )]

    // Grid Mode Selection Action
    [SettingsUIKeyboardAction(
        name: Mod.kGridModeActionName,
        type: ActionType.Button,
        rebindOptions: RebindOptions.All,
        modifierOptions: ModifierOptions.Allow,
        canBeEmpty: true,
        developerOnly: false,
        mode: Mode.DigitalNormalized,
        usages: new string[] { Usages.kToolUsage },
        interactions: new string[] { "Press" }
    )]

    // Spacing Adjustment Action
    [SettingsUIKeyboardAction(
        name: Mod.kSpacingActionName,
        type: ActionType.Axis,
        rebindOptions: RebindOptions.All,
        modifierOptions: ModifierOptions.Allow,
        canBeEmpty: true,
        developerOnly: false,
        mode: Mode.DigitalNormalized,
        usages: new string[] { Usages.kToolUsage },
        interactions: new string[] { "Press" }
    )]

    public class ModSettings : ModSetting
    {
        public const string kSection = "Main";

        public const string kGridGroup = "GridConfiguration";
        public const string kPlacementGroup = "PlacementOptions";
        public const string kVisualsGroup = "VisualSettings";
        public const string kKeybindingGroup = "KeyBindings";

        public ModSettings(IMod mod) : base(mod)
        {
        }

        // Grid Configuration Group

        [SettingsUISection(kSection, kGridGroup)]
        [SettingsUIDropdown(typeof(ModSettings), nameof(GetGridModeItems))]
        public int DefaultGridMode { get; set; } = 0;

        [SettingsUISection(kSection, kGridGroup)]
        [SettingsUISlider(min = 10f, max = 200f, step = 10f, scalarMultiplier = 1f, unit = Unit.kFloatSingleFraction)]
        public float DefaultSpacing { get; set; } = 50f;

        [SettingsUISection(kSection, kGridGroup)]
        [SettingsUISlider(min = 1, max = 20, step = 1, scalarMultiplier = 1)]
        public int DefaultGridWidth { get; set; } = 5;

        [SettingsUISection(kSection, kGridGroup)]
        [SettingsUISlider(min = 1, max = 20, step = 1, scalarMultiplier = 1)]
        public int DefaultGridHeight { get; set; } = 5;

        // Placement Options Group

        [SettingsUISection(kSection, kPlacementGroup)]
        public bool SnapToTerrain { get; set; } = true;

        [SettingsUISection(kSection, kPlacementGroup)]
        public bool SnapToExistingRoads { get; set; } = true;

        [SettingsUISection(kSection, kPlacementGroup)]
        public bool AutoCreateZones { get; set; } = false;

        [SettingsUISection(kSection, kPlacementGroup)]
        public bool AvoidWater { get; set; } = true;

        [SettingsUISection(kSection, kPlacementGroup)]
        [SettingsUISlider(min = 0f, max = 45f, step = 5f, scalarMultiplier = 1f, unit = Unit.kAngle)]
        public float MaximumSlope { get; set; } = 30f;

        // Visual Settings Group

        [SettingsUISection(kSection, kVisualsGroup)]
        [SettingsUISlider(min = 0f, max = 100f, step = 10f, scalarMultiplier = 0.01f)]
        public float GridLineTransparency { get; set; } = 50f;

        [SettingsUISection(kSection, kVisualsGroup)]
        public bool ShowGridNumbers { get; set; } = false;

        [SettingsUISection(kSection, kVisualsGroup)]
        public bool ShowMeasurements { get; set; } = true;

        [SettingsUISection(kSection, kVisualsGroup)]
        public bool AnimatePreview { get; set; } = true;

        // Key Bindings Group

        [SettingsUIKeyboardBinding(BindingKeyboard.G, Mod.kToolActivationActionName)]
        [SettingsUISection(kSection, kKeybindingGroup)]
        public ProxyBinding ToolActivationKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Tab, Mod.kGridModeActionName)]
        [SettingsUISection(kSection, kKeybindingGroup)]
        public ProxyBinding GridModeKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.LeftBracket, AxisComponent.Negative, Mod.kSpacingActionName)]
        [SettingsUISection(kSection, kKeybindingGroup)]
        public ProxyBinding DecreaseSpacingKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.RightBracket, AxisComponent.Positive, Mod.kSpacingActionName)]
        [SettingsUISection(kSection, kKeybindingGroup)]
        public ProxyBinding IncreaseSpacingKey { get; set; }

        // Note: Mouse wheel bindings are not directly supported through BindingMouse
        // This would need to be handled through a custom input action instead

        [SettingsUIButton]
        [SettingsUISection(kSection, kKeybindingGroup)]
        public bool ResetBindings
        {
            set
            {
                Mod.log.Info("Reset key bindings to defaults");
                ResetKeyBindings();
            }
        }

        // Dropdown item providers

        public DropdownItem<int>[] GetGridModeItems()
        {
            return new[]
            {
                new DropdownItem<int>() { value = 0, displayName = "Standard Grid" },
                new DropdownItem<int>() { value = 1, displayName = "Organic Grid" },
                new DropdownItem<int>() { value = 2, displayName = "Suburban Pattern" },
                new DropdownItem<int>() { value = 3, displayName = "Hexagonal Grid" },
                new DropdownItem<int>() { value = 4, displayName = "Curved Grid" },
                new DropdownItem<int>() { value = 5, displayName = "Terrain Adaptive" }
            };
        }

        public override void SetDefaults()
        {
            DefaultGridMode = 0;
            DefaultSpacing = 50f;
            DefaultGridWidth = 5;
            DefaultGridHeight = 5;
            SnapToTerrain = true;
            SnapToExistingRoads = true;
            AutoCreateZones = false;
            AvoidWater = true;
            MaximumSlope = 30f;
            GridLineTransparency = 50f;
            ShowGridNumbers = false;
            ShowMeasurements = true;
            AnimatePreview = true;
        }

        public void ApplySettings(AdvancedGridToolSystem gridToolSystem)
        {
            if (gridToolSystem == null) return;

            gridToolSystem.CurrentMode = (AdvancedGridToolSystem.GridMode)DefaultGridMode;
            gridToolSystem.Spacing = DefaultSpacing;
            gridToolSystem.GridDimensions = new Unity.Mathematics.int2(DefaultGridWidth, DefaultGridHeight);

            Mod.log.Info($"Applied settings to Grid Tool System");
        }
    }
}
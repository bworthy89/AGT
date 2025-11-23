using System;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game;
using Game.UI;
using Game.Tools;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace AdvancedGridTool
{
    /// <summary>
    /// UI system for Advanced Grid Tool - manages UI bindings and state (following Line Tool pattern)
    /// </summary>
    public sealed partial class AdvancedGridToolUISystem : UISystemBase
    {
        private static ILog _log;
        private AdvancedGridToolSystem _gridToolSystem;
        private ToolSystem _toolSystem;
        private bool _toolIsActive;
        private ToolBaseSystem _previousSystem;

        public static AdvancedGridToolUISystem Instance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            _log = LogManager.GetLogger($"{nameof(AdvancedGridTool)}.{nameof(AdvancedGridToolUISystem)}").SetShowsErrorsInUI(false);


            // Get system references
            _gridToolSystem = World.GetOrCreateSystemManaged<AdvancedGridToolSystem>();
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();

            // Subscribe to prefab changed events (like Line Tool)
            _toolSystem.EventPrefabChanged = (Action<PrefabBase>)Delegate.Combine(
                _toolSystem.EventPrefabChanged,
                new Action<PrefabBase>(OnPrefabChanged)
            );

            // Create value bindings (using GetterValueBinding like Line Tool)
            CreateBindings();

            // Create trigger bindings for UI actions
            CreateTriggers();

            _log.Info("Advanced Grid Tool UI System created");
        }

        protected override void OnDestroy()
        {
            Instance = null;
            base.OnDestroy();
        }

        private void CreateBindings()
        {
            // Tool active state (using GetterValueBinding like Line Tool)
            AddUpdateBinding(new GetterValueBinding<bool>(
                "AdvancedGridTool",
                "IsActive",
                () => _toolSystem.activeTool == _gridToolSystem
            ));

            // Current grid mode
            AddUpdateBinding(new GetterValueBinding<int>(
                "AdvancedGridTool",
                "CurrentMode",
                () => (int)_gridToolSystem.CurrentMode
            ));

            // Spacing value
            AddUpdateBinding(new GetterValueBinding<float>(
                "AdvancedGridTool",
                "Spacing",
                () => _gridToolSystem.Spacing
            ));

            // Grid dimensions
            AddUpdateBinding(new GetterValueBinding<int>(
                "AdvancedGridTool",
                "GridWidth",
                () => _gridToolSystem.GridDimensions.x
            ));

            AddUpdateBinding(new GetterValueBinding<int>(
                "AdvancedGridTool",
                "GridHeight",
                () => _gridToolSystem.GridDimensions.y
            ));

            // Show options panel
            AddUpdateBinding(new GetterValueBinding<bool>(
                "AdvancedGridTool",
                "ShowOptions",
                () => _toolSystem.activeTool == _gridToolSystem
            ));

            // Mode-specific options
            AddUpdateBinding(new GetterValueBinding<bool>(
                "AdvancedGridTool",
                "ShowStandardOptions",
                () => _gridToolSystem.CurrentMode == AdvancedGridToolSystem.GridMode.Standard
            ));

            AddUpdateBinding(new GetterValueBinding<bool>(
                "AdvancedGridTool",
                "ShowOrganicOptions",
                () => _gridToolSystem.CurrentMode == AdvancedGridToolSystem.GridMode.Organic
            ));

            AddUpdateBinding(new GetterValueBinding<bool>(
                "AdvancedGridTool",
                "ShowSuburbanOptions",
                () => _gridToolSystem.CurrentMode == AdvancedGridToolSystem.GridMode.Suburban
            ));

            _log.Info("Created UI value bindings");
        }

        private void CreateTriggers()
        {
            // Mode selection triggers (like Line Tool)
            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "SetModeStandard",
                () => SetGridMode(AdvancedGridToolSystem.GridMode.Standard)
            ));

            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "SetModeOrganic",
                () => SetGridMode(AdvancedGridToolSystem.GridMode.Organic)
            ));

            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "SetModeSuburban",
                () => SetGridMode(AdvancedGridToolSystem.GridMode.Suburban)
            ));

            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "SetModeHexagonal",
                () => SetGridMode(AdvancedGridToolSystem.GridMode.Hexagonal)
            ));

            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "SetModeCurved",
                () => SetGridMode(AdvancedGridToolSystem.GridMode.Curved)
            ));

            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "SetModeTerrainAdaptive",
                () => SetGridMode(AdvancedGridToolSystem.GridMode.TerrainAdaptive)
            ));

            // Spacing adjustment triggers
            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "IncreaseSpacing",
                () => AdjustSpacing(10f)
            ));

            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "DecreaseSpacing",
                () => AdjustSpacing(-10f)
            ));

            // Trigger with parameter for setting exact spacing (like Line Tool)
            AddBinding(new TriggerBinding<float>(
                "AdvancedGridTool",
                "SetSpacing",
                (float value) => { _gridToolSystem.Spacing = value; }
            ));

            // Grid dimension triggers
            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "IncreaseWidth",
                () => AdjustGridDimension(true, 1)
            ));

            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "DecreaseWidth",
                () => AdjustGridDimension(true, -1)
            ));

            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "IncreaseHeight",
                () => AdjustGridDimension(false, 1)
            ));

            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "DecreaseHeight",
                () => AdjustGridDimension(false, -1)
            ));

            // Tool activation trigger
            AddBinding(new TriggerBinding(
                "AdvancedGridTool",
                "ToggleTool",
                () => ToggleTool()
            ));

            _log.Info("Created UI trigger bindings");
        }

        private void SetGridMode(AdvancedGridToolSystem.GridMode mode)
        {
            _gridToolSystem.CurrentMode = mode;
            _log.Info($"UI: Set grid mode to {mode}");
        }

        private void AdjustSpacing(float delta)
        {
            // Apply modifier step (like Line Tool)
            float step = GetSpacingStep() * math.sign(delta);
            _gridToolSystem.Spacing += step;
            _log.Info($"UI: Adjusted spacing by {step} to {_gridToolSystem.Spacing}");
        }

        /// <summary>
        /// Gets the spacing step value to apply, including effects of shift- (x10) or control- (x0.1) modifiers (like Line Tool).
        /// </summary>
        /// <returns>10 if the shift key is pressed, 0.1 if the control key is pressed, and 1 otherwise.</returns>
        private float GetSpacingStep()
        {
            if (UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed)
            {
                // Shift; 10m.
                return 10f;
            }
            else if (UnityEngine.InputSystem.Keyboard.current.ctrlKey.isPressed)
            {
                // Control; 0.1m.
                return 0.1f;
            }

            // No modifiers pressed; just return the standard step (1m).
            return 1f;
        }

        private void AdjustGridDimension(bool width, int delta)
        {
            var dimensions = _gridToolSystem.GridDimensions;
            if (width)
            {
                dimensions.x = Math.Max(1, dimensions.x + delta);
            }
            else
            {
                dimensions.y = Math.Max(1, dimensions.y + delta);
            }
            _gridToolSystem.GridDimensions = dimensions;
            _log.Info($"UI: Adjusted grid dimensions to {dimensions}");
        }

        private void ToggleTool()
        {
            if (_gridToolSystem != null)
            {
                _gridToolSystem.EnableTool();
            }
        }

        /// <summary>
        /// Handles changes in the selected prefab (like Line Tool).
        /// </summary>
        /// <param name="prefab">New selected prefab.</param>
        private void OnPrefabChanged(PrefabBase prefab)
        {
            // If the grid tool is currently activated and the new prefab is a road, reactivate it
            // (the game will reset the tool to the relevant NetTool).
            if (_toolSystem.activeTool == _gridToolSystem && prefab is RoadPrefab)
            {
                _log.Info($"Prefab changed to {prefab.name}, reactivating grid tool");
                _gridToolSystem.EnableTool();
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Check for tool activation (like Line Tool)
            bool isToolActive = _toolSystem.activeTool == _gridToolSystem;

            if (isToolActive)
            {
                // Activate tool
                if (!_toolIsActive)
                {
                    _previousSystem = _gridToolSystem;
                    _toolIsActive = true;
                    _log.Info($"Grid tool UI activated. Tool ID: {_gridToolSystem?.toolID}");
                    _log.Info($"Binding values being sent to UI:");
                    _log.Info($"  - IsActive: {isToolActive}");
                    _log.Info($"  - ShowOptions: {isToolActive}");
                    _log.Info($"  - CurrentMode: {_gridToolSystem.CurrentMode}");
                    _log.Info($"  - Spacing: {_gridToolSystem.Spacing}");
                    _log.Info($"  - GridDimensions: {_gridToolSystem.GridDimensions}");
                }
            }
            else
            {
                // Tool not active - clean up if this is the first update after deactivation
                if (_toolIsActive)
                {
                    _toolIsActive = false;
                    _log.Info("Grid tool UI deactivated");
                    _log.Info($"Current active tool: {_toolSystem.activeTool?.toolID}");
                }
                else
                {
                    // Check if another tool change has occurred
                    if (_toolSystem.activeTool != _previousSystem)
                    {
                        _previousSystem = _toolSystem.activeTool;
                    }
                }
            }
        }
    }
}
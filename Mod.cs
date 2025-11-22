using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Input;
using Game.Modding;
using Game.SceneFlow;
using UnityEngine.InputSystem;

namespace AdvancedGridTool
{
    public class Mod : IMod
    {
        public const string ModName = "Advanced Grid Tool";

        // Logger
        public static ILog log = LogManager.GetLogger($"{nameof(AdvancedGridTool)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public ILog Log => log;

        // Settings
        private ModSettings m_Settings;
        public ModSettings ActiveSettings => m_Settings;

        // Actions
        public static ProxyAction m_ToolActivationAction;

        public const string kToolActivationActionName = "AdvancedGridToolActivation";
        public const string kGridModeActionName = "GridModeSelection";
        public const string kSpacingActionName = "SpacingAdjustment";

        // Static instance reference
        public static Mod Instance { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            // Set instance reference (like Line Tool)
            Instance = this;

            log.Info($"Loading {ModName}");

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            // Register mod settings to game options UI
            m_Settings = new ModSettings(this);
            m_Settings.RegisterInOptionsUI();

            // Load translations
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Settings));

            // Apply input bindings
            m_Settings.RegisterKeyBindings();

            // Get tool activation action
            m_ToolActivationAction = m_Settings.GetAction(kToolActivationActionName);
            if (m_ToolActivationAction != null)
            {
                m_ToolActivationAction.shouldBeEnabled = true;
                m_ToolActivationAction.onInteraction += OnToolActivation;
                log.Info($"Registered tool activation action");
            }

            // Load saved settings
            AssetDatabase.global.LoadSettings(nameof(AdvancedGridTool), m_Settings, new ModSettings(this));

            // Apply settings to default values
            m_Settings.SetDefaults();

            // ACTIVATE SYSTEMS - Using updateSystem directly like Line Tool!
            updateSystem.UpdateAt<AdvancedGridToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<AdvancedGridToolUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<AdvancedGridToolTooltipSystem>(SystemUpdatePhase.UITooltip);

            log.Info($"{ModName} loaded successfully");
        }

        private void OnToolActivation(ProxyAction action, InputActionPhase phase)
        {
            if (phase == InputActionPhase.Performed)
            {
                log.Info($"Tool activation triggered");

                // Get the grid tool system and enable it (like Line Tool)
                var gridToolSystem = AdvancedGridToolSystem.Instance;
                if (gridToolSystem != null)
                {
                    gridToolSystem.EnableTool();
                }
                else
                {
                    log.Error("AdvancedGridToolSystem.Instance is null - system may not be initialized yet");
                }
            }
        }

        public void OnDispose()
        {
            log.Info($"Disposing {ModName}");
            Instance = null;

            // Unregister actions
            if (m_ToolActivationAction != null)
            {
                m_ToolActivationAction.onInteraction -= OnToolActivation;
            }

            // Unregister settings
            if (m_Settings != null)
            {
                m_Settings.UnregisterInOptionsUI();
                m_Settings = null;
            }

            log.Info($"{ModName} disposed");
        }
    }
}
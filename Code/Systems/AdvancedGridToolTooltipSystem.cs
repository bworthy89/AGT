using System.Collections.Generic;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Tools;
using Game.UI.Localization;
using Game.UI.Tooltip;
using Unity.Entities;
using Unity.Mathematics;

namespace AdvancedGridTool
{
    /// <summary>
    /// Tooltip system for Advanced Grid Tool - displays measurements and information
    /// </summary>
    public sealed partial class AdvancedGridToolTooltipSystem : TooltipSystemBase
    {
        private static ILog _log;
        private AdvancedGridToolSystem _gridToolSystem;
        private ToolSystem _toolSystem;
        private EntityQuery _tooltipQuery;
        private List<TooltipInfo> _tooltips = new List<TooltipInfo>();

        public static AdvancedGridToolTooltipSystem Instance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            _log = LogManager.GetLogger($"{nameof(AdvancedGridTool)}.{nameof(AdvancedGridToolTooltipSystem)}").SetShowsErrorsInUI(false);

            // Get system references
            _gridToolSystem = World.GetOrCreateSystemManaged<AdvancedGridToolSystem>();
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();

            // Create query for tooltip UI elements
            _tooltipQuery = GetEntityQuery(ComponentType.ReadOnly<TooltipUITag>());

            _log.Info("Advanced Grid Tool Tooltip System created");
        }

        protected override void OnDestroy()
        {
            Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            // Only update when our tool is active
            if (_toolSystem.activeTool != _gridToolSystem)
            {
                ClearTooltips();
                return;
            }

            // Clear existing tooltips
            _tooltips.Clear();

            // Add tooltips based on current tool state
            AddModeTooltip();
            AddSpacingTooltip();
            AddDimensionsTooltip();
            AddDistanceTooltip();
            AddAreaTooltip();

            // Apply tooltips to UI
            ApplyTooltips();
        }

        private void AddModeTooltip()
        {
            string modeName = _gridToolSystem.CurrentMode switch
            {
                AdvancedGridToolSystem.GridMode.Standard => "Standard Grid",
                AdvancedGridToolSystem.GridMode.Organic => "Organic Grid",
                AdvancedGridToolSystem.GridMode.Suburban => "Suburban Pattern",
                AdvancedGridToolSystem.GridMode.Hexagonal => "Hexagonal Grid",
                AdvancedGridToolSystem.GridMode.Curved => "Curved Grid",
                AdvancedGridToolSystem.GridMode.TerrainAdaptive => "Terrain Adaptive",
                _ => "Unknown Mode"
            };

            _tooltips.Add(new TooltipInfo
            {
                m_Type = TooltipType.Title,
                m_TitleText = LocalizedString.Id("AdvancedGridTool.TOOLTIP_Mode"),
                m_Value = modeName,
                m_Priority = 100
            });
        }

        private void AddSpacingTooltip()
        {
            _tooltips.Add(new TooltipInfo
            {
                m_Type = TooltipType.Distance,
                m_TitleText = LocalizedString.Id("AdvancedGridTool.TOOLTIP_Spacing"),
                m_Value = $"{_gridToolSystem.Spacing:F1} m",
                m_Priority = 90
            });
        }

        private void AddDimensionsTooltip()
        {
            var dimensions = _gridToolSystem.GridDimensions;
            _tooltips.Add(new TooltipInfo
            {
                m_Type = TooltipType.Info,
                m_TitleText = LocalizedString.Id("AdvancedGridTool.TOOLTIP_Dimensions"),
                m_Value = $"{dimensions.x} × {dimensions.y}",
                m_Priority = 80
            });
        }

        private void AddDistanceTooltip()
        {
            // Calculate distance if we have start and end positions
            // This would need access to the grid tool's internal state
            // For now, showing placeholder

            if (_gridToolSystem.Spacing > 0)
            {
                float totalWidth = _gridToolSystem.GridDimensions.x * _gridToolSystem.Spacing;
                float totalHeight = _gridToolSystem.GridDimensions.y * _gridToolSystem.Spacing;

                _tooltips.Add(new TooltipInfo
                {
                    m_Type = TooltipType.Distance,
                    m_TitleText = LocalizedString.Id("AdvancedGridTool.TOOLTIP_TotalSize"),
                    m_Value = $"{totalWidth:F1} × {totalHeight:F1} m",
                    m_Priority = 70
                });
            }
        }

        private void AddAreaTooltip()
        {
            // Calculate total area covered by the grid
            float totalWidth = _gridToolSystem.GridDimensions.x * _gridToolSystem.Spacing;
            float totalHeight = _gridToolSystem.GridDimensions.y * _gridToolSystem.Spacing;
            float area = totalWidth * totalHeight;

            if (area > 0)
            {
                _tooltips.Add(new TooltipInfo
                {
                    m_Type = TooltipType.Area,
                    m_TitleText = LocalizedString.Id("AdvancedGridTool.TOOLTIP_Area"),
                    m_Value = FormatArea(area),
                    m_Priority = 60
                });
            }
        }

        private string FormatArea(float squareMeters)
        {
            if (squareMeters < 10000)
            {
                return $"{squareMeters:F0} m²";
            }
            else
            {
                float hectares = squareMeters / 10000f;
                return $"{hectares:F2} ha";
            }
        }

        private void ApplyTooltips()
        {
            // Sort tooltips by priority
            _tooltips.Sort((a, b) => b.m_Priority.CompareTo(a.m_Priority));

            // Create tooltip groups
            var groups = new List<TooltipGroup>();
            TooltipGroup? currentGroup = null;

            foreach (var tooltip in _tooltips)
            {
                if (!currentGroup.HasValue || currentGroup.Value.m_Type != tooltip.m_Type)
                {
                    var newGroup = new TooltipGroup
                    {
                        m_Type = tooltip.m_Type,
                        m_Items = new List<TooltipItem>(),
                        m_Priority = tooltip.m_Priority
                    };
                    currentGroup = newGroup;
                    groups.Add(newGroup);
                }

                if (currentGroup.HasValue)
                {
                    var group = currentGroup.Value;
                    group.m_Items.Add(new TooltipItem
                    {
                        m_Title = tooltip.m_TitleText,
                        m_Value = tooltip.m_Value
                    });
                    // Update the list since structs are value types
                    groups[groups.Count - 1] = group;
                }
            }

            // Apply to UI
            if (_tooltipQuery.CalculateEntityCount() > 0)
            {
                var tooltipEntity = _tooltipQuery.GetSingletonEntity();
                var tooltipData = EntityManager.GetComponentData<TooltipUIData>(tooltipEntity);

                // Update tooltip data (this would need proper implementation based on game's tooltip system)
                // For now, this is a placeholder
            }
        }

        private void ClearTooltips()
        {
            _tooltips.Clear();
            // Clear UI tooltips
        }

        // Helper structures
        private struct TooltipInfo
        {
            public TooltipType m_Type;
            public LocalizedString m_TitleText;
            public string m_Value;
            public int m_Priority;
        }

        private struct TooltipGroup
        {
            public TooltipType m_Type;
            public List<TooltipItem> m_Items;
            public int m_Priority;
        }

        private struct TooltipItem
        {
            public LocalizedString m_Title;
            public string m_Value;
        }

        private enum TooltipType
        {
            Title,
            Info,
            Distance,
            Area,
            Warning
        }

        // Component tags for the ECS system
        private struct TooltipUITag : IComponentData { }

#pragma warning disable CS0649 // Fields are assigned via ECS systems
        private struct TooltipUIData : IComponentData
        {
            public int GroupCount;
            public Entity FirstGroup;
        }
#pragma warning restore CS0649
    }
}
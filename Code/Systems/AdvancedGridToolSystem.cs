using System;
using System.Collections.Generic;
using Colossal.Logging;
using Colossal.Mathematics;
using Game;
using Game.Audio;
using Game.City;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Colossal.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using AdvancedGridTool.Components;
using static Game.Rendering.GuideLinesSystem;

namespace AdvancedGridTool
{
    /// <summary>
    /// Advanced Grid Tool - Works like in-game grid tool but with advanced patterns
    /// Provides guidelines and snapping, lets NetToolSystem handle actual road placement
    /// </summary>
    public sealed partial class AdvancedGridToolSystem : ObjectToolBaseSystem
    {
        // Constants
        public const string kToolID = "Advanced Grid Tool";
        private const float kMinSpacing = 10f;
        private const float kMaxSpacing = 200f;
        private const float kSpacingStep = 1f;

        // Enums
        public enum GridMode
        {
            Standard,
            Organic,
            Suburban,
            Hexagonal,
            Curved,
            TerrainAdaptive
        }

        // Private fields
        private static ILog _log;
        private GridMode _currentMode = GridMode.Standard;
        private float3 _startPosition;
        private float3 _endPosition;
        private bool _hasStartPosition;
        private bool _hasEndPosition;
        private float _spacing = 50f;
        private int2 _gridDimensions = new int2(5, 5);
        private PrefabBase _currentPrefab;
        private Entity _selectedPrefab = Entity.Null;
        private ToolBaseSystem _previousTool;

        // Grid generation data
        private List<float3> _gridPoints = new List<float3>();
        private List<GridLine> _gridLines = new List<GridLine>();

        // System references (following Line Tool pattern)
        private TerrainSystem _terrainSystem;
        private OverlayRenderSystem.Buffer _overlayBuffer;
        private AudioManager _audioManager;
        private NetToolSystem _netToolSystem;

        // Queries
        private EntityQuery _renderingSettingsQuery;
        private EntityQuery _soundEffectsQuery;

        // Properties
        public static AdvancedGridToolSystem Instance { get; private set; }

        public override string toolID => kToolID;

        public GridMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    RegenerateGrid();
                    _log.Info($"Switched to {_currentMode} mode");
                }
            }
        }

        public float Spacing
        {
            get => _spacing;
            set
            {
                float newSpacing = math.clamp(value, kMinSpacing, kMaxSpacing);
                if (math.abs(_spacing - newSpacing) > 0.01f)
                {
                    _spacing = newSpacing;
                    RegenerateGrid();
                }
            }
        }

        public int2 GridDimensions
        {
            get => _gridDimensions;
            set
            {
                _gridDimensions = math.max(value, new int2(1, 1));
                RegenerateGrid();
            }
        }

        protected override void OnCreate()
        {
            Instance = this;
            base.OnCreate();

            _log = LogManager.GetLogger($"{nameof(AdvancedGridTool)}.{nameof(AdvancedGridToolSystem)}").SetShowsErrorsInUI(false);
            _log.Info("Creating Advanced Grid Tool System (Guide Mode)");

            // Tool list management (like Line Tool)
            List<ToolBaseSystem> toolList = World.GetOrCreateSystemManaged<ToolSystem>().tools;
            ToolBaseSystem thisSystem = null;
            foreach (ToolBaseSystem tool in toolList)
            {
                if (tool == this)
                {
                    thisSystem = tool;
                    break;
                }
            }

            if (thisSystem != null)
            {
                toolList.Remove(this);
            }

            // Insert at position (after Tree Controller if present)
            if (toolList.Count > 0 && toolList[0].toolID.Equals("Tree Controller Tool"))
            {
                toolList.Insert(1, this);
            }
            else
            {
                toolList.Insert(0, this);
            }

            // Get system references
            _terrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            _overlayBuffer = World.GetOrCreateSystemManaged<OverlayRenderSystem>().GetBuffer(out var _);
            _audioManager = World.GetOrCreateSystemManaged<AudioManager>();
            _netToolSystem = World.GetOrCreateSystemManaged<NetToolSystem>();

            // Setup queries
            _renderingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<GuideLineSettingsData>());
            _soundEffectsQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());

            _log.Info("Advanced Grid Tool System created successfully (Guide Mode)");
        }

        protected override void OnDestroy()
        {
            Instance = null;
            base.OnDestroy();
            _log.Info("Advanced Grid Tool System destroyed");
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Net;
        }

        public override PrefabBase GetPrefab()
        {
            return _currentPrefab;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            // CRITICAL: Only accept prefab if tool is already active
            if (m_ToolSystem.activeTool == this && prefab is RoadPrefab roadPrefab)
            {
                _currentPrefab = prefab;
                _selectedPrefab = m_PrefabSystem.GetEntity(prefab);
                _log.Info($"Set prefab: {prefab.name}");
                return true;
            }

            // Store prefab for later use
            if (prefab is RoadPrefab)
            {
                _currentPrefab = prefab;
                _selectedPrefab = m_PrefabSystem.GetEntity(prefab);
            }

            return false;
        }

        internal void EnableTool()
        {
            if (m_ToolSystem.activeTool != this)
            {
                _previousTool = m_ToolSystem.activeTool;
                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = this;
                _log.Info("Advanced Grid Tool activated (Guide Mode)");
            }
            else
            {
                RestorePreviousTool();
            }
        }

        internal void RestorePreviousTool()
        {
            ClearGrid();
            if (_previousTool != null)
            {
                m_ToolSystem.activeTool = _previousTool;
                _log.Info("Restored previous tool");
            }
        }

        public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
        {
            base.GetAvailableSnapMask(out onMask, out offMask);
            // Enable snapping to our grid points
            onMask |= Snap.NetNode | Snap.NetMiddle | Snap.ContourLines | Snap.GuideLines;
            offMask = Snap.None;
        }

        public override void ElevationUp()
        {
            Spacing += kSpacingStep;
        }

        public override void ElevationDown()
        {
            Spacing -= kSpacingStep;
        }

        protected override void OnStartRunning()
        {
            _log.Info("OnStartRunning");
            base.OnStartRunning();
            applyAction.shouldBeEnabled = true;
            cancelAction.shouldBeEnabled = true;
        }

        protected override void OnStopRunning()
        {
            _log.Info("OnStopRunning");
            ClearGrid();
            base.OnStopRunning();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Handle input
            HandleInput();

            // Render overlay guidelines
            RenderGridOverlay();

            return inputDeps;
        }

        private void HandleInput()
        {
            ControlPoint controlPoint;
            if (!GetRaycastResult(out controlPoint))
                return;

            // Apply action - set grid corners
            if (applyAction.WasPressedThisFrame())
            {
                if (!_hasStartPosition)
                {
                    _startPosition = controlPoint.m_Position;
                    _hasStartPosition = true;
                    PlaySound(true);
                    _log.Info($"Set start position: {_startPosition}");
                }
                else if (!_hasEndPosition)
                {
                    _endPosition = controlPoint.m_Position;
                    _hasEndPosition = true;
                    GenerateGrid();
                    PlaySound(false);
                    _log.Info($"Set end position: {_endPosition}, grid generated");
                }
                else
                {
                    // Reset to start new grid
                    ClearGrid();
                    _startPosition = controlPoint.m_Position;
                    _hasStartPosition = true;
                    _log.Info("Reset and set new start position");
                }
            }

            // Cancel action
            if (cancelAction.WasPressedThisFrame())
            {
                if (_hasStartPosition || _hasEndPosition)
                {
                    ClearGrid();
                    _log.Info("Grid cleared");
                }
                else
                {
                    RestorePreviousTool();
                }
            }

            // Update end position preview while dragging
            if (_hasStartPosition && !_hasEndPosition)
            {
                _endPosition = controlPoint.m_Position;
                GenerateGrid();
            }
        }

        private void GenerateGrid()
        {
            _gridPoints.Clear();
            _gridLines.Clear();

            switch (_currentMode)
            {
                case GridMode.Standard:
                    GenerateStandardGrid();
                    break;
                case GridMode.Organic:
                case GridMode.Suburban:
                case GridMode.Hexagonal:
                case GridMode.Curved:
                case GridMode.TerrainAdaptive:
                    _log.Info($"{_currentMode} grid generation not yet implemented");
                    GenerateStandardGrid(); // Fallback
                    break;
            }

            _log.Info($"Generated {_gridPoints.Count} grid points and {_gridLines.Count} grid lines");
        }

        private void GenerateStandardGrid()
        {
            float3 direction = math.normalize(_endPosition - _startPosition);
            float3 perpendicular = new float3(-direction.z, 0, direction.x);

            TerrainHeightData heightData = _terrainSystem.GetHeightData();

            for (int x = 0; x <= _gridDimensions.x; x++)
            {
                for (int y = 0; y <= _gridDimensions.y; y++)
                {
                    float3 point = _startPosition +
                                  direction * (x * _spacing) +
                                  perpendicular * (y * _spacing);

                    point.y = TerrainUtils.SampleHeight(ref heightData, point);
                    _gridPoints.Add(point);
                }
            }

            // Generate grid lines
            int cols = _gridDimensions.x + 1;
            int rows = _gridDimensions.y + 1;

            // Horizontal lines
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols - 1; x++)
                {
                    _gridLines.Add(new GridLine
                    {
                        Start = _gridPoints[x * rows + y],
                        End = _gridPoints[(x + 1) * rows + y],
                        IsPrimary = (y % 2 == 0)
                    });
                }
            }

            // Vertical lines
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows - 1; y++)
                {
                    _gridLines.Add(new GridLine
                    {
                        Start = _gridPoints[x * rows + y],
                        End = _gridPoints[x * rows + (y + 1)],
                        IsPrimary = (x % 2 == 0)
                    });
                }
            }
        }

        private void RegenerateGrid()
        {
            if (_hasStartPosition && _hasEndPosition)
            {
                GenerateGrid();
            }
        }

        private void RenderGridOverlay()
        {
            if (!_hasStartPosition)
                return;

            GuideLineSettingsData guideLineSettings = _renderingSettingsQuery.GetSingleton<GuideLineSettingsData>();

            // Draw selection box
            if (_hasStartPosition)
            {
                UnityEngine.Color boxColor = _hasEndPosition ?
                    guideLineSettings.m_HighPriorityColor :
                    guideLineSettings.m_LowPriorityColor;

                // Draw box corners
                _overlayBuffer.DrawCircle(
                    guideLineSettings.m_HighPriorityColor,
                    guideLineSettings.m_MediumPriorityColor,
                    0.5f, 0, new float2(0, 1), _startPosition, 3f);

                if (_hasStartPosition)
                {
                    _overlayBuffer.DrawCircle(
                        boxColor,
                        guideLineSettings.m_MediumPriorityColor,
                        0.5f, 0, new float2(0, 1), _endPosition, 3f);

                    // Draw box outline
                    float3 corner1 = _startPosition;
                    float3 corner2 = new float3(_endPosition.x, _startPosition.y, _startPosition.z);
                    float3 corner3 = new float3(_endPosition.x, _startPosition.y, _endPosition.z);
                    float3 corner4 = new float3(_startPosition.x, _startPosition.y, _endPosition.z);

                    _overlayBuffer.DrawLine(boxColor, new Line3.Segment(corner1, corner2), 1f);
                    _overlayBuffer.DrawLine(boxColor, new Line3.Segment(corner2, corner3), 1f);
                    _overlayBuffer.DrawLine(boxColor, new Line3.Segment(corner3, corner4), 1f);
                    _overlayBuffer.DrawLine(boxColor, new Line3.Segment(corner4, corner1), 1f);
                }
            }

            // Draw grid lines
            foreach (var line in _gridLines)
            {
                UnityEngine.Color lineColor = line.IsPrimary ?
                    guideLineSettings.m_HighPriorityColor :
                    guideLineSettings.m_MediumPriorityColor;

                _overlayBuffer.DrawLine(lineColor, new Line3.Segment(line.Start, line.End), line.IsPrimary ? 2f : 1f);
            }

            // Draw grid intersection points
            foreach (var point in _gridPoints)
            {
                _overlayBuffer.DrawCircle(
                    guideLineSettings.m_HighPriorityColor,
                    guideLineSettings.m_MediumPriorityColor,
                    0.5f, 0, new float2(0, 1), point, 1.5f);
            }
        }

        private void ClearGrid()
        {
            _hasStartPosition = false;
            _hasEndPosition = false;
            _gridPoints.Clear();
            _gridLines.Clear();
        }

        private void PlaySound(bool start)
        {
            if (!_soundEffectsQuery.IsEmpty)
            {
                var soundSettings = _soundEffectsQuery.GetSingleton<ToolUXSoundSettingsData>();
                _audioManager.PlayUISound(start ? soundSettings.m_NetStartSound : soundSettings.m_NetCancelSound);
            }
        }

        public struct GridLine
        {
            public float3 Start;
            public float3 End;
            public bool IsPrimary;
        }
    }
}

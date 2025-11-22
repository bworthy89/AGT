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
    /// Main tool system for Advanced Grid Tool extending ObjectToolBaseSystem (like Line Tool)
    /// </summary>
    public sealed partial class AdvancedGridToolSystem : ObjectToolBaseSystem
    {
        // Constants
        public const string kToolID = "Advanced Grid Tool";
        private const float kMinSpacing = 10f;
        private const float kMaxSpacing = 200f;
        private const float kSpacingStep = 10f;

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

        public enum ToolState
        {
            Idle,
            SelectingStart,
            SelectingEnd,
            PreviewingGrid,
            Applying
        }

        // Private fields
        private static ILog _log;
        private GridMode _currentMode = GridMode.Standard;
        private ToolState _toolState = ToolState.Idle;
        private float3 _startPosition;
        private float3 _endPosition;
        private float3 _currentPosition;
        private quaternion _rotation = quaternion.identity;
        private float _spacing = 50f;
        private float2 _blockSize = new float2(100f, 100f);
        private int2 _gridDimensions = new int2(5, 5);
        private bool _fixedPreview = false;
        private PrefabBase _currentPrefab;
        private Entity _selectedPrefab = Entity.Null;
        private ToolBaseSystem _previousTool;

        // Grid generation data
        private NativeList<float3> _gridPoints;
        private NativeList<GridConnection> _gridConnections;
        private NativeList<Entity> _previewEntities;

        // System references (following Line Tool pattern)
        private TerrainSystem _terrainSystem;
        private OverlayRenderSystem _overlayRenderSystem;
        private OverlayRenderSystem.Buffer _overlayBuffer;
        private AudioManager _audioManager;
        private CityConfigurationSystem _cityConfigurationSystem;
        private ToolRaycastSystem _toolRaycastSystem;

        // Input actions (following Line Tool pattern)
        private InputAction _applyAction;
        private InputAction _cancelAction;
        private InputAction _fixedPreviewAction;

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
                    UpdateGridMode();
                }
            }
        }

        public float Spacing
        {
            get => _spacing;
            set => _spacing = math.clamp(value, kMinSpacing, kMaxSpacing);
        }

        public float2 BlockSize
        {
            get => _blockSize;
            set => _blockSize = math.max(value, new float2(kMinSpacing, kMinSpacing));
        }

        public int2 GridDimensions
        {
            get => _gridDimensions;
            set => _gridDimensions = math.max(value, new int2(1, 1));
        }

        protected override void OnCreate()
        {
            // Set instance reference (following Line Tool pattern)
            Instance = this;

            base.OnCreate();

            _log = LogManager.GetLogger($"{nameof(AdvancedGridTool)}.{nameof(AdvancedGridToolSystem)}").SetShowsErrorsInUI(false);
            _log.Info("Creating Advanced Grid Tool System");

            // Try to find Advanced Grid Tool reference from tool system tool list (Line Tool pattern)
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

            // Remove existing tool reference if found
            if (thisSystem != null)
            {
                toolList.Remove(this);
            }

            // Insert at position (after Tree Controller if present, like Line Tool)
            if (toolList.Count > 0 && toolList[0].toolID.Equals("Tree Controller Tool"))
            {
                toolList.Insert(1, this);
            }
            else
            {
                toolList.Insert(0, this);
            }

            // Get system references (following Line Tool pattern)
            _terrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            _overlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            _overlayBuffer = _overlayRenderSystem.GetBuffer(out var _);
            _cityConfigurationSystem = World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            _audioManager = World.GetOrCreateSystemManaged<AudioManager>();
            _toolRaycastSystem = World.GetOrCreateSystemManaged<ToolRaycastSystem>();

            // Initialize collections
            _gridPoints = new NativeList<float3>(Allocator.Persistent);
            _gridConnections = new NativeList<GridConnection>(Allocator.Persistent);
            _previewEntities = new NativeList<Entity>(Allocator.Persistent);

            // Setup queries
            _renderingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<GuideLineSettingsData>());
            _soundEffectsQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());

            // Setup input actions (Line Tool pattern)
            SetupInputActions();

            // Make sure the tool doesn't start active
            Enabled = false;

            _log.Info("Advanced Grid Tool System created successfully");
        }

        protected override void OnDestroy()
        {
            Instance = null;

            // Dispose native collections
            if (_gridPoints.IsCreated) _gridPoints.Dispose();
            if (_gridConnections.IsCreated) _gridConnections.Dispose();
            if (_previewEntities.IsCreated) _previewEntities.Dispose();

            // Cleanup input actions
            _applyAction?.Dispose();
            _cancelAction?.Dispose();
            _fixedPreviewAction?.Dispose();

            base.OnDestroy();
            _log.Info("Advanced Grid Tool System destroyed");
        }

        private void SetupInputActions()
        {
            // Fixed preview control (Ctrl+Click like Line Tool)
            _fixedPreviewAction = new InputAction("AdvancedGridTool-FixedPreview");
            _fixedPreviewAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("Modifier", "<Keyboard>/ctrl")
                .With("Button", "<Mouse>/leftButton");
            _fixedPreviewAction.Enable();

            // Standard apply action
            _applyAction = new InputAction("AdvancedGridTool-Apply");
            _applyAction.AddBinding("<Mouse>/leftButton");
            _applyAction.Enable();

            // Cancel action
            _cancelAction = new InputAction("AdvancedGridTool-Cancel");
            _cancelAction.AddBinding("<Mouse>/rightButton");
            _cancelAction.AddBinding("<Keyboard>/escape");
            _cancelAction.Enable();
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            // Configure raycast for terrain and networks
            m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Net;
            m_ToolRaycastSystem.netLayerMask = Layer.Road | Layer.Pathway;
            m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
        }

        public override PrefabBase GetPrefab()
        {
            return _currentPrefab;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            // CRITICAL: Only accept prefab if tool is already active (like Line Tool)
            // This prevents auto-activation when selecting a road
            if (m_ToolSystem.activeTool == this && prefab is RoadPrefab roadPrefab)
            {
                _currentPrefab = prefab;
                _selectedPrefab = m_PrefabSystem.GetEntity(prefab);
                _log.Info($"Set prefab: {prefab.name}");
                return true;
            }

            // Store prefab for later use when tool is activated via Ctrl+G
            if (prefab is RoadPrefab storedPrefab)
            {
                _currentPrefab = prefab;
                _selectedPrefab = m_PrefabSystem.GetEntity(prefab);
                _log.Info($"Stored prefab for later use: {prefab.name}");
            }

            return false;
        }

        // Enable/Disable tool methods (like Line Tool)
        internal void EnableTool()
        {
            _log.Info($"EnableTool called. Current active tool: {m_ToolSystem.activeTool?.toolID}, This tool: {this.toolID}");

            // Activate this tool if it isn't already active
            if (m_ToolSystem.activeTool != this)
            {
                _previousTool = m_ToolSystem.activeTool;
                _log.Info($"Storing previous tool: {_previousTool?.toolID}");

                // For now, just activate the tool without checking for prefab
                // This can be enhanced later to use the selected network prefab
                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = this;
                _log.Info($"Advanced Grid Tool activated. New active tool: {m_ToolSystem.activeTool?.toolID}");
            }
            else
            {
                // Tool already active - deactivate
                _log.Info("Tool already active, deactivating");
                RestorePreviousTool();
            }
        }

        internal void RestorePreviousTool()
        {
            _toolState = ToolState.Idle;
            ClearGridPreview();

            if (_previousTool != null)
            {
                m_ToolSystem.activeTool = _previousTool;
                _log.Info("Restored previous tool");
            }
        }

        public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
        {
            base.GetAvailableSnapMask(out onMask, out offMask);
            onMask |= Snap.NetMiddle | Snap.NetNode | Snap.ContourLines;
            offMask = Snap.None;
        }

        // Elevation controls (for spacing adjustment)
        public override void ElevationUp()
        {
            Spacing += kSpacingStep;
            _log.Info($"Spacing increased to {Spacing}");
        }

        public override void ElevationDown()
        {
            Spacing -= kSpacingStep;
            _log.Info($"Spacing decreased to {Spacing}");
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Handle input
            HandleInput();

            // Update grid preview based on current state
            UpdateGridPreview();

            // Render overlay
            RenderGridOverlay();

            // Apply grid if requested
            if (_toolState == ToolState.Applying)
            {
                inputDeps = ApplyGrid(inputDeps);
                _toolState = ToolState.Idle;
            }

            return inputDeps;
        }

        private void HandleInput()
        {
            // Get current raycast hit
            ControlPoint controlPoint;
            if (GetRaycastResult(out controlPoint))
            {
                _currentPosition = controlPoint.m_Position;

                // Handle fixed preview toggle
                if (_fixedPreviewAction.WasPressedThisFrame())
                {
                    _fixedPreview = !_fixedPreview;
                    _log.Info($"Fixed preview: {_fixedPreview}");
                }

                // Handle apply action
                if (_applyAction.WasPressedThisFrame() && !_fixedPreviewAction.WasPressedThisFrame())
                {
                    switch (_toolState)
                    {
                        case ToolState.Idle:
                        case ToolState.SelectingStart:
                            _startPosition = _currentPosition;
                            _toolState = ToolState.SelectingEnd;
                            PlaySound(true);
                            break;

                        case ToolState.SelectingEnd:
                            _endPosition = _currentPosition;
                            _toolState = ToolState.PreviewingGrid;
                            GenerateGridPreview();
                            break;

                        case ToolState.PreviewingGrid:
                            _toolState = ToolState.Applying;
                            break;
                    }
                }

                // Handle cancel action
                if (_cancelAction.WasPressedThisFrame())
                {
                    CancelCurrentOperation();
                }
            }
        }

        private void UpdateGridMode()
        {
            // Clear current preview
            ClearGridPreview();

            // Update generation parameters based on mode
            _log.Info($"Switched to {_currentMode} mode");
        }

        private void GenerateGridPreview()
        {
            ClearGridPreview();

            // Generate grid based on current mode
            switch (_currentMode)
            {
                case GridMode.Standard:
                    GenerateStandardGrid();
                    break;
                case GridMode.Organic:
                    GenerateOrganicGrid();
                    break;
                case GridMode.Suburban:
                    GenerateSuburbanGrid();
                    break;
                case GridMode.Hexagonal:
                    GenerateHexagonalGrid();
                    break;
                case GridMode.Curved:
                    GenerateCurvedGrid();
                    break;
                case GridMode.TerrainAdaptive:
                    GenerateTerrainAdaptiveGrid();
                    break;
            }

            _log.Info($"Generated {_gridPoints.Length} grid points with {_gridConnections.Length} connections");
        }

        private void GenerateStandardGrid()
        {
            float3 direction = math.normalize(_endPosition - _startPosition);
            float3 perpendicular = new float3(-direction.z, 0, direction.x);

            for (int x = 0; x < _gridDimensions.x; x++)
            {
                for (int y = 0; y < _gridDimensions.y; y++)
                {
                    float3 point = _startPosition +
                                  direction * (x * _spacing) +
                                  perpendicular * (y * _spacing);

                    // Sample terrain height at this position
                    if (_terrainSystem != null)
                    {
                        TerrainHeightData heightData = _terrainSystem.GetHeightData();
                        point.y = TerrainUtils.SampleHeight(ref heightData, point);
                    }

                    _gridPoints.Add(point);

                    // Add horizontal connection
                    if (x > 0)
                    {
                        _gridConnections.Add(new GridConnection
                        {
                            StartIndex = (x - 1) * _gridDimensions.y + y,
                            EndIndex = x * _gridDimensions.y + y,
                            Type = ConnectionType.Primary
                        });
                    }

                    // Add vertical connection
                    if (y > 0)
                    {
                        _gridConnections.Add(new GridConnection
                        {
                            StartIndex = x * _gridDimensions.y + (y - 1),
                            EndIndex = x * _gridDimensions.y + y,
                            Type = ConnectionType.Secondary
                        });
                    }
                }
            }
        }

        private void GenerateOrganicGrid()
        {
            _log.Info("Organic grid generation not yet implemented");
        }

        private void GenerateSuburbanGrid()
        {
            _log.Info("Suburban grid generation not yet implemented");
        }

        private void GenerateHexagonalGrid()
        {
            _log.Info("Hexagonal grid generation not yet implemented");
        }

        private void GenerateCurvedGrid()
        {
            _log.Info("Curved grid generation not yet implemented");
        }

        private void GenerateTerrainAdaptiveGrid()
        {
            _log.Info("Terrain adaptive grid generation not yet implemented");
        }

        private void UpdateGridPreview()
        {
            if (_toolState == ToolState.SelectingEnd && !_fixedPreview)
            {
                _endPosition = _currentPosition;
                GenerateGridPreview();
            }
        }

        private void RenderGridOverlay()
        {
            if (_gridPoints.Length == 0) return;

            // Get guideline settings (like Line Tool)
            GuideLineSettingsData guideLineSettings = _renderingSettingsQuery.GetSingleton<GuideLineSettingsData>();

            // Draw grid points
            foreach (var point in _gridPoints)
            {
                _overlayBuffer.DrawCircle(guideLineSettings.m_HighPriorityColor, guideLineSettings.m_MediumPriorityColor, 0.5f, 0, new float2(0, 1), point, 2f);
            }

            // Draw grid connections (using Line3.Segment like Line Tool)
            foreach (var connection in _gridConnections)
            {
                if (connection.StartIndex < _gridPoints.Length &&
                    connection.EndIndex < _gridPoints.Length)
                {
                    _overlayBuffer.DrawLine(guideLineSettings.m_HighPriorityColor,
                        new Line3.Segment(_gridPoints[connection.StartIndex], _gridPoints[connection.EndIndex]),
                        1f);
                }
            }

            // Draw selection box
            if (_toolState == ToolState.SelectingEnd || _toolState == ToolState.PreviewingGrid)
            {
                UnityEngine.Color selectionColor = guideLineSettings.m_LowPriorityColor;

                // Draw the four sides of the selection box
                _overlayBuffer.DrawLine(selectionColor,
                    new Line3.Segment(_startPosition, new float3(_endPosition.x, _startPosition.y, _startPosition.z)),
                    1f);
                _overlayBuffer.DrawLine(selectionColor,
                    new Line3.Segment(new float3(_endPosition.x, _startPosition.y, _startPosition.z), new float3(_endPosition.x, _startPosition.y, _endPosition.z)),
                    1f);
                _overlayBuffer.DrawLine(selectionColor,
                    new Line3.Segment(new float3(_endPosition.x, _startPosition.y, _endPosition.z), new float3(_startPosition.x, _startPosition.y, _endPosition.z)),
                    1f);
                _overlayBuffer.DrawLine(selectionColor,
                    new Line3.Segment(new float3(_startPosition.x, _startPosition.y, _endPosition.z), _startPosition),
                    1f);
            }
        }

        private JobHandle ApplyGrid(JobHandle inputDeps)
        {
            _log.Info($"Applying grid with {_gridPoints.Length} points and {_gridConnections.Length} connections");

            // Check if we have a valid road prefab
            if (_currentPrefab == null || _selectedPrefab == Entity.Null)
            {
                _log.Error("No road prefab selected");
                ClearGridPreview();
                return inputDeps;
            }

            // Create roads from grid connections
            EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Random.Range(1, int.MaxValue));

            // Create road segments for each connection
            foreach (var connection in _gridConnections)
            {
                if (connection.StartIndex >= 0 && connection.StartIndex < _gridPoints.Length &&
                    connection.EndIndex >= 0 && connection.EndIndex < _gridPoints.Length)
                {
                    float3 startPos = _gridPoints[connection.StartIndex];
                    float3 endPos = _gridPoints[connection.EndIndex];

                    // Create the road segment entity
                    Entity roadEntity = commandBuffer.CreateEntity();

                    // Add CreationDefinition component (following NetToolSystem pattern)
                    CreationDefinition creationDef = new CreationDefinition
                    {
                        m_Prefab = _selectedPrefab,
                        m_SubPrefab = Entity.Null, // Will be filled by the game systems if needed
                        m_RandomSeed = random.NextInt(),
                        m_Flags = CreationFlags.SubElevation // Allow elevation handling
                    };
                    commandBuffer.AddComponent(roadEntity, creationDef);

                    // Add NetCourse component to define the path
                    NetCourse netCourse = new NetCourse
                    {
                        // Create a straight line curve between the two points
                        m_Curve = NetUtils.StraightCurve(startPos, endPos),
                        m_Length = math.distance(startPos, endPos),
                        m_Elevation = new float2(startPos.y, endPos.y),
                        m_FixedIndex = -1
                    };

                    // Calculate direction for rotation
                    float3 direction = math.normalizesafe(endPos - startPos);
                    quaternion rotation = quaternion.LookRotationSafe(direction, math.up());

                    // Set start position details
                    netCourse.m_StartPosition = new CoursePos
                    {
                        m_Position = startPos,
                        m_Elevation = new float2(startPos.y, startPos.y),
                        m_Rotation = rotation,
                        m_CourseDelta = 0f,
                        m_Flags = CoursePosFlags.IsGrid | CoursePosFlags.FreeHeight,
                        m_Entity = Entity.Null,
                        m_SplitPosition = 0f,
                        m_ParentMesh = -1
                    };

                    // Set end position details
                    netCourse.m_EndPosition = new CoursePos
                    {
                        m_Position = endPos,
                        m_Elevation = new float2(endPos.y, endPos.y),
                        m_Rotation = rotation,
                        m_CourseDelta = 1f,
                        m_Flags = CoursePosFlags.IsGrid | CoursePosFlags.FreeHeight,
                        m_Entity = Entity.Null,
                        m_SplitPosition = 1f,
                        m_ParentMesh = -1
                    };

                    // Mark as primary or secondary based on connection type
                    if (connection.Type == ConnectionType.Primary)
                    {
                        netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsGrid;
                        netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsGrid;
                    }
                    else
                    {
                        netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsParallel;
                        netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsParallel;
                    }

                    commandBuffer.AddComponent(roadEntity, netCourse);

                    // Add Updated component to mark for processing
                    commandBuffer.AddComponent(roadEntity, default(Updated));

                    _log.Info($"Created road segment from ({startPos.x:F1}, {startPos.z:F1}) to ({endPos.x:F1}, {endPos.z:F1})");
                }
            }

            // Execute the command buffer
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            // Clear the preview
            ClearGridPreview();
            PlaySound(false);

            _log.Info($"Grid applied - created {_gridConnections.Length} road segments");

            return inputDeps;
        }

        private void ClearGridPreview()
        {
            _gridPoints.Clear();
            _gridConnections.Clear();

            // Clear preview entities if any
            if (_previewEntities.Length > 0)
            {
                EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
                foreach (var entity in _previewEntities)
                {
                    commandBuffer.DestroyEntity(entity);
                }
                commandBuffer.Playback(EntityManager);
                commandBuffer.Dispose();
                _previewEntities.Clear();
            }
        }

        private void CancelCurrentOperation()
        {
            _toolState = ToolState.Idle;
            ClearGridPreview();
            _fixedPreview = false;
            PlaySound(false);
            _log.Info("Operation cancelled");
        }

        private void PlaySound(bool start)
        {
            if (!_soundEffectsQuery.IsEmpty)
            {
                var soundSettings = _soundEffectsQuery.GetSingleton<ToolUXSoundSettingsData>();
                _audioManager.PlayUISound(start ? soundSettings.m_NetStartSound : soundSettings.m_NetCancelSound);
            }
        }

        // Support structure for grid connections
        public struct GridConnection
        {
            public int StartIndex;
            public int EndIndex;
            public ConnectionType Type;
        }

        public enum ConnectionType
        {
            Primary,
            Secondary,
            Intersection
        }
    }
}
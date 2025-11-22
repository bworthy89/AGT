using Unity.Entities;
using Unity.Mathematics;

namespace AdvancedGridTool.Components
{
    /// <summary>
    /// Represents a node/intersection point in the grid
    /// </summary>
    public struct GridNode : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public NodeType Type;
        public GridValidationFlags ValidationFlags;
        public int Index;
    }

    /// <summary>
    /// Represents a connection between two grid nodes
    /// </summary>
    public struct GridSegment : IBufferElementData
    {
        public int StartNodeIndex;
        public int EndNodeIndex;
        public SegmentType Type;
        public float Length;
        public bool IsValid;
    }

    /// <summary>
    /// Configuration for grid generation
    /// </summary>
    public struct GridConfiguration : IComponentData
    {
        public float3 Origin;
        public quaternion Rotation;
        public float2 Spacing;
        public float2 BlockSize;
        public int2 Dimensions;
        public GridPattern Pattern;
        public float MaxSlope;
        public float WaterAvoidanceDistance;
        public bool SnapToTerrain;
        public bool AvoidObstacles;
    }

    /// <summary>
    /// Grid validation results
    /// </summary>
    public struct GridValidation : IComponentData
    {
        public bool IsValid;
        public int InvalidNodeCount;
        public int InvalidSegmentCount;
        public ValidationErrorType ErrorType;
    }

    // Enumerations

    public enum NodeType : byte
    {
        Standard,
        MajorIntersection,
        MinorIntersection,
        Terminal,
        CulDeSac,
        Roundabout
    }

    public enum SegmentType : byte
    {
        Primary,
        Secondary,
        Tertiary,
        Collector,
        Local,
        Pathway
    }

    public enum GridPattern : byte
    {
        Standard,
        Organic,
        Suburban,
        Hexagonal,
        Curved,
        TerrainAdaptive
    }

    [System.Flags]
    public enum GridValidationFlags : byte
    {
        None = 0,
        ValidPosition = 1 << 0,
        ValidSlope = 1 << 1,
        NoWaterConflict = 1 << 2,
        NoObstacleConflict = 1 << 3,
        Connected = 1 << 4,
        All = ValidPosition | ValidSlope | NoWaterConflict | NoObstacleConflict | Connected
    }

    public enum ValidationErrorType : byte
    {
        None,
        TooSteep,
        InWater,
        Obstructed,
        Disconnected,
        TooClose,
        OutOfBounds
    }
}
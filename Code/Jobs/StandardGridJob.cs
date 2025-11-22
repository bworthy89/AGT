using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Game.Simulation;
using AdvancedGridTool.Components;

namespace AdvancedGridTool.Jobs
{
    /// <summary>
    /// Burst-compiled job for generating standard rectangular grid patterns
    /// </summary>
    [BurstCompile]
    public struct StandardGridJob : IJobParallelFor
    {
        // Input parameters
        [ReadOnly] public float3 Origin;
        [ReadOnly] public float3 EndPoint;
        [ReadOnly] public quaternion Rotation;
        [ReadOnly] public float2 Spacing;
        [ReadOnly] public int2 Dimensions;
        [ReadOnly] public float2 BlockSize;
        [ReadOnly] public int2 MajorAxisInterval;
        [ReadOnly] public bool GenerateAxes;

        // Terrain data (optional)
        [ReadOnly] public bool UseTerrainHeight;
        [ReadOnly] public TerrainHeightData TerrainData;

        // Output arrays
        [NativeDisableParallelForRestriction]
        public NativeArray<GridNode> Nodes;

        [NativeDisableParallelForRestriction]
        public NativeArray<GridSegment> HorizontalSegments;

        [NativeDisableParallelForRestriction]
        public NativeArray<GridSegment> VerticalSegments;

        public void Execute(int index)
        {
            // Calculate grid position from linear index
            int row = index / Dimensions.x;
            int col = index % Dimensions.x;

            // Skip if out of bounds
            if (row >= Dimensions.y || col >= Dimensions.x)
                return;

            // Calculate local position
            float2 localPos = new float2(col * Spacing.x, row * Spacing.y);

            // Apply rotation to get world position
            float3 rotatedPos = math.mul(Rotation, new float3(localPos.x, 0, localPos.y));
            float3 worldPos = Origin + rotatedPos;

            // Sample terrain height if enabled
            if (UseTerrainHeight)
            {
                float height = SampleTerrainHeight(worldPos);
                worldPos.y = height;
            }

            // Determine node type based on axis intervals
            NodeType nodeType = DetermineNodeType(row, col);

            // Create and store the node
            Nodes[index] = new GridNode
            {
                Position = worldPos,
                Rotation = Rotation,
                Type = nodeType,
                ValidationFlags = GridValidationFlags.ValidPosition | GridValidationFlags.Connected,
                Index = index
            };

            // Generate segments to neighboring nodes
            GenerateSegments(index, row, col);
        }

        private NodeType DetermineNodeType(int row, int col)
        {
            bool isMajorX = MajorAxisInterval.x > 0 && (col % MajorAxisInterval.x) == 0;
            bool isMajorY = MajorAxisInterval.y > 0 && (row % MajorAxisInterval.y) == 0;

            if (isMajorX && isMajorY)
                return NodeType.MajorIntersection;
            else if (isMajorX || isMajorY)
                return NodeType.MinorIntersection;
            else
                return NodeType.Standard;
        }

        private void GenerateSegments(int nodeIndex, int row, int col)
        {
            // Generate horizontal segment to the right
            if (col < Dimensions.x - 1)
            {
                int rightNodeIndex = nodeIndex + 1;
                int segmentIndex = row * (Dimensions.x - 1) + col;

                if (segmentIndex >= 0 && segmentIndex < HorizontalSegments.Length)
                {
                    float3 startPos = Nodes[nodeIndex].Position;
                    float3 endPos = Nodes[rightNodeIndex].Position;
                    float length = math.distance(startPos, endPos);

                    SegmentType segmentType = DetermineSegmentType(row, col, true);

                    HorizontalSegments[segmentIndex] = new GridSegment
                    {
                        StartNodeIndex = nodeIndex,
                        EndNodeIndex = rightNodeIndex,
                        Type = segmentType,
                        Length = length,
                        IsValid = true
                    };
                }
            }

            // Generate vertical segment upward
            if (row < Dimensions.y - 1)
            {
                int upNodeIndex = nodeIndex + Dimensions.x;
                int segmentIndex = col * (Dimensions.y - 1) + row;

                if (segmentIndex >= 0 && segmentIndex < VerticalSegments.Length)
                {
                    float3 startPos = Nodes[nodeIndex].Position;
                    float3 endPos = Nodes[upNodeIndex].Position;
                    float length = math.distance(startPos, endPos);

                    SegmentType segmentType = DetermineSegmentType(row, col, false);

                    VerticalSegments[segmentIndex] = new GridSegment
                    {
                        StartNodeIndex = nodeIndex,
                        EndNodeIndex = upNodeIndex,
                        Type = segmentType,
                        Length = length,
                        IsValid = true
                    };
                }
            }
        }

        private SegmentType DetermineSegmentType(int row, int col, bool isHorizontal)
        {
            if (isHorizontal)
            {
                bool isMajor = MajorAxisInterval.y > 0 && (row % MajorAxisInterval.y) == 0;
                return isMajor ? SegmentType.Primary : SegmentType.Secondary;
            }
            else
            {
                bool isMajor = MajorAxisInterval.x > 0 && (col % MajorAxisInterval.x) == 0;
                return isMajor ? SegmentType.Primary : SegmentType.Secondary;
            }
        }

        private float SampleTerrainHeight(float3 position)
        {
            // This is a simplified terrain sampling
            // In the actual implementation, you would use the TerrainSystem API
            // For now, return the original Y value
            return position.y;
        }
    }

    /// <summary>
    /// Validation job for checking grid placement validity
    /// </summary>
    [BurstCompile]
    public struct GridValidationJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<GridNode> Nodes;
        [ReadOnly] public float MaxSlope;
        [ReadOnly] public float MinSegmentLength;
        [ReadOnly] public float MaxSegmentLength;
        [ReadOnly] public TerrainHeightData TerrainData;

        [NativeDisableParallelForRestriction]
        public NativeArray<GridValidationFlags> ValidationResults;

        public void Execute(int index)
        {
            GridNode node = Nodes[index];
            GridValidationFlags flags = GridValidationFlags.None;

            // Check position validity
            if (IsPositionValid(node.Position))
                flags |= GridValidationFlags.ValidPosition;

            // Check slope
            if (IsSlopeValid(node.Position))
                flags |= GridValidationFlags.ValidSlope;

            // Check water conflict
            if (!IsInWater(node.Position))
                flags |= GridValidationFlags.NoWaterConflict;

            // Check obstacle conflict
            if (!HasObstacle(node.Position))
                flags |= GridValidationFlags.NoObstacleConflict;

            // Check connectivity (simplified - in real implementation would check actual connections)
            if (index > 0 && index < Nodes.Length - 1)
                flags |= GridValidationFlags.Connected;

            ValidationResults[index] = flags;
        }

        private bool IsPositionValid(float3 position)
        {
            // Check if position is within reasonable bounds
            return math.abs(position.x) < 10000f && math.abs(position.z) < 10000f;
        }

        private bool IsSlopeValid(float3 position)
        {
            // Calculate terrain slope at position
            // This is simplified - actual implementation would use terrain gradient
            return true; // Placeholder
        }

        private bool IsInWater(float3 position)
        {
            // Check if position is below water level
            // This would use the WaterSystem in actual implementation
            return false; // Placeholder
        }

        private bool HasObstacle(float3 position)
        {
            // Check for existing buildings or other obstacles
            // This would use spatial queries in actual implementation
            return false; // Placeholder
        }
    }

    /// <summary>
    /// Post-processing job for grid refinement
    /// </summary>
    [BurstCompile]
    public struct GridRefinementJob : IJob
    {
        public NativeArray<GridNode> Nodes;
        [ReadOnly] public NativeArray<GridValidationFlags> ValidationResults;
        [ReadOnly] public float RelaxationFactor;
        [ReadOnly] public int RelaxationIterations;

        public void Execute()
        {
            // Apply relaxation to smooth grid positions
            for (int iteration = 0; iteration < RelaxationIterations; iteration++)
            {
                for (int i = 1; i < Nodes.Length - 1; i++)
                {
                    // Only relax valid nodes
                    if ((ValidationResults[i] & GridValidationFlags.ValidPosition) == 0)
                        continue;

                    GridNode node = Nodes[i];
                    float3 avgPosition = node.Position;
                    int neighborCount = 0;

                    // Average with neighbors (simplified - would need actual connectivity info)
                    if (i > 0)
                    {
                        avgPosition += Nodes[i - 1].Position;
                        neighborCount++;
                    }
                    if (i < Nodes.Length - 1)
                    {
                        avgPosition += Nodes[i + 1].Position;
                        neighborCount++;
                    }

                    if (neighborCount > 0)
                    {
                        avgPosition /= (neighborCount + 1);
                        node.Position = math.lerp(node.Position, avgPosition, RelaxationFactor);
                        Nodes[i] = node;
                    }
                }
            }
        }
    }
}
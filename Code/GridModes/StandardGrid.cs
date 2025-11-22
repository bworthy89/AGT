using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using AdvancedGridTool.Components;
using AdvancedGridTool.Jobs;

namespace AdvancedGridTool.GridModes
{
    /// <summary>
    /// Standard rectangular grid pattern with major/minor axes
    /// </summary>
    public class StandardGrid : GridBase
    {
        private int2 m_MajorAxisInterval;
        private bool m_GenerateAxes;

        public override string ModeName => "Standard Grid";
        public override GridPattern Pattern => GridPattern.Standard;

        public int2 MajorAxisInterval
        {
            get => m_MajorAxisInterval;
            set => m_MajorAxisInterval = math.max(value, new int2(2, 2));
        }

        public bool GenerateAxes
        {
            get => m_GenerateAxes;
            set => m_GenerateAxes = value;
        }

        public StandardGrid(GridBase previousMode, Color previewColor, Color invalidColor)
            : base(previousMode, previewColor, invalidColor)
        {
            m_MajorAxisInterval = new int2(3, 3); // Every 3rd street is a major street
            m_GenerateAxes = true;
        }

        public override void GenerateGrid(ref NativeList<GridNode> nodes, ref NativeList<GridSegment> segments)
        {
            // Clear existing data
            nodes.Clear();
            segments.Clear();

            // Calculate total number of nodes and segments
            int totalNodes = m_Dimensions.x * m_Dimensions.y;
            int horizontalSegments = (m_Dimensions.x - 1) * m_Dimensions.y;
            int verticalSegments = m_Dimensions.x * (m_Dimensions.y - 1);

            // Ensure capacity
            if (nodes.Capacity < totalNodes)
                nodes.Capacity = totalNodes;
            if (segments.Capacity < horizontalSegments + verticalSegments)
                segments.Capacity = horizontalSegments + verticalSegments;

            // Allocate temporary arrays for job
            NativeArray<GridNode> nodeArray = new NativeArray<GridNode>(totalNodes, Allocator.TempJob);
            NativeArray<GridSegment> horizontalArray = new NativeArray<GridSegment>(horizontalSegments, Allocator.TempJob);
            NativeArray<GridSegment> verticalArray = new NativeArray<GridSegment>(verticalSegments, Allocator.TempJob);

            // Create and schedule the job
            StandardGridJob job = new StandardGridJob
            {
                Origin = m_StartPosition,
                EndPoint = m_EndPosition,
                Rotation = m_Rotation,
                Spacing = m_Spacing,
                Dimensions = m_Dimensions,
                BlockSize = m_Spacing * new float2(m_MajorAxisInterval.x, m_MajorAxisInterval.y),
                MajorAxisInterval = m_MajorAxisInterval,
                GenerateAxes = m_GenerateAxes,
                UseTerrainHeight = false, // TODO: Get from settings
                Nodes = nodeArray,
                HorizontalSegments = horizontalArray,
                VerticalSegments = verticalArray
            };

            // Execute job
            JobHandle jobHandle = job.Schedule(totalNodes, 32);
            jobHandle.Complete();

            // Copy results to output lists
            for (int i = 0; i < nodeArray.Length; i++)
            {
                nodes.Add(nodeArray[i]);
            }

            for (int i = 0; i < horizontalArray.Length; i++)
            {
                segments.Add(horizontalArray[i]);
            }

            for (int i = 0; i < verticalArray.Length; i++)
            {
                segments.Add(verticalArray[i]);
            }

            // Dispose temporary arrays
            nodeArray.Dispose();
            horizontalArray.Dispose();
            verticalArray.Dispose();
        }

        public override void ValidateGrid(ref NativeArray<GridNode> nodes, ref NativeArray<GridValidationFlags> validationFlags)
        {
            // Create validation job
            GridValidationJob validationJob = new GridValidationJob
            {
                Nodes = nodes,
                MaxSlope = 30f, // TODO: Get from settings
                MinSegmentLength = 10f,
                MaxSegmentLength = 200f,
                ValidationResults = validationFlags
            };

            // Execute validation
            JobHandle handle = validationJob.Schedule(nodes.Length, 64);
            handle.Complete();
        }

        public override void RefineGrid(ref NativeArray<GridNode> nodes, int iterations)
        {
            // Skip refinement for standard grid - it should maintain perfect alignment
            // Refinement is more useful for organic patterns
        }

        public override void UpdateFromInput(float3 currentPosition, bool fixedPreview)
        {
            base.UpdateFromInput(currentPosition, fixedPreview);

            // For standard grid, align to cardinal directions if close
            float3 delta = currentPosition - m_StartPosition;
            float angle = math.atan2(delta.z, delta.x);

            // Snap to 90-degree increments if within 10 degrees
            float snapAngle = math.radians(10f);
            float[] cardinalAngles = { 0, math.PI * 0.5f, math.PI, math.PI * 1.5f, math.PI * 2f };

            foreach (float cardinal in cardinalAngles)
            {
                if (math.abs(angle - cardinal) < snapAngle)
                {
                    // Snap to cardinal direction
                    float distance = math.length(delta.xz);
                    float3 snappedDirection = new float3(math.cos(cardinal), 0, math.sin(cardinal));
                    m_EndPosition = m_StartPosition + snappedDirection * distance;
                    m_EndPosition.y = currentPosition.y;
                    break;
                }
            }
        }

        public override void CalculateDimensionsFromPositions()
        {
            float3 delta = m_EndPosition - m_StartPosition;

            // For standard grid, use the actual distance in each direction
            float width = math.abs(delta.x);
            float height = math.abs(delta.z);

            if (m_Spacing.x > 0 && m_Spacing.y > 0)
            {
                m_Dimensions = new int2(
                    math.max(2, (int)math.round(width / m_Spacing.x) + 1),
                    math.max(2, (int)math.round(height / m_Spacing.y) + 1)
                );
            }
        }

        public Color GetMajorAxisColor()
        {
            return Color.Lerp(m_PreviewColor, Color.yellow, 0.3f);
        }

        public Color GetMinorAxisColor()
        {
            return Color.Lerp(m_PreviewColor, Color.white, 0.15f);
        }
    }
}
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Game.Tools;
using AdvancedGridTool.Components;

namespace AdvancedGridTool.GridModes
{
    /// <summary>
    /// Base class for all grid generation modes
    /// </summary>
    public abstract class GridBase
    {
        // Protected fields accessible to derived classes
        protected float3 m_StartPosition;
        protected float3 m_EndPosition;
        protected float2 m_Spacing;
        protected int2 m_Dimensions;
        protected quaternion m_Rotation;
        protected Color m_PreviewColor;
        protected Color m_InvalidColor;

        // Public properties
        public float3 StartPosition
        {
            get => m_StartPosition;
            set => m_StartPosition = value;
        }

        public float3 EndPosition
        {
            get => m_EndPosition;
            set => m_EndPosition = value;
        }

        public float2 Spacing
        {
            get => m_Spacing;
            set => m_Spacing = math.max(value, new float2(10f, 10f));
        }

        public int2 Dimensions
        {
            get => m_Dimensions;
            set => m_Dimensions = math.max(value, new int2(1, 1));
        }

        public quaternion Rotation
        {
            get => m_Rotation;
            set => m_Rotation = value;
        }

        public abstract string ModeName { get; }
        public abstract GridPattern Pattern { get; }

        // Constructor
        protected GridBase(GridBase previousMode, Color previewColor, Color invalidColor)
        {
            if (previousMode != null)
            {
                // Copy settings from previous mode
                m_StartPosition = previousMode.m_StartPosition;
                m_EndPosition = previousMode.m_EndPosition;
                m_Spacing = previousMode.m_Spacing;
                m_Dimensions = previousMode.m_Dimensions;
                m_Rotation = previousMode.m_Rotation;
            }
            else
            {
                // Set defaults
                m_StartPosition = float3.zero;
                m_EndPosition = float3.zero;
                m_Spacing = new float2(50f, 50f);
                m_Dimensions = new int2(5, 5);
                m_Rotation = quaternion.identity;
            }

            m_PreviewColor = previewColor;
            m_InvalidColor = invalidColor;
        }

        // Abstract methods that must be implemented by derived classes
        public abstract void GenerateGrid(ref NativeList<GridNode> nodes, ref NativeList<GridSegment> segments);
        public abstract void ValidateGrid(ref NativeArray<GridNode> nodes, ref NativeArray<GridValidationFlags> validationFlags);
        public abstract void RefineGrid(ref NativeArray<GridNode> nodes, int iterations);

        // Virtual methods that can be overridden
        public virtual void UpdateFromInput(float3 currentPosition, bool fixedPreview)
        {
            if (!fixedPreview)
            {
                m_EndPosition = currentPosition;
                CalculateDimensionsFromPositions();
            }
        }

        public virtual void CalculateDimensionsFromPositions()
        {
            float3 delta = m_EndPosition - m_StartPosition;
            float distance = math.length(delta.xz);

            if (distance > 0 && m_Spacing.x > 0 && m_Spacing.y > 0)
            {
                m_Dimensions = new int2(
                    math.max(1, (int)(math.abs(delta.x) / m_Spacing.x)),
                    math.max(1, (int)(math.abs(delta.z) / m_Spacing.y))
                );
            }
        }

        public virtual void SetRotationFromDirection()
        {
            float3 direction = math.normalize(m_EndPosition - m_StartPosition);
            if (math.lengthsq(direction) > 0.01f)
            {
                m_Rotation = quaternion.LookRotation(direction, math.up());
            }
        }

        public virtual Color GetNodeColor(NodeType nodeType, GridValidationFlags validationFlags)
        {
            if ((validationFlags & GridValidationFlags.All) != GridValidationFlags.All)
            {
                return m_InvalidColor;
            }

            switch (nodeType)
            {
                case NodeType.MajorIntersection:
                    return Color.Lerp(m_PreviewColor, Color.white, 0.3f);
                case NodeType.MinorIntersection:
                    return Color.Lerp(m_PreviewColor, Color.white, 0.1f);
                default:
                    return m_PreviewColor;
            }
        }

        public virtual Color GetSegmentColor(SegmentType segmentType, bool isValid)
        {
            if (!isValid)
                return m_InvalidColor;

            switch (segmentType)
            {
                case SegmentType.Primary:
                    return Color.Lerp(m_PreviewColor, Color.white, 0.2f);
                case SegmentType.Secondary:
                    return m_PreviewColor;
                default:
                    return Color.Lerp(m_PreviewColor, Color.black, 0.2f);
            }
        }

        // Helper methods
        protected float3 GetWorldPosition(float2 gridPosition)
        {
            float3 localPos = new float3(gridPosition.x * m_Spacing.x, 0, gridPosition.y * m_Spacing.y);
            return m_StartPosition + math.mul(m_Rotation, localPos);
        }

        protected float2 GetGridPosition(float3 worldPosition)
        {
            float3 localPos = math.mul(math.inverse(m_Rotation), worldPosition - m_StartPosition);
            return new float2(localPos.x / m_Spacing.x, localPos.z / m_Spacing.y);
        }

        protected bool IsWithinBounds(int2 gridCoord)
        {
            return gridCoord.x >= 0 && gridCoord.x < m_Dimensions.x &&
                   gridCoord.y >= 0 && gridCoord.y < m_Dimensions.y;
        }

        protected int GetNodeIndex(int2 gridCoord)
        {
            return gridCoord.y * m_Dimensions.x + gridCoord.x;
        }

        protected int2 GetGridCoord(int nodeIndex)
        {
            return new int2(nodeIndex % m_Dimensions.x, nodeIndex / m_Dimensions.x);
        }

        // Utility method for distance calculations
        protected float GetDistance(float3 a, float3 b)
        {
            return math.length(b - a);
        }

        protected float GetDistance2D(float3 a, float3 b)
        {
            float2 delta = new float2(b.x - a.x, b.z - a.z);
            return math.length(delta);
        }
    }
}
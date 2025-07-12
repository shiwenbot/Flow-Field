using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FlowFieldGrid
{
    [Header("网格基本信息")]
    public int width;
    public int height;
    
    [Header("Field 数据")]
    public int[] costField;           // 消耗值字段
    public float[] integrationField;  // 积分字段
    public Vector2[] flowDirectionField; // 流向字段（后续使用）
    
    // 常量
    public const float IMPASSABLE = float.MaxValue;
    public const float UNVISITED = -1f;
    
    public FlowFieldGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
        
        costField = new int[width * height];
        integrationField = new float[width * height];
        flowDirectionField = new Vector2[width * height];
        
        InitializeFields();
    }
    
    private void InitializeFields()
    {
        for (int i = 0; i < integrationField.Length; i++)
        {
            integrationField[i] = UNVISITED;
            flowDirectionField[i] = Vector2.zero;
        }
    }
    
    public void LoadFromMapData(MapData mapData)
    {
        if (mapData == null || mapData.costData == null)
        {
            Debug.LogError("MapData 或 costData 为空！");
            return;
        }
        
        width = mapData.width;
        height = mapData.height;
        
        // 重新初始化数组
        costField = new int[width * height];
        integrationField = new float[width * height];
        flowDirectionField = new Vector2[width * height];
        
        // 复制消耗值数据
        System.Array.Copy(mapData.costData, costField, costField.Length);
        
        // 重置积分字段
        for (int i = 0; i < integrationField.Length; i++)
        {
            integrationField[i] = UNVISITED;
            flowDirectionField[i] = Vector2.zero;
        }
        
        Debug.Log($"成功加载地图数据: {width}x{height}");
    }
    
    public int GetCost(int x, int y)
    {
        if (!IsValidPosition(x, y)) return int.MaxValue;
        return costField[y * width + x];
    }
    
    public void SetCost(int x, int y, int cost)
    {
        if (!IsValidPosition(x, y)) return;
        costField[y * width + x] = cost;
    }
    
    public float GetIntegration(int x, int y)
    {
        if (!IsValidPosition(x, y)) return IMPASSABLE;
        return integrationField[y * width + x];
    }
    
    public void SetIntegration(int x, int y, float integration)
    {
        if (!IsValidPosition(x, y)) return;
        integrationField[y * width + x] = integration;
    }
    
    public Vector2 GetFlowDirection(int x, int y)
    {
        if (!IsValidPosition(x, y)) return Vector2.zero;
        return flowDirectionField[y * width + x];
    }
    
    public void SetFlowDirection(int x, int y, Vector2 direction)
    {
        if (!IsValidPosition(x, y)) return;
        flowDirectionField[y * width + x] = direction;
    }
    
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
    
    public bool IsImpassable(int x, int y)
    {
        return GetCost(x, y) <= 0; // 消耗值为0或负数视为不可通过
    }
    
    public List<Vector2Int> GetNeighbors(int x, int y)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        
        // 四个方向的邻居
        Vector2Int[] directions = {
            new Vector2Int(0, 1),   // 上
            new Vector2Int(1, 0),   // 右
            new Vector2Int(0, -1),  // 下
            new Vector2Int(-1, 0)   // 左
        };
        
        foreach (var dir in directions)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            
            if (IsValidPosition(nx, ny) && !IsImpassable(nx, ny))
            {
                neighbors.Add(new Vector2Int(nx, ny));
            }
        }
        
        return neighbors;
    }
    
    public void ResetIntegrationField()
    {
        for (int i = 0; i < integrationField.Length; i++)
        {
            integrationField[i] = UNVISITED;
        }
    }
}
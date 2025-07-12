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
    public Vector2[] flowDirectionField; // 流向字段
    
    // 常量
    public const float IMPASSABLE = float.MaxValue;
    public const float UNVISITED = -1f;
    
    // 8个方向的偏移量（包括斜角）
    private static readonly Vector2Int[] directions = {
        new Vector2Int(0, 1),   // 上
        new Vector2Int(1, 1),   // 右上
        new Vector2Int(1, 0),   // 右
        new Vector2Int(1, -1),  // 右下
        new Vector2Int(0, -1),  // 下
        new Vector2Int(-1, -1), // 左下
        new Vector2Int(-1, 0),  // 左
        new Vector2Int(-1, 1)   // 左上
    };
    
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
        Vector2Int[] fourDirections = {
            new Vector2Int(0, 1),   // 上
            new Vector2Int(1, 0),   // 右
            new Vector2Int(0, -1),  // 下
            new Vector2Int(-1, 0)   // 左
        };
        
        foreach (var dir in fourDirections)
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
    
    public void ResetFlowDirectionField()
    {
        for (int i = 0; i < flowDirectionField.Length; i++)
        {
            flowDirectionField[i] = Vector2.zero;
        }
    }
    
    /// <summary>
    /// 生成流场方向
    /// </summary>
    public void GenerateFlowDirectionField()
    {
        ResetFlowDirectionField();
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 跳过不可通过的格子
                if (IsImpassable(x, y))
                {
                    continue;
                }
                
                // 跳过未访问的格子
                if (GetIntegration(x, y) == UNVISITED)
                {
                    continue;
                }
                
                // 生成该格子的流向
                Vector2 direction = CalculateFlowDirection(x, y);
                SetFlowDirection(x, y, direction);
            }
        }
    }
    
    /// <summary>
    /// 计算指定格子的流向
    /// </summary>
    private Vector2 CalculateFlowDirection(int x, int y)
    {
        float currentIntegration = GetIntegration(x, y);
        float minIntegration = float.MaxValue;
        Vector2 bestDirection = Vector2.zero;
        
        // 遍历8个方向的邻居
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int dir = directions[i];
            int nx = x + dir.x;
            int ny = y + dir.y;
            
            // 检查位置是否有效
            if (!IsValidPosition(nx, ny) || IsImpassable(nx, ny))
            {
                continue;
            }
            
            float neighborIntegration = GetIntegration(nx, ny);
            
            // 跳过未访问的邻居
            if (neighborIntegration == UNVISITED)
            {
                continue;
            }
            
            // 找到integration值最小的邻居
            if (neighborIntegration < minIntegration)
            {
                minIntegration = neighborIntegration;
                bestDirection = new Vector2(dir.x, dir.y);
            }
        }
        
        // 如果没有找到更好的邻居，返回零向量
        if (bestDirection == Vector2.zero)
        {
            return Vector2.zero;
        }
        
        // 应用双线性插值来平滑方向
        Vector2 smoothedDirection = ApplyBilinearInterpolation(x, y, bestDirection);
        
        return smoothedDirection.normalized;
    }
    
    /// <summary>
    /// 对流向进行双线性插值平滑
    /// </summary>
    private Vector2 ApplyBilinearInterpolation(int x, int y, Vector2 baseDirection)
    {
        Vector2 interpolatedDirection = baseDirection;
        int validNeighbors = 0;
        
        // 获取周围格子的方向并进行加权平均
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int dir = directions[i];
            int nx = x + dir.x;
            int ny = y + dir.y;
            
            if (!IsValidPosition(nx, ny) || IsImpassable(nx, ny))
            {
                continue;
            }
            
            float neighborIntegration = GetIntegration(nx, ny);
            if (neighborIntegration == UNVISITED)
            {
                continue;
            }
            
            // 计算权重（距离越近权重越大）
            float distance = Vector2.Distance(Vector2.zero, new Vector2(dir.x, dir.y));
            float weight = 1.0f / (distance + 0.1f); // 避免除零
            
            // 计算邻居的方向
            Vector2 neighborDirection = CalculateBasicDirection(nx, ny);
            
            if (neighborDirection != Vector2.zero)
            {
                interpolatedDirection += neighborDirection * weight;
                validNeighbors++;
            }
        }
        
        // 如果有有效邻居，进行平均
        if (validNeighbors > 0)
        {
            interpolatedDirection = interpolatedDirection / (validNeighbors + 1); // +1 for base direction
        }
        
        return interpolatedDirection;
    }
    
    /// <summary>
    /// 计算基础方向（不进行插值）
    /// </summary>
    private Vector2 CalculateBasicDirection(int x, int y)
    {
        float currentIntegration = GetIntegration(x, y);
        float minIntegration = float.MaxValue;
        Vector2 bestDirection = Vector2.zero;
        
        // 遍历8个方向的邻居
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int dir = directions[i];
            int nx = x + dir.x;
            int ny = y + dir.y;
            
            if (!IsValidPosition(nx, ny) || IsImpassable(nx, ny))
            {
                continue;
            }
            
            float neighborIntegration = GetIntegration(nx, ny);
            
            if (neighborIntegration == UNVISITED)
            {
                continue;
            }
            
            if (neighborIntegration < minIntegration)
            {
                minIntegration = neighborIntegration;
                bestDirection = new Vector2(dir.x, dir.y);
            }
        }
        
        return bestDirection;
    }
    
    /// <summary>
    /// 获取指定位置的平滑流向（使用双线性插值）
    /// </summary>
    public Vector2 GetSmoothFlowDirection(float x, float y)
    {
        // 获取整数坐标
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;
        
        // 获取小数部分
        float fx = x - x0;
        float fy = y - y0;
        
        // 获取四个角的方向
        Vector2 dir00 = IsValidPosition(x0, y0) ? GetFlowDirection(x0, y0) : Vector2.zero;
        Vector2 dir10 = IsValidPosition(x1, y0) ? GetFlowDirection(x1, y0) : Vector2.zero;
        Vector2 dir01 = IsValidPosition(x0, y1) ? GetFlowDirection(x0, y1) : Vector2.zero;
        Vector2 dir11 = IsValidPosition(x1, y1) ? GetFlowDirection(x1, y1) : Vector2.zero;
        
        // 双线性插值
        Vector2 dirX0 = Vector2.Lerp(dir00, dir10, fx);
        Vector2 dirX1 = Vector2.Lerp(dir01, dir11, fx);
        Vector2 finalDir = Vector2.Lerp(dirX0, dirX1, fy);
        
        return finalDir.normalized;
    }
}
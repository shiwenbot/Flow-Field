using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class FlowFieldManager : MonoBehaviour
{
    [Header("数据和组件")]
    public MapData mapData;
    public FlowFieldView flowFieldView;
    
    [Header("调试设置")]
    public bool enableDebugLog = true;
    public bool showGenerationProcess = false;
    
    // 私有变量
    private FlowFieldGrid flowFieldGrid;
    private Vector2Int currentTarget = new Vector2Int(-1, -1);
    
    private void Start()
    {
        InitializeFlowField();
    }
    
    private void InitializeFlowField()
    {
        if (mapData == null)
        {
            Debug.LogError("MapData 未设置！");
            return;
        }
        
        // 创建并初始化网格
        flowFieldGrid = new FlowFieldGrid(mapData.width, mapData.height);
        flowFieldGrid.LoadFromMapData(mapData);
        
        // 初始化视图
        if (flowFieldView != null)
        {
            flowFieldView.Initialize(flowFieldGrid);
            flowFieldView.OnCellClicked += HandleCellClicked;
        }
        
        Debug.Log("Flow Field 系统初始化完成");
    }
    
    private void HandleCellClicked(int x, int y)
    {
        if (flowFieldGrid == null) return;
        
        if (flowFieldGrid.IsImpassable(x, y))
        {
            Debug.LogWarning($"无法将不可通过的格子 ({x}, {y}) 设为目标");
            return;
        }
        
        SetTarget(x, y);
    }
    
    public void SetTarget(int x, int y)
    {
        currentTarget = new Vector2Int(x, y);
        
        if (enableDebugLog)
        {
            Debug.Log($"设置目标点: ({x}, {y})");
        }
        
        // 更新视图显示目标
        if (flowFieldView != null)
        {
            flowFieldView.SetTarget(x, y);
        }
        
        // 生成 Integration Field
        GenerateIntegrationField(x, y);
    }
    
    private void GenerateIntegrationField(int targetX, int targetY)
    {
        if (flowFieldGrid == null) return;
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // 重置积分字段
        flowFieldGrid.ResetIntegrationField();
        
        // 使用 Dijkstra 算法生成积分字段
        var openSet = new SortedDictionary<float, Queue<Vector2Int>>();
        var visited = new HashSet<Vector2Int>();
        
        // 设置目标点的积分值为 0
        flowFieldGrid.SetIntegration(targetX, targetY, 0f);
        
        // 将目标点加入开放列表
        var startQueue = new Queue<Vector2Int>();
        startQueue.Enqueue(new Vector2Int(targetX, targetY));
        openSet[0f] = startQueue;
        
        int processedCells = 0;
        
        while (openSet.Count > 0)
        {
            // 获取最小积分值的格子
            var firstEntry = openSet.GetEnumerator();
            firstEntry.MoveNext();
            float currentIntegration = firstEntry.Current.Key;
            var currentQueue = firstEntry.Current.Value;
            
            Vector2Int current = currentQueue.Dequeue();
            
            // 如果队列为空，移除这个积分值
            if (currentQueue.Count == 0)
            {
                openSet.Remove(currentIntegration);
            }
            
            // 跳过已访问的格子
            if (visited.Contains(current))
            {
                continue;
            }
            
            visited.Add(current);
            processedCells++;
            
            // 处理邻居
            var neighbors = flowFieldGrid.GetNeighbors(current.x, current.y);
            foreach (var neighbor in neighbors)
            {
                if (visited.Contains(neighbor))
                {
                    continue;
                }
                
                int cost = flowFieldGrid.GetCost(neighbor.x, neighbor.y);
                float newIntegration = currentIntegration + cost;
                float existingIntegration = flowFieldGrid.GetIntegration(neighbor.x, neighbor.y);
                
                // 如果找到更好的路径，更新积分值
                if (existingIntegration == FlowFieldGrid.UNVISITED || newIntegration < existingIntegration)
                {
                    flowFieldGrid.SetIntegration(neighbor.x, neighbor.y, newIntegration);
                    
                    // 将邻居加入开放列表
                    if (!openSet.ContainsKey(newIntegration))
                    {
                        openSet[newIntegration] = new Queue<Vector2Int>();
                    }
                    openSet[newIntegration].Enqueue(neighbor);
                }
            }
            
            // 如果启用了显示生成过程，可以在这里暂停
            if (showGenerationProcess && processedCells % 10 == 0)
            {
                if (flowFieldView != null)
                {
                    flowFieldView.UpdateDisplay();
                }
                // 可以添加协程暂停来显示生成过程
            }
        }
        
        stopwatch.Stop();
        
        if (enableDebugLog)
        {
            Debug.Log($"Integration Field 生成完成！处理了 {processedCells} 个格子，耗时 {stopwatch.ElapsedMilliseconds}ms");
        }
        
        // 更新显示
        if (flowFieldView != null)
        {
            flowFieldView.UpdateDisplay();
        }
    }
    
    public void ClearTarget()
    {
        currentTarget = new Vector2Int(-1, -1);
        
        if (flowFieldGrid != null)
        {
            flowFieldGrid.ResetIntegrationField();
        }
        
        if (flowFieldView != null)
        {
            flowFieldView.ClearTarget();
            flowFieldView.UpdateDisplay();
        }
        
        Debug.Log("已清除目标点");
    }
    
    public void ReloadMapData()
    {
        if (mapData != null && flowFieldGrid != null)
        {
            flowFieldGrid.LoadFromMapData(mapData);
            
            if (flowFieldView != null)
            {
                flowFieldView.UpdateDisplay();
            }
            
            Debug.Log("已重新加载地图数据");
        }
    }
    
    // 调试方法
    [ContextMenu("输出积分字段信息")]
    public void PrintIntegrationField()
    {
        if (flowFieldGrid == null) return;
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Integration Field ===");
        
        for (int y = flowFieldGrid.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < flowFieldGrid.width; x++)
            {
                float integration = flowFieldGrid.GetIntegration(x, y);
                if (integration == FlowFieldGrid.UNVISITED)
                {
                    sb.Append("  -  ");
                }
                else
                {
                    sb.Append($"{integration,4:F1}");
                }
                sb.Append(" ");
            }
            sb.AppendLine();
        }
        
        Debug.Log(sb.ToString());
    }
}
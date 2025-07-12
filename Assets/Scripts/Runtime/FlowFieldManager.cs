// FlowFieldManager3D - 3D版本的流场管理器（增强版）
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Serialization;

public class FlowFieldManager : MonoBehaviour
{
    [Header("数据和组件")]
    public MapData mapData;
    [FormerlySerializedAs("flowFieldView3D")] public FlowFieldView flowFieldView;
    
    [Header("物理设置")]
    public bool enablePhysics = false;
    public bool kinematicCells = true;
    
    [Header("调试设置")]
    public bool enableDebugLog = true;
    public bool showGenerationProcess = false;
    
    [Header("Flow Field 可视化")]
    public bool showFlowDirections = true;
    public float arrowLength = 0.8f;
    public float arrowHeadSize = 0.2f;
    public Color arrowColor = Color.red;
    public Color targetArrowColor = Color.yellow;
    
    [Header("Agent Settings")]
    public GameObject agentPrefab; // Assign your Agent prefab in the Inspector
    public KeyCode spawnAgentKey = KeyCode.A; // Press this key to spawn an agent
    
    // 私有变量
    private FlowFieldGrid flowFieldGrid;
    private Vector2Int currentTarget = new Vector2Int(-1, -1);
    
    private void Start()
    {
        InitializeFlowField();
    }
    
    // It's good practice to have an Update method for handling input.
    // If you don't have one, add this entire method.
    private void Update()
    {
        // Check for key press to spawn a new agent
        if (Input.GetKeyDown(spawnAgentKey))
        {
            if (agentPrefab != null && currentTarget != new Vector2Int(-1, -1))
            {
                SpawnAgent();
            }
            else if (agentPrefab == null)
            {
                Debug.LogWarning("Agent Prefab is not set in the FlowFieldManager inspector.");
            }
            else
            {
                Debug.LogWarning("Cannot spawn agent. Please set a target first by clicking on the grid.");
            }
        }
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
        
        // 初始化3D视图
        if (flowFieldView != null)
        {
            flowFieldView.Initialize(flowFieldGrid);
            flowFieldView.OnCellClicked += HandleCellClicked;
            
            // 设置物理属性
            if (enablePhysics)
            {
                flowFieldView.EnablePhysicsForAllCells(true);
                flowFieldView.SetAllCellsKinematic(kinematicCells);
            }
        }
        
        Debug.Log("3D Flow Field 系统初始化完成");
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
            Debug.Log($"设置3D目标点: ({x}, {y})");
        }
        
        // 更新视图显示目标
        if (flowFieldView != null)
        {
            flowFieldView.SetTarget(x, y);
        }
        
        // 生成 Integration Field
        GenerateIntegrationField(x, y);
        
        // 生成 Flow Direction Field
        GenerateFlowDirectionField();
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
            
            // 如果启用了显示生成过程
            if (showGenerationProcess && processedCells % 10 == 0)
            {
                if (flowFieldView != null)
                {
                    flowFieldView.UpdateDisplay();
                }
            }
        }
        
        stopwatch.Stop();
        
        if (enableDebugLog)
        {
            Debug.Log($"3D Integration Field 生成完成！处理了 {processedCells} 个格子，耗时 {stopwatch.ElapsedMilliseconds}ms");
        }
        
        // 更新显示
        if (flowFieldView != null)
        {
            flowFieldView.UpdateDisplay();
        }
    }
    
    private void GenerateFlowDirectionField()
    {
        if (flowFieldGrid == null) return;
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // 生成流场方向
        flowFieldGrid.GenerateFlowDirectionField();
        
        stopwatch.Stop();
        
        if (enableDebugLog)
        {
            Debug.Log($"Flow Direction Field 生成完成！耗时 {stopwatch.ElapsedMilliseconds}ms");
        }
    }
    
    // Gizmos绘制箭头
    private void OnDrawGizmos()
    {
        if (!showFlowDirections || flowFieldGrid == null) return;
        
        float cellSize = flowFieldView != null ? flowFieldView.cellSize : 1f;
        
        for (int y = 0; y < flowFieldGrid.height; y++)
        {
            for (int x = 0; x < flowFieldGrid.width; x++)
            {
                // 跳过不可通过的格子
                if (flowFieldGrid.IsImpassable(x, y))
                {
                    continue;
                }
                
                Vector2 direction = flowFieldGrid.GetFlowDirection(x, y);
                
                // 跳过没有方向的格子
                if (direction == Vector2.zero)
                {
                    continue;
                }
                
                // 计算箭头的世界坐标
                Vector3 cellWorldPos = new Vector3(x * cellSize, 0.1f, y * cellSize);
                Vector3 arrowStart = cellWorldPos;
                Vector3 arrowEnd = cellWorldPos + new Vector3(direction.x, 0, direction.y) * arrowLength;
                
                // 设置箭头颜色
                Color currentArrowColor = (x == currentTarget.x && y == currentTarget.y) ? targetArrowColor : arrowColor;
                Gizmos.color = currentArrowColor;
                
                // 绘制箭头主体
                Gizmos.DrawLine(arrowStart, arrowEnd);
                
                // 绘制箭头头部
                DrawArrowHead(arrowStart, arrowEnd, arrowHeadSize);
            }
        }
    }
    
    private void DrawArrowHead(Vector3 start, Vector3 end, float headSize)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        Vector3 up = Vector3.up;
        
        // 箭头头部的两个分支
        Vector3 arrowHead1 = end - direction * headSize + right * headSize * 0.5f;
        Vector3 arrowHead2 = end - direction * headSize - right * headSize * 0.5f;
        Vector3 arrowHead3 = end - direction * headSize + up * headSize * 0.5f;
        Vector3 arrowHead4 = end - direction * headSize - up * headSize * 0.5f;
        
        Gizmos.DrawLine(end, arrowHead1);
        Gizmos.DrawLine(end, arrowHead2);
        Gizmos.DrawLine(end, arrowHead3);
        Gizmos.DrawLine(end, arrowHead4);
    }
    
    public void ClearTarget()
    {
        currentTarget = new Vector2Int(-1, -1);
        
        if (flowFieldGrid != null)
        {
            flowFieldGrid.ResetIntegrationField();
            flowFieldGrid.ResetFlowDirectionField();
        }
        
        if (flowFieldView != null)
        {
            flowFieldView.ClearTarget();
            flowFieldView.UpdateDisplay();
        }
        
        Debug.Log("已清除目标点");
    }

    #region Agent
    /// <summary>
    /// Spawns a single agent at a random, valid (passable) location on the grid.
    /// </summary>
    public void SpawnAgent()
    {
        if (agentPrefab == null || flowFieldGrid == null)
        {
            Debug.LogError("Agent Prefab or FlowFieldGrid is not ready.");
            return;
        }

        // Find a random position that is not an obstacle
        Vector2Int spawnPos = FindRandomValidSpawnPoint();

        if (spawnPos.x == -1) // Check if a valid point was found
        {
            Debug.LogError("Could not find a valid, passable spawn point for the agent after multiple attempts.");
            return;
        }

        // Calculate world position from grid position
        float cellSize = flowFieldView != null ? flowFieldView.cellSize : 1f;
        Vector3 worldPos = new Vector3(spawnPos.x * cellSize, 1f, spawnPos.y * cellSize); // Spawn 1 unit above the grid

        // Instantiate the agent prefab
        GameObject agentObj = Instantiate(agentPrefab, worldPos, Quaternion.identity);
        agentObj.name = "FlowField Agent";
        FlowFieldAgent agent = agentObj.GetComponent<FlowFieldAgent>();

        // Initialize the agent with a reference to this manager
        if (agent != null)
        {
            agent.Initialize(this);
            if (enableDebugLog)
            {
                Debug.Log($"Spawned agent at grid position ({spawnPos.x}, {spawnPos.y})");
            }
        }
        else
        {
            Debug.LogError("The assigned Agent Prefab is missing the 'FlowFieldAgent' component!");
            Destroy(agentObj); // Clean up the improperly configured object
        }
    }

    /// <summary>
    /// Finds a random non-impassable cell to serve as a spawn point.
    /// </summary>
    /// <returns>A valid Vector2Int position, or (-1, -1) if none is found.</returns>
    private Vector2Int FindRandomValidSpawnPoint()
    {
        // Limit attempts to avoid an infinite loop on a fully impassable map
        int maxAttempts = mapData.width * mapData.height;
        for (int i = 0; i < maxAttempts; i++)
        {
            int x = Random.Range(0, flowFieldGrid.width);
            int y = Random.Range(0, flowFieldGrid.height);

            // Check if the randomly chosen cell is passable
            if (!flowFieldGrid.IsImpassable(x, y))
            {
                return new Vector2Int(x, y);
            }
        }
        return new Vector2Int(-1, -1); // Indicate failure
    }
    
    // 物理系统相关方法
    public void EnablePhysics(bool enable)
    {
        enablePhysics = enable;
        if (flowFieldView != null)
        {
            flowFieldView.EnablePhysicsForAllCells(enable);
        }
    }
        
    public void SetKinematicMode(bool kinematic)
    {
        kinematicCells = kinematic;
        if (flowFieldView != null)
        {
            flowFieldView.SetAllCellsKinematic(kinematic);
        }
    }

    #endregion
    

    
    // 获取指定位置的3D格子（用于后续物理交互）
    public FlowFieldCell GetCellAt(int x, int y)
    {
        if (flowFieldView != null)
        {
            return flowFieldView.GetCellAt(x, y);
        }
        return null;
    }
    
    // 获取所有格子（用于批量物理操作）
    public List<FlowFieldCell> GetAllCells()
    {
        if (flowFieldView != null)
        {
            return flowFieldView.GetAllCells();
        }
        return new List<FlowFieldCell>();
    }
    
    // Flow Field 相关的公共方法
    public Vector2 GetFlowDirection(int x, int y)
    {
        if (flowFieldGrid != null)
        {
            return flowFieldGrid.GetFlowDirection(x, y);
        }
        return Vector2.zero;
    }
    
    public Vector2 GetSmoothFlowDirection(float x, float y)
    {
        if (flowFieldGrid != null)
        {
            return flowFieldGrid.GetSmoothFlowDirection(x, y);
        }
        return Vector2.zero;
    }
    
    // 切换文本显示
    public void SetTextVisibility(bool visible)
    {
        if (flowFieldView != null)
        {
            flowFieldView.SetTextVisibility(visible);
        }
    }
    
    // 切换箭头显示
    public void SetArrowVisibility(bool visible)
    {
        showFlowDirections = visible;
    }
}
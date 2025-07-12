// FlowFieldView3D - 3D版本的流场视图（完整版）
using UnityEngine;
using System.Collections.Generic;

public class FlowFieldView : MonoBehaviour
{
    [Header("3D 设置")]
    public Transform cellContainer;  // 存放所有格子的父物体
    public GameObject cellPrefab;    // 格子预制体（3D版本）
    public float cellSize = 1f;      // 格子大小
    public float cellHeight = 0.2f;  // 格子高度
    
    [Header("材质设置")]
    public Material defaultMaterial;
    public Material targetMaterial;
    public Material impassableMaterial;
    
    [Header("颜色设置")]
    public Color defaultColor = Color.green;
    public Color targetColor = Color.red;
    public Color impassableColor = Color.black;
    public Color minIntegrationColor = Color.white;
    public Color maxIntegrationColor = Color.blue;
    
    [Header("显示设置")]
    public bool showIntegrationValues = true;
    public bool showCostValues = false;
    public bool showText = true;
    
    [Header("相机设置")]
    public Camera topDownCamera;
    public bool autoSetupCamera = true;
    
    [Header("调试设置")]
    public bool enableDebugLogs = false;
    
    // 私有变量
    private FlowFieldGrid flowFieldGrid;
    private List<FlowFieldCell> cells = new List<FlowFieldCell>();
    private Vector2Int currentTarget = new Vector2Int(-1, -1);
    private bool isInitialized = false;
    
    // 事件
    public System.Action<int, int> OnCellClicked;
    
    private void Awake()
    {
        // 确保容器存在
        if (cellContainer == null)
        {
            GameObject container = new GameObject("CellContainer");
            container.transform.SetParent(transform);
            cellContainer = container.transform;
        }
        
        // 如果没有预制体，创建一个基础的
        if (cellPrefab == null)
        {
            CreateDefaultCellPrefab();
        }
    }
    
    private void Start()
    {
        SetupCamera();
    }
    
    private void CreateDefaultCellPrefab()
    {
        GameObject prefab = new GameObject("DefaultCellPrefab");
        
        // 添加基础组件
        prefab.AddComponent<MeshFilter>().mesh = CreateCubeMesh();
        prefab.AddComponent<MeshRenderer>();
        prefab.AddComponent<BoxCollider>();
        
        // 添加FlowFieldCell组件
        FlowFieldCell cellComponent = prefab.AddComponent<FlowFieldCell>();
        
        // 设置默认材质
        if (defaultMaterial != null)
        {
            prefab.GetComponent<MeshRenderer>().material = defaultMaterial;
        }
        
        cellPrefab = prefab;
        
        if (enableDebugLogs)
        {
            Debug.Log("创建了默认的 CellPrefab");
        }
    }
    
    private Mesh CreateCubeMesh()
    {
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        
        if (Application.isPlaying)
            Destroy(tempCube);
        else
            DestroyImmediate(tempCube);
        
        return mesh;
    }
    
    private void SetupCamera()
    {
        if (autoSetupCamera && topDownCamera != null)
        {
            // 设置为正交相机，俯视角
            topDownCamera.orthographic = true;
            topDownCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }
    
    public void Initialize(FlowFieldGrid grid)
    {
        if (grid == null)
        {
            Debug.LogError("FlowFieldGrid 不能为空！");
            return;
        }
        
        flowFieldGrid = grid;
        
        if (enableDebugLogs)
        {
            Debug.Log($"初始化 FlowFieldView，网格大小: {grid.width}x{grid.height}");
        }
        
        CreateCells();
        UpdateDisplay();
        
        // 自动调整相机视野
        if (autoSetupCamera && topDownCamera != null)
        {
            AdjustCameraView();
        }
        
        isInitialized = true;
    }
    
    private void CreateCells()
    {
        ClearCells();
        
        if (flowFieldGrid == null || cellPrefab == null)
        {
            Debug.LogError("FlowFieldGrid 或 cellPrefab 为空！");
            return;
        }
        
        // 创建格子
        for (int y = 0; y < flowFieldGrid.height; y++)
        {
            for (int x = 0; x < flowFieldGrid.width; x++)
            {
                CreateCell(x, y);
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"创建了 {cells.Count} 个3D格子");
        }
    }
    
    private void CreateCell(int x, int y)
    {
        GameObject cellObj = Instantiate(cellPrefab, cellContainer);
        cellObj.name = $"Cell3D_{x}_{y}";
        
        FlowFieldCell cell = cellObj.GetComponent<FlowFieldCell>();
        if (cell == null)
        {
            cell = cellObj.AddComponent<FlowFieldCell>();
        }
        
        // 设置材质
        cell.defaultMaterial = defaultMaterial;
        cell.targetMaterial = targetMaterial;
        cell.impassableMaterial = impassableMaterial;
        
        // 初始化格子
        cell.Initialize(x, y, cellSize);
        cell.OnCellClicked += HandleCellClicked;
        cell.SetTextVisibility(showText);
        
        // 调整格子高度
        Vector3 scale = cell.transform.localScale;
        scale.y = cellHeight;
        cell.transform.localScale = scale;
        
        cells.Add(cell);
    }
    
    private void ClearCells()
    {
        foreach (var cell in cells)
        {
            if (cell != null)
            {
                cell.OnCellClicked -= HandleCellClicked;
                if (Application.isPlaying)
                    Destroy(cell.gameObject);
                else
                    DestroyImmediate(cell.gameObject);
            }
        }
        cells.Clear();
    }
    
    private void HandleCellClicked(int x, int y)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"点击了3D格子: ({x}, {y})");
        }
        OnCellClicked?.Invoke(x, y);
    }
    
    public void UpdateDisplay()
    {
        if (flowFieldGrid == null || !isInitialized) return;
        
        foreach (var cell in cells)
        {
            if (cell != null)
            {
                UpdateCell(cell);
            }
        }
    }
    
    private void UpdateCell(FlowFieldCell cell)
    {
        int x = cell.GridX;
        int y = cell.GridY;
        
        // 设置颜色和材质
        SetCellAppearance(cell, x, y);
        
        // 设置文本
        string displayText = GetDisplayText(x, y);
        cell.SetText(displayText);
        
        // 设置目标标记
        bool isTarget = (x == currentTarget.x && y == currentTarget.y);
        cell.SetTarget(isTarget);
    }
    
    private void SetCellAppearance(FlowFieldCell cell, int x, int y)
    {
        // 不可通过的格子
        if (flowFieldGrid.IsImpassable(x, y))
        {
            if (impassableMaterial != null)
                cell.SetMaterial(impassableMaterial);
            else
                cell.SetColor(impassableColor);
            return;
        }
        
        // 目标格子
        if (x == currentTarget.x && y == currentTarget.y)
        {
            if (targetMaterial != null)
                cell.SetMaterial(targetMaterial);
            else
                cell.SetColor(targetColor);
            return;
        }
        
        // 根据 integration 值设置颜色
        float integration = flowFieldGrid.GetIntegration(x, y);
        if (integration == FlowFieldGrid.UNVISITED)
        {
            if (defaultMaterial != null)
                cell.SetMaterial(defaultMaterial);
            else
                cell.SetColor(defaultColor);
        }
        else
        {
            // 将 integration 值映射到颜色
            float maxIntegration = GetMaxIntegration();
            if (maxIntegration > 0)
            {
                float normalizedIntegration = Mathf.Clamp01(integration / maxIntegration);
                Color integrationColor = Color.Lerp(minIntegrationColor, maxIntegrationColor, normalizedIntegration);
                cell.SetColor(integrationColor);
            }
            else
            {
                cell.SetColor(defaultColor);
            }
        }
    }
    
    private string GetDisplayText(int x, int y)
    {
        if (!showText) return "";
        
        if (showIntegrationValues)
        {
            float integration = flowFieldGrid.GetIntegration(x, y);
            if (integration == FlowFieldGrid.UNVISITED)
            {
                return showCostValues ? flowFieldGrid.GetCost(x, y).ToString() : "";
            }
            return integration.ToString("F1");
        }
        else if (showCostValues)
        {
            return flowFieldGrid.GetCost(x, y).ToString();
        }
        
        return "";
    }
    
    private float GetMaxIntegration()
    {
        float max = 0f;
        for (int y = 0; y < flowFieldGrid.height; y++)
        {
            for (int x = 0; x < flowFieldGrid.width; x++)
            {
                float integration = flowFieldGrid.GetIntegration(x, y);
                if (integration != FlowFieldGrid.UNVISITED && integration > max)
                {
                    max = integration;
                }
            }
        }
        return max;
    }
    
    public void SetTarget(int x, int y)
    {
        currentTarget = new Vector2Int(x, y);
        
        if (enableDebugLogs)
        {
            Debug.Log($"设置目标: ({x}, {y})");
        }
        
        UpdateDisplay();
    }
    
    public void ClearTarget()
    {
        currentTarget = new Vector2Int(-1, -1);
        UpdateDisplay();
    }
    
    private void AdjustCameraView()
    {
        if (topDownCamera == null || flowFieldGrid == null) return;
        
        // 计算网格中心
        Vector3 gridCenter = new Vector3(
            (flowFieldGrid.width - 1) * cellSize * 0.5f,
            0,
            (flowFieldGrid.height - 1) * cellSize * 0.5f
        );
        
        // 设置相机位置
        float cameraHeight = Mathf.Max(flowFieldGrid.width, flowFieldGrid.height) * cellSize * 0.75f;
        topDownCamera.transform.position = gridCenter + Vector3.up * cameraHeight;
        
        // 调整正交相机大小
        float gridWidth = flowFieldGrid.width * cellSize;
        float gridHeight = flowFieldGrid.height * cellSize;
        float maxSize = Mathf.Max(gridWidth, gridHeight) * 0.6f;
        
        topDownCamera.orthographicSize = maxSize;
        
        if (enableDebugLogs)
        {
            Debug.Log($"调整相机视野: 中心{gridCenter}, 高度{cameraHeight}, 正交大小{maxSize}");
        }
    }
    
    // 为物理系统准备的方法
    public void EnablePhysicsForAllCells(bool enable)
    {
        foreach (var cell in cells)
        {
            if (cell != null)
            {
                cell.EnablePhysics(enable);
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"为所有格子{(enable ? "启用" : "禁用")}物理系统");
        }
    }
    
    public void SetAllCellsKinematic(bool kinematic)
    {
        foreach (var cell in cells)
        {
            if (cell != null)
            {
                cell.SetKinematic(kinematic);
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"设置所有格子为{(kinematic ? "运动学" : "动力学")}模式");
        }
    }
    
    public void SetTextVisibility(bool visible)
    {
        showText = visible;
        foreach (var cell in cells)
        {
            if (cell != null)
            {
                cell.SetTextVisibility(visible);
            }
        }
    }
    
    public void SetShowIntegrationValues(bool show)
    {
        showIntegrationValues = show;
        UpdateDisplay();
    }
    
    public void SetShowCostValues(bool show)
    {
        showCostValues = show;
        UpdateDisplay();
    }
    
    // 获取指定位置的3D格子
    public FlowFieldCell GetCellAt(int x, int y)
    {
        return cells.Find(cell => cell != null && cell.GridX == x && cell.GridY == y);
    }
    
    // 获取所有格子
    public List<FlowFieldCell> GetAllCells()
    {
        return new List<FlowFieldCell>(cells);
    }
    
    // 重新加载网格数据
    public void ReloadGrid(FlowFieldGrid newGrid)
    {
        if (newGrid == null)
        {
            Debug.LogError("新的FlowFieldGrid不能为空！");
            return;
        }
        
        flowFieldGrid = newGrid;
        CreateCells();
        UpdateDisplay();
        
        if (autoSetupCamera && topDownCamera != null)
        {
            AdjustCameraView();
        }
    }
    
    // 验证组件完整性
    public bool ValidateComponents()
    {
        bool isValid = true;
        
        if (cellContainer == null)
        {
            Debug.LogError("cellContainer 未设置！");
            isValid = false;
        }
        
        if (cellPrefab == null)
        {
            Debug.LogError("cellPrefab 未设置！");
            isValid = false;
        }
        
        if (flowFieldGrid == null)
        {
            Debug.LogWarning("flowFieldGrid 未初始化！");
            isValid = false;
        }
        
        return isValid;
    }
    
    // 清理资源
    private void OnDestroy()
    {
        ClearCells();
        OnCellClicked = null;
    }
    
    // Unity Inspector 按钮
    [ContextMenu("强制更新显示")]
    public void ForceUpdateDisplay()
    {
        UpdateDisplay();
    }
    
    [ContextMenu("重新创建格子")]
    public void RecreateAllCells()
    {
        if (flowFieldGrid != null)
        {
            CreateCells();
            UpdateDisplay();
        }
    }
    
    [ContextMenu("验证组件")]
    public void ValidateInInspector()
    {
        bool isValid = ValidateComponents();
        Debug.Log($"组件验证结果: {(isValid ? "通过" : "失败")}");
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FlowFieldView : MonoBehaviour
{
    [Header("UI 组件")]
    public Transform cellContainer;  // 存放所有格子的父物体
    public GameObject cellPrefab;    // 格子预制体（带有 Image 和 Button 组件）
    
    [Header("显示设置")]
    public Color defaultColor = Color.green;
    public Color targetColor = Color.red;
    public Color impassableColor = Color.black;
    public Font textFont;
    
    [Header("调试信息")]
    public bool showIntegrationValues = true;
    public bool showCostValues = false;
    
    // 私有变量
    private FlowFieldGrid flowFieldGrid;
    private List<FlowFieldCell> cells = new List<FlowFieldCell>();
    private Vector2Int currentTarget = new Vector2Int(-1, -1);
    
    // 事件
    public System.Action<int, int> OnCellClicked;
    
    private void Start()
    {
        if (cellContainer == null)
        {
            cellContainer = transform;
        }
    }
    
    public void Initialize(FlowFieldGrid grid)
    {
        flowFieldGrid = grid;
        CreateCells();
        UpdateDisplay();
    }
    
    private void CreateCells()
    {
        ClearCells();
        
        if (flowFieldGrid == null || cellPrefab == null)
        {
            Debug.LogError("FlowFieldGrid 或 cellPrefab 为空！");
            return;
        }
        
        // 设置 GridLayoutGroup（如果存在）
        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.constraintCount = flowFieldGrid.width;
        }
        
        // 创建格子
        for (int y = flowFieldGrid.height - 1; y >= 0; y--) // 从上到下
        {
            for (int x = 0; x < flowFieldGrid.width; x++)
            {
                CreateCell(x, y);
            }
        }
        
        Debug.Log($"创建了 {cells.Count} 个格子");
    }
    
    private void CreateCell(int x, int y)
    {
        GameObject cellObj = Instantiate(cellPrefab, cellContainer);
        cellObj.name = $"Cell_{x}_{y}";
        
        FlowFieldCell cell = cellObj.GetComponent<FlowFieldCell>();
        if (cell == null)
        {
            cell = cellObj.AddComponent<FlowFieldCell>();
        }
        
        cell.Initialize(x, y);
        cell.OnCellClicked += HandleCellClicked;
        
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
        Debug.Log($"点击了格子: ({x}, {y})");
        OnCellClicked?.Invoke(x, y);
    }
    
    public void UpdateDisplay()
    {
        if (flowFieldGrid == null) return;
        
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
        
        // 设置颜色
        Color cellColor = GetCellColor(x, y);
        cell.SetColor(cellColor);
        
        // 设置文本
        string displayText = GetDisplayText(x, y);
        cell.SetText(displayText);
        
        // 设置目标标记
        bool isTarget = (x == currentTarget.x && y == currentTarget.y);
        cell.SetTarget(isTarget);
    }
    
    private Color GetCellColor(int x, int y)
    {
        if (flowFieldGrid.IsImpassable(x, y))
        {
            return impassableColor;
        }
        
        if (x == currentTarget.x && y == currentTarget.y)
        {
            return targetColor;
        }
        
        // 根据 integration 值调整颜色深浅
        float integration = flowFieldGrid.GetIntegration(x, y);
        if (integration == FlowFieldGrid.UNVISITED)
        {
            return defaultColor;
        }
        
        // 将 integration 值映射到颜色强度
        float maxIntegration = GetMaxIntegration();
        if (maxIntegration > 0)
        {
            float intensity = 1f - (integration / maxIntegration);
            return Color.Lerp(Color.white, defaultColor, intensity);
        }
        
        return defaultColor;
    }
    
    private string GetDisplayText(int x, int y)
    {
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
        UpdateDisplay();
    }
    
    public void ClearTarget()
    {
        currentTarget = new Vector2Int(-1, -1);
        UpdateDisplay();
    }
}

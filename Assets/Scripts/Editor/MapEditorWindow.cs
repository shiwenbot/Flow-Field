using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;

public class MapEditor : OdinEditorWindow
{
    [Title("地图编辑器")]
    [InlineEditor(InlineEditorModes.FullEditor)]
    [LabelText("当前地图数据")]
    public MapData currentMapData;
    
    [Title("编辑工具")]
    [LabelText("笔刷大小")]
    [Range(1, 10)]
    public int brushSize = 1;
    
    [LabelText("笔刷值")]
    [Range(0, 100)]
    public int brushValue = 1;
    
    [LabelText("显示网格")]
    public bool showGrid = true;
    
    [LabelText("网格大小")]
    [Range(10, 50)]
    public int gridSize = 20;
    
    [Title("显示设置")]
    [LabelText("缩放等级")]
    [Range(0.1f, 2f)]
    public float zoomLevel = 1f;
    
    [LabelText("显示数值")]
    public bool showValues = false;
    
    // 私有变量
    private Vector2 scrollPosition;
    private bool isDragging = false;
    private Color[] costColors;
    private GUIStyle gridButtonStyle;
    private GUIStyle labelStyle;
    
    [MenuItem("Tools/地图编辑器")]
    public static void OpenWindow()
    {
        GetWindow<MapEditor>("地图编辑器").Show();
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        GenerateColorPalette();
    }
    
    private void InitializeStyles()
    {
        if (gridButtonStyle == null)
        {
            gridButtonStyle = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(1, 1, 1, 1)
            };
        }
        
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 8
            };
        }
    }
    
    private void GenerateColorPalette()
    {
        costColors = new Color[101]; // 0-100 的颜色
        for (int i = 0; i <= 100; i++)
        {
            float t = i / 100f;
            costColors[i] = Color.Lerp(Color.white, Color.black, t);
        }
    }
    
    protected override void OnImGUI()
    {
        // 确保样式已初始化
        InitializeStyles();
        
        base.OnImGUI();
        
        if (currentMapData == null)
        {
            DrawNoMapDataUI();
            return;
        }
        
        DrawMapEditor();
    }
    
    private void DrawNoMapDataUI()
    {
        GUILayout.Space(20);
        EditorGUILayout.HelpBox("请先创建或加载地图数据", MessageType.Info);
        
        GUILayout.Space(10);
        if (GUILayout.Button("创建新地图", GUILayout.Height(30)))
        {
            CreateNewMapData();
        }
        
        GUILayout.Space(10);
        if (GUILayout.Button("加载现有地图", GUILayout.Height(30)))
        {
            LoadExistingMapData();
        }
    }
    
    private void DrawMapEditor()
    {
        // 工具栏
        DrawToolbar();
        
        GUILayout.Space(10);
        
        // 地图显示区域
        DrawMapView();
    }
    
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("新建", EditorStyles.toolbarButton))
        {
            CreateNewMapData();
        }
        
        if (GUILayout.Button("加载", EditorStyles.toolbarButton))
        {
            LoadExistingMapData();
        }
        
        if (GUILayout.Button("保存", EditorStyles.toolbarButton))
        {
            SaveMapData();
        }
        
        if (GUILayout.Button("另存为", EditorStyles.toolbarButton))
        {
            SaveAsMapData();
        }
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("清空地图", EditorStyles.toolbarButton))
        {
            ClearMap();
        }
        
        if (GUILayout.Button("填充地图", EditorStyles.toolbarButton))
        {
            FillMap();
        }
        
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.LabelField($"地图: {currentMapData.width}x{currentMapData.height}", 
            EditorStyles.toolbarButton);
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawMapView()
    {
        if (currentMapData.costData == null)
        {
            currentMapData.InitializeMap();
        }
        
        // 计算显示尺寸
        int displaySize = Mathf.RoundToInt(gridSize * zoomLevel);
        int totalWidth = currentMapData.width * displaySize;
        int totalHeight = currentMapData.height * displaySize;
        
        // 滚动视图
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 创建地图区域
        Rect mapRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);
        
        // 处理鼠标事件
        HandleMouseEvents(mapRect, displaySize);
        
        // 绘制地图
        DrawMapGrid(mapRect, displaySize);
        
        EditorGUILayout.EndScrollView();
    }
    
    private void HandleMouseEvents(Rect mapRect, int displaySize)
    {
        Event currentEvent = Event.current;
        Vector2 mousePos = currentEvent.mousePosition;
        
        if (mapRect.Contains(mousePos))
        {
            Vector2 localPos = mousePos - mapRect.position;
            int gridX = Mathf.FloorToInt(localPos.x / displaySize);
            int gridY = Mathf.FloorToInt(localPos.y / displaySize);
            
            if (gridX >= 0 && gridX < currentMapData.width && 
                gridY >= 0 && gridY < currentMapData.height)
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    isDragging = true;
                    ModifyTile(gridX, gridY, currentEvent.button);
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseDrag && isDragging)
                {
                    ModifyTile(gridX, gridY, currentEvent.button);
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseUp)
                {
                    isDragging = false;
                    currentEvent.Use();
                }
            }
        }
        
        if (currentEvent.type == EventType.MouseUp)
        {
            isDragging = false;
        }
    }
    
    private void ModifyTile(int x, int y, int mouseButton)
    {
        // 应用笔刷
        for (int dx = -brushSize + 1; dx < brushSize; dx++)
        {
            for (int dy = -brushSize + 1; dy < brushSize; dy++)
            {
                int targetX = x + dx;
                int targetY = y + dy;
                
                if (targetX >= 0 && targetX < currentMapData.width && 
                    targetY >= 0 && targetY < currentMapData.height)
                {
                    int currentCost = currentMapData.GetCost(targetX, targetY);
                    int newCost = currentCost;
                    
                    if (mouseButton == 0) // 左键：设置为笔刷值
                    {
                        newCost = brushValue;
                    }
                    else if (mouseButton == 1) // 右键：减少值
                    {
                        newCost = Mathf.Max(0, currentCost - 1);
                    }
                    
                    currentMapData.SetCost(targetX, targetY, newCost);
                }
            }
        }
        
        EditorUtility.SetDirty(currentMapData);
        Repaint();
    }
    
    private void DrawMapGrid(Rect mapRect, int displaySize)
    {
        for (int y = 0; y < currentMapData.height; y++)
        {
            for (int x = 0; x < currentMapData.width; x++)
            {
                Rect cellRect = new Rect(
                    mapRect.x + x * displaySize,
                    mapRect.y + y * displaySize,
                    displaySize,
                    displaySize
                );
                
                int cost = currentMapData.GetCost(x, y);
                Color cellColor = GetCostColor(cost);
                
                // 绘制格子
                EditorGUI.DrawRect(cellRect, cellColor);
                
                // 绘制边框
                if (showGrid)
                {
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), Color.gray);
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), Color.gray);
                }
                
                // 绘制数值
                if (showValues && displaySize > 15)
                {
                    labelStyle.normal.textColor = GetContrastColor(cellColor);
                    GUI.Label(cellRect, cost.ToString(), labelStyle);
                }
            }
        }
    }
    
    private Color GetCostColor(int cost)
    {
        if (currentMapData.maxCost == 0) return Color.white;
        
        float normalizedCost = (float)cost / currentMapData.maxCost;
        return Color.Lerp(Color.green, Color.red, normalizedCost);
    }
    
    private Color GetContrastColor(Color backgroundColor)
    {
        float brightness = backgroundColor.r * 0.299f + backgroundColor.g * 0.587f + backgroundColor.b * 0.114f;
        return brightness > 0.5f ? Color.black : Color.white;
    }
    
    [Button("创建新地图", ButtonSizes.Medium)]
    private void CreateNewMapData()
    {
        string path = EditorUtility.SaveFilePanel("创建新地图", "Assets", "NewMap", "asset");
        if (!string.IsNullOrEmpty(path))
        {
            path = FileUtil.GetProjectRelativePath(path);
            MapData newMapData = CreateInstance<MapData>();
            newMapData.InitializeMap();
            
            AssetDatabase.CreateAsset(newMapData, path);
            AssetDatabase.SaveAssets();
            
            currentMapData = newMapData;
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newMapData;
        }
    }
    
    [Button("加载现有地图", ButtonSizes.Medium)]
    private void LoadExistingMapData()
    {
        string path = EditorUtility.OpenFilePanel("加载地图", "Assets", "asset");
        if (!string.IsNullOrEmpty(path))
        {
            path = FileUtil.GetProjectRelativePath(path);
            MapData mapData = AssetDatabase.LoadAssetAtPath<MapData>(path);
            if (mapData != null)
            {
                currentMapData = mapData;
                Selection.activeObject = mapData;
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "无法加载地图数据", "确定");
            }
        }
    }
    
    [Button("保存地图", ButtonSizes.Medium)]
    private void SaveMapData()
    {
        if (currentMapData != null)
        {
            EditorUtility.SetDirty(currentMapData);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("保存成功", "地图数据已保存", "确定");
        }
    }
    
    [Button("另存为", ButtonSizes.Medium)]
    private void SaveAsMapData()
    {
        if (currentMapData != null)
        {
            string path = EditorUtility.SaveFilePanel("另存为地图", "Assets", currentMapData.mapName, "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
                MapData newMapData = Instantiate(currentMapData);
                AssetDatabase.CreateAsset(newMapData, path);
                AssetDatabase.SaveAssets();
                
                currentMapData = newMapData;
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = newMapData;
            }
        }
    }
    
    [Button("清空地图", ButtonSizes.Medium)]
    private void ClearMap()
    {
        if (currentMapData != null && EditorUtility.DisplayDialog("确认", "确定要清空地图吗？", "确定", "取消"))
        {
            for (int i = 0; i < currentMapData.costData.Length; i++)
            {
                currentMapData.costData[i] = 0;
            }
            EditorUtility.SetDirty(currentMapData);
            Repaint();
        }
    }
    
    [Button("填充地图", ButtonSizes.Medium)]
    private void FillMap()
    {
        if (currentMapData != null)
        {
            for (int i = 0; i < currentMapData.costData.Length; i++)
            {
                currentMapData.costData[i] = brushValue;
            }
            EditorUtility.SetDirty(currentMapData);
            Repaint();
        }
    }
}
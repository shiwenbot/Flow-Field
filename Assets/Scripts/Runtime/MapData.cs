using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "MapData", menuName = "Map/MapData")]
public class MapData : ScriptableObject
{
    [Title("地图基本信息")]
    [LabelText("地图名称")]
    public string mapName = "新地图";
    
    [LabelText("地图宽度")]
    [Range(10, 500)]
    public int width = 200;
    
    [LabelText("地图高度")]
    [Range(10, 500)]
    public int height = 200;
    
    [Title("地图数据")]
    [HideInInspector]
    public int[] costData;
    
    [Title("显示设置")]
    [LabelText("最大消耗值")]
    [Range(1, 100)]
    public int maxCost = 10;
    
    [LabelText("默认消耗值")]
    [Range(0, 100)]
    public int defaultCost = 1;
    
    private void OnValidate()
    {
        if (costData == null || costData.Length != width * height)
        {
            InitializeMap();
        }
    }
    
    public void InitializeMap()
    {
        costData = new int[width * height];
        for (int i = 0; i < costData.Length; i++)
        {
            costData[i] = defaultCost;
        }
    }
    
    public int GetCost(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return 0;
        return costData[y * width + x];
    }
    
    public void SetCost(int x, int y, int cost)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;
        costData[y * width + x] = Mathf.Clamp(cost, 0, maxCost);
    }
}
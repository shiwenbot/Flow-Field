// 3. FlowFieldCell - 单个格子组件
using UnityEngine;
using UnityEngine.UI;

public class FlowFieldCell : MonoBehaviour
{
    [Header("UI 组件")]
    public Image backgroundImage;
    public Text valueText;
    public Image targetIndicator;
    public Button button;
    
    // 网格位置
    public int GridX { get; private set; }
    public int GridY { get; private set; }
    
    // 事件
    public System.Action<int, int> OnCellClicked;
    
    private void Awake()
    {
        // 自动获取组件
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        
        if (valueText == null)
            valueText = GetComponentInChildren<Text>();
        
        if (button == null)
            button = GetComponent<Button>();
        
        // 设置按钮点击事件
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }
    
    public void Initialize(int x, int y)
    {
        GridX = x;
        GridY = y;
        
        // 创建目标指示器（如果不存在）
        if (targetIndicator == null)
        {
            CreateTargetIndicator();
        }
        
        SetTarget(false);
    }
    
    private void CreateTargetIndicator()
    {
        GameObject indicatorObj = new GameObject("TargetIndicator");
        indicatorObj.transform.SetParent(transform);
        
        RectTransform rectTransform = indicatorObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        targetIndicator = indicatorObj.AddComponent<Image>();
        targetIndicator.color = Color.yellow;
        targetIndicator.enabled = false;
    }
    
    private void OnButtonClicked()
    {
        OnCellClicked?.Invoke(GridX, GridY);
    }
    
    public void SetColor(Color color)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = color;
        }
    }
    
    public void SetText(string text)
    {
        if (valueText != null)
        {
            valueText.text = text;
        }
    }
    
    public void SetTarget(bool isTarget)
    {
        if (targetIndicator != null)
        {
            targetIndicator.enabled = isTarget;
        }
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
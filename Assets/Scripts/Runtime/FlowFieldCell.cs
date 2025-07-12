// FlowFieldCell3D - 3D版本的单个格子组件（改进版）
using UnityEngine;

public class FlowFieldCell : MonoBehaviour
{
    [Header("3D 组件")]
    public MeshRenderer meshRenderer;
    public Collider cellCollider;
    public Transform textTransform;
    
    [Header("材质")]
    public Material defaultMaterial;
    public Material targetMaterial;
    public Material impassableMaterial;
    
    [Header("文本显示")]
    public TextMesh textMesh;
    public bool showText = true;
    
    [Header("调试")]
    public bool enableDebugLogs = false;
    
    // 网格位置
    public int GridX { get; private set; }
    public int GridY { get; private set; }
    
    // 目标指示器
    private GameObject targetIndicator;
    private MeshRenderer targetRenderer;
    
    // 初始化状态
    private bool isInitialized = false;
    
    // 事件
    public System.Action<int, int> OnCellClicked;
    
    public void Initialize(int x, int y, float cellSize = 1f)
    {
        GridX = x;
        GridY = y;
        
        // 设置位置
        transform.position = new Vector3(x * cellSize, 0, y * cellSize);
        
        // 设置缩放
        transform.localScale = new Vector3(cellSize, transform.localScale.y, cellSize);
        
        // 创建目标指示器
        CreateTargetIndicator();
        
        // 创建文本显示
        CreateTextMesh();
        
        SetTarget(false);
        
        isInitialized = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"初始化格子 ({x}, {y})，位置: {transform.position}");
        }
    }
    
    private void CreateTargetIndicator()
    {
        if (targetIndicator == null)
        {
            targetIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetIndicator.name = "TargetIndicator";
            targetIndicator.transform.SetParent(transform);
            targetIndicator.transform.localPosition = new Vector3(0, 0.6f, 0);
            targetIndicator.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
            
            // 移除碰撞体，避免干扰点击
            Destroy(targetIndicator.GetComponent<Collider>());
            
            targetRenderer = targetIndicator.GetComponent<MeshRenderer>();
            if (targetRenderer != null)
            {
                Material targetMat = new Material(Shader.Find("Standard"));
                targetMat.color = Color.yellow;
                //targetMat.emission = Color.yellow * 0.3f;
                targetMat.EnableKeyword("_EMISSION");
                targetRenderer.material = targetMat;
            }
            
            targetIndicator.SetActive(false);
        }
    }
    
    private void CreateTextMesh()
    {
        if (textMesh == null && showText)
        {
            GameObject textObj = new GameObject("CellText");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = new Vector3(0, 0.6f, 0);
            textObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            textObj.transform.localScale = Vector3.one * 0.1f;
            
            textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = "";
            textMesh.fontSize = 80;
            textMesh.color = Color.black;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            
            textTransform = textObj.transform;
        }
    }
    
    private void OnMouseDown()
    {
        if (isInitialized)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"点击格子 ({GridX}, {GridY})");
            }
            OnCellClicked?.Invoke(GridX, GridY);
        }
    }
    
    public void SetColor(Color color)
    {
        if (meshRenderer != null)
        {
            // 创建新材质实例以避免共享材质问题
            if (meshRenderer.material != null)
            {
                Material newMaterial = new Material(meshRenderer.material);
                newMaterial.color = color;
                meshRenderer.material = newMaterial;
            }
            else
            {
                // 如果没有材质，创建一个默认材质
                Material newMaterial = new Material(Shader.Find("Standard"));
                newMaterial.color = color;
                meshRenderer.material = newMaterial;
            }
        }
    }
    
    public void SetMaterial(Material material)
    {
        if (meshRenderer != null && material != null)
        {
            meshRenderer.material = material;
        }
    }
    
    public void SetText(string text)
    {
        if (textMesh != null)
        {
            textMesh.text = text;
        }
    }
    
    public void SetTarget(bool isTarget)
    {
        if (targetIndicator != null)
        {
            targetIndicator.SetActive(isTarget);
        }
    }
    
    public void SetTextVisibility(bool visible)
    {
        if (textMesh != null)
        {
            textMesh.gameObject.SetActive(visible);
        }
    }
    
    // 为物理系统准备的方法
    public void EnablePhysics(bool enable)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (enable && rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // 默认为运动学刚体
        }
        else if (!enable && rb != null)
        {
            if (Application.isPlaying)
                Destroy(rb);
            else
                DestroyImmediate(rb);
        }
    }
    
    public void SetKinematic(bool kinematic)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = kinematic;
        }
    }
    
    // 获取当前颜色
    public Color GetCurrentColor()
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            return meshRenderer.material.color;
        }
        return Color.white;
    }
    
    // 重置为默认状态
    public void ResetToDefault()
    {
        SetTarget(false);
        SetText("");
        
        if (defaultMaterial != null)
        {
            SetMaterial(defaultMaterial);
        }
        else
        {
            SetColor(Color.white);
        }
    }
    
    private void OnDestroy()
    {
        // 清理事件
        OnCellClicked = null;
    }
}
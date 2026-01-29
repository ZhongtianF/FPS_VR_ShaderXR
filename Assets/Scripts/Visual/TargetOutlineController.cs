using Unity.XR.CoreUtils;
using UnityEngine;

public class TargetOutlineController : MonoBehaviour
{
    [Header("目标设置")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform targetObject;
    [SerializeField] private LayerMask obstacleMask = -1;

    [Header("轮廓参数")]
    [SerializeField] private Color outlineColor = Color.green;
    [SerializeField] private float outlineThickness = 0.05f;
    [SerializeField] private float outlineIntensity = 1.0f;
    [SerializeField] private float outlineAlpha = 0.8f;

    private Renderer targetRenderer;
    private MaterialPropertyBlock mpb;

    // 属性ID缓存
    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessID = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineIntensityID = Shader.PropertyToID("_OutlineIntensity");
    private static readonly int OutlineAlphaID = Shader.PropertyToID("_OutlineAlpha");
    private static readonly int OutlineEnabledID = Shader.PropertyToID("_OutlineEnabled");

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (targetObject == null)
        {
            targetObject = transform;
        }

        targetRenderer = targetObject.GetComponent<Renderer>();
        if (targetRenderer == null)
        {
            Debug.LogError("Target object must have a Renderer component!");
            enabled = false;
            return;
        }

        mpb = new MaterialPropertyBlock();

        // 设置相机
        if (playerCamera == null)
        {
            // 自动查找主相机或XR相机
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerCamera = mainCam.transform;
            }
            else
            {
                Debug.LogWarning("Player camera not assigned. Looking for XR camera...");
                var xrCamera = FindObjectOfType<XROrigin>()?.GetComponentInChildren<Camera>();
                if (xrCamera != null)
                {
                    playerCamera = xrCamera.transform;
                }
            }
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("Player camera is not assigned and cannot be found automatically.");
        }
    }

    void Update()
    {
        if (targetRenderer == null || playerCamera == null) return;

        // 检测遮挡
        bool isBlocked = CheckIfBlocked();

        // 设置轮廓显示
        UpdateOutline(isBlocked);

        // 调试绘制
        DrawDebugRay(isBlocked);
    }

    bool CheckIfBlocked()
    {
        Vector3 direction = targetObject.position - playerCamera.position;
        float distance = direction.magnitude;

        // 如果距离太近，不算遮挡
        if (distance < 0.5f) return false;

        RaycastHit hit;
        bool isBlocked = Physics.Raycast(
            playerCamera.position,
            direction.normalized,
            out hit,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        // 检查是否被自己遮挡（避免自遮挡）
        if (isBlocked && hit.collider.transform == targetObject)
        {
            return false;
        }

        return isBlocked;
    }

    void UpdateOutline(bool isBlocked)
    {
        targetRenderer.GetPropertyBlock(mpb);

        // 设置轮廓参数
        mpb.SetColor(OutlineColorID, outlineColor);
        mpb.SetFloat(OutlineThicknessID, isBlocked ? outlineThickness : 0f);
        mpb.SetFloat(OutlineIntensityID, outlineIntensity);
        mpb.SetFloat(OutlineAlphaID, outlineAlpha);
        mpb.SetFloat(OutlineEnabledID, isBlocked ? 1f : 0f);

        targetRenderer.SetPropertyBlock(mpb);
    }

    void DrawDebugRay(bool isBlocked)
    {
        Debug.DrawRay(
            playerCamera.position,
            (targetObject.position - playerCamera.position).normalized * 100f,
            isBlocked ? Color.red : Color.green,
            0.1f
        );
    }

    // 公开方法，用于外部控制
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
        if (mpb != null)
        {
            mpb.SetColor(OutlineColorID, outlineColor);
            targetRenderer?.SetPropertyBlock(mpb);
        }
    }

    public void SetOutlineThickness(float thickness)
    {
        outlineThickness = Mathf.Clamp(thickness, 0f, 0.1f);
    }

    public void SetTargetObject(Transform newTarget)
    {
        targetObject = newTarget;
        if (targetObject != null)
        {
            targetRenderer = targetObject.GetComponent<Renderer>();
        }
    }
}
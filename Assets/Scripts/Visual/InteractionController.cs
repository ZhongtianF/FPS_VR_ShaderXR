using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable), typeof(Renderer))]
public class InteractionController : MonoBehaviour
{
    [Header("视觉设置")]
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color grabColor = Color.cyan;
    [SerializeField] private float hoverEmission = 1.0f;
    [SerializeField] private float grabEmission = 3.0f;

    // URP Lit Shader内置属性ID
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");

    private Renderer objectRenderer;
    private MaterialPropertyBlock mpb;
    private XRGrabInteractable grabInteractable;

    // 原始属性备份
    private Color originalBaseColor;
    private Color originalEmissionColor;
    private float originalSmoothness;

    void Awake()
    {
        InitializeComponents();
        BackupOriginalProperties();
        SetupInteractionEvents();
    }

    void InitializeComponents()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError($"Renderer component required on {gameObject.name}");
            enabled = false;
            return;
        }

        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogWarning($"Adding XRGrabInteractable to {gameObject.name}");
            grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
        }

        mpb = new MaterialPropertyBlock();
    }

    void BackupOriginalProperties()
    {
        if (objectRenderer == null) return;

        objectRenderer.GetPropertyBlock(mpb);

        // 备份原始属性
        originalBaseColor = mpb.GetColor(BaseColorID);
        originalEmissionColor = mpb.GetColor(EmissionColorID);
        originalSmoothness = mpb.GetFloat(SmoothnessID);

        // 如果没有设置过，使用默认值
        if (originalBaseColor == Color.clear) originalBaseColor = Color.white;
        if (originalEmissionColor == Color.clear) originalEmissionColor = Color.black;
    }

    void SetupInteractionEvents()
    {
        // Hover事件
        grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        grabInteractable.hoverExited.AddListener(OnHoverExited);

        // Grab事件
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        SetHighlight(hoverColor, hoverEmission, "hover");
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        ResetHighlight();
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        SetHighlight(grabColor, grabEmission, "grab");
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        // 如果仍然在悬停状态，恢复悬停高亮
        if (grabInteractable.isHovered)
        {
            SetHighlight(hoverColor, hoverEmission, "hover");
        }
        else
        {
            ResetHighlight();
        }
    }

    void SetHighlight(Color color, float emissionIntensity, string state = "")
    {
        if (objectRenderer == null || mpb == null) return;

        objectRenderer.GetPropertyBlock(mpb);

        // 设置基础颜色 - 轻微染色
        Color tintedBaseColor = Color.Lerp(originalBaseColor, color, 0.3f);
        mpb.SetColor(BaseColorID, tintedBaseColor);

        // 设置自发光颜色和强度
        Color emissionColor = color * emissionIntensity;
        mpb.SetColor(EmissionColorID, emissionColor);

        // 增加光滑度增强效果
        float enhancedSmoothness = originalSmoothness + (emissionIntensity * 0.2f);
        mpb.SetFloat(SmoothnessID, Mathf.Clamp01(enhancedSmoothness));

        objectRenderer.SetPropertyBlock(mpb);

        // 调试信息
        Debug.Log($"[{gameObject.name}] Highlight set - State: {state}, Color: {color}, Emission: {emissionIntensity}");
    }

    void ResetHighlight()
    {
        if (objectRenderer == null || mpb == null) return;

        objectRenderer.GetPropertyBlock(mpb);

        // 恢复原始属性
        mpb.SetColor(BaseColorID, originalBaseColor);
        mpb.SetColor(EmissionColorID, originalEmissionColor);
        mpb.SetFloat(SmoothnessID, originalSmoothness);

        objectRenderer.SetPropertyBlock(mpb);

        Debug.Log($"[{gameObject.name}] Highlight reset");
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }
}
using UnityEngine;

public class DroneVisualFeedback : MonoBehaviour
{
    [Header("渲染组件")]
    [SerializeField] private Renderer droneRenderer;
    [SerializeField] private Light droneLight;

    [Header("颜色配置")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color followColor = Color.cyan;
    [SerializeField] private Color avoidColor = Color.yellow;
    [SerializeField] private Color stuckColor = Color.red;
    [SerializeField] private Color alertColor = new Color(1f, 0.5f, 0f);

    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        if (droneRenderer != null)
        {
            droneRenderer.GetPropertyBlock(mpb);
        }
    }

    public void UpdateVisuals(DroneState state, float intensityMultiplier = 1f)
    {
        if (droneRenderer == null) return;

        Color targetColor = GetStateColor(state);
        float brightness = 0.5f + intensityMultiplier * 0.5f;

        // 设置基础颜色
        mpb.SetColor(BaseColorID, targetColor * brightness);

        // 设置自发光（稍微减弱）
        mpb.SetColor(EmissionColorID, targetColor * intensityMultiplier * 0.3f);

        droneRenderer.SetPropertyBlock(mpb);

        // 更新灯光
        if (droneLight != null)
        {
            droneLight.color = targetColor;
            droneLight.intensity = 0.5f + intensityMultiplier;
        }
    }

    private Color GetStateColor(DroneState state)
    {
        return state switch
        {
            DroneState.Idle => idleColor,
            DroneState.Following => followColor,
            DroneState.Avoiding => avoidColor,
            DroneState.Stuck => stuckColor,
            DroneState.Alert => alertColor,
            _ => Color.white
        };
    }

    public void PulseEffect(float duration = 0.5f)
    {
        // 简单的闪烁效果
        StartCoroutine(PulseCoroutine(duration));
    }

    private System.Collections.IEnumerator PulseCoroutine(float duration)
    {
        float elapsed = 0f;
        Color originalColor = mpb.GetColor(BaseColorID);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float pulse = Mathf.Sin(t * Mathf.PI * 4f) * 0.5f + 0.5f;

            mpb.SetColor(BaseColorID, originalColor * (1f + pulse * 0.3f));
            droneRenderer.SetPropertyBlock(mpb);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mpb.SetColor(BaseColorID, originalColor);
        droneRenderer.SetPropertyBlock(mpb);
    }
}
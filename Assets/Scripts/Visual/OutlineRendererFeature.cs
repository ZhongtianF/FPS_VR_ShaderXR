using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material outlineMaterial;
        public LayerMask outlineLayer = -1;
        public Color outlineColor = Color.green;
        [Range(0.01f, 0.1f)]
        public float outlineThickness = 0.05f;

        [Header("调试")]
        public bool showInSceneView = true;
    }

    public Settings settings = new Settings();

    private OutlineRenderPass outlinePass;
    private bool isInitialized = false;

    public override void Create()
    {
        if (outlinePass == null)
        {
            outlinePass = new OutlineRenderPass(settings);
            isInitialized = true;
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 检查是否应该在SceneView中显示
        Camera camera = renderingData.cameraData.camera;
        bool shouldRender = settings.showInSceneView || camera.cameraType == CameraType.Game;

        if (settings.outlineMaterial != null && shouldRender && isInitialized)
        {
            // 配置输入
            outlinePass.ConfigureInput(ScriptableRenderPassInput.Color);
            outlinePass.ConfigureInput(ScriptableRenderPassInput.Depth);

            renderer.EnqueuePass(outlinePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (outlinePass != null)
        {
            outlinePass.Dispose();
            outlinePass = null;
        }
        base.Dispose(disposing);
    }
}
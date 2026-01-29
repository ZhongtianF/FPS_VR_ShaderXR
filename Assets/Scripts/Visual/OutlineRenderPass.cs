using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineRenderPass : ScriptableRenderPass
{
    private Material outlineMaterial;
    private LayerMask outlineLayer;
    private Color outlineColor;
    private float outlineThickness;

    // 使用 RTHandle 替代旧的 RenderTargetHandle
    private RTHandle cameraColorTarget;
    private RTHandle tempTexture;

    // 过滤器设置
    private FilteringSettings filteringSettings;

    // 渲染器列表
    private List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();

    public OutlineRenderPass(OutlineRendererFeature.Settings settings)
    {
        this.outlineMaterial = settings.outlineMaterial;
        this.outlineLayer = settings.outlineLayer;
        this.outlineColor = settings.outlineColor;
        this.outlineThickness = settings.outlineThickness;

        // 设置渲染事件（在天空盒之后渲染）
        renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

        // 配置过滤设置
        filteringSettings = new FilteringSettings(
            RenderQueueRange.opaque,
            settings.outlineLayer
        );

        // 设置要渲染的Shader标签
        shaderTagIdList.Clear();
        shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
        shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
        shaderTagIdList.Add(new ShaderTagId("LightweightForward"));

        // 使用RenderingUtils分配RTHandle
        tempTexture = RTHandles.Alloc("_OutlineTempTexture", name: "_OutlineTempTexture");
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // 获取相机的颜色目标
        cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

        // 配置临时纹理的描述符
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;

        // 重新分配临时纹理
        RenderingUtils.ReAllocateIfNeeded(ref tempTexture, descriptor,
            FilterMode.Bilinear, TextureWrapMode.Clamp,
            name: "_OutlineTempTexture");

        // 配置渲染目标
        ConfigureTarget(cameraColorTarget, renderingData.cameraData.renderer.cameraDepthTargetHandle);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (outlineMaterial == null)
        {
            Debug.LogWarning("Outline material is not assigned.");
            return;
        }

        // 获取主相机
        Camera camera = renderingData.cameraData.camera;
        if (camera.cameraType != CameraType.Game)
            return;

        // 创建命令缓冲区
        CommandBuffer cmd = CommandBufferPool.Get("Outline Render Pass");

        using (new ProfilingScope(cmd, new ProfilingSampler("Outline Rendering")))
        {
            // 设置材质参数
            outlineMaterial.SetColor("_OutlineColor", outlineColor);
            outlineMaterial.SetFloat("_OutlineThickness", outlineThickness);

            // 创建绘制设置
            DrawingSettings drawingSettings = CreateDrawingSettings(
                shaderTagIdList,
                ref renderingData,
                renderingData.cameraData.defaultOpaqueSortFlags
            );

            // 覆盖材质
            drawingSettings.overrideMaterial = outlineMaterial;
            drawingSettings.overrideMaterialPassIndex = 0;
            drawingSettings.perObjectData = PerObjectData.None;

            // 渲染轮廓物体
            context.DrawRenderers(
                renderingData.cullResults,
                ref drawingSettings,
                ref filteringSettings
            );
        }

        // 执行命令缓冲区
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // 清理临时纹理
        if (tempTexture != null)
        {
            // 如果不再需要，可以释放纹理
            // RTHandles.Release(tempTexture);
        }
    }

    // 释放资源
    public void Dispose()
    {
        if (tempTexture != null)
        {
            RTHandles.Release(tempTexture);
            tempTexture = null;
        }
    }
}
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SceneManager : MonoBehaviour
{
    [Header("视觉系统配置")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Material highlightMaterial;

    [Header("目标物体")]
    [SerializeField] private GameObject outlineTarget;
    [SerializeField] private GameObject[] interactiveObjects;

    void Start()
    {
        InitializeVisualSystems();
        SetupInteractiveObjects();
    }

    void InitializeVisualSystems()
    {
        // 创建URP Renderer Feature的简化版本
        SetupOutlineSystem();
    }

    void SetupOutlineSystem()
    {
        if (outlineTarget != null && outlineMaterial != null)
        {
            // 给目标物体添加轮廓控制器
            var outlineController = outlineTarget.AddComponent<TargetOutlineController>();

            // 设置轮廓材质
            var renderer = outlineTarget.GetComponent<Renderer>();
            if (renderer != null)
            {
                // 复制原始材质并添加轮廓Shader
                var materials = renderer.sharedMaterials;
                var newMaterials = new Material[materials.Length + 1];
                materials.CopyTo(newMaterials, 0);
                newMaterials[materials.Length] = outlineMaterial;
                renderer.sharedMaterials = newMaterials;
            }
        }
    }

    void SetupInteractiveObjects()
    {
        foreach (var obj in interactiveObjects)
        {
            if (obj != null)
            {
                // 确保物体有Renderer
                var renderer = obj.GetComponent<Renderer>();
                if (renderer == null)
                {
                    Debug.LogWarning($"Interactive object {obj.name} has no Renderer component.");
                    continue;
                }

                // 设置高亮材质
                if (highlightMaterial != null)
                {
                    renderer.sharedMaterial = highlightMaterial;
                }

                // 添加交互控制器
                if (obj.GetComponent<InteractionController>() == null)
                {
                    obj.AddComponent<InteractionController>();
                }

                // 添加XR Grab Interactable（如果还没有）
                if (obj.GetComponent<XRGrabInteractable>() == null)
                {
                    var grabInteractable = obj.AddComponent<XRGrabInteractable>();
                    // 配置抓取设置
                    grabInteractable.movementType = XRGrabInteractable.MovementType.VelocityTracking;
                    grabInteractable.throwOnDetach = false;
                }
            }
        }
    }

    // 公开方法，用于UI控制
    public void SetOutlineTarget(GameObject target)
    {
        outlineTarget = target;
        SetupOutlineSystem();
    }

    public void AddInteractiveObject(GameObject obj)
    {
        if (obj != null)
        {
            var list = new System.Collections.Generic.List<GameObject>(interactiveObjects);
            if (!list.Contains(obj))
            {
                list.Add(obj);
                interactiveObjects = list.ToArray();

                // 立即设置
                var tempArray = new GameObject[] { obj };
                interactiveObjects = tempArray;
                SetupInteractiveObjects();
            }
        }
    }
}
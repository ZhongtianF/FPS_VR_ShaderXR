using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DualControllerSync : MonoBehaviour
{
    public enum SyncMode
    {
        PlayerLeads,    // Player控制移动，XRRig跟随
        XRLeads,        // XRRig控制移动，Player跟随
        Hybrid          // 混合模式
    }

    [Header("同步模式")]
    [SerializeField] private SyncMode syncMode = SyncMode.PlayerLeads;

    [Header("引用")]
    [SerializeField] private Transform playerController;   // 传统Player控制器
    [SerializeField] private XROrigin xrRig;               // XR设备

    [Header("同步参数")]
    [SerializeField] private float positionSyncSpeed = 10f;
    [SerializeField] private float rotationSyncSpeed = 5f;
    [SerializeField] private float maxSyncDistance = 5f;   // 最大同步距离

    // 状态
    private Vector3 lastPlayerPosition;
    private Quaternion lastPlayerRotation;

    private void Start()
    {
        // 初始化引用
        if (playerController == null)
            playerController = transform;

        if (xrRig == null)
            xrRig = FindObjectOfType<XROrigin>();

        // 记录初始位置
        lastPlayerPosition = playerController.position;
        lastPlayerRotation = playerController.rotation;
    }

    private void Update()
    {
        switch (syncMode)
        {
            case SyncMode.PlayerLeads:
                SyncXRToPlayer();
                break;

            case SyncMode.XRLeads:
                SyncPlayerToXR();
                break;

            case SyncMode.Hybrid:
                SyncHybrid();
                break;
        }
    }

    private void SyncXRToPlayer()
    {
        if (xrRig == null) return;

        // 计算Player的移动量
        Vector3 playerMovement = playerController.position - lastPlayerPosition;
        Quaternion playerRotationChange = playerController.rotation * Quaternion.Inverse(lastPlayerRotation);

        // 应用移动量到XRRig
        xrRig.transform.position += playerMovement;

        // 应用旋转（可选）
        Vector3 rotationEuler = playerRotationChange.eulerAngles;
        if (rotationEuler.magnitude > 0.1f)
        {
            xrRig.transform.rotation *= playerRotationChange;
        }

        // 更新记录
        lastPlayerPosition = playerController.position;
        lastPlayerRotation = playerController.rotation;
    }

    private void SyncPlayerToXR()
    {
        if (xrRig == null) return;

        // Player平滑跟随XRRig
        playerController.position = Vector3.Lerp(
            playerController.position,
            xrRig.transform.position,
            positionSyncSpeed * Time.deltaTime
        );

        playerController.rotation = Quaternion.Slerp(
            playerController.rotation,
            xrRig.transform.rotation,
            rotationSyncSpeed * Time.deltaTime
        );
    }

    private void SyncHybrid()
    {
        // 水平移动由Player控制
        Vector3 playerPos = playerController.position;
        Vector3 xrPos = xrRig.transform.position;

        // 只同步XZ平面位置
        Vector3 targetPos = new Vector3(
            playerPos.x,
            xrPos.y,  // 高度由XR控制（玩家真实身高）
            playerPos.z
        );

        xrRig.transform.position = Vector3.Lerp(
            xrRig.transform.position,
            targetPos,
            positionSyncSpeed * Time.deltaTime
        );

        // 旋转由XR控制
        playerController.rotation = Quaternion.Slerp(
            playerController.rotation,
            xrRig.transform.rotation,
            rotationSyncSpeed * Time.deltaTime
        );
    }

    // 防止XR与Player距离过远
    private void CheckDistance()
    {
        if (Vector3.Distance(playerController.position, xrRig.transform.position) > maxSyncDistance)
        {
            Debug.LogWarning("Player与XRRig距离过远，强制同步");
            playerController.position = xrRig.transform.position;
        }
    }
}
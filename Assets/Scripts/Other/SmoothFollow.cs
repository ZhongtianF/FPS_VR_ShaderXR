using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    [Header("跟随设置")]
    [SerializeField] private Transform target;          // 要跟随的目标
    [SerializeField] private bool followPosition = true; // 是否跟随位置
    [SerializeField] private bool followRotation = true; // 是否跟随旋转

    [Header("平滑参数")]
    [SerializeField] private float positionSmoothTime = 0.1f; // 位置平滑时间
    [SerializeField] private float rotationSmoothTime = 0.1f; // 旋转平滑时间
    [SerializeField] private float maxPositionSpeed = Mathf.Infinity; // 最大移动速度
    [SerializeField] private float maxRotationSpeed = Mathf.Infinity; // 最大旋转速度

    // 用于SmoothDamp的当前速度
    private Vector3 positionVelocity = Vector3.zero;
    private Vector3 rotationVelocity = Vector3.zero;

    private void Update()
    {
        if (target == null) return;

        // 平滑跟随位置
        if (followPosition)
        {
            Vector3 targetPosition = target.position;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref positionVelocity,
                positionSmoothTime,
                maxPositionSpeed
            );
        }

        // 平滑跟随旋转
        if (followRotation)
        {
            Quaternion targetRotation = target.rotation;

            // 将四元数转换为欧拉角进行SmoothDamp
            Vector3 currentEuler = transform.eulerAngles;
            Vector3 targetEuler = targetRotation.eulerAngles;

            // 处理角度环绕
            targetEuler.x = Mathf.DeltaAngle(currentEuler.x, targetEuler.x) + currentEuler.x;
            targetEuler.y = Mathf.DeltaAngle(currentEuler.y, targetEuler.y) + currentEuler.y;
            targetEuler.z = Mathf.DeltaAngle(currentEuler.z, targetEuler.z) + currentEuler.z;

            Vector3 smoothedEuler = Vector3.SmoothDamp(
                currentEuler,
                targetEuler,
                ref rotationVelocity,
                rotationSmoothTime,
                maxRotationSpeed
            );

            transform.rotation = Quaternion.Euler(smoothedEuler);
        }
    }

    // 可选：在LateUpdate中更新，确保在目标移动后执行
    // private void LateUpdate() { ... 相同代码 ... }
}
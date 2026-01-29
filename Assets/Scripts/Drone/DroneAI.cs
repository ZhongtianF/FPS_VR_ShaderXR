using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroneAI : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] private Transform playerTarget;

    [Header("配置")]
    [SerializeField] private DroneData droneData;

    [Header("组件")]
    [SerializeField] private DroneStateManager stateManager;
    [SerializeField] private DroneVisualFeedback visualFeedback;

    // 物理组件
    private Rigidbody rb;

    // 计算变量
    private Vector3 targetPosition;
    private Vector3 desiredDirection;
    private Vector3 smoothedVelocity;
    private Vector3 velocitySmoothVelocity;

    // 旋转控制
    private Quaternion targetRotation;
    private float rotationSpeedMultiplier = 1f;

    // 状态变量
    private bool isNearObstacle = false;
    private bool hasClearPath = true;
    private float lastPathCheckTime = 0f;

    // 稳定变量
    private Vector3 lastStablePosition;
    private float idleTimer = 0f;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        InitializeDrone();
        lastStablePosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (playerTarget == null || droneData == null) return;

        // 更新状态
        UpdateStateVariables();
        stateManager.UpdateState(transform.position, rb.velocity,
                                isNearObstacle, hasClearPath, droneData);

        // 计算移动
        desiredDirection = CalculateMoveDirection();
        float targetSpeed = CalculateTargetSpeed();

        // 计算最终速度
        Vector3 finalVelocity = desiredDirection.normalized * targetSpeed;

        // 应用平滑
        smoothedVelocity = Vector3.SmoothDamp(smoothedVelocity,
                                            finalVelocity,
                                            ref velocitySmoothVelocity,
                                            droneData.velocitySmoothTime);

        // 应用移动
        ApplyMovement(smoothedVelocity);

        // 更新旋转
        UpdateRotation();

        // 更新视觉反馈
        UpdateVisualFeedback();
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();

        if (stateManager == null)
            stateManager = GetComponent<DroneStateManager>();

        if (visualFeedback == null)
            visualFeedback = GetComponent<DroneVisualFeedback>();
    }

    private void InitializeDrone()
    {
        rb.useGravity = false;
        rb.drag = 2f;           // 增加阻尼，更快停止
        rb.angularDrag = 3f;    // 增加角阻尼，减少旋转

        // 冻结不必要的旋转
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ;

        if (stateManager != null)
        {
            stateManager.Initialize();
            stateManager.OnStateChanged += HandleStateChanged;
        }
    }

    private void UpdateStateVariables()
    {
        // 检查前方是否有障碍
        CheckForwardObstacle();

        // 定期检查路径
        if (Time.time - lastPathCheckTime > 0.3f)
        {
            hasClearPath = CheckClearPathToTarget();
            lastPathCheckTime = Time.time;
        }
    }

    private void CheckForwardObstacle()
    {
        Vector3 forward = transform.forward;
        float checkDistance = droneData.lookAheadDistance;

        RaycastHit hit;
        isNearObstacle = Physics.SphereCast(transform.position,
                                           droneData.obstacleCheckRadius,
                                           forward,
                                           out hit,
                                           checkDistance,
                                           droneData.obstacleMask);

        if (isNearObstacle && hit.distance < droneData.alertDistance)
        {
            stateManager.ForceState(DroneState.Alert);
        }
    }

    private bool CheckClearPathToTarget()
    {
        if (playerTarget == null) return false;

        Vector3 toTarget = playerTarget.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance < 1f) return true;

        RaycastHit hit;
        bool hasHit = Physics.SphereCast(transform.position,
                                        droneData.obstacleCheckRadius * 0.5f,
                                        toTarget.normalized,
                                        out hit,
                                        distance,
                                        droneData.obstacleMask,
                                        QueryTriggerInteraction.Ignore);

        return !hasHit;
    }

    private Vector3 CalculateMoveDirection()
    {
        // 计算相对于玩家的目标位置
        Vector3 desiredOffset = CalculateDesiredOffset();
        targetPosition = playerTarget.position + desiredOffset;

        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // 当距离很近时，停止精确朝向
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < 0.5f)
        {
            return Vector3.zero; // 完全停止
        }

        // 如果路径清晰且没有障碍，直接朝向目标
        if (hasClearPath && !isNearObstacle)
        {
            return directionToTarget;
        }

        // 需要避障的情况
        return CalculateAvoidanceDirection(directionToTarget);
    }

    private Vector3 CalculateDesiredOffset()
    {
        // 使用玩家旋转后的偏移，这样无人机保持在玩家视角的后上方
        Vector3 playerForward = playerTarget.forward;
        Vector3 playerRight = playerTarget.right;

        Vector3 offset = Vector3.zero;
        offset += -playerForward * droneData.followDistance;  // 后方
        offset += playerRight * droneData.horizontalOffset;   // 右侧
        offset += Vector3.up * droneData.heightOffset;        // 上方

        return offset;
    }

    private Vector3 CalculateAvoidanceDirection(Vector3 desiredDirection)
    {
        List<Vector3> directions = new List<Vector3>();

        // 生成主要方向
        directions.Add(desiredDirection); // 首要方向
        directions.Add(transform.forward); // 当前方向
        directions.Add(Vector3.up); // 向上
        directions.Add((desiredDirection + Vector3.up).normalized); // 斜向上

        // 生成更多采样方向
        for (int i = 0; i < droneData.sampleDirections - 4; i++)
        {
            float angle = (i / (float)(droneData.sampleDirections - 4)) * 360f;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * desiredDirection;
            direction.Normalize();
            directions.Add(direction);
        }

        // 评估每个方向
        Vector3 bestDirection = desiredDirection;
        float bestScore = -Mathf.Infinity;

        foreach (Vector3 dir in directions)
        {
            float score = EvaluateDirection(dir, desiredDirection);

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = dir;
            }
        }

        return bestDirection;
    }

    private float EvaluateDirection(Vector3 direction, Vector3 desiredDirection)
    {
        float score = 0f;

        // 1. 安全分数（60%权重）
        float safetyScore = GetDirectionSafety(direction);
        score += safetyScore * 0.6f;

        // 2. 朝向目标分数（40%权重）
        float alignmentScore = Vector3.Dot(direction, desiredDirection) * 0.5f + 0.5f;
        score += alignmentScore * 0.4f;

        return score;
    }

    private float GetDirectionSafety(Vector3 direction)
    {
        float checkDistance = droneData.sampleDistance;

        RaycastHit hit;
        bool hasHit = Physics.SphereCast(transform.position,
                                        droneData.obstacleCheckRadius,
                                        direction,
                                        out hit,
                                        checkDistance,
                                        droneData.obstacleMask);

        if (!hasHit) return 1.0f;

        float normalizedDistance = hit.distance / checkDistance;
        return Mathf.Clamp01(normalizedDistance * 2f);
    }

    private float CalculateTargetSpeed()
    {
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        // 根据状态调整速度
        if (stateManager.currentState == DroneState.Idle ||
            stateManager.currentState == DroneState.Stuck)
        {
            return 0f; // 完全停止
        }

        if (distanceToTarget > droneData.followDistance * 1.5f)
        {
            // 距离很远，快速接近
            return droneData.moveSpeed;
        }
        else if (distanceToTarget < droneData.followDistance * 0.7f)
        {
            // 太近了，缓慢后退
            return -droneData.moveSpeed * 0.2f;
        }
        else
        {
            // 理想距离，低速调整
            return droneData.moveSpeed * 0.3f;
        }
    }

    private void ApplyMovement(Vector3 velocity)
    {
        // 当速度很小时，直接归零避免微小抖动
        if (velocity.magnitude < 0.1f)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        // 限制最大速度
        if (velocity.magnitude > droneData.maxSpeed)
        {
            velocity = velocity.normalized * droneData.maxSpeed;
        }

        rb.velocity = velocity;
    }

    private void UpdateRotation()
    {
        float currentSpeed = rb.velocity.magnitude;

        // 当速度很小时，保持当前旋转，不要来回摇摆
        if (currentSpeed < 0.1f)
        {
            // 轻微阻尼，帮助稳定
            rb.angularVelocity = Vector3.zero;
            return;
        }

        // 计算目标旋转：看向移动方向
        Vector3 lookDirection = rb.velocity.normalized;

        // 避免看向零向量
        if (lookDirection.magnitude > 0.01f)
        {
            targetRotation = Quaternion.LookRotation(lookDirection);

            // 根据速度调整旋转速度（速度越快转得越快）
            float speedFactor = Mathf.Clamp01(currentSpeed / droneData.moveSpeed);
            float dynamicRotationSpeed = droneData.rotationSpeed * speedFactor;

            // 平滑旋转
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                targetRotation,
                                                Time.fixedDeltaTime * dynamicRotationSpeed);
        }
    }

    private void UpdateVisualFeedback()
    {
        if (visualFeedback != null)
        {
            float intensityMultiplier = Mathf.Clamp01(rb.velocity.magnitude / droneData.moveSpeed);
            visualFeedback.UpdateVisuals(stateManager.currentState, intensityMultiplier);
        }
    }

    private void HandleStateChanged(DroneState oldState, DroneState newState)
    {
        Debug.Log($"无人机状态: {oldState} -> {newState}");

        // 状态变化时的特殊处理
        switch (newState)
        {
            case DroneState.Idle:
                // 空闲状态：完全停止
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                break;

            case DroneState.Stuck:
                // 卡住状态：尝试向上飞
                rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
                break;
        }

        if (newState == DroneState.Alert || newState == DroneState.Stuck)
        {
            if (visualFeedback != null)
                visualFeedback.PulseEffect();
        }
    }

    // 公共方法
    public void SetTarget(Transform newTarget)
    {
        playerTarget = newTarget;
    }

    public void SetDroneData(DroneData newData)
    {
        droneData = newData;
    }

    // 调试绘制
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || playerTarget == null) return;

        // 绘制目标位置（青色）
        Gizmos.color = Color.cyan;
        Vector3 desiredOffset = CalculateDesiredOffset();
        Vector3 targetPos = playerTarget.position + desiredOffset;
        Gizmos.DrawWireSphere(targetPos, 0.5f);
        Gizmos.DrawLine(transform.position, targetPos);

        // 绘制当前速度方向（蓝色）
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, rb.velocity.normalized * 2f);

        // 绘制到玩家的方向（绿色）
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, playerTarget.position);
    }
}
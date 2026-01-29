using UnityEngine;

[CreateAssetMenu(fileName = "DroneData", menuName = "Drone/Drone Configuration")]
public class DroneData : ScriptableObject
{
    [Header("跟随设置")]
    [Tooltip("距离玩家的跟随距离")]
    public float followDistance = 4f;

    [Tooltip("相对于玩家的高度偏移")]
    public float heightOffset = 2f;

    [Tooltip("相对于玩家的水平偏移")]
    public float horizontalOffset = 1.5f;

    [Header("移动参数")]
    [Tooltip("基础移动速度")]
    [Range(0.1f, 10f)]
    public float moveSpeed = 3f;

    [Tooltip("转向速度")]
    [Range(0.1f, 10f)]
    public float rotationSpeed = 4f;

    [Tooltip("最大移动速度")]
    [Range(1f, 20f)]
    public float maxSpeed = 8f;

    [Header("避障参数")]
    [Tooltip("障碍检测半径")]
    [Range(0.1f, 5f)]
    public float obstacleCheckRadius = 0.8f;

    [Tooltip("避障强度")]
    [Range(1f, 20f)]
    public float obstacleAvoidanceForce = 5f;

    [Tooltip("前瞻检测距离")]
    [Range(1f, 10f)]
    public float lookAheadDistance = 3f;

    [Tooltip("障碍物层级")]
    public LayerMask obstacleMask = -1;

    [Header("智能参数")]
    [Tooltip("采样方向数量（越多越智能，但越耗性能）")]
    [Range(4, 16)]
    public int sampleDirections = 8;

    [Tooltip("采样距离")]
    [Range(1f, 10f)]
    public float sampleDistance = 4f;

    [Tooltip("安全距离乘数")]
    [Range(0.5f, 3f)]
    public float safetyMargin = 1.5f;

    [Header("平滑设置")]
    [Tooltip("位置平滑时间")]
    [Range(0.01f, 1f)]
    public float positionSmoothTime = 0.2f;

    [Tooltip("速度平滑时间")]
    [Range(0.01f, 1f)]
    public float velocitySmoothTime = 0.15f;

    [Tooltip("最大加速度")]
    [Range(1f, 30f)]
    public float maxAcceleration = 12f;

    [Header("状态参数")]
    [Tooltip("空闲速度阈值")]
    [Range(0.01f, 1f)]
    public float idleThreshold = 0.1f;

    [Tooltip("警戒距离（离障碍太近）")]
    [Range(0.5f, 5f)]
    public float alertDistance = 1.5f;

    [Tooltip("卡住时间阈值")]
    [Range(1f, 10f)]
    public float stuckTimeThreshold = 3f;

    [Tooltip("理想跟随距离范围（最小）")]
    public float minFollowDistance = 3f;

    [Tooltip("理想跟随距离范围（最大）")]
    public float maxFollowDistance = 6f;
}
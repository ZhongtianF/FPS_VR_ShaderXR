using UnityEngine;

public enum DroneState
{
    Idle,       // 空闲
    Following,  // 跟随
    Avoiding,   // 避障
    Stuck,      // 卡住
    Alert       // 警戒
}

public class DroneStateManager : MonoBehaviour
{
    [Header("状态")]
    public DroneState currentState = DroneState.Idle;
    public DroneState previousState = DroneState.Idle;

    [Header("状态参数")]
    [SerializeField] private float stateChangeCooldown = 0.5f;
    [SerializeField] private float stuckCheckInterval = 0.5f;

    // 状态变量
    private float lastStateChangeTime = 0f;
    private float lastStuckCheckTime = 0f;
    private Vector3 lastPosition = Vector3.zero;
    private float stuckTimer = 0f;
    private float idleTimer = 0f;
    private const float IDLE_TIME_THRESHOLD = 2f;

    // 事件
    public System.Action<DroneState, DroneState> OnStateChanged;

    public void Initialize()
    {
        lastPosition = transform.position;
        ChangeState(DroneState.Following); // 初始化为跟随状态
    }

    public void UpdateState(Vector3 currentPosition, Vector3 velocity,
                           bool isNearObstacle, bool hasClearPath,
                           DroneData data)
    {
        // 状态切换冷却
        if (Time.time - lastStateChangeTime < stateChangeCooldown)
            return;

        // 检查是否卡住
        CheckIfStuck(currentPosition, data);

        // 根据条件切换状态
        DroneState newState = DetermineState(velocity, isNearObstacle, hasClearPath, data);

        if (newState != currentState)
        {
            ChangeState(newState);
        }
    }

    private DroneState DetermineState(Vector3 velocity, bool isNearObstacle,
                                     bool hasClearPath, DroneData data)
    {
        float speed = velocity.magnitude;

        // 检查是否接近障碍
        if (isNearObstacle)
            return DroneState.Alert;

        // 检查是否有清晰路径
        if (!hasClearPath)
            return DroneState.Avoiding;

        // 检查是否卡住
        if (currentState == DroneState.Stuck)
            return DroneState.Stuck;

        // 检查是否应该空闲（长时间低速）
        if (speed < data.idleThreshold)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= IDLE_TIME_THRESHOLD)
            {
                idleTimer = 0f;
                return DroneState.Idle;
            }
        }
        else
        {
            idleTimer = 0f;
        }

        // 默认跟随状态
        return DroneState.Following;
    }

    private void CheckIfStuck(Vector3 currentPosition, DroneData data)
    {
        if (Time.time - lastStuckCheckTime < stuckCheckInterval)
            return;

        lastStuckCheckTime = Time.time;

        float distanceMoved = Vector3.Distance(currentPosition, lastPosition);
        lastPosition = currentPosition;

        // 只有在移动状态且移动距离很小时才增加卡住计时器
        if (distanceMoved < 0.05f && currentState != DroneState.Idle)
        {
            stuckTimer += stuckCheckInterval;

            if (stuckTimer >= data.stuckTimeThreshold)
            {
                ChangeState(DroneState.Stuck);
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = Mathf.Max(0f, stuckTimer - stuckCheckInterval);
        }
    }

    private void ChangeState(DroneState newState)
    {
        previousState = currentState;
        currentState = newState;
        lastStateChangeTime = Time.time;

        Debug.Log($"无人机状态变化: {previousState} -> {currentState}");

        OnStateChanged?.Invoke(previousState, currentState);
    }

    public void ForceState(DroneState newState)
    {
        ChangeState(newState);
    }

    public bool IsMoving()
    {
        return currentState == DroneState.Following ||
               currentState == DroneState.Avoiding;
    }

    public bool IsInAlert()
    {
        return currentState == DroneState.Alert ||
               currentState == DroneState.Stuck;
    }
}
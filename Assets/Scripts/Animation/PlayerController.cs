using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("动画组件")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;

    [Header("相机")]
    [SerializeField] private Transform cameraTransform;

    // 输入变量
    private float horizontalInput;
    private float verticalInput;
    private bool isRunning;

    // 动画参数ID缓存（性能优化）
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
    private static readonly int IsReloadingHash = Animator.StringToHash("IsReloading");
    private static readonly int EquipWeaponHash = Animator.StringToHash("EquipWeapon");

    void Start()
    {
        // 自动获取组件
        if (animator == null)
            animator = GetComponent<Animator>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        GetInput();
        HandleMovement();
        HandleAnimation();
        HandleActions();
    }

    void GetInput()
    {
        // 移动输入
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        // 奔跑输入
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // 重置动画参数
        if (horizontalInput == 0 && verticalInput == 0)
        {
            animator.SetFloat(SpeedHash, 0f);
        }
    }

    void HandleMovement()
    {
        // 计算移动方向
        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput);

        // 相对于相机的方向
        if (cameraTransform != null)
        {
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            moveDirection = cameraForward * verticalInput + cameraRight * horizontalInput;
        }

        // 应用移动
        if (moveDirection.magnitude > 0.1f)
        {
            // 计算速度
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            // 移动角色
            characterController.Move(moveDirection.normalized * currentSpeed * Time.deltaTime);

            // 平滑转向
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                                                rotationSpeed * Time.deltaTime);
        }
    }

    void HandleAnimation()
    {
        // 计算移动速度（0-1范围）
        float moveMagnitude = new Vector3(horizontalInput, 0, verticalInput).magnitude;
        float animationSpeed = moveMagnitude * (isRunning ? 1f : 0.5f);

        // 设置动画参数
        animator.SetFloat(SpeedHash, animationSpeed);

        // 瞄准控制
        if (Input.GetMouseButtonDown(1)) // 右键按下
        {
            animator.SetBool(IsAimingHash, true);
        }
        else if (Input.GetMouseButtonUp(1)) // 右键释放
        {
            animator.SetBool(IsAimingHash, false);
        }

        // 装弹控制
        if (Input.GetKeyDown(KeyCode.R))
        {
            animator.SetTrigger("Reload");
        }
    }

    void HandleActions()
    {
        // 装备武器
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            animator.SetTrigger(EquipWeaponHash);
        }

        // 射击
        if (Input.GetMouseButtonDown(0) && animator.GetBool(IsAimingHash))
        {
            animator.SetTrigger("Fire");
        }
    }

    // 供Animation Events调用的方法
    public void OnEquipAnimationStart()
    {
        Debug.Log("装备动画开始");
    }

    public void OnEquipAnimationEnd()
    {
        Debug.Log("装备动画结束，武器已装备");
    }

    public void OnReloadAnimationEnd()
    {
        Debug.Log("装弹完成");
    }
}
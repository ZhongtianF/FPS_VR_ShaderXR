using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

/* 注意：角色和胶囊的动画通过控制器调用，使用了animator空值检查 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("角色移动设置")]
        [Tooltip("角色移动速度（米/秒）")]
        public float MoveSpeed = 2.0f;
        [Tooltip("角色奔跑速度（米/秒）")]
        public float SprintSpeed = 5.335f;
        [Tooltip("角色转向移动方向的速度")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        [Tooltip("加速和减速速率")]
        public float SpeedChangeRate = 10.0f;

        [Header("动画速度调整")]
        [Tooltip("动画播放速度缩放（不影响实际移动速度）")]
        [Range(0.5f, 2.0f)]
        public float AnimationSpeedMultiplier = 1.0f;
        [Tooltip("基础动画混合速度")]
        [Range(0.5f, 2.0f)]
        public float AnimationBlendSpeed = 1.0f;

        [Space(10)]
        [Tooltip("玩家跳跃高度")]
        public float JumpHeight = 1.2f;
        [Tooltip("角色使用自定义重力值，引擎默认值为-9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("再次跳跃前需要等待的时间，设置为0f可以立即再次跳跃")]
        public float JumpTimeout = 0.50f;
        [Tooltip("进入下落状态前需要等待的时间，用于下楼梯")]
        public float FallTimeout = 0.15f;

        [Header("角色地面检测")]
        [Tooltip("角色是否在地面上，不是CharacterController内置的地面检测")]
        public bool Grounded = true;
        [Tooltip("用于粗糙地面的偏移")]
        public float GroundedOffset = -0.14f;
        [Tooltip("地面检测半径，应与CharacterController的半径匹配")]
        public float GroundedRadius = 0.28f;
        [Tooltip("角色视为地面的层")]
        public LayerMask GroundLayers;

        [Header("摄影机设置")]
        [Tooltip("Cinemachine虚拟摄影机中设置的跟随目标")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("相机向上移动的最大角度（度）")]
        public float TopClamp = 70.0f;
        [Tooltip("相机向下移动的最大角度（度）")]
        public float BottomClamp = -30.0f;
        [Tooltip("覆盖相机的额外角度，用于微调锁定相机位置")]
        public float CameraAngleOverride = 0.0f;
        [Tooltip("锁定相机在所有轴上的位置")]
        public bool LockCameraPosition = false;

        // cinemachine变量
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // 角色变量
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // 超时变量
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // 动画ID
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private void Awake()
        {
            // 获取主相机引用
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

            AssignAnimationIDs();

            // 开始时重置超时计时器
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // 设置球体位置，包含偏移
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            // 如果使用角色，更新动画器
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // 如果有输入且相机位置未锁定
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                _cinemachineTargetYaw += _input.look.x * Time.deltaTime;
                _cinemachineTargetPitch += _input.look.y * Time.deltaTime;
            }

            // 限制旋转角度在360度范围内
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine将跟随此目标
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // 基于移动速度、奔跑速度和是否按下奔跑键设置目标速度
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // 注意：Vector2的==运算符使用近似值，不易出现浮点误差，且比magnitude更便宜
            // 如果没有输入，将目标速度设置为0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // 玩家当前水平速度的引用
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // 加速或减速到目标速度
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // 创建曲线结果而不是线性结果，提供更自然的变速
                // 注意Lerp中的T是限制的，因此我们不需要限制速度
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // 将速度四舍五入到3位小数
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // 动画混合 - 使用动画速度乘数
            float targetAnimationBlend = targetSpeed;
            _animationBlend = Mathf.Lerp(_animationBlend, targetAnimationBlend, Time.deltaTime * SpeedChangeRate * AnimationBlendSpeed);

            // 计算动画速度（与实际移动速度分离）
            float animationSpeed = _animationBlend * AnimationSpeedMultiplier;

            // 标准化输入方向
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // 注意：Vector2的!=运算符使用近似值，不易出现浮点误差，且比magnitude更便宜
            // 如果有移动输入，当角色移动时旋转角色
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                // 旋转以面向相对于相机位置的输入方向
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // 移动角色
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // 如果使用角色，更新动画器
            if (_hasAnimator)
            {
                // 设置动画速度（使用动画速度乘数）
                _animator.SetFloat(_animIDSpeed, animationSpeed);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // 重置下落超时计时器
                _fallTimeoutDelta = FallTimeout;

                // 如果使用角色，更新动画器
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // 停止角色在地面时速度无限下降
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // 跳跃
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // H * -2 * G 的平方根 = 达到所需高度所需的速度
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // 如果使用角色，更新动画器
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // 跳跃超时
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // 重置跳跃超时计时器
                _jumpTimeoutDelta = JumpTimeout;

                // 下落超时
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // 如果使用角色，更新动画器
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // 如果角色不在地面，不允许跳跃
                _input.jump = false;
            }

            // 如果速度小于终端速度，随时间应用重力（乘以deltaTime两次以随时间线性加速）
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // 选中时，在接地碰撞器的位置绘制一个匹配半径的辅助球体
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }
    }
}
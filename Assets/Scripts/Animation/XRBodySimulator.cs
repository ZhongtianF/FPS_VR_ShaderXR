// XRBodySimulator.cs
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class XRBodySimulator : MonoBehaviour
{
    [Header("目标点")]
    [SerializeField] private Transform headTarget;
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    [Header("平滑设置")]
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float maxMoveSpeed = 5f;

    [Header("IK约束")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private MultiAimConstraint spineConstraint;

    // 平滑变量
    private Vector3 headVelocity;
    private Vector3 leftHandVelocity;
    private Vector3 rightHandVelocity;

    // 测试用：键盘控制
    void Update()
    {
        if (Input.GetKey(KeyCode.W)) MoveTarget(Vector3.forward);
        if (Input.GetKey(KeyCode.S)) MoveTarget(Vector3.back);
        if (Input.GetKey(KeyCode.A)) MoveTarget(Vector3.left);
        if (Input.GetKey(KeyCode.D)) MoveTarget(Vector3.right);

        // 平滑移动
        SmoothUpdateTargets();

        // 更新IK
        UpdateIKConstraints();
    }

    private void MoveTarget(Vector3 direction)
    {
        float speed = maxMoveSpeed * Time.deltaTime;
        headTarget.position += transform.TransformDirection(direction) * speed;
    }

    private void SmoothUpdateTargets()
    {
        // 平滑头部跟随
        Vector3 smoothHeadPos = Vector3.SmoothDamp(
            transform.position,
            headTarget.position,
            ref headVelocity,
            smoothTime,
            maxMoveSpeed
        );
        transform.position = smoothHeadPos;

        // 平滑手部位置
        if (leftHandIK != null)
        {
            Vector3 smoothLeftPos = Vector3.SmoothDamp(
                leftHandIK.data.target.position,
                leftHandTarget.position,
                ref leftHandVelocity,
                smoothTime,
                maxMoveSpeed
            );
            leftHandIK.data.target.position = smoothLeftPos;
        }

        if (rightHandIK != null)
        {
            Vector3 smoothRightPos = Vector3.SmoothDamp(
                rightHandIK.data.target.position,
                rightHandTarget.position,
                ref rightHandVelocity,
                smoothTime,
                maxMoveSpeed
            );
            rightHandIK.data.target.position = smoothRightPos;
        }
    }

    private void UpdateIKConstraints()
    {
        // 更新MultiAim约束跟随头部旋转
        if (spineConstraint != null && headTarget != null)
        {
            var sourceObjects = spineConstraint.data.sourceObjects;
            sourceObjects.SetTransform(0, headTarget);
            spineConstraint.data.sourceObjects = sourceObjects;
        }
    }
}
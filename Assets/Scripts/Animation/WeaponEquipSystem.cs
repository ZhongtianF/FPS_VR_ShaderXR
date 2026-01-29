// WeaponEquipSystem.cs
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class WeaponEquipSystem : MonoBehaviour
{
    [Header("装备设置")]
    [SerializeField] private Transform weaponPrefab;
    [SerializeField] private Transform rightHandGrip;
    [SerializeField] private string equipAnimName = "EquipWeapon";

    [Header("IK设置")]
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private MultiAimConstraint spineAimConstraint;

    private Animator animator;
    private GameObject currentWeapon;
    private bool isEquipped = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void EquipWeapon()
    {
        if (isEquipped) return;

        // 播放装备动画
        animator.SetTrigger("Equip");

        // 通过Animation Event或协程在动画结束时挂载武器
        StartCoroutine(AttachWeaponAfterAnimation());
    }

    private System.Collections.IEnumerator AttachWeaponAfterAnimation()
    {
        // 等待动画播放时间
        yield return new WaitForSeconds(0.5f);

        // 实例化武器
        currentWeapon = Instantiate(weaponPrefab, rightHandGrip).gameObject;
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;

        // 设置IK目标
        if (rightHandIK != null)
        {
            rightHandIK.data.target = currentWeapon.transform;
        }

        isEquipped = true;
    }

    // Animation Event调用
    public void OnEquipAnimationEnd()
    {
        // 确保武器挂载
        if (currentWeapon == null)
        {
            AttachWeapon();
        }
    }

    private void AttachWeapon()
    {
        // 直接挂载武器
        currentWeapon = Instantiate(weaponPrefab, rightHandGrip).gameObject;
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
    }
}
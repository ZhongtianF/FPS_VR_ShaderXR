
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("枪械列表")]
    [SerializeField] private List<GameObject> gunPrefabs = new List<GameObject>(); // 枪械预制体
    [SerializeField] private Transform gunSocket; // 枪械挂载点（手部位置）

    [Header("按键设置")]
    [SerializeField]
    private KeyCode[] switchKeys =
        { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };
    [SerializeField] private KeyCode reloadKey = KeyCode.R;
    [SerializeField] private KeyCode aimKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;

    [Header("动画参数")]
    [SerializeField] private Animator animator;
    [SerializeField] private string reloadTrigger = "Reload";
    [SerializeField] private string aimBool = "IsAiming";
    [SerializeField] private string fireBool = "IsShooting";

    private List<GameObject> gunInstances = new List<GameObject>();
    private int currentGunIndex = 0;
    private bool isAiming = false;

    void Start()
    {
        InitializeGuns();
        ActivateGun(0);
    }

    void Update()
    {
        HandleInput();
    }

    void InitializeGuns()
    {
        if (gunSocket == null)
        {
            // 自动创建挂载点
            gunSocket = new GameObject("GunSocket").transform;
            gunSocket.SetParent(transform);
            gunSocket.localPosition = new Vector3(0.3f, 0, 0.3f);
        }

        // 实例化所有枪械并隐藏
        foreach (var gunPrefab in gunPrefabs)
        {
            if (gunPrefab != null)
            {
                GameObject gun = Instantiate(gunPrefab, gunSocket);
                gun.transform.localPosition = Vector3.zero;
                gun.transform.localRotation = Quaternion.identity;
                gun.SetActive(false); // 初始隐藏
                gunInstances.Add(gun);
            }
        }
    }

    void HandleInput()
    {
        // 切换枪械
        for (int i = 0; i < switchKeys.Length && i < gunInstances.Count; i++)
        {
            if (Input.GetKeyDown(switchKeys[i]))
            {
                SwitchToGun(i);
            }
        }

        // 装弹
        if (Input.GetKeyDown(reloadKey))
        {
            Reload();
        }

        // 瞄准
        isAiming = Input.GetKey(aimKey);
        if (animator != null)
        {
            animator.SetBool(aimBool, isAiming);
        }

        // 射击
        if (Input.GetKey(fireKey))
        {
            Shoot();
        }
        else if (Input.GetKeyUp(fireKey) && animator != null)
        {
            animator.SetBool(fireBool, false);
        }
    }

    void SwitchToGun(int index)
    {
        if (index < 0 || index >= gunInstances.Count || index == currentGunIndex)
            return;

        // 隐藏当前枪械
        if (currentGunIndex >= 0 && currentGunIndex < gunInstances.Count)
        {
            gunInstances[currentGunIndex].SetActive(false);
        }

        // 激活新枪械
        currentGunIndex = index;
        gunInstances[currentGunIndex].SetActive(true);

        Debug.Log($"切换到枪械: {index + 1}");
    }

    void ActivateGun(int index)
    {
        if (index < 0 || index >= gunInstances.Count) return;

        // 隐藏所有枪械
        foreach (var gun in gunInstances)
        {
            gun.SetActive(false);
        }

        // 激活指定枪械
        currentGunIndex = index;
        gunInstances[currentGunIndex].SetActive(true);
    }

    void Reload()
    {
        if (animator != null)
        {
            animator.SetTrigger(reloadTrigger);
        }

        Debug.Log("装弹中...");
        // 这里可以添加装弹完成后的逻辑（如恢复弹药）
    }

    void Shoot()
    {
        if (!isAiming) return; // 只有瞄准时才能射击

        if (animator != null)
        {
            animator.SetBool(fireBool, true);
        }

        // 这里可以添加射击逻辑（射线检测、音效等）
    }

    // 供Animation Event调用的方法
    public void OnReloadComplete()
    {
        Debug.Log("装弹完成");
    }

    public void OnShoot()
    {
        // 射击时触发的逻辑
        Debug.Log("射击!");
    }
}
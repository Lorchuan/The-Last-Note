using UnityEngine;
using System.Collections;

public class WandController : MonoBehaviour
{
    [Header("魔杖基础设置")]
    public Transform wandTip;

    [Header("技能预制体 (旧版技能已移除)")]
    // 保留这些槽位，确保你的Prefab不会丢失引用
    public GameObject fireExplosionPrefab;      // ID 2
    public GameObject swordComboPrefab;         // ID 3
    public GameObject iceSpikePrefab;           // ID 4
    public GameObject swordAuraPrefab;          // ID 5

    [Header("方向控制模式")]
    public WandControlMode controlMode = WandControlMode.FollowMouse;

    [Header("自动瞄准设置")]
    public LayerMask enemyLayerMask = 1;
    public float autoAimRadius = 20f;
    public float autoAimAngle = 60f;
    public bool maintainAimDuringCasting = true;

    [Header("平滑设置")]
    public float returnSpeed = 5f;

    [Header("技能管理器引用")]
    public SkillManager skillManager;

    // 内部变量
    private float currentPitch = 0f;
    private float currentYaw = 0f;
    private Quaternion targetRotation;
    private Camera playerCamera;
    private Transform lockedEnemy;
    private Vector3 lastAimDirection;

    public enum WandControlMode { FollowMouse, AutoAim, PhysicsBased }

    void Start()
    {
        targetRotation = transform.localRotation;
        playerCamera = Camera.main;

        if (skillManager == null) skillManager = GetComponent<SkillManager>();
    }

    void Update()
    {
        if (controlMode == WandControlMode.AutoAim)
        {
            UpdateAutoAim();
        }
        else
        {
            UpdateMouseControl();
        }

        ApplyRotation();
        DebugDrawAim();
    }

    void UpdateMouseControl()
    {
        if (InputManager.Instance.isCasting)
        {
            Vector2 mouseLook = InputManager.Instance.lookInput;
            currentYaw += mouseLook.x;
            currentPitch -= mouseLook.y;
            currentYaw = Mathf.Clamp(currentYaw, -60, 60);
            currentPitch = Mathf.Clamp(currentPitch, -80, 80);
        }
        else
        {
            currentYaw = Mathf.Lerp(currentYaw, 0f, Time.deltaTime * returnSpeed);
            currentPitch = Mathf.Lerp(currentPitch, 0f, Time.deltaTime * returnSpeed);
        }
        targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    public void UpdateAutoAim()
    {
        Transform nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            lockedEnemy = nearestEnemy;
            if (!InputManager.Instance.isCasting || maintainAimDuringCasting)
            {
                Vector3 directionToEnemy = nearestEnemy.position - wandTip.position;
                targetRotation = Quaternion.Inverse(transform.parent.rotation) * Quaternion.LookRotation(directionToEnemy);
            }
        }
        else
        {
            lockedEnemy = null;
            UpdateMouseControl();
        }
    }

    void ApplyRotation()
    {
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * 10f);
    }

    Transform FindNearestEnemy()
    {
        if (playerCamera == null) return null;
        Collider[] enemies = Physics.OverlapSphere(playerCamera.transform.position, autoAimRadius, enemyLayerMask);
        Transform nearest = null;
        float minDst = float.MaxValue;

        foreach (var enemy in enemies)
        {
            Vector3 dir = enemy.transform.position - playerCamera.transform.position;
            if (Vector3.Angle(playerCamera.transform.forward, dir) < autoAimAngle)
            {
                float dst = dir.sqrMagnitude;
                if (dst < minDst) { minDst = dst; nearest = enemy.transform; }
            }
        }
        return nearest;
    }

    void DebugDrawAim()
    {
        if (wandTip) Debug.DrawRay(wandTip.position, wandTip.forward * 10f, Color.red);
        if (lockedEnemy) Debug.DrawLine(wandTip.position, lockedEnemy.position, Color.yellow);
    }

    // 核心施法入口
    public void FireSpell(int spellId)
    {
        if (skillManager != null)
        {
            skillManager.CastSkill(spellId);
        }
    }

    public Transform GetLockedEnemy() => lockedEnemy;

    [ContextMenu("测试: 火爆术 (ID 2)")]
    public void TestFireExplosion() => FireSpell(2);
}   
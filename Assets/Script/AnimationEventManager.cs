using UnityEngine;

public class AnimationEventManager : MonoBehaviour
{
    [Header("怪物控制器引用")]
    public EnemyController enemyController;

    [Header("攻击事件配置")]
    public float attackEventTime = 0.5f; // 攻击事件在动画中的时间点

    private Animator animator;
    private float currentAnimTime;
    private string currentState;
    private bool attackEventTriggered;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }
    }

    void Update()
    {
        // 监听攻击动画状态，手动触发攻击事件
        CheckAnimationEvents();
    }

    void CheckAnimationEvents()
    {
        if (animator == null || enemyController == null) return;

        // 获取当前动画状态信息
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 检查是否在攻击状态
        if (stateInfo.IsTag("Attack")) // 需要在动画编辑器中为攻击动画添加Attack标签
        {
            // 获取动画的标准化时间（0-1）
            float normalizedTime = stateInfo.normalizedTime % 1.0f;

            // 如果动画循环播放，我们需要处理循环
            if (stateInfo.loop)
            {
                normalizedTime = stateInfo.normalizedTime - Mathf.Floor(stateInfo.normalizedTime);
            }

            // 在攻击动画的特定时间点触发攻击事件
            if (normalizedTime >= attackEventTime && !attackEventTriggered)
            {
                // 触发攻击
                enemyController.OnAttackHit();
                attackEventTriggered = true;

                Debug.Log($"攻击事件触发在: {normalizedTime:F2}");
            }

            // 重置事件触发标志（当动画时间回到攻击点之前时）
            if (normalizedTime < attackEventTime && attackEventTriggered)
            {
                attackEventTriggered = false;
            }
        }
        else
        {
            // 不在攻击状态时重置标志
            attackEventTriggered = false;
        }
    }
#if UNITY_EDITOR
    // 调试信息
    void OnGUI()
    {
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = stateInfo.normalizedTime % 1.0f;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 12;
            style.normal.textColor = Color.yellow;

            GUI.Label(new Rect(10, 500, 300, 20), $"动画状态: {currentState}", style);
            GUI.Label(new Rect(10, 520, 300, 20), $"动画时间: {normalizedTime:F2}", style);
            GUI.Label(new Rect(10, 540, 300, 20), $"攻击事件已触发: {attackEventTriggered}", style);
        }
    }
#endif
}
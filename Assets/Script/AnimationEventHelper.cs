using UnityEngine;

public class AnimationEventHelper : MonoBehaviour
{
    [Header("动画事件")]
    public bool debugAnimationEvents = true;

    // 动画事件回调方法
    public void OnStartMoveBegin()
    {
        if (debugAnimationEvents) Debug.Log("动画事件: StartMove动画开始");
        // 可以在这里添加音效或其他效果
    }

    public void OnStartMoveEnd()
    {
        if (debugAnimationEvents) Debug.Log("动画事件: StartMove动画结束");
    }

    public void OnStopMoveBegin()
    {
        if (debugAnimationEvents) Debug.Log("动画事件: StopMove动画开始");
        // 可以在这里添加停止音效或粒子效果
    }

    public void OnStopMoveEnd()
    {
        if (debugAnimationEvents) Debug.Log("动画事件: StopMove动画结束");
    }

    public void OnWalkStep()
    {
        if (debugAnimationEvents) Debug.Log("动画事件: 行走步幅");
        // 在这里添加脚步声
    }

    public void OnRunStep()
    {
        if (debugAnimationEvents) Debug.Log("动画事件: 奔跑步幅");
        // 在这里添加奔跑脚步声
    }
}
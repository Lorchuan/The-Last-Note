using UnityEngine;

[CreateAssetMenu(fileName = "WandConfig", menuName = "VR Magic/Wand Configuration")]
public class WandConfig : ScriptableObject
{
    [Header("控制设置")]
    public WandController.WandControlMode defaultControlMode = WandController.WandControlMode.FollowMouse;
    public float mouseSensitivity = 2.0f;
    public float smoothTime = 0.1f;

    [Header("限制设置")]
    public float maxPitchAngle = 80f;
    public float maxYawAngle = 60f;

    [Header("自动瞄准")]
    public float autoAimRadius = 10f;
    public float autoAimAngle = 30f;
    public LayerMask targetableLayers = 1;

    [Header("视觉效果")]
    public GameObject trailEffect;
    public GameObject aimIndicator;
}
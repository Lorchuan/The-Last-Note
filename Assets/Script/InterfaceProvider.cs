using UnityEngine;

// 输入提供者接口 - 未来只需实现这个接口来支持不同输入设备
public interface IInputProvider
{
    // 头部视角控制
    Vector2 LookInput { get; }

    // 身体移动控制  
    Vector2 MoveInput { get; }

    // 魔杖旋转
    Quaternion WandRotation { get; }

    // 画符状态
    bool IsCasting { get; }

    // 确认/功能键
    bool IsConfirmPressed { get; }

    // 获取画符位置（屏幕坐标）
    Vector2 CastingPosition { get; }

    // 初始化设备
    void Initialize();

    // 更新输入数据
    void UpdateInput();

    // 清理资源
    void Cleanup();
}
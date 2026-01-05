using UnityEngine;

// 伤害接口
public interface IDamageable
{
    void TakeDamage(int damageAmount);
    void TakeDamage(int damageAmount, string damageSource);
}

// 扩展方法

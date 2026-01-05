using UnityEngine;

public class FireballBehavior : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 1;
    public float lifeTime = 5f;

    void Start()
    {
        // 自动向前飞
        GetComponent<Rigidbody>().velocity = transform.forward * speed;
        Destroy(gameObject, lifeTime); // 超时自毁
    }

    void OnCollisionEnter(Collision collision)
    {
        // 检查撞到的物体是不是“可受伤的”
        IDamageable target = collision.gameObject.GetComponent<IDamageable>();

        if (target != null)
        {
            target.TakeDamage(damage); // 造成伤害
        }

        // 撞到任何东西（墙壁或怪物）都销毁火球自己
        // 可以在这里Instantiate一个爆炸特效
        Destroy(gameObject);
    }
}
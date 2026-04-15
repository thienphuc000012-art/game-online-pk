using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float speed = 25f;
    [SerializeField] private int damage = 30;           // ← Có thể chỉnh sát thương

    [Networked]
    public int Direction { get; set; } = 1;

    [Networked]
    private TickTimer _despawnTimer { get; set; }

    public void Initialize(int dir)
    {
        Direction = dir;
    }

    public override void Spawned()
    {
        _despawnTimer = TickTimer.CreateFromSeconds(Runner, 3f);
    }

    public override void FixedUpdateNetwork()
    {
        // Di chuyển đạn
        transform.Translate(Vector3.right * Direction * speed * Runner.DeltaTime);

        // Tự hủy sau 3 giây
        if (_despawnTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;   // Chỉ Server mới gây damage

        var target = other.GetComponent<NetworkedPlayerController>();
        if (target != null)
        {
            target.RPC_TakeDamage(damage);
            Debug.Log($"[BULLET] Đạn trúng {target.PlayerName} - {damage} damage");

            Runner.Despawn(Object);   // Hủy đạn ngay khi trúng
        }
    }
}
using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float speed = 25f;
    [SerializeField] private int damage = 30;
    [SerializeField] private float hitRadius = 0.85f;
    [SerializeField] private LayerMask hitLayer = ~0;
    [Header("=== EFFECTS ===")]
    [SerializeField] private GameObject bulletExplosionPrefab;

    [Networked] public int Direction { get; set; } = 1;
    [Networked] public PlayerRef Shooter { get; set; }
    [Networked] private TickTimer _despawnTimer { get; set; }

    // ==================== THÊM DÒNG NÀY ====================
    private bool _hasHit = false;   // ← Chỉ authority dùng, không cần Networked
    // ======================================================

    public void Initialize(int dir, PlayerRef shooter)
    {
        Direction = dir;
        Shooter = shooter;
    }

    public override void Spawned()
    {
        _despawnTimer = TickTimer.CreateFromSeconds(Runner, 3f);
        _hasHit = false;                    // reset khi spawn
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Nếu đã hit thì dừng di chuyển + chỉ chờ despawn
        if (_hasHit)
        {
            if (_despawnTimer.Expired(Runner))
                Runner.Despawn(Object);
            return;
        }

        transform.Translate(Vector3.right * Direction * speed * Runner.DeltaTime);

        CheckForHit();

        if (_despawnTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    private void CheckForHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius, hitLayer);

        foreach (var col in hits)
        {
            var target = col.GetComponent<NetworkedPlayerController>();
            if (target == null) continue;
            if (target.Object.InputAuthority == Shooter) continue;

            target.RPC_TakeDamage(damage);
            Debug.Log($"[BULLET] Đạn trúng {target.PlayerName} - {damage} damage");

            RPC_PlayBulletExplosion(transform.position);

            // FIX: Chỉ gọi 1 lần và dừng mọi thứ
            _despawnTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
            _hasHit = true;
            return;   // thoát ngay
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayBulletExplosion(Vector3 position)
    {
        if (bulletExplosionPrefab != null)
        {
            GameObject effect = Instantiate(bulletExplosionPrefab, position, Quaternion.identity);
            Destroy(effect, 0.5f);
        }
    }
}
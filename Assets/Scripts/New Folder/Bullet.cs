using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float speed = 25f;
    [SerializeField] private int damage = 30;
    [SerializeField] private float hitRadius = 0.85f;     
    [SerializeField] private LayerMask hitLayer = ~0;     

    [Networked] public int Direction { get; set; } = 1;
    [Networked] public PlayerRef Shooter { get; set; }     
    [Networked] private TickTimer _despawnTimer { get; set; }

    public void Initialize(int dir, PlayerRef shooter)
    {
        Direction = dir;
        Shooter = shooter;
    }

    public override void Spawned()
    {
        _despawnTimer = TickTimer.CreateFromSeconds(Runner, 3f);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;


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

            Runner.Despawn(Object);
            return;
        }
    }
}
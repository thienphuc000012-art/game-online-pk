//using Fusion;
//using UnityEngine;

//public class SuperHitHitbox : NetworkBehaviour
//{
//    [SerializeField] private int damage = 45;
//    [SerializeField] private bool showDebug = true;

//    private NetworkedPlayerController _owner;

//    public void Initialize(NetworkedPlayerController owner)
//    {
//        _owner = owner;
//    }

//    private void OnTriggerEnter(Collider other)
//    {
//        if (!Object.HasStateAuthority) return;

//        var target = other.GetComponent<NetworkedPlayerController>();
//        if (target != null && target != _owner)
//        {
//            target.RPC_TakeDamage(damage);
//            if (showDebug)
//                Debug.Log($"[SUPERHIT] Hitbox trúng {target.PlayerName} - {damage} damage");
//        }
//    }
//}
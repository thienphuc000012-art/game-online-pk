using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(CharacterController))]
public class NetworkedPlayerController : NetworkBehaviour
{
    private NetworkCharacterController _ncc;
    private Animator _ani;
    private SpriteRenderer _sprite;
    private Camera _localCamera;

    [Networked] public string PlayerName { get; set; } = "Player";

    [Header("Stats - Networked")]
    [Networked] public int CurHealthy { get; set; }
    [Networked] public int CurPower { get; set; }
    [Networked] public bool IsDamaged { get; set; }
    [Networked] public bool IsFacingRight { get; set; } = true;
    [Networked] public bool IsPower { get; set; }
    [Networked] public Vector3 NetVelocity { get; set; }
    [Networked] public bool NetGrounded { get; set; }
    [Networked] public bool IsHost { get; set; }

    public enum StatePlayer { Normal, Damaged, Die }
    [Networked] public StatePlayer State { get; set; } = StatePlayer.Normal;
    [Networked] public float DamageEndTime { get; set; }
    [Networked] public float PowerRegenRate { get; set; } = 80f;

    [Header("Movement & Jump Settings")]
    [SerializeField] private float speedMove = 9f;
    [SerializeField] private float speedJump = 70f;

    [Header("Projectile")]
    [SerializeField] private NetworkPrefabRef bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;

    [Header("DEBUG")]
    [SerializeField] private bool showDebugLogs = true;

    private bool _previousSuperHit = false;
    private bool _previousShoot = false;
    private bool _previousFlash = false;

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHit() => _ani.SetTrigger("hit");

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayKick() => _ani.SetTrigger("kick");

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlaySuperHit()
    {
        _ani.SetTrigger("superHit");
        if (showDebugLogs) Debug.Log($"[RPC] 🔥 SUPERHIT animation fired on ALL clients");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayShootAndSpawnBullet()
    {
        _ani.SetTrigger("shoot");
        if (Object.HasStateAuthority && bulletPrefab != default(NetworkPrefabRef))
        {
            Vector3 spawnPos = bulletSpawnPoint != null
                ? bulletSpawnPoint.position
                : transform.position + (IsFacingRight ? new Vector3(1.2f, 0.8f, 0f) : new Vector3(-1.2f, 0.8f, 0f));

            var bullet = Runner.Spawn(bulletPrefab, spawnPos, Quaternion.identity);
            if (bullet.TryGetComponent(out Bullet bulletScript))
                bulletScript.Initialize(IsFacingRight ? 1 : -1);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayJump() => _ani.SetTrigger("jump");

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayFlash() => _ani.SetTrigger("flash");

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_TakeDamage(int damage)
    {
        CurHealthy = Mathf.Max(0, CurHealthy - damage);
        IsDamaged = true;
        DamageEndTime = Runner.SimulationTime + 0.6f;
        if (CurHealthy <= 0) State = StatePlayer.Die;
        Debug.Log($"[DAMAGE] {PlayerName} nhận {damage} damage → HP còn {CurHealthy}");
    }

    public override void Spawned()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _ani = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();

        if (Object.HasInputAuthority)
        {
            var camObj = new GameObject("LocalCamera");
            camObj.transform.parent = transform;
            _localCamera = camObj.AddComponent<Camera>();
            var follow = camObj.AddComponent<CameraFollow>();
            follow.SetTarget(transform);
            _localCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        transform.rotation = Quaternion.identity;
        CurHealthy = 1000;
        CurPower = 100;
        IsPower = false;
        NetGrounded = true;
        if (scaleHitbox != null)
        {
            scaleHitbox.gameObject.SetActive(false);
            scaleHitbox.localScale = Vector3.one;
        }

        Debug.Log($"[Spawned] {PlayerName} (IsHost = {IsHost}) ready");
    }

    public override void FixedUpdateNetwork()
    {
        if (State == StatePlayer.Die) return;


        if (Object.HasStateAuthority && IsDamaged && Runner.SimulationTime > DamageEndTime)
        {
            IsDamaged = false;
        }


        if (Object.HasStateAuthority && IsPower && CurPower < 100)
        {
            CurPower = Mathf.Min(100, CurPower + (int)(PowerRegenRate * Runner.DeltaTime));
        }

        if (GetInput(out NetworkInputData input))
        {
            Vector3 move = new Vector3(input.MoveDirection.x * speedMove, 0, 0);

            if (input.Jump && _ncc.Grounded)
            {
                _ncc.Jump();
                if (Object.HasStateAuthority) RPC_PlayJump();
            }

            _ncc.Move(move * Runner.DeltaTime);
            UpdateFacing(input.MoveDirection.x);
            HandleAttacks(input);
        }

        if (Object.HasStateAuthority)
        {
            NetVelocity = _ncc.Velocity;
            NetGrounded = _ncc.Grounded;
        }
    }

    private void UpdateFacing(float moveX)
    {
        bool newFacing = IsFacingRight;
        if (moveX > 0.1f) newFacing = true;
        else if (moveX < -0.1f) newFacing = false;

        if (newFacing != IsFacingRight && Object.HasInputAuthority)
            RPC_SetFacing(newFacing);
    }

    private void HandleAttacks(NetworkInputData input)
    {
        if (input.Attack)
        {
            if (Object.HasInputAuthority) _ani.SetTrigger("hit");
            if (Object.HasStateAuthority) { RPC_PlayHit(); TryApplyDamage(15); }
        }

        if (input.Block)
        {
            if (Object.HasInputAuthority) _ani.SetTrigger("kick");
            if (Object.HasStateAuthority) { RPC_PlayKick(); TryApplyDamage(25); }
        }

        if (input.SuperHit && Object.HasInputAuthority && !_previousSuperHit)
        {
            RPC_RequestSuperHit();       
        }

        if (input.Shoot && Object.HasInputAuthority && !_previousShoot)
            RPC_RequestShoot();

        if (input.Flash && Object.HasInputAuthority && !_previousFlash)
            RPC_RequestFlash();

        if (Object.HasInputAuthority)
            RPC_SetIsPower(input.ChargePower);

        _previousSuperHit = input.SuperHit;
        _previousShoot = input.Shoot;
        _previousFlash = input.Flash;
    }

    private void TryApplyDamage(int damage)
    {
        foreach (var p in Runner.ActivePlayers)
        {
            if (p == Object.InputAuthority) continue;
            var target = Runner.GetPlayerObject(p);
            if (target == null || target == Object) continue;

            if (Vector3.Distance(transform.position, target.transform.position) < 3.5f)
            {
                target.GetComponent<NetworkedPlayerController>().RPC_TakeDamage(damage);
                break;
            }
        }
    }

    public override void Render()
    {
        transform.rotation = Quaternion.Euler(0, 0, 0);
        if (_sprite != null)
            transform.localScale = new Vector3(IsFacingRight ? 3.7f : -3.7f, 3.7f, 1f);

        UpdateAnimation();
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0, 0, 0);
        if (_sprite != null)
            transform.localScale = new Vector3(IsFacingRight ? 3.7f : -3.7f, 3.7f, 1f);
    }

    private void UpdateAnimation()
    {
        float speedForAnim = Object.HasInputAuthority ? Mathf.Abs(_ncc.Velocity.x) : Mathf.Abs(NetVelocity.x);
        bool groundedForAnim = Object.HasInputAuthority ? _ncc.Grounded : NetGrounded;

        _ani.SetBool("isGround", groundedForAnim);
        _ani.SetFloat("speed", speedForAnim);
        _ani.SetBool("isDamaged", IsDamaged);
        _ani.SetBool("isPower", IsPower);
        _ani.SetInteger("power", CurPower);
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSuperHit()
    {
        if (CurPower >= 60)
        {
            CurPower -= 60;
            RPC_PlaySuperHit();        
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestShoot()
    {
        if (CurPower >= 40)
        {
            CurPower -= 40;
            RPC_PlayShootAndSpawnBullet();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestFlash()
    {
        if (Object.HasStateAuthority)
        {
            Vector3 dashDir = IsFacingRight ? Vector3.right : Vector3.left;
            _ncc.Velocity += dashDir * 45f;
        }
        RPC_PlayFlash();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetIsPower(bool charging) => IsPower = charging;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetFacing(bool facingRight) => IsFacingRight = facingRight;

    public void CanUsingSkillSpecial() { }
    [SerializeField] private Transform scaleHitbox; 

    public void SuperHit_Activate()
    {
        if (scaleHitbox != null)
        {
            scaleHitbox.gameObject.SetActive(true);
            scaleHitbox.localScale = new Vector3(3.5f, 1f, 1f);     
            var col = scaleHitbox.GetComponent<BoxCollider>();
            if (col != null) col.enabled = true;
        }
    }

    public void SuperHit_Deactivate()
    {
        if (scaleHitbox != null)
        {
            scaleHitbox.gameObject.SetActive(false);
            scaleHitbox.localScale = Vector3.one;
            var col = scaleHitbox.GetComponent<BoxCollider>();
            if (col != null) col.enabled = false;
        }
    }
    public void SuperHit_DealDamage()
    {
        if (!Object.HasStateAuthority) return;  
        const int superDamage = 60;              
        const float beamRange = 8.5f;            

        foreach (var p in Runner.ActivePlayers)
        {
            if (p == Object.InputAuthority) continue;
            var target = Runner.GetPlayerObject(p);
            if (target == null || target == Object) continue;

            if (Vector3.Distance(transform.position, target.transform.position) < beamRange)
            {
                target.GetComponent<NetworkedPlayerController>().RPC_TakeDamage(superDamage);
                Debug.Log($"[SUPERHIT] {PlayerName} gây {superDamage} damage cho {target.GetComponent<NetworkedPlayerController>().PlayerName}");
                break; 
            }
        }
    }
}
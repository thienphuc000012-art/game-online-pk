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

    [Header("Stats - Networked")]
    [Networked] public int CurHealthy { get; set; }
    [Networked] public int CurPower { get; set; }
    [Networked] public bool IsDamaged { get; set; }
    [Networked] public bool IsFacingRight { get; set; } = true;
    [Networked] public Vector3 NetVelocity { get; set; }

    public enum StatePlayer { Normal, Damaged, Die }
    [Networked] public StatePlayer State { get; set; } = StatePlayer.Normal;

    [Header("Movement & Jump Settings")]
    [SerializeField] private float speedMove = 9f;
    [SerializeField] private float speedJump = 70f;

    [Header("DEBUG")]
    [SerializeField] private bool showDebugLogs = true;

    private bool _previousSuperHit = false;
    private bool _previousShoot = false;

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHit() => _ani.SetTrigger("hit");

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayKick() => _ani.SetTrigger("kick");

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlaySuperHit()
    {
        _ani.SetTrigger("superHit");
        if (showDebugLogs) Debug.Log($"[RPC] 🔥 SUPERHIT animation fired on ALL clients | Player {Runner.LocalPlayer}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayShoot()
    {
        _ani.SetTrigger("shoot");
        if (showDebugLogs) Debug.Log($"[RPC] 🔥 SHOOT animation fired on ALL clients | Player {Runner.LocalPlayer}");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSuperHit()
    {
        if (CurPower >= 60)
        {
            CurPower -= 60;
            if (showDebugLogs) Debug.Log($"[STATE] SuperHit APPROVED - Power left: {CurPower}");
            RPC_PlaySuperHit();
        }
        else if (showDebugLogs)
            Debug.Log($"[STATE] SuperHit REJECTED - Not enough power ({CurPower})");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestShoot()
    {
        if (CurPower >= 40)
        {
            CurPower -= 40;
            if (showDebugLogs) Debug.Log($"[STATE] Shoot APPROVED - Power left: {CurPower}");
            RPC_PlayShoot();
        }
        else if (showDebugLogs)
            Debug.Log($"[STATE] Shoot REJECTED - Not enough power ({CurPower})");
    }

    public override void Spawned()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _ani = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();

        transform.rotation = Quaternion.identity;

        CurHealthy = 100;
        CurPower = 10000000;

        _previousSuperHit = false;
        _previousShoot = false;

        Debug.Log($"[Spawned] Player ready - InputAuth: {Object.HasInputAuthority} | StateAuth: {Object.HasStateAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        if (State == StatePlayer.Die) return;

        if (GetInput(out NetworkInputData input))
        {
            Vector3 move = new Vector3(input.MoveDirection.x * speedMove, 0, 0);

            if (input.Jump && _ncc.Grounded)
                _ncc.Jump();

            _ncc.Move(move * Runner.DeltaTime);

            UpdateFacing(input.MoveDirection.x);
            HandleAttacks(input);

            if (Object.HasStateAuthority)
                NetVelocity = _ncc.Velocity;
        }

        UpdateAnimation();
    }

    private void UpdateFacing(float moveX)
    {
        if (moveX > 0.1f) IsFacingRight = true;
        else if (moveX < -0.1f) IsFacingRight = false;
    }

    public override void Render()
    {
        transform.rotation = Quaternion.Euler(0, 0, 0);
        if (_sprite != null)
            _sprite.flipX = !IsFacingRight;
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0, 0, 0);
        if (_sprite != null)
            _sprite.flipX = !IsFacingRight;
    }

    private void HandleAttacks(NetworkInputData input)
    {
        // === ATTACK & BLOCK (giữ nguyên) ===
        if (input.Attack)
        {
            if (Object.HasInputAuthority) _ani.SetTrigger("hit");
            if (Object.HasStateAuthority) RPC_PlayHit();
        }

        if (input.Block)
        {
            if (Object.HasInputAuthority) _ani.SetTrigger("kick");
            if (Object.HasStateAuthority) RPC_PlayKick();
        }

        if (input.SuperHit && Object.HasInputAuthority && !_previousSuperHit)
        {
            if (showDebugLogs) Debug.Log("[INPUT] SuperHit button pressed → sending request");
            RPC_RequestSuperHit();
        }

        if (input.Shoot && Object.HasInputAuthority && !_previousShoot)
        {
            if (showDebugLogs) Debug.Log("[INPUT] Shoot button pressed → sending request");
            RPC_RequestShoot();
        }

        _previousSuperHit = input.SuperHit;
        _previousShoot = input.Shoot;
    }

    private void UpdateAnimation()
    {
        float speedForAnim = Object.HasInputAuthority
            ? Mathf.Abs(_ncc.Velocity.x)
            : Mathf.Abs(NetVelocity.x);

        _ani.SetBool("isGround", _ncc.Grounded);
        _ani.SetFloat("speed", speedForAnim);
        _ani.SetBool("isDamaged", IsDamaged);
        _ani.SetInteger("power", CurPower);
    }

    public void CanUsingSkillSpecial() { }
}
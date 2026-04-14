using Fusion;
using UnityEngine;
using XXX.UI.Popup;

public class PlayerControll : NetworkBehaviour
{
    public enum StatePlayer { Normal, Damaged, Die }

    [Networked] public StatePlayer statePlayer { get; set; }
    [Networked] public int curHealthy { get; set; }
    [Networked] public int curPower { get; set; }
    [Networked] public bool IsFacingRight { get; set; } = true;

    [SerializeField] private Transform visual;

    [HideInInspector] public Rigidbody2D rib;
    [HideInInspector] public Animator ani;

    [Header("healthy")] public int healthy = 100;
    [Header("speed")] public float speedMove = 5f, speedJump = 26f;
    [Header("damagedHealthy")] public int damagedHit = 10, damagedKick = 10, damagedKick2 = 15, damagedSuperHit = 40, damagedShoot = 5;
    [Header("power")] public int power = 100;
    [Header("damagedPower")] public int powerHit = 60, powerShoot = 40;

    private GameManager gameManager;
    private float horizontal;
    private float scaleX, scaleY, gravity;
    private bool isGround, isDamaged;
    private float timeHit;
    private float timePressD, timePressA;
    private int countPressD, countPressA, countHit;
    [HideInInspector] public bool isFinshFlash;
    private float timerDamaged, damagedRate;

    public override void Spawned()
    {
        rib = GetComponent<Rigidbody2D>();
        ani = GetComponentInChildren<Animator>();

        // Force ngay từ đầu
        rib.bodyType = HasInputAuthority ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rib.interpolation = HasInputAuthority ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.None;

        scaleX = transform.localScale.x;
        scaleY = transform.localScale.y;
        gravity = rib.gravityScale;

        curHealthy = healthy;
        curPower = power;
        statePlayer = StatePlayer.Normal;
        IsFacingRight = true;
        if (visual != null) visual.localScale = Vector3.one;

        gameManager = GameManager.GetIntance();

        Debug.Log($"[PlayerControll] Spawned → InputAuthority = {Object.InputAuthority} | HasInputAuthority = {HasInputAuthority} | LocalPlayer = {Runner.LocalPlayer}");
    }

    public override void FixedUpdateNetwork()
    {
        // force bodyType mỗi frame
        if (rib != null)
        {
            rib.bodyType = HasInputAuthority ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            rib.interpolation = HasInputAuthority ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.None;
        }

        // === DEBUG MỚI ===
        if (HasInputAuthority)
        {
            Debug.Log($"[DEBUG PlayerControll] Authority TRUE - LocalPlayer {Runner.LocalPlayer} | InputAuth {Object.InputAuthority}");
        }

        if (!HasInputAuthority) return;

        if (GetInput(out NetworkInputData input))
        {
            Debug.Log($"[DEBUG Move] Input nhận được - Horizontal = {input.MoveDirection.x}");
            MovePlayer(input);
            Attack(input);
        }
        else
        {
            Debug.LogWarning("[DEBUG] GetInput FAILED dù HasInputAuthority = True");
        }

        UpdateAnimation();
        Blocking();
    }

    private void MovePlayer(NetworkInputData input)
    {
        horizontal = input.MoveDirection.x;
        if (horizontal != 0 && !isFinshFlash)
        {
            rib.linearVelocity = new Vector2(horizontal * speedMove, rib.linearVelocity.y);

            bool shouldFaceRight = horizontal > 0;
            if (IsFacingRight != shouldFaceRight)
                IsFacingRight = shouldFaceRight;
        }

        if (input.Jump && isGround)
        {
            rib.linearVelocity = new Vector2(rib.linearVelocity.x, speedJump);
            ani.SetBool("jump", true);
        }

        FlashAnimation();
    }

    private void FlashAnimation()
    {
        if (Input.GetKeyDown(KeyCode.D) && isGround)
        {
            if (timePressD > 0 && countPressD == 1)
            {
                ani.SetTrigger("flash");
                isFinshFlash = true;
                rib.linearVelocity = new Vector2(15, 0);
                timePressD = 0;
                countHit = 0;
            }
            else
            {
                timePressD = 0.5f;
                countPressD++;
            }
        }
        else if (Input.GetKeyDown(KeyCode.A) && isGround)
        {
            if (timePressA > 0 && countPressA == 1)
            {
                ani.SetTrigger("flash");
                isFinshFlash = true;
                rib.linearVelocity = new Vector2(-15, 0);
                timePressA = 0;
                countHit = 0;
            }
            else
            {
                timePressA = 0.5f;
                countPressA++;
            }
        }

        if (timePressD > 0 || timePressA > 0)
        {
            timePressD -= Runner.DeltaTime;
            timePressA -= Runner.DeltaTime;
        }
        else
        {
            countPressD = 0;
            countPressA = 0;
        }
    }

    private void Attack(NetworkInputData input)
    {
        if (input.Attack)
        {
            if (timeHit > 0 && countHit == 1)
                ani.SetFloat("timeHit", 0.6f);
            else
            {
                ani.SetTrigger("hit");
                ani.SetFloat("timeHit", 0f);
                timeHit = 0.5f;
                countHit++;
            }
        }
        if (input.Block)
        {
            ani.SetTrigger("block");
        }
        if (Input.GetKeyDown(KeyCode.I) && isGround && curPower >= powerHit)
        {
            curPower -= powerHit;
            ani.SetTrigger("superHit");
            gameManager.UpdatePower(curPower);
        }
        if (Input.GetKeyDown(KeyCode.U) && isGround && curPower >= powerShoot)
        {
            curPower -= powerShoot;
            ani.SetTrigger("shoot");
            gameManager.UpdatePower(curPower);
        }
    }

    private void UpdateAnimation()
    {
        ani.SetBool("isGround", isGround);
        ani.SetFloat("speed", Mathf.Abs(horizontal));
        ani.SetBool("isDamaged", isDamaged);
        ani.SetInteger("power", curPower);
    }

    private void Blocking()
    {
        if (isDamaged)
        {
            statePlayer = StatePlayer.Damaged;
            timerDamaged += Runner.DeltaTime;
            if (rib.gravityScale < gravity) rib.gravityScale += Runner.DeltaTime;
            if (timerDamaged > damagedRate)
            {
                isDamaged = false;
                timerDamaged = 0;
                statePlayer = StatePlayer.Normal;
                rib.gravityScale = gravity;
            }
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, 0.1f);
    }

    public override void Render()
    {
        if (visual != null)
        {
            float xScale = IsFacingRight ? 1f : -1f;
            visual.localScale = new Vector3(xScale, 1f, 1f);
        }
    }

    public void TakeDamage(int dmg)
    {
        curHealthy -= dmg;
        if (curHealthy <= 0)
        {
            statePlayer = StatePlayer.Die;
            ani.SetTrigger("die");
            GameOver();
        }
        else
        {
            isDamaged = true;
            damagedRate = 1f;
            ani.SetTrigger("damaged");
        }
    }

    public void GameOver()
    {
        BasePopupManager.Instance.ShowPopupLose();
        GameManager.GetIntance().state = GameState.Pause;
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.name == "ground") isGround = true;
    }

    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.name == "ground") isGround = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Paste phần OnTriggerEnter2D cũ của bạn vào đây
    }
}
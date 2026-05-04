using UnityEngine;

public class EventAnimationetwork : MonoBehaviour
{
    private NetworkedPlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<NetworkedPlayerController>();
        if (playerController == null)
            Debug.LogError("[EventAnimation] Không tìm thấy NetworkedPlayerController trên GameObject!");
    }

    public void ScaleSkill(GameObject obj)
    {
        iTween.PunchScale(obj, obj.transform.localScale + new Vector3(7, 0, 0), 2f);
    }

    //public void FinishFlash()
    //{
    //    if (playerController != null)
    //        playerController.FinishFlash();
    //}

    //public void AddJumpForce(float speed)
    //{
    //    if (playerController != null)
    //        playerController.AddJumpForce(speed);
    //}

    public void CallBullet()
    {
        // TODO: Nên dùng RPC hoặc Runner.Spawn để bullet đồng bộ
        // Hiện tại giữ nguyên logic cũ (chỉ local)
        if (playerController != null)
        {
            // Gọi logic bullet của bạn (có thể di chuyển CallBulletPlayer vào đây)
         //   Debug.Log("[EventAnimation] CallBullet - Nên thay bằng Networked spawn sau");
        }
    }

    // Các hàm khác bạn đang dùng (CanUsingSkillSpecial, CallBulletAI...) giữ nguyên hoặc chuyển sang RPC
}
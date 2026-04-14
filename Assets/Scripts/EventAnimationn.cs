//using System.Collections;
//using System.Collections.Generic;
//using Fusion;
//using UnityEngine;

//public class EventAnimationn : MonoBehaviour
//{
//    private PlayerControll _player;
//    private NetworkRunner _runner;

//    private void Awake()
//    {
//        _player = GetComponent<PlayerControll>();
//        if (_player != null)
//            _runner = _player.Runner;
//    }

//    public void ScaleSkill(GameObject obj)
//    {
//        iTween.PunchScale(obj, obj.transform.localScale + new Vector3(7, 0, 0), 2f);
//    }

//    public void FinishFlash()
//    {
//        if (_player != null)
//            _player.isFinshFlash = false;
//        else
//            Debug.LogError("[EventAnimation] PlayerControll không tìm thấy trên " + gameObject.name);
//    }

//    public void AddJumpForce(float speed)
//    {
//        if (_player != null && _player.HasInputAuthority)
//        {
//            var rb = _player.GetComponent<Rigidbody2D>();
//            if (rb != null)
//                rb.linearVelocity = new Vector2(0, speed);
//        }
//    }

//    public void CallBullet()
//    {
//        if (_player != null && _player.HasInputAuthority)
//            CallBulletPlayer(_player);
//    }

//    private void CallBulletPlayer(PlayerControll player)
//    {
//        if (_runner == null) return;

//        Transform pos = player.transform.Find("posCallBullet");
//        if (pos == null) return;

//        string nameBullet = player.gameObject.name.Replace("AI", "");
//        GameObject temp = SelectBullet(nameBullet);

//        // Spawn networked bullet
//        var bulletNO = _runner.Spawn(temp, pos.position, Quaternion.identity, player.Object.InputAuthority);

//        if (bulletNO != null)
//        {
//            bulletNO.gameObject.tag = "bulletPlayer";

//            // Set velocity (sẽ sync qua NetworkTransform)
//            var bulletRB = bulletNO.GetComponent<Rigidbody2D>();
//            if (bulletRB != null)
//            {
//                Vector3 direc = player.transform.localScale.x > 0
//                    ? new Vector3(20, 0, 0)
//                    : new Vector3(-20, 0, 0);
//                bulletRB.linearVelocity = direc;
//            }

//            // Despawn sau đúng 2 giây (fix lỗi Despawn overload)
//            StartCoroutine(DespawnBulletAfterDelay(bulletNO));
//        }
//    }

//    private IEnumerator DespawnBulletAfterDelay(NetworkObject bulletNO)
//    {
//        yield return new WaitForSeconds(2f);
//        if (bulletNO != null && bulletNO.IsValid && _runner != null)
//            _runner.Despawn(bulletNO);
//    }

//    private GameObject SelectBullet(string nameBullet)
//    {
//        PrefabGO prefabs = PrefabGO.GetInstance();
//        return nameBullet switch
//        {
//            "Songoku" => prefabs.bulletGoku,
//            "Buu" => prefabs.bulletBuu,
//            "FatBuu" => prefabs.bulletFatBuu,
//            "Gotenk" => prefabs.bulletGotenk,
//            "SuperSongoku" => prefabs.bulletSuperGoku,
//            _ => prefabs.bulletGoku
//        };
//    }

//    // Giữ lại cho AI (nếu bạn vẫn dùng)
//    public void CanUsingSkillSpecial()
//    {
//        AIControl AI = GetComponent<AIControl>();
//        if (AI != null) AI.isUsingSkillSpecial = false;
//    }
//}
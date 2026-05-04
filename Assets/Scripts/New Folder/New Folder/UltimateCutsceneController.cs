//using Fusion;
//using UnityEngine;
//using UnityEngine.Playables;
//using UnityEngine.Timeline;

//public class UltimateCutsceneController : NetworkBehaviour
//{
//    private PlayableDirector _director;

//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    public void RPC_SetupCutscene(NetworkObject selfNO, NetworkObject enemyNO, Vector3 spawnPos, bool isFacingRight)
//    {
//        transform.position = spawnPos;
//        transform.rotation = Quaternion.Euler(0, isFacingRight ? 0 : 180, 0);

//        _director = GetComponent<PlayableDirector>();
//        if (_director == null)
//            _director = gameObject.AddComponent<PlayableDirector>();

//        var selfController = selfNO.GetComponent<NetworkedPlayerController>();
//        if (selfController == null || selfController.ultiTimeline == null)
//        {
//            Debug.LogError("[Cutscene] ultiTimeline chưa được gán!");
//            return;
//        }

//        _director.playableAsset = selfController.ultiTimeline;

//        BindTracks(selfNO, enemyNO);

//        _director.time = 0f;
//        _director.RebuildGraph();
//        _director.Play();

//        _director.stopped += OnCutsceneStopped;

//        Debug.Log("[Ultimate Cutscene] Setup finished - Binding done");
//    }

//    private void BindTracks(NetworkObject selfNO, NetworkObject enemyNO)
//    {
//        var timelineAsset = _director.playableAsset as TimelineAsset;
//        if (timelineAsset == null) return;

//        Animator selfAnimator = selfNO.GetComponent<Animator>();
//        Animator enemyAnimator = enemyNO.GetComponent<Animator>();

//        // Tìm SpriteRenderer trong tất cả children (bao gồm cả khi disabled)
//        SpriteRenderer selfSprite = selfNO.GetComponentInChildren<SpriteRenderer>(true);
//        SpriteRenderer enemySprite = enemyNO.GetComponentInChildren<SpriteRenderer>(true);

//        Debug.Log($"[Cutscene] BindTracks - selfSprite tồn tại: {selfSprite != null} | enemySprite tồn tại: {enemySprite != null}");

//        foreach (var track in timelineAsset.GetOutputTracks())
//        {
//            string trackNameLower = track.name.ToLowerInvariant();
//            Debug.Log($"[Cutscene] Checking track: '{track.name}'");

//            // Bind Animator (giữ nguyên)
//            if (trackNameLower.Contains("player") || trackNameLower.Contains("self") ||
//                trackNameLower.Contains("naruto") || trackNameLower.Contains("player (animator)"))
//            {
//                if (selfAnimator != null)
//                {
//                    _director.SetGenericBinding(track, selfAnimator);
//                    Debug.Log($"[Cutscene] ✓ Bound SELF Animator to '{track.name}'");
//                }
//            }
//            else if (trackNameLower.Contains("enemytrack") || trackNameLower.Contains("enemy") ||
//                     trackNameLower.Contains("2_301"))
//            {
//                if (enemyAnimator != null)
//                {
//                    _director.SetGenericBinding(track, enemyAnimator);
//                    Debug.Log($"[Cutscene] ✓ Bound ENEMY Animator to '{track.name}'");
//                }
//            }

//            // Bind SpriteRenderer - dành riêng cho EnemyTrack và các track sprite
//            if (trackNameLower.Contains("enemytrack") ||
//                trackNameLower.Contains("charicon") ||
//                trackNameLower.Contains("sprite") ||
//                trackNameLower.Contains("portrait") ||
//                trackNameLower.Contains("icon"))
//            {
//                NetworkedPlayerController selfController = selfNO.GetComponent<NetworkedPlayerController>();
//                bool isHostDoingUltimate = selfController != null && selfController.IsHost;

//                SpriteRenderer targetSprite = isHostDoingUltimate ? enemySprite : selfSprite;

//                if (targetSprite != null)
//                {
//                    _director.SetGenericBinding(track, targetSprite);
//                    string who = isHostDoingUltimate ? "ENEMY (Client)" : "SELF (Host)";
//                    Debug.Log($"[Cutscene] ✓ SUCCESS BOUND SPRITE → {who} vào track '{track.name}'");
//                }
//                else
//                {
//                    Debug.LogError($"[Cutscene] ❌ KHÔNG TÌM THẤY SpriteRenderer cho track '{track.name}'");
//                }
//            }
//        }
//    }

//    private void OnCutsceneStopped(PlayableDirector director)
//    {
//        if (Object != null && Object.HasStateAuthority && Runner != null)
//        {
//            Runner.Despawn(Object);
//        }
//        if (_director != null)
//            _director.stopped -= OnCutsceneStopped;
//    }
//}
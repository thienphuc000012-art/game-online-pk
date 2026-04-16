using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Host UI - Top Left")]
    public Slider hostHpSlider;
    public Slider hostPowerSlider;
    public TMP_Text hostNameText;

    [Header("Client UI - Top Right")]
    public Slider clientHpSlider;
    public Slider clientPowerSlider;
    public TMP_Text clientNameText;

    [Header("=== TIMER ===")]
    public TMP_Text timerText;

    [Header("=== END GAME PANELS ===")]
    public GameObject winPanel;
    public GameObject losePanel;

    private NetworkedPlayerController _hostPlayer;
    private NetworkedPlayerController _clientPlayer;

    private float matchDuration = 99f;
    private float matchStartTime;

    private bool _gameEnded = false;
    private float _endPanelShowTime = 0f;
    private bool _isDeathEnd = false;

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    private void Update()
    {
        if (_hostPlayer == null || _clientPlayer == null)
        {
            var players = FindObjectsByType<NetworkedPlayerController>(FindObjectsSortMode.None);
            if (players.Length == 2)
            {
                foreach (var p in players)
                {
                    if (p.IsHost) _hostPlayer = p;
                    else _clientPlayer = p;
                }

                if (hostNameText != null && _hostPlayer != null) hostNameText.text = _hostPlayer.PlayerName;
                if (clientNameText != null && _clientPlayer != null) clientNameText.text = _clientPlayer.PlayerName;
            }
        }

        if (_hostPlayer == null || _clientPlayer == null) return;
        if (!_gameEnded)
        {
            if (matchStartTime == 0f) matchStartTime = Time.time;
            float timeLeft = matchDuration - (Time.time - matchStartTime);
            if (timeLeft < 0) timeLeft = 0;
            timerText.text = Mathf.Ceil(timeLeft).ToString("00");
        }
        else
        {
            timerText.text = "00";
        }

        hostHpSlider.value = _hostPlayer.CurHealthy / 1000f;
        hostPowerSlider.value = _hostPlayer.CurPower / 100f;
        clientHpSlider.value = _clientPlayer.CurHealthy / 1000f;
        clientPowerSlider.value = _clientPlayer.CurPower / 100f;

        CheckGameEnd();

        if (_gameEnded && _isDeathEnd && Time.unscaledTime >= _endPanelShowTime)
        {
            HideKOEffects();

            Time.timeScale = 0f;

            bool hostDead = _hostPlayer.State == NetworkedPlayerController.StatePlayer.Die;

            if (hostDead)
            {
                if (_hostPlayer.Object.HasInputAuthority)
                    losePanel.SetActive(true);
                else
                    winPanel.SetActive(true);
            }
            else
            {
                if (_clientPlayer.Object.HasInputAuthority)
                    losePanel.SetActive(true);
                else
                    winPanel.SetActive(true);
            }
        }
    }

    private void CheckGameEnd()
    {
        if (_gameEnded) return;

        bool hostDead = _hostPlayer.State == NetworkedPlayerController.StatePlayer.Die;
        bool clientDead = _clientPlayer.State == NetworkedPlayerController.StatePlayer.Die;

        if (hostDead || clientDead)
        {
            _gameEnded = true;
            _isDeathEnd = true;
            _endPanelShowTime = Time.unscaledTime + 3f; // K.O. chạy  giây
            return;
        }

        float timeLeft = matchDuration - (Time.time - matchStartTime);
        if (timeLeft <= 0f)
        {
            _gameEnded = true;
            _isDeathEnd = false;
            Time.timeScale = 0f;

            int hostHP = _hostPlayer.CurHealthy;
            int clientHP = _clientPlayer.CurHealthy;

            if (hostHP > clientHP || hostHP == clientHP)
            {
                if (_hostPlayer.Object.HasInputAuthority) winPanel.SetActive(true);
                else losePanel.SetActive(true);
            }
            else
            {
                if (_clientPlayer.Object.HasInputAuthority) winPanel.SetActive(true);
                else losePanel.SetActive(true);
            }
        }
    }

    private void HideKOEffects()
    {
        var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("KO") || obj.name.Contains("ko") || obj.name.Contains("K.O"))
            {
                Destroy(obj);
            }
        }
    }
}
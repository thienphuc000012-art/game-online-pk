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

    private NetworkedPlayerController _hostPlayer;
    private NetworkedPlayerController _clientPlayer;

    private void Update()
    {
        // Tự động tìm player (chạy trên cả Host và Client)
        if (_hostPlayer == null || _clientPlayer == null)
        {
            var players = FindObjectsByType<NetworkedPlayerController>(FindObjectsSortMode.None);
            if (players.Length == 2)
            {
                foreach (var p in players)
                {
                    if (p.IsHost)
                        _hostPlayer = p;
                    else
                        _clientPlayer = p;
                }

                // Set tên một lần
                if (hostNameText != null && _hostPlayer != null) hostNameText.text = _hostPlayer.PlayerName;
                if (clientNameText != null && _clientPlayer != null) clientNameText.text = _clientPlayer.PlayerName;
            }
        }

        if (_hostPlayer != null)
        {
            hostHpSlider.value = _hostPlayer.CurHealthy / 1000f;
            hostPowerSlider.value = _hostPlayer.CurPower / 100f;
        }
        if (_clientPlayer != null)
        {
            clientHpSlider.value = _clientPlayer.CurHealthy / 1000f;
            clientPowerSlider.value = _clientPlayer.CurPower / 100f;
        }
    }
}
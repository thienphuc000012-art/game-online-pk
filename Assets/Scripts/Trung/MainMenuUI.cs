using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MainMenuUI : MonoBehaviour
{
    public GameObject mainmenuPanel;
    public GameObject optionsPanel;
    public GameObject tutorialPanel;
    public VolumeUI volumeUI;
    public void StartGame()
    {
        SceneManager.LoadScene("lobby");

    }
    public void OpenOptions()
    {
        mainmenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
        volumeUI.CacheCurrentVolume();
    }
    public void BackToMenu()
    {
        mainmenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }
    public void CancelOptions()
    {
        volumeUI.RevertVolume();
        optionsPanel.SetActive(false);
        mainmenuPanel.SetActive(true);
    }
    public void OpenTutorial()
    {
        optionsPanel.SetActive(false);
        tutorialPanel.SetActive(true);
    }
    public void BackToOptionsFromTutorial()
    {
        tutorialPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}

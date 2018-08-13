using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManagerUI : MonoBehaviour {

    [SerializeField]
    Transform StagesContainer;

    private void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        Time.timeScale = 1f;

        int currentStage = PlayerInfo.CurrentStage;
        StageInstanceUI stage;
        for(int i=0;i<StagesContainer.childCount;i++)
        {
            for (int c = 0; c < StagesContainer.GetChild(i).childCount; c++)
            {
                stage = StagesContainer.GetChild(i).GetChild(c).GetComponent<StageInstanceUI>();

                if (stage.StageNumber == currentStage)
                {
                    stage.SetState(StageInstanceUI.StageInstanceState.Selected);
                }
                else
                {
                    stage.SetState((currentStage > stage.StageNumber) ? StageInstanceUI.StageInstanceState.Available : StageInstanceUI.StageInstanceState.Locked);
                }
            }
        }

        GuideManager.Instance.TryTeach(0);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void SelectStage(int stage)
    {
        PlayerInfo.SelectedStage = stage;
        SceneManager.LoadScene("Game");
    }

    public void SelectStage(StageInstanceUI instance)
    {
        PlayerInfo.SelectedStage = instance.StageNumber;
        SceneManager.LoadScene("Game");
    }

    public void ResetProgress()
    {
        PlayerInfo.ResetProgress();
        SceneManager.LoadScene(1);
    }

    public void UnlockAll()
    {
        PlayerInfo.UnlockProgress();
        SceneManager.LoadScene(0);
    }
}

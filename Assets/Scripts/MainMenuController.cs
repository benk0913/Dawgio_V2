using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour {

    [SerializeField]
    Animator m_Anim;

    [SerializeField]
    InputField m_FeedBackField;

    [SerializeField]
    List<Sprite> GuideSprites = new List<Sprite>();

    [SerializeField]
    Image m_RulesImage;

    int GuideIndex = 0;

    public bool isVisible = false;

    [SerializeField]
    string FEEDBACK_URL;

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Escape))
        {
            if(isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
    }

    public void Show()
    {
        m_Anim.SetTrigger("Show");
        isVisible = true;
        AudioControl.Instance.Play("woosh");
    }

    public void Hide()
    {
        m_Anim.SetTrigger("Hide");
        isVisible = false;
        AudioControl.Instance.Play("swosh");
    }

    public void GoToMenu(int menuID)
    {
        m_Anim.SetInteger("Menu", menuID);
        AudioControl.Instance.Play("sound_click");
        AudioControl.Instance.Play("woosh");
    }

    public void Restart()
    {
        SceneManager.LoadScene(0);
    }

    public void GuideNext()
    {
        if(GuideIndex >= GuideSprites.Count - 1)
        {
            return;
        }

        GuideIndex++;

        m_RulesImage.sprite = GuideSprites[GuideIndex];
        AudioControl.Instance.Play("sound_click");
    }


    public void GuidePrevious()
    {
        if (GuideIndex <= 0)
        {
            return;
        }

        GuideIndex--;

        m_RulesImage.sprite = GuideSprites[GuideIndex];
        AudioControl.Instance.Play("sound_click");
    }

    public void Quit()
    {
        AudioControl.Instance.Play("sound_click");
        Application.Quit();
    }

    public void SendFeedBack()
    {
        if(string.IsNullOrEmpty(m_FeedBackField.text))
        {
            return;
        }

        if(SendFeedbackInstance != null)
        {
            return;
        }

        SendFeedbackInstance = StartCoroutine(SendFeedbackRoutine());

        AudioControl.Instance.Play("fp_play_button");
        GoToMenu(0);
    }

    Coroutine SendFeedbackInstance;
    IEnumerator SendFeedbackRoutine()
    {
        WWWForm form = new WWWForm();

        form.AddField("entry.459344423", m_FeedBackField.text);
        m_FeedBackField.text = "";


        byte[] rawData = form.data;

        WWW request = new WWW(FEEDBACK_URL, rawData);

        while(!request.isDone)
        {
            yield return 0;
        }

        SendFeedbackInstance = null;
    }
}

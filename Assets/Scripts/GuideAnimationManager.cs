using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuideAnimationManager : MonoBehaviour {

    [SerializeField]
    Text m_BubbleText;

    [SerializeField]
    Image m_FaceImage;

    [SerializeField]
    Sprite NormalAvatar;

    [SerializeField]
    Sprite HappyAvatar;

    [SerializeField]
    Sprite SleepyAvatar;

    [SerializeField]
    CanvasGroup BubbleCG;

    [SerializeField]
    Animator m_Animator;

    public void SetMessage(string message)
    {
        if(BubbleRoutineInstance != null)
        {
            StopCoroutine(BubbleRoutineInstance);
        }

        BubbleRoutineInstance = StartCoroutine(BubbleRoutine(message));
    }

    public void SetEmote(GuideEmote Emote = GuideEmote.Normal)
    {
        switch(Emote)
        {
            case GuideEmote.Normal:
                {
                    m_FaceImage.sprite = NormalAvatar;
                    break;
                }
            case GuideEmote.Happy:
                {
                    m_FaceImage.sprite = HappyAvatar;
                    break;
                }
            case GuideEmote.Sleepy:
                {
                    m_FaceImage.sprite = SleepyAvatar;
                    break;
                }
        }
    }

    public void SetPhase(int Phase)
    {
        BubbleCG.alpha = 0f;
        m_BubbleText.text = "";

        m_Animator.SetInteger("Guide", Phase);
        m_Animator.SetTrigger("Show");
    }

    public void Shut()
    {
        m_Animator.SetInteger("Guide", -1);
        m_Animator.SetTrigger("Show");
    }

    public enum GuideEmote
    {
        Normal,
        Happy,
        Sleepy
    }

    Coroutine BubbleRoutineInstance;

    IEnumerator BubbleRoutine(string text)
    {
        while(BubbleCG.alpha > 0f)
        {
            BubbleCG.alpha -= 3f * Time.deltaTime;

            yield return 0;
        }

        if (!string.IsNullOrEmpty(text))
        {
            m_BubbleText.text = text;

            BubbleCG.alpha = 0f;
            while (BubbleCG.alpha < 1f)
            {
                BubbleCG.alpha += 3f * Time.deltaTime;

                yield return 0;
            }
        }

        BubbleRoutineInstance = null;


    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideManager : MonoBehaviour {

    [SerializeField]
    GuideAnimationManager m_Anim;

    public static GuideManager Instance;

    public bool ListeningToGame = false;

    void Awake()
    {
        Instance = this;
    }

    public void SetPhase(int Phase)
    {
        PlayerInfo.LearningPhase = Phase;

        m_Anim.SetPhase(Phase);
    }

    public void TryTeach(int Phase)
    {
        if(PlayerInfo.LearningPhase != Phase - 1)
        {
            return;
        }

        SetPhase(Phase);
    }

    public void ShutCurrent()
    {
        m_Anim.Shut();
    }

}

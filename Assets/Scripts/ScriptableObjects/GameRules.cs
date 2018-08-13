using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Game Rules", order = 1)]
public class GameRules : ScriptableObject, IRuleManager {

    [SerializeField]
    public List<StageRules> Rules = new List<StageRules>();

    public StageRule GetRule(string ruleKey, int stage)
    {
        for (int i = 0; i < Rules.Count; i++)
        {
            if (Rules[i].StageNumber == stage)
            {
                return Rules[i].GetRule(ruleKey);
            }
        }

        return null;
    }

    public bool isRuleTrue(string ruleKey, int stage)
    {
        StageRule rule = GetRule(ruleKey, stage);

        return (rule != null && rule.Value == "true");
    }

    public StageRule GetRule(string ruleKey)
    {
        return GetRule(ruleKey, PlayerInfo.SelectedStage);
    }

    public bool isRuleTrue(string ruleKey)
    {
        return isRuleTrue(ruleKey, PlayerInfo.SelectedStage);
    }

    public StageRules GetStageRules(int stageNumber)
    {
        for(int i=0;i<Rules.Count;i++)
        {
            if(Rules[i].StageNumber == stageNumber)
            {
                return Rules[i];
            }
        }

        return null;
    }
}

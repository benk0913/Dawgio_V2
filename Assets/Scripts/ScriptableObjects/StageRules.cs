using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Data", menuName = "Stage Rules", order = 1)]
public class StageRules : ScriptableObject, IRuleManager
{
    [SerializeField]
    public int StageNumber;

    [SerializeField]
    public string briefPrefab;

    [SerializeField]
    public List<StageRule> Rules = new List<StageRule>();

    public StageRule GetRule(string ruleKey)
    {
        for(int i=0;i<Rules.Count;i++)
        {
            if (Rules[i].Rule == ruleKey)
            {
                return Rules[i];
            }
        }

        return null;
    }

    public bool isRuleTrue(string ruleKey)
    {
        StageRule rule = GetRule(ruleKey);

        return (rule != null && rule.Value == "true");
    }
}
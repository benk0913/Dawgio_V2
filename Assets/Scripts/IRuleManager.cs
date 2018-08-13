using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRuleManager
{
    StageRule GetRule(string ruleKey);

    bool isRuleTrue(string ruleKey);
}

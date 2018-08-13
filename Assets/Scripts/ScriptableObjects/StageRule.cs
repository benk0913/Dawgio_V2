using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Data", menuName = "Stage Rule", order = 1)]
public class StageRule : ScriptableObject
{
    [SerializeField]
    public string Rule;

    [SerializeField]
    public string Value;

}
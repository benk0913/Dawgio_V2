using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BriefPrefabUI : MonoBehaviour {

	public void Continue()
    {
        Game.Instance.PlayerReady = true;
    }
}

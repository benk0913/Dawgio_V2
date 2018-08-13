using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawnerEntity : MonoBehaviour {

    [SerializeField]
    string ObjectKey;

    GameObject tempObj;
    public void SpawnObject()
    {
        tempObj = ResourcesLoader.Instance.GetRecycledObject(ObjectKey);
        tempObj.transform.position = transform.position;
        tempObj.transform.rotation = transform.rotation;
        tempObj.transform.SetParent(transform);
    }

    public void DespawnObject()
    {
        if(tempObj == null)
        {
            return;
        }

        tempObj.SetActive(false);
        tempObj.transform.SetParent(null);
        tempObj = null;
    }
}

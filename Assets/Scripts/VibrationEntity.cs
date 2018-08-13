using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VibrationEntity : MonoBehaviour {

    Vector3 initPos;

    Vector3 targetPos;

    [SerializeField]
    float Strength = 1f;

    [SerializeField]
    float Speed = 1f;

    private void Awake()
    {
        initPos = transform.position;
    }

    private void Update()
    {
        targetPos = initPos + new Vector3(Random.Range(-Strength, Strength), Random.Range(-Strength, Strength), Random.Range(-Strength, Strength));

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * Speed);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialDim : MonoBehaviour {

    [SerializeField]
    Material ManipulatedMaterial;

    [SerializeField]
    Color fromColor;

    [SerializeField]
    Color toColor;

    [SerializeField]
    float Speed = 1f;

    private void OnEnable()
    {
        StopAllCoroutines();

        StartCoroutine(DimRoutine());
    }

    IEnumerator DimRoutine()
    {
        float t;
        while(true)
        {
            t = 0f;
            while(t<1f)
            {
                t += Speed * Time.deltaTime;
                
                ManipulatedMaterial.color = Color.Lerp(fromColor, toColor, t);

                yield return 0;
            }

            t = 0f;
            while (t < 1f)
            {
                t += Speed * Time.deltaTime;

                ManipulatedMaterial.color = Color.Lerp(toColor, fromColor, t);

                yield return 0;
            }
        }
    }

}

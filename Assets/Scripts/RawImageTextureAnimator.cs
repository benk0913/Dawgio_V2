using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RawImageTextureAnimator : MonoBehaviour {

    [SerializeField]
    RawImage m_Image;

    [SerializeField]
    Vector2 Direction;

    [SerializeField]
    float Speed = 1f;

    private void Update()
    {
        m_Image.uvRect = new Rect(m_Image.uvRect.x + Direction.x * Speed * Time.deltaTime
            , m_Image.uvRect.y + Direction.y * Speed * Time.deltaTime
            , m_Image.uvRect.width
            , m_Image.uvRect.height);
    }
}

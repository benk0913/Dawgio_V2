using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour {

    public CardData Data;

    [SerializeField]
    Text m_txtTitle;

    [SerializeField]
    Text m_txtPower;

    [SerializeField]
    Text m_txtDescription;

    [SerializeField]
    Image m_imgAvatar;

    [SerializeField]
    Collider CardCollider;

    [SerializeField]
    Image CardFront;

    [SerializeField]
    Image CardBack;

    [SerializeField]
    Canvas FrontCanvas;

    [SerializeField]
    Canvas BackCanvas;

    public bool isLerping = false;

    Coroutine LerpRotInstance;
    Coroutine DoubleClickerInstance;

    public PlayerModule ParentPlayer;
    public CardSlot ParentSlot;

    public void SetInfo(CardData data)
    {
        if(data == null)
        {
            return;
        }

        Data = data.Clone();

        m_txtTitle.text = Data.Name;
        m_txtPower.text = Data.Power.ToString();
        m_txtDescription.text = Data.Description;
        m_imgAvatar.sprite = Data.Icon;
    }

    public void Face(Transform target)
    {
        Face(target.transform.position);
    }

    public void FaceDown()
    {
        if (LerpRotInstance != null)
        {
            StopCoroutine(LerpRotInstance);
        }

        LerpRotInstance = StartCoroutine(FaceDownRoutine());
    }

    public void FaceUp()
    {
        if (LerpRotInstance != null)
        {
            StopCoroutine(LerpRotInstance);
        }

        LerpRotInstance = StartCoroutine(FaceUpRoutine());
    }

    public void Face(Vector3 target)
    {
        if (LerpRotInstance != null)
        {
            StopCoroutine(LerpRotInstance);
        }

        LerpRotInstance = StartCoroutine(LerpRotation(target));
    }

    public void Align(Transform target, PlayerModule byPlayer = null, bool animateHands = true, bool autoReleaseHand = true)
    {
        isLerping = true;

        if (LerpRotInstance != null)
        {
            StopCoroutine(LerpRotInstance);
        }

        if(!this.gameObject.activeInHierarchy)
        {
            return;
        }

        LerpRotInstance = StartCoroutine(AlignRoutine(target, byPlayer, animateHands, autoReleaseHand));
    }

    public void Clicked()
    {
        Game.Instance.InteractWithCard(this);

        if (DoubleClickerInstance != null)
        {
            StopCoroutine(DoubleClickerInstance);
            DoubleClickerInstance = null;
            Game.Instance.InteractWithCard(this, true);
        }
        else
        {
            DoubleClickerInstance = StartCoroutine(DoubleClicker());
        }
    }

    IEnumerator DoubleClicker()
    {
        yield return new WaitForSeconds(1f);

        DoubleClickerInstance = null;
    }

    public void DoubleClicked()
    {
        Game.Instance.InteractWithCard(this);
    }

    public void SetUninteractable()
    {
        CardCollider.enabled = false;
    }

    public void SetInteractable()
    {
        CardCollider.enabled = true;
    }

    public void Select()
    {
        CardFront.color = Color.green;
        CardBack.color = Color.green;
    }

    public void Deselect()
    {
        CardFront.color = Color.white;
        CardBack.color = Color.white;
    }

    public void BlinkRed(int cycles = 1)
    {
        if(!this.gameObject.activeInHierarchy)
        {
            return;
        }

        StartCoroutine(BlinkRedRoutine(cycles));
    }

    public void SetSortingLayer(int order = -1, string layer = "")
    {
        if(order == -1)
        {
            order = FrontCanvas.sortingOrder;
        }

        if (string.IsNullOrEmpty(layer))
        {
            layer = FrontCanvas.sortingLayerName;
        }

        FrontCanvas.sortingLayerName = layer;
        FrontCanvas.sortingOrder = order;

        BackCanvas.sortingLayerName = layer;
        BackCanvas.sortingOrder = order;
    }

    IEnumerator FaceDownRoutine()
    {
        while (isLerping)
        {
            yield return 0;
        }

        float t = 0f;

        Vector3 initPos = transform.position;

        while (t < 1f)
        {
            t += 1f * Time.deltaTime;

            if(t<0.5f)
            {
                transform.position = Vector3.Lerp(initPos, initPos + new Vector3(0f, 0.5f, 0f), t * 2f);
            }
            else
            {
                transform.position = Vector3.Lerp(initPos + new Vector3(0f, 0.5f, 0f), initPos, (t - 0.5f) * 4f);
            }

            transform.rotation = Quaternion.Euler(Mathf.Lerp(270f, 90f, t * 2f), 90f, 90f);

            yield return 0;
        }

        LerpRotInstance = null;
    }

    IEnumerator FaceUpRoutine()
    {
        while (isLerping)
        {
            yield return 0;
        }

        float t = 0f;

        Vector3 initPos = transform.position;

        while (t < 1f)
        {
            t += 1f * Time.deltaTime;


            if (t < 0.5f)
            {
                transform.position = Vector3.Lerp(initPos, initPos + new Vector3(0f, 0.5f, 0f), t * 2f);
            }
            else
            {
                transform.position = Vector3.Lerp(initPos + new Vector3(0f, 0.5f, 0f), initPos, (t - 0.5f) * 4f);
            }

            transform.rotation = Quaternion.Euler(Mathf.Lerp(90f, 270f, t * 2f), 90f, 90f);

            yield return 0;
        }

        LerpRotInstance = null;
    }

    private IEnumerator LerpRotation(Vector3 target)
    {
        float t = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(target - transform.position);

        while(t<1f)
        {
            t += 1f * Time.deltaTime;

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, t);

            yield return 0;
        }

        LerpRotInstance = null;
    }

    private IEnumerator AlignRoutine(Transform target, PlayerModule byPlayer = null, bool animateHands = true, bool autoReleaseHand = true)
    {
        float t = 0f;
        float rndHeight = UnityEngine.Random.Range(0.1f, 1f);

        Transform holdingHand = null;

        if (animateHands)
        {
            if (byPlayer != null)
            {
                holdingHand = byPlayer.Hand;
            }
            else
            {
                if (ParentPlayer != null)
                {
                    byPlayer = ParentPlayer;
                    
                }
            }
        }

        while (t < 1f)
        {
            t += Mathf.Clamp(Game.Instance.ActionQue.Count,1f,6f) * 2f  * Time.deltaTime;
            
            transform.rotation = Quaternion.Lerp(transform.rotation, target.transform.rotation, t);
            transform.position = Game.SplineLerp(transform.position, target.transform.position, rndHeight, t);

            if (animateHands && byPlayer != null)
            {
                byPlayer.PositionHand(transform.position + Vector3.up);
            }
            
            yield return 0;
        }

        if (autoReleaseHand && byPlayer != null)
        {
            byPlayer.SendHandBack();
        }

        LerpRotInstance = null;

        isLerping = false;
    }

    IEnumerator BlinkRedRoutine(int cycles = 1)
    {
        for (int i = 0; i < cycles; i++)
        {
            CardFront.color = Color.red;
            CardBack.color = Color.red;

            yield return new WaitForSeconds(0.1f);

            CardFront.color = Color.white;
            CardBack.color = Color.white;

            if(i < cycles)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

   
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameCamera : MonoBehaviour {

    [SerializeField]
    Camera Cam;

    [SerializeField]
    float LerpSpeed = 1f;

    [SerializeField]
    LayerMask MouseMask;

    [SerializeField]
    public Transform FocusSpot;

    [SerializeField]
    public Transform FocusSpot2;

    Quaternion InitialRotation;
    Vector3 InitialPosition;

    bool zoomMode;

    Vector3 MousePositionWorld;

    RaycastHit rhit;
    bool isHovering;

    Vector3 LastLerpPos;

    public GameObject HoveringObject;

    public static GameCamera Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitialRotation = Cam.transform.rotation;
        InitialPosition = Cam.transform.position;
        LastLerpPos = InitialPosition;
    }

    private void Update()
    {
        isHovering = Physics.Raycast(Cam.ScreenPointToRay(Input.mousePosition), out rhit, Mathf.Infinity, MouseMask);

        if(isHovering)
        {
            if (HoveringObject != rhit.collider.gameObject)
            {
                if (HoveringObject != null)
                {
                    OnUnhover();
                }

                HoveringObject = rhit.collider.gameObject;

                OnHover();
            }

            if(Input.GetMouseButtonUp(0))
            {
                OnClick();
            }
        }
        else
        {
            HoveringObject = null;
        }

        if (Input.GetMouseButtonUp(1))
        {
            zoomMode = !zoomMode;

            if (HoveringObject != null)
            {
                MousePositionWorld = rhit.point;

                LastLerpPos = MousePositionWorld + new Vector3(0f, 5f, 0f);
            }

        }
    }

    void OnHover()
    {
        if(HoveringObject.GetComponent<EntityHoverable>() != null)
        {
            HoveringObject.GetComponent<EntityHoverable>().OnHover();
        }
    }

    void OnUnhover()
    {
        if (HoveringObject.GetComponent<EntityHoverable>() != null)
        {
            HoveringObject.GetComponent<EntityHoverable>().OnUnhover();
        }
    }

    void OnClick()
    {
        if (HoveringObject != null && HoveringObject.GetComponent<EntityClickable>() != null)
        {
            HoveringObject.GetComponent<EntityClickable>().Click();
        }
    }

    private void LateUpdate()
    {
        if (zoomMode)
        {

            Cam.transform.position = Vector3.Lerp(Cam.transform.position, LastLerpPos, Time.deltaTime * LerpSpeed);
        }
        else
        {
            DefaultView();
        }
    }

    void DefaultView()
    {
        Cam.transform.rotation = Quaternion.Lerp(Cam.transform.rotation, InitialRotation, Time.deltaTime * LerpSpeed);
        Cam.transform.position = Vector3.Lerp(Cam.transform.position, InitialPosition, Time.deltaTime * LerpSpeed);
    }

    public void FocusOn(Vector3 focusTarget)
    {
        zoomMode = true;
        LastLerpPos = focusTarget + new Vector3(0f, 5f, 0f);
    }

    public void FocusOff()
    {
        zoomMode = false;
    }
}

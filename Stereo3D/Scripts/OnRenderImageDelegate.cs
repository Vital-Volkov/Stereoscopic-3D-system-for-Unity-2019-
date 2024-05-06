using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class OnRenderImageDelegate : MonoBehaviour
{
    public static OnRenderImageDelegate instance;

    void Awake()
    {
        //if (instance == null)
        //    instance = this;
        //else if (instance != this)
        //    Destroy(gameObject);
    }


    //public delegate void RenderImageDelegate(RenderTexture src, RenderTexture dest);
    public delegate void RenderImageDelegate(RenderTexture src, RenderTexture dest, Camera c);
    //public delegate void RenderImageDelegate(Camera c);
    public event RenderImageDelegate RenderImageEvent;
    public void OnRenderImage(RenderTexture src, RenderTexture dest)
    //public void OnPostRender()
    {

        if (RenderImageEvent != null)
            //RenderImageEvent(src, dest);
            RenderImageEvent(src, dest, GetComponent<Camera>());
            //RenderImageEvent(GetComponent<Camera>());

        //Debug.Log("OnRenderImageDelegate OnRenderImage " + Time.time);
    }
}
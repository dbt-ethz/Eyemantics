using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Projecting2D : MonoBehaviour
{
    public GameObject testPt;
    public Image spot;
    private Camera camera;
    void Start()
    {
        camera = Camera.main;
    }
    void Update()
    {
        Vector3 screenPos = camera.WorldToScreenPoint(testPt.transform.position);
        spot.transform.position = screenPos;
    }
}

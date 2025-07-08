using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindEffect : MonoBehaviour
{
    public float swayStrength = 5f;
    public float swaySpeed = 1f;

    private Quaternion startRot;

    void Start()
    {
        startRot = transform.rotation;
    }

    void Update()
    {
        float sway = Mathf.Sin(Time.time * swaySpeed) * swayStrength;
        transform.rotation = startRot * Quaternion.Euler(0f, sway, 0f);
    }
}
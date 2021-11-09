using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;

    void Start()
    {
        target = GameObject.Find("Player").transform;
        offset = target.position - transform.position;
    }

    void LateUpdate()
    {
        transform.position = target.position - offset;
        transform.LookAt(target);
        // transform.rotation = Quaternion.
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCollider : MonoBehaviour
{
    private List<ContactPoint> collisionContactPoints;
    // Start is called before the first frame update
    void Start()
    {
        collisionContactPoints = new List<ContactPoint>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision other) {
        if (other.gameObject.layer == 6)
            other.GetContacts(collisionContactPoints);
    }

    private void OnCollisionExit(Collision other) {
        if (other.gameObject.layer == 6)
            collisionContactPoints = new List<ContactPoint>();
    }
}

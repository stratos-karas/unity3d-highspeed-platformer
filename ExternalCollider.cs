using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExternalCollider : MonoBehaviour
{
    private BoxCollider boxCollider;
    public List<ContactPoint> collisionContactPoints;
    // private RigidBody rigidBody;
    // Start is called before the first frame update
    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        collisionContactPoints = new List<ContactPoint>();
        // rigidBody = transform.GetComponent<RigidBody>();
    }

    private void Update() {
        //
    }

    private void OnCollisionEnter(Collision other) {
        if (other.gameObject.layer == 6 || other.gameObject.layer == 7)
            other.GetContacts(collisionContactPoints);
    }

    private void OnCollisionStay(Collision other) {
        if (other.gameObject.layer == 6 || other.gameObject.layer == 7)
            other.GetContacts(collisionContactPoints);
    }

    private void OnCollisionExit(Collision other) {
        if (other.gameObject.layer == 6 || other.gameObject.layer == 7)
            collisionContactPoints = new List<ContactPoint>();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        if (collisionContactPoints != null)
            foreach (ContactPoint cPoint in collisionContactPoints)
                Gizmos.DrawSphere(cPoint.point, 0.1f);
    }

}

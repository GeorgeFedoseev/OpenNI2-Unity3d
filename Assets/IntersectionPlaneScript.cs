using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Parabox.CSG;

public class IntersectionPlaneScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // void OnCollisionStay(Collision collision) {

    //     Debug.Log("OnCollisionStay");
    //     // Check if the collider we hit has a rigidbody
    //     // Then apply the force
    //     foreach (var contact  in collision.contacts) {
    //         print(contact.thisCollider.name + " hit " + contact.otherCollider.name);
    //         // Visualize the contact point
    //         Debug.DrawRay(contact.point, contact.normal, Color.white);
    //     }
    // }

    void OnCollisionEnter(Collision collision){
        print("OnCollisionEnter");
        
    }

    void OnCollisionStay(Collision collision){
        print("OnCollisionStay");
    }

    float _lastTimeComputedIntersection = -999f;
    void OnTriggerStay(Collider col){        
        Debug.Log($"OnTriggerStay {col.name}");        
        
    }
}

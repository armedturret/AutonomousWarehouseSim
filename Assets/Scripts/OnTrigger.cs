using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnTrigger : MonoBehaviour
{
    public HashSet<GameObject> currentGameObjects = new HashSet<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger || other.GetComponent<Rigidbody>() == null) return;
        currentGameObjects.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.isTrigger || other.GetComponent<Rigidbody>() == null) return;
        currentGameObjects.Remove(other.gameObject);
    }
}
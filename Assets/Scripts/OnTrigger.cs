using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TriggerEvent : UnityEvent<OnTrigger>
{

}

public class OnTrigger : MonoBehaviour
{

    public TriggerEvent onTriggerUpdate;

    public HashSet<GameObject> currentGameObjects = new HashSet<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger || other.GetComponent<Rigidbody>() == null) return;
        currentGameObjects.Add(other.gameObject);
        onTriggerUpdate.Invoke(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.isTrigger || other.GetComponent<Rigidbody>() == null) return;
        currentGameObjects.Remove(other.gameObject);
        onTriggerUpdate.Invoke(this);
    }
}
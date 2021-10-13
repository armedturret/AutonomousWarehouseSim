using System.Collections;
using UnityEngine;

public class TextBillboard : MonoBehaviour
{

    private void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, 
            Camera.main.transform.rotation * Vector3.up);
    }
}
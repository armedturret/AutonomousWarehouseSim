using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class Truck : MonoBehaviour
{
    public Transform[] CrateSpawns;

    public string location;

    private string m_truckInfo;

    public void Arrive(string info, string location)
    {
        m_truckInfo = info;
        this.location = location;
        //send a signal to the control entity that a truck has arrived
        Control.Instance.TruckArrived(this, m_truckInfo, location);
    }

    private void OnDestroy()
    {
        //destroy all crate objects in the trigger
        for(int i = 0; i < m_inTruck.Count; i++)
        {
            if(m_inTruck[i].GetComponent<Crate>())
                Destroy(m_inTruck[i]);
        }

        m_inTruck.Clear();
    }

    private List<GameObject> m_inTruck = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (!m_inTruck.Contains(other.gameObject))
            m_inTruck.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if(m_inTruck.Contains(other.gameObject))
            m_inTruck.Remove(other.gameObject);
    }
}
using System.Collections;
using UnityEngine;

public class Truck : MonoBehaviour
{
    public Transform[] CrateSpawns;

    private string m_truckInfo;

    public void Arrive(string info)
    {
        m_truckInfo = info;
        //send a signal to the control entity that a truck has arrived
        Control.Instance.TruckArrived(m_truckInfo);
    }
}
using System.Collections;
using UnityEngine;

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
}
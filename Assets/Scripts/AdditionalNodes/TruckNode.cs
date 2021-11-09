using System.Collections;
using UnityEngine;

public class TruckNode : LineNode
{
    public Transform truckEnd;

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.DrawIcon(transform.position, "truckicon");
        if(truckEnd != null)
        {
            Gizmos.DrawIcon(truckEnd.position, "redflag");
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, truckEnd.position);
        }
            
    }
}
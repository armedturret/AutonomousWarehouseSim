using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelfNode : LineNode
{
    public Transform shelfTransform;

    public int rowNumber;

    public float baseHeight = 0.669f;
    public float offset = 3f;

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        //draw the extra shelf tranform
        Gizmos.color = Color.red;
        Gizmos.DrawIcon(transform.position, "shelficon", true);
        if(shelfTransform != null)
        {
            Gizmos.DrawLine(transform.position, shelfTransform.position);
            Gizmos.DrawIcon(shelfTransform.position, "redflag", true);
        }
    }
}

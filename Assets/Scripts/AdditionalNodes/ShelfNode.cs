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
        if(shelfTransform != null)
        {
            Gizmos.DrawLine(transform.position, shelfTransform.position);
            Gizmos.DrawCube(shelfTransform.position, new Vector3(1f, 1f, 1f));
        }
    }
}

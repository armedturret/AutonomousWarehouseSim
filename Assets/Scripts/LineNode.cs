using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineNode : MonoBehaviour
{
    public string nodeId = ""; //"" if not important spot
    public List<LineNode> nextNodes = new List<LineNode>();
    public List<string> nextNodeDirections;

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < nextNodes.Count; i++)
        {
            if(nextNodes[i] != null)
                Gizmos.DrawLine(transform.position, nextNodes[i].transform.position);
        }
        Gizmos.color = Color.blue;
    }
}

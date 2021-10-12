using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineNode : MonoBehaviour
{
    public string nodeId = ""; //"" if not important spot
    public List<LineNode> nextNodes;
    public List<string> nextNodeDirections;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < nextNodes.Count; i++)
        {
            if(nextNodes[i] != null)
                Gizmos.DrawLine(transform.position, nextNodes[i].transform.position);
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(transform.position, new Vector3(1f, 1f, 1f));
    }
}

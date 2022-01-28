using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineNode : MonoBehaviour
{
    public string nodeId = ""; //"" if not important spot
    public List<LineNode> nextNodes = new List<LineNode>();
    public List<string> nextNodeDirections;
    public GameObject linePrefab;

    public void CreateConnection(Transform previousNode)
    {
        //create a new line object
        GameObject lineObject = Instantiate(linePrefab, transform);
        LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();

        Vector3[] positions = {previousNode.position, transform.position};
        lineRenderer.SetPositions(positions);

    }

    private void Start()
    {
        foreach(LineNode node in nextNodes)
        {
            node.CreateConnection(transform);
        }
    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < nextNodes.Count; i++)
        {
            if(nextNodes[i] != null)
                Gizmos.DrawLine(transform.position, nextNodes[i].transform.position);
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(transform.position, new Vector3(0.2f, 0.2f, 0.2f));
    }
}

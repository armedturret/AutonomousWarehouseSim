using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Forklift : MonoBehaviour
{
    [SerializeField]
    private LineNode startNode = null;

    [SerializeField]
    private float turnSpeed = 15f;
    [SerializeField]
    private float moveSpeed = 1f;

    private string m_currentGoal = "WANDER";
    private LineNode m_targetNode = null;
    private LineNode m_previousNode = null;

    private float m_yRotation = 0f;
    private float m_targetRotation = 0f;
    private Vector3 m_targetPosition;

    void Start()
    {
        //teleport to the start pos
        m_targetNode = startNode;
        transform.position = AsFlatPos(startNode.transform.position);
        m_targetPosition = transform.position;
        m_yRotation = startNode.transform.rotation.eulerAngles.y;
        m_targetRotation = m_yRotation;
        transform.rotation = Quaternion.Euler(0f, m_yRotation, 0f);

        //TEMPORARY SINCE NO ORDER SYSTEM - start moving immediately
        UpdateTargetNode();
    }

    void Update()
    {
        //how much time for actions in this frame
        float timePassed = SimManager.Instance.ScaleDeltaTime(Time.deltaTime);
        

        while ((m_targetRotation != m_yRotation || m_targetPosition != transform.position) && timePassed > 0f)
        {
            //start turning to face the target direction
            float deltaRot = m_targetRotation - m_yRotation;
            deltaRot = Mathf.Clamp(deltaRot, -turnSpeed * timePassed, turnSpeed * timePassed);
            transform.rotation = Quaternion.Euler(0f, deltaRot + m_yRotation, 0f);
            m_yRotation = deltaRot + m_yRotation;
            if (Mathf.Abs(m_targetRotation - m_yRotation) < turnSpeed * timePassed)
            {
                timePassed -= deltaRot / turnSpeed;
                transform.rotation = Quaternion.Euler(0f, m_targetRotation, 0f);
            }
            else
            {
                timePassed = 0f;
            }

            //start moving in the target direction
            Vector3 deltaPos = m_targetPosition - transform.position;
            deltaPos = Vector3.ClampMagnitude(deltaPos, moveSpeed * timePassed);
            transform.position += deltaPos;

            if((m_targetPosition - transform.position).magnitude < moveSpeed * timePassed)
            {
                //snap to target if near
                timePassed -= deltaPos.magnitude / moveSpeed;
                transform.position = m_targetPosition;

                if(m_targetNode != m_previousNode)
                {
                    UpdateTargetNode();
                }
            }
            else
            {
                timePassed = 0f;
            }
        }
    }

    //based on current desitination, choose what the next node should be in its path
    private void UpdateTargetNode()
    {
        m_previousNode = m_targetNode;

        int defaultIndex = -1;
        int targetIndex = -1;
        //find the default node and the next target node
        for(int i = 0; i < m_previousNode.nextNodeDirections.Count; i++)
        {
            if(m_previousNode.nextNodeDirections[i].ToUpper() == "DEFAULT")
            {
                defaultIndex = i;
            }
            else if(m_previousNode.nextNodeDirections[i].ToUpper() == m_currentGoal)
            {
                targetIndex = i;
            }
        }

        if(targetIndex != -1)
        {
            m_targetNode = m_previousNode.nextNodes[targetIndex];
            Debug.Log("Set next node to: " + m_targetNode.gameObject);
        }
        else if(defaultIndex != -1)
        {
            m_targetNode = m_previousNode.nextNodes[defaultIndex];
            Debug.Log("Set next node to: " + m_targetNode.gameObject);
        }
        else
        {
            Debug.LogError("No next nodes found for: " + m_previousNode.gameObject);
        }

        FaceTowards(m_targetNode.transform.position);
        m_targetPosition = AsFlatPos(m_targetNode.transform.position);
    }

    private void FaceTowards(Vector3 targetObject)
    {
        Vector3 deltaVector = targetObject - transform.position;
        deltaVector.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(deltaVector, Vector3.up);
        m_targetRotation = targetRotation.eulerAngles.y;
        //clamp to +/- 180 range
        while(Mathf.Abs(m_targetRotation - m_yRotation) > 180f)
        {
            if(m_targetRotation - m_yRotation > 180f)
            {
                m_targetRotation -= 360f;
            }
            else
            {
                m_targetRotation += 360f;
            }
        }
    }

    private Vector3 AsFlatPos(Vector3 pos)
    {
        return new Vector3(pos.x, 0f, pos.z);
    }
}

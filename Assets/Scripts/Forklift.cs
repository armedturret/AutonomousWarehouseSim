using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Forklift : MonoBehaviour
{
    public TMPro.TextMeshPro orderText;

    public Transform armExtension;
    public Transform arm;
    public GameObject crateObject;
    public Transform crateTransform;
    public OnTrigger detectTrigger;
    public OnTrigger interactTrigger;

    [SerializeField]
    private LineNode startNode = null;

    [Header("Movement Settings")]
    [SerializeField]
    private float turnSpeed = 15f;
    [SerializeField]
    private float moveSpeed = 1f;
    [SerializeField]
    private float minArmPos = 0f;
    [SerializeField]
    private float maxArmPos = 1f;
    [SerializeField]
    private float minArmExtension = 0f;
    [SerializeField]
    private float maxArmExtension = 1f;
    [SerializeField]
    private float armExtensionSpeed = 1f;

    private string m_order = "";
    private string m_currentGoal = "";
    private string m_targetId = "";
    private LineNode m_targetNode = null;
    private LineNode m_previousNode = null;

    private float m_yRotation = 0f;
    private float m_targetRotation = 0f;
    private Vector3 m_targetPosition;
    private float m_targetHeight = 0f;

    private FakeSocket m_socket;

    void Start()
    {
        //teleport to the start pos
        m_targetNode = startNode;
        transform.position = AsFlatPos(startNode.transform.position);
        m_targetPosition = transform.position;
        m_yRotation = startNode.transform.rotation.eulerAngles.y;
        m_targetRotation = m_yRotation;
        transform.rotation = Quaternion.Euler(0f, m_yRotation, 0f);

        //connect to the control
        m_socket = Control.Instance.CreateSocket();

        m_order = "IDLE";
        m_currentGoal = startNode.nodeId;
        if(orderText != null)
            orderText.text = m_order;
        Arrived();
        UpdateTargetNode();

        m_targetHeight = arm.position.y;
    }

    void FixedUpdate()
    {
        //clear order queue
        string order = m_socket.Recieve();
        while(order != null)
        {
            order = order.ToUpper();
            string[] args = order.Split(',');
            //Debug.Log("Message recieved: " + order);
            switch (args[0])
            {
                case "ORDERREQ":
                    m_socket.Send("ORDERREP " + m_order);
                    break;
                case "GOTO":
                    if(args.Length == 2)
                    {
                        m_order = order;
                        m_currentGoal = args[1];
                        if(orderText != null)
                            orderText.text = m_order;
                        UpdateTargetNode();
                    }
                    break;
                case "CRATE":
                    if(args.Length == 3)
                    {
                        m_order = order;
                        m_currentGoal = args[1];
                        m_targetId = args[2];
                        if (orderText != null)
                            orderText.text = m_order;
                        UpdateTargetNode();
                    }
                    break;
                case "IDLE":
                    m_order = order;
                    m_currentGoal = startNode.nodeId;
                    if (orderText != null)
                        orderText.text = m_order;
                    UpdateTargetNode();
                    break;
            }
            

            order = m_socket.Recieve();
        }

        //how much time for actions in this frame
        float timePassed = SimManager.Instance.ScaleDeltaTime(Time.fixedDeltaTime);

        int iterations = 0;
        //do motion updates based on current orders
        while (iterations < 6 && (m_targetRotation != m_yRotation || m_targetPosition != transform.position || Mathf.Abs(arm.position.y - m_targetHeight) > 0.001f) && timePassed > 0f)
        {
            iterations++;
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

            RecalculateObstructed();

            if (m_obstructed) return;

            //pickup any crates
            foreach(var crate in interactTrigger.currentGameObjects)
            {
                if(crate.GetComponent<Crate>() && crate.GetComponent<Crate>().ID == m_targetId && m_targetId != "")
                {
                    crate.transform.SetParent(crateTransform);
                    crate.transform.localPosition = Vector3.zero;
                    crateObject = crate;
                    m_targetId = "";
                    m_socket.Send("ORDERCOMP");
                    return;
                }
            }

            //change forklift arm height
            if(Mathf.Abs(arm.position.y - m_targetHeight) > 0.001f)
            {
                bool goingUp = m_targetHeight > arm.position.y;
                float boundingArmPos = goingUp ? maxArmPos : minArmPos;
                float boundingArmExPos = goingUp ? maxArmExtension : minArmExtension;
                //distance to be covered this frame
                float distanceCovered = timePassed *  armExtensionSpeed * (goingUp ? 1f : -1f);
                float targetDelta = Mathf.Abs(m_targetHeight - arm.position.y);
                distanceCovered = Mathf.Clamp(distanceCovered, -targetDelta, targetDelta);

                
                if(Mathf.Abs(distanceCovered) > Mathf.Abs(boundingArmPos - arm.localPosition.y))
                {
                    //snap to boundingArmPos and recalculate distance to travel
                    timePassed -= (boundingArmPos - arm.localPosition.y) / armExtensionSpeed;
                    arm.localPosition = new Vector3(arm.localPosition.x, boundingArmPos, arm.localPosition.z);

                    //recalculate distance
                    distanceCovered = timePassed * armExtensionSpeed;
                    targetDelta = m_targetHeight - arm.position.y;
                    distanceCovered = Mathf.Clamp(distanceCovered, -targetDelta, targetDelta);
                }
                else
                {
                    //move towards boundingArmPos
                    arm.localPosition += new Vector3(0f, distanceCovered, 0f);
                    distanceCovered = 0f;
                    timePassed = 0f;
                }

                if (Mathf.Abs(distanceCovered) > Mathf.Abs(boundingArmExPos - armExtension.localPosition.y))
                {
                    //snap to boundingArmExPos and recalculate distance to travel
                    timePassed -= (boundingArmExPos - armExtension.localPosition.y) / armExtensionSpeed;
                    armExtension.localPosition = new Vector3(armExtension.localPosition.x, boundingArmExPos, armExtension.localPosition.z);
                }
                else
                {
                    //move towards boundingArmExPos
                    armExtension.localPosition += new Vector3(0f, distanceCovered, 0f);
                    timePassed = 0f;
                }
            }

            //start moving in the target direction
            Vector3 deltaPos = m_targetPosition - transform.position;
            deltaPos = Vector3.ClampMagnitude(deltaPos, moveSpeed * timePassed);
            transform.position += deltaPos;

            if ((m_targetPosition - transform.position).magnitude < moveSpeed * timePassed)
            {
                //snap to target if near
                timePassed -= deltaPos.magnitude / moveSpeed;
                transform.position = m_targetPosition;

                if(m_targetNode != m_previousNode)
                {
                    Arrived();
                    UpdateTargetNode();
                }
            }
            else
            {
                timePassed = 0f;
            }
        }
    }

    //updates current node to be previous node
    private void Arrived()
    {
        m_previousNode = m_targetNode;
        m_inTransit = false;
    }

    private bool m_obstructed = false;

    public void RecalculateObstructed()
    {
        //something is detected, stop for now treating it as an obstruction
        int count = detectTrigger.currentGameObjects.Count;
        for(int i = 0; i < detectTrigger.currentGameObjects.Count; i++)
        {
            GameObject currentIndex = detectTrigger.currentGameObjects[i];
            if(currentIndex == gameObject || 
                (currentIndex.GetComponent<Crate>() && currentIndex.GetComponent<Crate>().ID == m_targetId && m_targetId != "")
                || (crateObject != null && currentIndex == crateObject))
            {
                count--;
            }
        }

        m_obstructed = count > 0;
    }

    //currently in transit and cannot change destination
    private bool m_inTransit = false;

    //based on current desitination, choose what the next node should be in its path
    private void UpdateTargetNode()
    {
        if (m_currentGoal == "" || m_inTransit) return;
        if(m_currentGoal == m_targetNode.nodeId && m_currentGoal != "")
        {
            m_currentGoal = "";
            //only have complete an order if not idling
            if (m_order != "IDLE")
            {
                if(m_targetId != "")
                {
                    m_socket.Send("ORDERINV");
                    m_targetId = "";
                }
                else
                {
                    m_socket.Send("ORDERCOMP");
                }
            } 
                
            return;
        }

        m_inTransit = true;

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
            //Debug.Log("Set next node to: " + m_targetNode.gameObject);
        }
        else if(defaultIndex != -1)
        {
            m_targetNode = m_previousNode.nextNodes[defaultIndex];
            //Debug.Log("Set next node to: " + m_targetNode.gameObject);
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

    private void OnDrawGizmos()
    {
        if(m_targetNode != null && m_previousNode != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(m_targetNode.transform.position, 0.5f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(m_previousNode.transform.position, 0.5f);
        }
    }
}

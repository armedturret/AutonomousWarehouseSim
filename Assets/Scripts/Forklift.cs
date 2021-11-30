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
    private string m_specialNodeGoal = "";
    private string m_targetNodeAction = "";
    private string m_arriveAction = "";
    private string m_currentGoal = "";
    private string m_targetId = "";
    private string m_lastHeldCrate = "";
    private bool m_rotationFirst = true;
    private LineNode m_targetNode = null;
    private LineNode m_previousNode = null;

    private float m_yRotation = 0f;
    private float m_targetRotation = 0f;
    private Vector3 m_targetPosition;
    private float m_targetHeight = 0f;

    private FakeSocket m_socket;

    private HashSet<string> m_blockedNodes = new HashSet<string>();

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

        m_targetHeight = arm.position.y;
    }

    void FixedUpdate()
    {
        ReadOrderQueue();

        //how much time for actions in this frame
        float timePassed = SimManager.Instance.ScaleDeltaTime(Time.fixedDeltaTime);

        int iterations = 0;
        //do motion updates based on current orders
        while (iterations < 6 && (m_targetRotation != m_yRotation || m_targetPosition != transform.position || Mathf.Abs(arm.position.y - m_targetHeight) > 0.001f) && timePassed > 0f)
        {
            iterations++;

            RecalculateObstructed();

            //arrived at the location
            if (m_arriveAction == "returnstartnocrate" && m_obstructed)
                Arrived();
            if (m_obstructed) return;

            if (m_rotationFirst)
            {
                timePassed = UpdateRotation(timePassed);
                timePassed = UpdateHeight(timePassed);
            }
            else
            {
                timePassed = UpdateHeight(timePassed);
                timePassed = UpdateRotation(timePassed);
            }

            //pickup any crates
            if(timePassed > 0)
            {
                foreach (var crate in interactTrigger.currentGameObjects)
                {
                    if (crate.GetComponent<Crate>() && IsTargetCrate(crate.GetComponent<Crate>()) && m_targetId != "")
                    {
                        crate.transform.SetParent(crateTransform);
                        crate.transform.localPosition = Vector3.zero;
                        crate.transform.localRotation = Quaternion.identity;
                        crateObject = crate;
                        m_lastHeldCrate = crate.GetComponent<Crate>().Id;
                        m_targetId = "";
                        //check if it needs to back up first, otherwise complete the order
                        if (m_arriveAction == "returnstart")
                        {
                            Arrived();
                        }
                        else if(m_arriveAction != "compifcrate")
                        {
                            Debug.Log("COMP through crate");
                            m_socket.Send("ORDERCOMP," + m_lastHeldCrate);
                        }   
                        return;
                    }
                }
            }

            timePassed = UpdatePosition(timePassed);
        }
    }

    private float UpdateRotation(float timePassed)
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

        return timePassed;
    }

    private float UpdateHeight(float timePassed)
    {
        //change forklift arm height
        if (Mathf.Abs(arm.position.y - m_targetHeight) > 0.001f)
        {
            bool goingUp = m_targetHeight > arm.position.y;
            float boundingArmPos = goingUp ? maxArmPos : minArmPos;
            float boundingArmExPos = goingUp ? maxArmExtension : minArmExtension;
            //distance to be covered this frame
            float distanceCovered = timePassed * armExtensionSpeed * (goingUp ? 1f : -1f);
            float targetDelta = Mathf.Abs(m_targetHeight - arm.position.y);
            distanceCovered = Mathf.Clamp(distanceCovered, -targetDelta, targetDelta);


            if (Mathf.Abs(distanceCovered) > Mathf.Abs(boundingArmPos - arm.localPosition.y))
            {
                //snap to boundingArmPos and recalculate distance to travel
                timePassed -= Mathf.Abs(boundingArmPos - arm.localPosition.y) / armExtensionSpeed;
                arm.localPosition = new Vector3(arm.localPosition.x, boundingArmPos, arm.localPosition.z);

                //recalculate distance
                distanceCovered = timePassed * armExtensionSpeed * (goingUp ? 1f : -1f);
                targetDelta = Mathf.Abs(m_targetHeight - arm.position.y);
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
                timePassed -= Mathf.Abs(boundingArmExPos - armExtension.localPosition.y) / armExtensionSpeed;
                armExtension.localPosition = new Vector3(armExtension.localPosition.x, boundingArmExPos, armExtension.localPosition.z);
            }
            else
            {
                //move towards boundingArmExPos
                armExtension.localPosition += new Vector3(0f, distanceCovered, 0f);
                timePassed = 0f;
            }
        }

        return timePassed;
    }

    private float UpdatePosition(float timePassed)
    {
        BlockNode blockNode = m_targetNode.GetComponent<BlockNode>();
        if(blockNode && blockNode.blockType == BlockNode.BlockType.ENTRANCE && m_blockedNodes.Contains(blockNode.blockId)) return timePassed;

        //start moving in the target direction
        Vector3 deltaPos = m_targetPosition - transform.position;
        deltaPos = Vector3.ClampMagnitude(deltaPos, moveSpeed * timePassed);
        transform.position += deltaPos;

        if ((m_targetPosition - transform.position).magnitude < moveSpeed * timePassed)
        {
            //snap to target if near
            timePassed -= deltaPos.magnitude / moveSpeed;
            transform.position = m_targetPosition;

            Arrived();
        }
        else
        {
            return 0f;
        }

        return timePassed;
    }

    //updates current node to be previous node
    private void Arrived()
    {
        if(m_arriveAction != "")
        {
            Debug.Log("Executing arrive action: " + m_arriveAction);
            //drop the crate and then back off
            switch (m_arriveAction)
            {
                case "dropcrateshelf":
                    //drop the crate at the right location upon arrival
                    crateObject.transform.SetParent(null);
                    crateObject = null;
                    m_targetPosition = AsFlatPos(m_targetNode.transform.position);
                    m_arriveAction = "resetheightshelf";
                    break;
                case "resetheightshelf":
                    //set height to 0 upon arrival
                    m_arriveAction = "";
                    m_rotationFirst = false;
                    m_targetHeight = 0f;
                    Debug.Log("COMP through height");
                    var orderArgs = m_order.Split(',');
                    m_socket.Send("DELIVEREDSHELF,"+m_lastHeldCrate+","+orderArgs[1] + "," + orderArgs[2] + "," + orderArgs[3]);
                    break;
                case "returnstart":
                    //return to the start of the truck node and check if theres a crate held
                    m_arriveAction = "compifcrate";
                    m_targetPosition = AsFlatPos(m_targetNode.transform.position);
                    break;
                case "returnstartnocrate":
                    m_arriveAction = "comp";
                    m_targetPosition = AsFlatPos(m_targetNode.transform.position);
                    crateObject.transform.SetParent(null);
                    crateObject = null;
                    break;
                case "comp":
                    m_arriveAction = "";
                    Debug.Log("COMP through no crate");
                    m_socket.Send("ORDERCOMP");
                    break;
                case "compifcrate":
                    m_arriveAction = "";
                    m_rotationFirst = false;
                    m_targetHeight = 0f;
                    Debug.Log("COMP through ifcrate");
                    if (crateObject != null)
                        m_socket.Send("ORDERCOMP," + m_lastHeldCrate);
                    else
                        m_socket.Send("ORDERINV");
                    break;
            }
        }
        else if(m_targetNode != m_previousNode)
        {
            m_inTransit = false;

            //check if the target node was a block node and occupy this block
            BlockNode blockNode = m_targetNode.GetComponent<BlockNode>();
            if (blockNode && blockNode.blockType == BlockNode.BlockType.ENTRANCE)
            {
                m_socket.Send("BLOCKENTERED," + blockNode.blockId);
            }else if(blockNode && blockNode.blockType == BlockNode.BlockType.EXIT && m_blockedNodes.Contains(blockNode.blockId))
            {
                m_socket.Send("BLOCKLEFT," + blockNode.blockId);
            }

            m_previousNode = m_targetNode;
            UpdateTargetNode();
        }
    }

    private bool m_obstructed = false;

    private bool IsTargetCrate(Crate crate)
    {
        string[] targets = m_targetId.Split(',');
        for(int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == crate.Id)
                return true;
        }
        return false;
    }

    public void RecalculateObstructed()
    {
        //something is detected, stop for now treating it as an obstruction
        int count = detectTrigger.currentGameObjects.Count;
        for(int i = 0; i < detectTrigger.currentGameObjects.Count; i++)
        {
            GameObject currentIndex = detectTrigger.currentGameObjects[i];
            if (currentIndex == gameObject ||
                (currentIndex.GetComponent<Crate>()
                && IsTargetCrate(currentIndex.GetComponent<Crate>())
                && m_targetId != "" 
                && crateObject == null)
                || (crateObject != null && currentIndex == crateObject))
            {
                count--;
            }
        }

        m_obstructed = count > 0;
        if (m_arriveAction != "" && m_arriveAction != "returnstartnocrate")
            m_obstructed = false;
    }

    //currently in transit and cannot change destination
    private bool m_inTransit = false;

    private void ReadOrderQueue()
    {
        //clear order queue
        string order = m_socket.Recieve();
        while (order != null)
        {
            order = order.ToUpper();
            string[] args = order.Split(',');
            Debug.Log("Message recieved: " + order);
            switch (args[0])
            {
                case "ORDERREQ":
                    m_socket.Send("ORDERREP " + m_order);
                    break;
                case "GOTO":
                    if (args.Length == 2)
                    {
                        m_order = order;
                        m_currentGoal = args[1];
                        if (orderText != null)
                            orderText.text = m_order;
                        UpdateTargetNode();
                        break;
                    }
                    goto default;
                case "CRATE":
                    if (args.Length >= 3 && crateObject == null)
                    {
                        m_order = order;
                        m_currentGoal = args[1];
                        string crateArg = args[2];
                        for (int i = 3; i < args.Length; i++)
                        {
                            crateArg += "," + args[i];
                        }
                        m_targetId = crateArg;
                        //goal is to check if its a truck node and move down it to complete its actions but not required
                        m_targetNodeAction = "checktruckgrab";
                        if (orderText != null)
                            orderText.text = m_order;
                        UpdateTargetNode();
                        break;
                    }
                    goto default;
                case "CRATESHELF":
                    if (args.Length == 5)
                    {
                        m_order = order;
                        //go for the target row using currentGoal
                        m_currentGoal = args[1];
                        m_targetId = args[4];
                        //set the special node goal value
                        m_specialNodeGoal = "SHELF," + args[2] + "," + args[3];
                        m_targetNodeAction = "grabshelf";
                        if (orderText != null)
                            orderText.text = m_order;
                        UpdateTargetNode();
                        break;
                    }
                    goto default;
                case "DELIVER":
                    if (args.Length == 4 && crateObject != null)
                    {
                        m_order = order;
                        //go for the target row using currentGoal
                        m_currentGoal = args[1];
                        //set the special node goal value
                        m_specialNodeGoal = "SHELF," + args[2] + "," + args[3];
                        m_targetNodeAction = "delivershelf";
                        if (orderText != null)
                            orderText.text = m_order;
                        UpdateTargetNode();
                        break;
                    }
                    goto default;
                case "DROPOFF":
                    m_order = order;
                    m_currentGoal = args[1];
                    m_targetNodeAction = "delivertruck";
                    if (orderText != null)
                        orderText.text = m_order;
                    UpdateTargetNode();
                    break;
                case "IDLE":
                    m_order = order;
                    m_currentGoal = startNode.nodeId;
                    if (orderText != null)
                        orderText.text = m_order;
                    UpdateTargetNode();
                    break;
                case "BLOCKOCCUPIED":
                    m_blockedNodes.Add(args[1]);
                    break;
                case "BLOCKFREED":
                    if(m_blockedNodes.Contains(args[1]))
                        m_blockedNodes.Remove(args[1]);
                    break;
                default:
                    m_socket.Send("ORDERINV");
                    break;
            }


            order = m_socket.Recieve();
        }
    }

    private bool CheckSpecialNodeConditions()
    {
        if (m_specialNodeGoal == "") return false;
        var args = m_specialNodeGoal.Split(',');
        //check shelf conditions
        switch (args[0])
        {
            case "SHELF":
                if (m_targetNode.GetComponent<ShelfNode>())
                {
                    var shelfTarget = m_targetNode.GetComponent<ShelfNode>();
                    if (shelfTarget.rowNumber == int.Parse(args[1]))
                    {
                        return true;
                    }
                }
                break;
        }
        //m_specialNodeGoal = "SHELF,"+args[2] + "," + args[3];
        return false;
    }

    //based on current destination, choose what the next node should be in its path
    private void UpdateTargetNode()
    {
        //mid movement or no goal, do not update
        if (m_currentGoal == "" || m_inTransit) return;
        //check final destination goals
        if((m_currentGoal == m_targetNode.nodeId || CheckSpecialNodeConditions()) && m_currentGoal != "")
        {
            m_specialNodeGoal = "";
            m_currentGoal = "";
            //only have complete an order if not idling
            if (m_order != "IDLE" && m_targetNodeAction == "")
            {
                if(m_targetId != "")
                {
                    m_targetId = "";
                    m_socket.Send("ORDERINV");
                }
                else
                {
                    m_targetId = "";
                    Debug.Log("COMP through node arrival");
                    m_socket.Send("ORDERCOMP");
                }
            }else if (m_targetNodeAction != "")
            {
                Debug.Log("Executing target action: " + m_targetNodeAction);
                switch (m_targetNodeAction)
                {
                    case "delivershelf":
                        m_arriveAction = "dropcrateshelf";
                        goto case "goshelf";
                    case "grabshelf":
                        m_arriveAction = "returnstart";
                        goto case "goshelf";
                    case "goshelf":
                        //rotate, change height, then moveforward
                        var shelfRow = int.Parse(m_order.Split(',')[3]);
                        ShelfNode shelfNode = m_targetNode.GetComponent<ShelfNode>();
                        FaceTowards(shelfNode.shelfTransform.position);
                        m_targetHeight = shelfNode.baseHeight + shelfNode.offset * shelfRow;
                        m_targetPosition = AsFlatPos(shelfNode.shelfTransform.position);
                        m_targetNodeAction = "";
                        m_rotationFirst = true;
                        break;
                    case "checktruckgrab":
                        {
                            //check if this is a truck node, otherwise send an order invalid
                            TruckNode truckNode = m_targetNode.GetComponent<TruckNode>();
                            if (truckNode)
                            {
                                //traverse the truck node
                                m_arriveAction = "returnstart";
                                FaceTowards(truckNode.truckEnd.position);
                                m_targetNodeAction = "";
                                m_targetPosition = AsFlatPos(truckNode.truckEnd.position);
                            }
                            else
                            {
                                m_targetId = "";
                                m_socket.Send("ORDERINV");
                            }
                            break;
                        }
                    case "delivertruck":
                        {
                            //move towards the target node, drop if obstructed or reaches the end
                            TruckNode truckNode = m_targetNode.GetComponent<TruckNode>();
                            if (truckNode)
                            {
                                //traverse the truck node
                                m_arriveAction = "returnstartnocrate";
                                FaceTowards(truckNode.truckEnd.position);
                                m_targetNodeAction = "";
                                m_targetPosition = AsFlatPos(truckNode.truckEnd.position);
                            }
                            else
                            {
                                m_targetId = "";
                                m_socket.Send("ORDERINV");
                            }
                            break;
                        }
                }
            }

            return;
        }

        //mid movement, do no update
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
        }
        else if(defaultIndex != -1)
        {
            m_targetNode = m_previousNode.nextNodes[defaultIndex];
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

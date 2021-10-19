using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control : MonoBehaviour
{
    public static Control Instance;

    public TMPro.TextMeshProUGUI orderQueueUI;

    //list of all currently open sockets
    private List<FakeSocket> m_forklifts = new List<FakeSocket>();
    //current known list of orders on forklifts
    private List<string> m_forkliftOrders = new List<string>();

    private List<string> m_queuedOrders = new List<string>();

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //current behavior is just assign all forklifts to go to A then return
        //recieve every queue
        for(int i = 0; i < m_forklifts.Count; i++){
            var forklift = m_forklifts[i];
            var order = forklift.Recieve();
            while (order != null)
            {
                order = order.ToUpper().Trim();
                string[] args = order.Split(',');
                Debug.Log("Message recieved: " + order);
                switch (args[0])
                {
                    case "ORDERCOMP":
                    case "ORDERINV":
                        if(m_queuedOrders.Count > 0)
                        {
                            string nextOrder = m_queuedOrders[0];
                            m_queuedOrders.RemoveAt(0);
                            forklift.Send(nextOrder);
                            m_forkliftOrders[i] = nextOrder;
                            UpdateOrderView();
                        }
                        else
                        {
                            forklift.Send("IDLE");
                            //update orders
                            m_forkliftOrders[i] = "IDLE";
                        }
                        break;
                    case "ORDERREP":
                        m_forkliftOrders[i] = order.Substring(order.IndexOf(',') + 1);
                        break;
                }

                order = forklift.Recieve();
            }
        }
    }

    //connect a new forklift to the central control system and return the socket
    public FakeSocket CreateSocket()
    {
        FakeSocket temp = new FakeSocket();
        m_forklifts.Add(temp);
        m_forkliftOrders.Add("IDLE");

        return temp.pair;
    }

    //assigns a command to the first available forklift
    public void AssignCommand(string command)
    {
        command = command.ToUpper();
        for(int i = 0; i < m_forkliftOrders.Count; i++)
        {
            if(m_forkliftOrders[i] == "IDLE")
            {
                Debug.Log("Assigning: " + command + " to " + i);
                m_forkliftOrders[i] = command;
                m_forklifts[i].Send(command);
                return;
            }
        }

        Debug.Log("No available forklifts to assign order to. Adding to order queue.");
        m_queuedOrders.Add(command);
        UpdateOrderView();
    }

    private void UpdateOrderView()
    {
        if(orderQueueUI != null)
        {
            orderQueueUI.text = "Queued Orders:\n";
            foreach(var order in m_queuedOrders)
            {
                orderQueueUI.text += order + "\n";
            }
            if(m_queuedOrders.Count == 0)
            {
                orderQueueUI.text = "";
            }
        }
    }
}

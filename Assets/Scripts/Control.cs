using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control : MonoBehaviour
{
    public static Control Instance;

    //list of all currently open sockets
    private List<FakeSocket> m_forklifts = new List<FakeSocket>();

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
        foreach(var forklift in m_forklifts){
            var order = forklift.Recieve();
            while (order != null)
            {
                order = order.ToUpper().Trim();
                string[] args = order.Split(',');
                Debug.Log("Message recieved: " + order);
                switch (args[0])
                {
                    case "ORDERCOMP":
                        //TEMPORARY MEASURE TO JUST SEND THEM BACK TO HOME
                        forklift.Send("IDLE");
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

        //TEMPORARY GO TO B
        temp.Send("GOTO,POINTB");

        return temp.pair;
    }
}

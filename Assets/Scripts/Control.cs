using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control : MonoBehaviour
{
    public static Control Instance;

    public TMPro.TextMeshProUGUI orderQueueUI;

    public List<ShelfInfo> shelfInfos = new List<ShelfInfo>();

    class Shelf
    {
        public int width = 2;
        public int height = 3;
        //location in row
        public int rowLocation = 0;

        public string rowName = "";

        List<string> occupied = null;

        public void InitList()
        {
            occupied = new List<string>();
            for (int i = 0; i < width * height; i++)
            {
                occupied.Add("");
            }
        }

        //returns first and column available
        public (int, int) FirstAvailable()
        {
            for(int i = 0; i < occupied.Count; i++)
            {
                if (occupied[i] == "")
                {
                    return (i / height + rowLocation, i % height);
                }
            }

            //failure
            return (-1, -1);
        }

        public (int, int) FindCrate(string crateId)
        {
            for (int i = 0; i < occupied.Count; i++)
            {
                //check if the crate is there and not in transit
                if (occupied[i] == crateId)
                {
                    return (i / height + rowLocation, i % height);
                }
            }

            //failure
            return (-1, -1);
        }

        //checks if the rowLocation and height fits here
        public bool IsLocationHere((int, int) location)
        {
            return location.Item1 >= rowLocation && location.Item1 < rowLocation + width && location.Item2 < height;
        }

        //this takes the int in terms of rowLocation not just row
        public void CrateArrived((int, int) location, string crateId)
        {
            int index = (location.Item1 - rowLocation) * height + location.Item2;
            occupied[index] = crateId;
        }
    }

    private List<Shelf> m_shelves = new List<Shelf>();

    //list of all currently open sockets
    private List<FakeSocket> m_forklifts = new List<FakeSocket>();
    //current known list of orders on forklifts
    private List<string> m_forkliftOrders = new List<string>();

    private List<string> m_queuedOrders = new List<string>();

    //current list of requests and droppoffs from various trucks (type, cratesremaining, Truck)
    private List<(string, string, Truck)> m_truckRequisitions = new List<(string, string, Truck)>();

    Control()
    {
        Instance = this;
    }

    private void Awake()
    {
        
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //recieve every queued message
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
                    case "RECHARGING":
                        m_forkliftOrders[i] = "RECHARGING";
                        //update if it was a dropoff order
                        if (m_forkliftOrders[i].Split(',')[0] == "DROPOFF")
                        {
                            UpdateTruckRequisition(args[1]);
                        }
                        break;
                    case "DELIVEREDSHELF":
                        //determine what shelf to occupy
                        (string, int, int) occupyLocation = (args[2], int.Parse(args[3]), int.Parse(args[4]));
                        UpdateShelf(occupyLocation, args[1]);
                        if (args.Length == 6 && args[5] == "RECHARGING")
                        {
                            m_forkliftOrders[i] = "RECHARGING";
                            break;
                        }
                        goto case "ORDERCOMP";
                    case "ORDERCOMP":
                        //send a delivery order
                        if (m_forkliftOrders[i].Split(',')[0] == "CRATE")
                        {
                            //update any truck requistions
                            UpdateTruckRequisition(args[1]);

                            //find the proper shelf
                            (string, int, int) location = FirstAvailable();
                            UpdateShelf(location, args[1]);
                            string directions = "DELIVER," + location.Item1 + "," + location.Item2 + "," + location.Item3;
                            forklift.Send(directions);
                            m_forkliftOrders[i] = directions;

                            break;
                        }
                        else if(m_forkliftOrders[i].Split(',')[0] == "CRATESHELF")
                        {
                            SendCommand(UpdateTruckRequisition(args[1], true), i);
                            break;
                        }
                        else if(m_forkliftOrders[i].Split(',')[0] == "DROPOFF")
                        {
                            UpdateTruckRequisition(args[1]);
                        }

                        if (m_queuedOrders.Count > 0)
                        {
                            string nextOrder = m_queuedOrders[0];
                            m_queuedOrders.RemoveAt(0);
                            SendCommand(nextOrder, i);
                            UpdateOrderView();
                        }
                        else
                        {
                            forklift.Send("IDLE");
                            m_forkliftOrders[i] = "IDLE";
                        }
                        break;
                    case "ORDERINV":
                        Debug.LogError("Invalid order given: " + m_forkliftOrders[i]);
                        goto case "ORDERCOMP";
                    case "ORDERREP":
                        m_forkliftOrders[i] = order.Substring(order.IndexOf(',') + 1);
                        break;
                    case "BLOCKLEFT":
                        SendAll("BLOCKFREED,"+args[1]);
                        break;
                    case "BLOCKENTERED":
                        SendAll("BLOCKOCCUPIED," + args[1]);
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

    public void AddShelfInfo(ShelfInfo shelfInfo)
    {   
        //add the availability
        var temp = new Shelf();
        temp.height = shelfInfo.height;
        temp.width = shelfInfo.width;
        temp.rowLocation = shelfInfo.rowLocation;
        temp.rowName = shelfInfo.rowName;
        temp.InitList();
        m_shelves.Add(temp);
    }

    //assigns a command to the first available forklift
    public void AssignCommand(string command)
    {
        for (int i = 0; i < m_forkliftOrders.Count; i++)
        {
            if(m_forkliftOrders[i] == "IDLE")
            {
                SendCommand(command, i);
                return;
            }
        }

        m_queuedOrders.Add(command);
        UpdateOrderView();
    }

    private void SendCommand(string command, int forkliftIndex)
    {
        command = command.ToUpper();
        //check if its a find crate order
        var args = command.Split(',');
        if (args[0] == "CRATESHELF")
        {
            var location = FindCrate(args[1]);
            if (location != ("", -1, -1))
            {
                command = "CRATESHELF," + location.Item1 + "," + location.Item2 + "," + location.Item3 + "," + args[1];
            }
            else
            {
                m_queuedOrders.Add(command);
                UpdateOrderView();
            }
        }

        Debug.Log("Assigning: " + command + " to " + forkliftIndex);
        m_forkliftOrders[forkliftIndex] = command;
        m_forklifts[forkliftIndex].Send(command);
        return;
    }

    //finds the first available shelf returns rowName, row, height ("", -1, -1) if not found
    private (string, int, int) FirstAvailable()
    {
        foreach(var shelf in m_shelves)
        {
            //see if the shelf is available
            var returnVal = shelf.FirstAvailable();
            if (returnVal != (-1, -1))
            {
                return (shelf.rowName, returnVal.Item1, returnVal.Item2);
            }
        }

        Debug.LogError("Warehouse overoccupied");
        return ("", -1, -1);
    }

    //searches all shelves and returns rowName, row, height ("", -1, -1) if not found
    private (string, int, int) FindCrate(string crateId)
    {
        for (int i = 0; i < m_shelves.Count; i++)
        {
            var returnVal = m_shelves[i].FindCrate(crateId);
            if (returnVal != (-1, -1))
                return (m_shelves[i].rowName, returnVal.Item1, returnVal.Item2);
        }

        return ("", -1, -1);
    }

    private void UpdateShelf((string, int, int) location, string crateId)
    {
        for(int i = 0; i < m_shelves.Count; i++)
        {
            if(m_shelves[i].rowName == location.Item1 && m_shelves[i].IsLocationHere((location.Item2, location.Item3)))
            {
                m_shelves[i].CrateArrived((location.Item2, location.Item3), crateId);
            }
        }
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

    private void SendAll(string message)
    {
        foreach(var forklift in m_forklifts){
            forklift.Send(message);
        }
    }

    public void TruckArrived(Truck truck, string arguments, string location)
    {
        //spawn crates if it is a dropoff argument
        string[] args = arguments.Split(',');
        //make a string of all crates in the truck
        string crateArg = args[2];
        for(int i = 3; i < args.Length; i++)
        {
            crateArg += "," + args[i]; 
        }

        if (args.Length > 2 && args[0] == "DROPOFF")
        {
            for (int i = 2; i < args.Length; i++)
            {
                AssignCommand("CRATE,"+location+","+crateArg);
            }
        }

        //request crates if it is a request argument
        if(args.Length > 2 && args[0] == "PICKUP")
        {
            //assign the command to grab each of the crates
            for(int i = 2; i < args.Length; i++)
            {
                AssignCommand("CRATESHELF," + args[i]);
            }
        }

        m_truckRequisitions.Add((args[0], crateArg, truck));
        Debug.Log("New truck requisition: " + m_truckRequisitions[m_truckRequisitions.Count - 1]);
    }

    public void TruckLeft(string loadingBayId)
    {

    }

    //updates the necessary truck requisition and returns the command needed
    private string UpdateTruckRequisition(string crateId, bool locate = false)
    {
        //check every order linearly
        for(int i = 0; i < m_truckRequisitions.Count; i++)
        {
            var crates = new List<string>(m_truckRequisitions[i].Item2.Split(','));
            if (crates.Contains(crateId))
            {
                //check what to do
                string type = m_truckRequisitions[i].Item1;
                if (type == "DROPOFF")
                {
                    //remove from list and return nothing
                    crates.Remove(crateId);
                    m_truckRequisitions[i] = (m_truckRequisitions[i].Item1, string.Join(",", crates), m_truckRequisitions[i].Item3);
                    UpdateTruckIndex(i);
                    Debug.Log("Crate removed from truck: " + crateId);
                    return "";
                }
                else if (type == "PICKUP")
                {
                    if (locate)
                    {
                        //determine if crate needs to be delivered to specific location
                        return "DROPOFF," + m_truckRequisitions[i].Item3.location;
                    }
                    else
                    {
                        //dropped on truck so remove from list
                        crates.Remove(crateId);
                        m_truckRequisitions[i] = (m_truckRequisitions[i].Item1, string.Join(",", crates), m_truckRequisitions[i].Item3);
                        UpdateTruckIndex(i);
                        Debug.Log("Crate loaded on truck: " + crateId);
                        return "";
                    }
                }
            }
        }

        Debug.Log(crateId + " not found in truck requisitions");
        return "";
    }

    private void UpdateTruckIndex(int index)
    {
        //check if nothing left
        if(m_truckRequisitions[index].Item2.Length == 0)
        {
            Destroy(m_truckRequisitions[index].Item3.gameObject);
            m_truckRequisitions.RemoveAt(index);
        }
    }
}

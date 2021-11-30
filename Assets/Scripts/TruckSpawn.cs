using System.Collections;
using UnityEngine;

public class TruckSpawn : MonoBehaviour
{
    public GameObject truckPrefab;
    public GameObject cratePrefab;

    public string location = "";

    //spawns a truck at a location with specific arguments
    public void SpawnTruck(string arguments)
    {
        //spawn a truck and specify its arguments
        GameObject truckObject = Instantiate(truckPrefab, transform.position, transform.rotation);
        Truck truck = truckObject.GetComponent<Truck>();
        truck.Arrive(arguments, location);

        //spawn crates if it is a dropoff argument
        string[] args = arguments.Split(',');
        if(args.Length > 2 && args[0] == "DROPOFF")
        {
            for(int i = 2; i < args.Length; i++)
            {
                GameObject crateObject = Instantiate(cratePrefab, truck.CrateSpawns[i - 2]);
                crateObject.GetComponent<Crate>().Id = args[i];
            }
        }
    }
}
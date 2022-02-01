using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TruckSpawnManager : MonoBehaviour
{
    public List<TruckSpawn> truckSpawns;

    public TMP_Dropdown spawnDropdown;

    private string m_mode = "DROPOFF";
    private string m_spawn = "TRUCKONE";
    private string m_crates = "";

    public void SetMode(int index)
    {
        m_mode = index == 0 ? "DROPOFF" : "PICKUP";
    }

    public void SetTruckSpawn(int index)
    {
        m_spawn = spawnDropdown.options[index].text;
    }

    public void SetCrates(string crates)
    {
        m_crates = crates;
    }

    public void SpawnTruck()
    {
        //spawn a truck at the correct spot with the correct parameters
        for(int i = 0; i < truckSpawns.Count; i++)
        {
            if(truckSpawns[i].location == m_spawn)
            {
                truckSpawns[i].SpawnTruck(m_mode + "," + m_spawn + "," + m_crates);
            }
        }
    }
}
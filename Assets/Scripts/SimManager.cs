using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimManager : MonoBehaviour
{
    public static SimManager Instance;

    [SerializeField]
    private float simulationSpeed = 1f;

    [SerializeField]
    [Tooltip("Max time in seconds of scaled time to compute movement")]
    private float maxCalcTime = 360f;

    private void Awake()
    {
        Instance = this;
    }

    public float ScaleDeltaTime(float deltaTime)
    {
        return Mathf.Clamp(deltaTime * simulationSpeed, 0f, maxCalcTime);
    }
}

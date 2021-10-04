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

    private bool paused = false;

    private void Awake()
    {
        Instance = this;
    }

    public float ScaleDeltaTime(float deltaTime)
    {
        if (paused) return 0f;

        return Mathf.Clamp(deltaTime * simulationSpeed, 0f, maxCalcTime);
    }

    public void TogglePause()
    {
        paused = !paused;
    }

    public void UpdateSpeed(string input)
    {
        paused = false;

        input = input.Trim();
        if (input == "")
            paused = true;
        else
            simulationSpeed = float.Parse(input);
    }
}

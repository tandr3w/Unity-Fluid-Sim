using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    // Start is called before the first frame update
    public Simulator Simulator;
    public float totalTime = 0f;
    public float prevTime = 0f;

    void Start()
    {
        totalTime = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        totalTime += Time.deltaTime;
        if (totalTime - prevTime > Simulator.SpawnInterval){
            prevTime = totalTime;
            Simulator.CreateParticle(transform.position.x, transform.position.y, Simulator.LaunchVelX, Simulator.LaunchVelY);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ParticleArray = System.Collections.Generic.List<Particle>;


public class Simulator : MonoBehaviour
{
    [SerializeField] private GameObject ParticlePrefab;
    public ParticleArray particleArray = new ParticleArray();
    public float SMOOTHING_RADIUS = 5f;
    public bool GRAVITY_ENABLED = true;
    public float GRAVITY = -9.81f;
    public bool BOUNDING_BOX_ENABLED = true;
    public float TARGET_DENSITY = 1.5f;
    public float k = 1f; // Pressure Multiplier
    public float k_near = 0.01f;

    public float SpawnInterval = 1f;

    public float LaunchVelX = 5f;
    public float LaunchVelY = 5f;
    public float MaxVel = 7f;

    public bool disableInertia = false;
    public float mouseMult = 1f;
    public bool REPEL = false;

    public int CREATE_Y = 5;
    public int CREATE_X = 10;

    public bool CREATE = true;

    public float BOUNDING_Y_TOP = 8f;
    public float BOUNDING_Y_BOTTOM = -10f;
    public float BOUNDING_X_LEFT = -21f;
    public float BOUNDING_X_RIGHT = 21f;
    public float VISCOSITY_CONSTANT = 0.1f;
    public float TIMESTEP = 1f;
    public bool COLORBYPRESSURE = false;
    public ParticleArray[] grid;


    public Particle CreateParticle(float posx, float posy, float velx, float vely){
        Particle newParticle = Instantiate(ParticlePrefab).GetComponent<Particle>();
        Vector2 newPos = Vector2.zero;
        Vector2 newVel = Vector2.zero;
        newPos.x = posx;
        newPos.y = posy;
        newVel.x = velx;
        newVel.y = vely;

        newParticle.transform.position = newPos;
        newParticle.velocity = newVel;
        newParticle.BOUNDING_BOX_ENABLED = BOUNDING_BOX_ENABLED;
        newParticle.GRAVITY_ENABLED = GRAVITY_ENABLED;
        newParticle.GRAVITY = GRAVITY;
        newParticle.MaxVel = MaxVel;
        newParticle.BOUNDING_X_LEFT = BOUNDING_X_LEFT;
        newParticle.BOUNDING_X_RIGHT = BOUNDING_X_RIGHT;
        newParticle.BOUNDING_Y_BOTTOM = BOUNDING_Y_BOTTOM;
        newParticle.BOUNDING_Y_TOP = BOUNDING_Y_TOP;
        newParticle.TIMESTEP = TIMESTEP;
        if (GRAVITY_ENABLED){
            newParticle.force.y = GRAVITY;    
        }
        particleArray.Add(newParticle);

        return newParticle;
    }

    float SmoothingFunction(float dist, float radius){
        float q = 1f - (dist / radius);
        return q*q;
    }

    float SmoothingFunctionNear(float dist, float radius){
        float q = 1f - (dist / radius);
        return q*q*q;
    }

    Vector2 PosToGrid(float xPos, float yPos){
        Vector2 res = Vector2.zero;
        res.x = (int) Math.Max(0, (xPos - BOUNDING_X_LEFT) / SMOOTHING_RADIUS);
        res.y = (int) Math.Max(0, (yPos - BOUNDING_Y_BOTTOM) / SMOOTHING_RADIUS);
        return res;
    }

    int PosToIndex(float xPos, float yPos){
        int yCoordCount = (int) ((BOUNDING_Y_TOP - BOUNDING_Y_BOTTOM) / SMOOTHING_RADIUS);
        return (int) xPos*yCoordCount + (int) yPos;
    }

    void CalculateGrid(){
        int xCoordCount = (int) ((BOUNDING_X_RIGHT - BOUNDING_X_LEFT) / SMOOTHING_RADIUS);
        int yCoordCount = (int) ((BOUNDING_Y_TOP - BOUNDING_Y_BOTTOM) / SMOOTHING_RADIUS);
        grid = new ParticleArray[(xCoordCount+10)*(yCoordCount+10)];
        for (int i=0; i<grid.Length; i++){
            grid[i] = new ParticleArray();
        }
        for (int i=0; i<particleArray.Count; i++){
            Particle p = particleArray[i];
            Vector2 gridPos = PosToGrid(p.position.x, p.position.y);
            int index = PosToIndex(gridPos.x, gridPos.y);
            if (index >= 0 && index < grid.Length){
                grid[PosToIndex(gridPos.x, gridPos.y)].Add(p);
            }
        }
    }

    // Start is called before the first frame update
    void CalculateDensities(){
        for (int i = 0; i < particleArray.Count; i++){
            Particle p1 = particleArray[i];
            float density = 0f;
            float density_near = 0f;
            Vector2 ParticleGridPos = PosToGrid(p1.position.x, p1.position.y);
            for (int gridx=-1; gridx<=1; gridx++){
                for (int gridy=-1; gridy<=1; gridy++){
                    int pIndex = PosToIndex(ParticleGridPos.x + gridx, ParticleGridPos.y + gridy);
                    if (pIndex < 0 || pIndex > grid.Length){
                        continue;
                    }
                    foreach (Particle p2 in grid[pIndex]){
                        float dist = Vector2.Distance(p1.position + p1.velocity * Time.deltaTime * TIMESTEP, p2.position);
                        if (dist < SMOOTHING_RADIUS){
                            float res = SmoothingFunction(dist, SMOOTHING_RADIUS);
                            float res_near = SmoothingFunctionNear(dist, SMOOTHING_RADIUS);
                            density += res;
                            density_near += res_near;
                            p2.density += res;
                            p2.density_near += res_near;
                            p1.neighbors.Add(p2);
                            p2.neighbors.Add(p1);
                        }
                    }
                }
            }
            p1.density += density;
            p1.density_near += density_near;
        }
    }
    void CalculatePressures(){
        foreach (Particle p in particleArray){
            p.pressure = (p.density - TARGET_DENSITY) * k;
            p.pressure_near = p.density_near * k_near;
            if (COLORBYPRESSURE) {
                SpriteRenderer sprite = p.GetComponent<SpriteRenderer>();
                // if (p.pressure > 0){
                //     sprite.color = new Color(Math.Min(1, p.pressure), 0, 0, 1);
                // }
                // else {
                //     sprite.color = new Color(0, 0, Math.Min(1, Math.Abs(p.pressure))*10, 1);
                // }
                sprite.color = new Color(Math.Max(0, 1-p.pressure/2), Math.Max(0, 1-p.pressure/2), Math.Max(0, 1-p.pressure/2), 1);

            }
        }

    }

    void ApplyPressureForce(){
        foreach (Particle p in particleArray){
            Vector2 pressure_force = Vector2.zero;
            foreach (Particle neighbor in p.neighbors){
                Vector2 direction = (neighbor.position - p.position).normalized; // Represents direction to neighbor
                float dist = Vector2.Distance(p.position, neighbor.position);
                float q = 1f - (dist / SMOOTHING_RADIUS);
                Vector2 addedPressure = direction * ((p.pressure + neighbor.pressure) * q + (p.pressure_near + neighbor.pressure_near) * q * q);
                pressure_force += addedPressure;
                neighbor.force += addedPressure;
            }
            // Based on Newton's third law, an equal and opposite force must be applied on the original particle
            p.force -= pressure_force;
        }
    }

    void ApplyViscosityForce(){
        foreach (Particle p in particleArray){
            Vector2 pressure_force = Vector2.zero;
            foreach (Particle neighbor in p.neighbors){
                Vector2 pos_diff = p.position - neighbor.position;
                float dist = Vector2.Distance(p.position, neighbor.position);
                float q = 1 - dist / SMOOTHING_RADIUS;
                float vel_diff = Vector2.Dot(p.velocity - neighbor.velocity, pos_diff.normalized);   
                if (vel_diff > 0){
                    Vector2 I = q * VISCOSITY_CONSTANT * vel_diff * pos_diff.normalized;
                    p.velocity -= I * 0.5f;
                    neighbor.velocity += I * 0.5f;
                }
            }
        }    
    }

   
    void Start()
    {
        // Particle n = CreateParticle(0, -1, 0, 0);
        // SpriteRenderer sprite = n.GetComponent<SpriteRenderer>();
        // sprite.color = new Color(1, 0, 0, 1);
        if (CREATE){
            for (int i=-CREATE_X; i<CREATE_X; i++){
                for (int j=-CREATE_Y; j<CREATE_Y; j++){
                    CreateParticle(i, j, 0, 0);
                    CreateParticle(i+0.35f, j+0.35f, 0, 0);
                    CreateParticle(i-0.35f, j-0.35f, 0, 0);
                    CreateParticle(i+0.35f, j-0.35f, 0, 0);
                    CreateParticle(i-0.35f, j+0.35f, 0, 0);
                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
        // particleArray[0].position = mousePos;
        // Debug.Log(particleArray[0].density);

        bool mouseDown = Input.GetMouseButton(0);
        particleArray.RemoveAll(particle => particle.destroyed == true);

        foreach (Particle p in particleArray){
            p.UpdateManual();
            p.GRAVITY_ENABLED = GRAVITY_ENABLED;
            p.GRAVITY = GRAVITY;
            p.BOUNDING_BOX_ENABLED = BOUNDING_BOX_ENABLED;
            p.MaxVel = MaxVel;
            p.BOUNDING_X_LEFT = BOUNDING_X_LEFT;
            p.BOUNDING_X_RIGHT = BOUNDING_X_RIGHT;
            p.BOUNDING_Y_BOTTOM = BOUNDING_Y_BOTTOM;
            p.BOUNDING_Y_TOP = BOUNDING_Y_TOP;
            p.TIMESTEP = TIMESTEP;
            if (disableInertia){
                p.velocity = Vector2.zero;
            }
            if (mouseDown){
                Vector2 dir = (mousePos-p.position).normalized;
                float dist_ = Vector2.Distance(mousePos, p.position);
                if (dist_ > 4f){
                    continue;
                }
                float q_ = 1f - (dist_ / 4f);
                if (REPEL){
                    p.force += mouseMult*q_*-dir;

                }
                else {
                    p.force += mouseMult*q_*dir;
                }
            }
            
        }
        CalculateGrid();
        CalculateDensities();
        CalculatePressures();
        ApplyPressureForce();
        ApplyViscosityForce();

        // Debug.Log("DENSITY: " + particleArray[0].density + " PRESSURE: " + particleArray[0].pressure + " FORCE: " + particleArray[0].force);
    }  
}

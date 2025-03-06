using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using ParticleArray = System.Collections.Generic.List<Particle>;


public class Particle : MonoBehaviour
{

    // All values are in standard SI units (meters, seconds, kg)
    public Vector2 force = Vector2.zero;
    public Vector2 velocity = Vector2.zero;
    public Vector2 position = Vector2.zero;
    private Vector2 prev = Vector2.zero;
    public float density = 0f;
    public float density_near = 0f;
    public float pressure = 0f;
    public float pressure_near = 0f;
    private float DAMPING_FACTOR = 0.3f;
    public float BOUNDING_Y_TOP;
    public float BOUNDING_Y_BOTTOM;
    public float BOUNDING_X_LEFT;
    public float BOUNDING_X_RIGHT;
    public float TIMESTEP;  
    public ParticleArray neighbors = new ParticleArray();
    public bool BOUNDING_BOX_ENABLED;
    public bool GRAVITY_ENABLED;
    public float GRAVITY;
    public float MaxVel;
    public bool destroyed = false;

    void Start()
    {
        position = transform.position;
        prev = position;
        destroyed = false;
    }

    // Update is called once per frame
    public void UpdateManual()
    {
        prev = position;
        // Apply force based on Newton's second law, assuming mass = 1

        if (Time.deltaTime > 0.05f){
            Vector2 newVelocity = velocity + force * 0.05f * TIMESTEP;
            position += 0.5f * (velocity + newVelocity) * 0.05f * TIMESTEP; 
            velocity = newVelocity;
        }
        else {
            Vector2 newVelocity = velocity + force * Time.deltaTime * TIMESTEP;
            position += 0.5f * (velocity + newVelocity) * Time.deltaTime * TIMESTEP; 
            velocity = newVelocity;
        }

        if (velocity.magnitude > MaxVel){
            velocity = velocity.normalized * MaxVel;
        }

        force = Vector2.zero;
        if (GRAVITY_ENABLED){
            force.y = GRAVITY;
        }
        

        if (BOUNDING_BOX_ENABLED){
            ResolveBoundingBoxCollisions();
        }

        // Update position
        transform.position = position;

        density = 0f;
        density_near = 0f;
        pressure = 0f;
        pressure_near = 0f;

        neighbors = new ParticleArray();

        if (position.y < BOUNDING_Y_BOTTOM - 1){
            Destroy(gameObject);
            destroyed = true;
        }
    }

    void ResolveBoundingBoxCollisions(){
        if(position.x > BOUNDING_X_RIGHT){
            position.x = BOUNDING_X_RIGHT;
            velocity.x *= -1 * DAMPING_FACTOR;
        }
        if(position.x < BOUNDING_X_LEFT){
            position.x = BOUNDING_X_LEFT;
            velocity.x *= -1 * DAMPING_FACTOR;
        }
        if(position.y > BOUNDING_Y_TOP){
            position.y = BOUNDING_Y_TOP;
            velocity.y *= -1 * DAMPING_FACTOR;
        }
        if(position.y < BOUNDING_Y_BOTTOM){
            position.y = BOUNDING_Y_BOTTOM;
            velocity.y *= -1 * DAMPING_FACTOR;
        }
    }

    void OnCollisionStay2D(Collision2D collision){
        Vector2 normal = collision.contacts[0].normal;
        float vel_normal = Vector2.Dot(velocity, normal);
        if (vel_normal > 0){
            return;
        }
        Vector2 vel_tangent = velocity - normal * vel_normal;
        velocity = vel_tangent - normal * vel_normal * DAMPING_FACTOR;
        position = collision.contacts[0].point + normal * 0.2f;
    }
}

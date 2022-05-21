using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct VerletParticle
{
    public Vector3 curr, prev;
}

public struct VerletAnchorConstraint
{
    public int particle;
    public Vector3 position;
}

public struct VerletDistanceConstraint
{
    public int partA, partB;
    public float distance;
}

public class VerletSim
{
    public VerletParticle[] particles = null;
    public Vector3[] forces = null;
    public List<VerletAnchorConstraint> staticConst = new List<VerletAnchorConstraint>();
    public List<VerletDistanceConstraint> distConst = new List<VerletDistanceConstraint>();

    private int numOfParticles;
    public float gravity = -9.8f;
    private float timeStep = 1/60.0f;
    private int iterations = 10;

    //Sphere Collision
    public Vector3 sphere_pos;
    public float sphere_rad;
    
    public void Initialise(int inNumOfParticles)
    {
        numOfParticles = inNumOfParticles;
        particles = new VerletParticle[numOfParticles];
        forces = new Vector3[numOfParticles];
        staticConst.Clear();
        distConst.Clear();
        ClearForces();
        for (int i = 0; i < numOfParticles; ++i)
        {
            particles[i].curr = Vector3.zero;
            particles[i].prev = Vector3.zero;
        }
    }

    public void FixedUpdate()
    {
        SumForces();
        VerletIntergateParticles();
        SolveConstraints();
        ClearForces();

        //Comment or uncomment to swap between moving and static collision.
        //MovingCollisionSphere();
    }

    public void CreateCollisionSphere()
    {
        //Create the sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        //Set sphere location and size
        sphere.transform.position = new Vector3(10,7,0);
        sphere.transform.localScale = new Vector3(3,3,3);
 
        sphere_pos = sphere.transform.position;
        sphere_rad = 1.5f;
    }

    public void MovingCollisionSphere()
    {
        //Create the sphere
        GameObject sphere = GameObject.Find("SphereMoving");;

        //Set sphere size
        sphere.transform.localScale = new Vector3(3,3,3);
 
        sphere_pos = sphere.transform.position;
        sphere_rad = 1.5f;
    }

    private void VerletIntergateParticles()
    {
        float timeStepPow2 = timeStep*timeStep;
        for (int i = 0; i < numOfParticles; ++i)
        {
            Vector3 x = particles[i].curr;
            Vector3 px = particles[i].prev; 
            Vector3 a = forces[i];
            x += x-px+a*timeStepPow2;
            particles[i].prev = particles[i].curr;
            particles[i].curr = x;
        }
    }

    private void SumForces()
    {
        // Just adds gravity to all particles.
        // External things can add forces to this list before calling FixedUpdate
        Vector3 vGravity = new Vector3(0, gravity, 0);
        for (int i = 0; i < numOfParticles; ++i)
        {
            forces[i] += vGravity;
        }
    }

    private void ClearForces()
    {
        for (int i = 0; i < numOfParticles; ++i)
        {
            forces[i] = Vector3.zero;
        }
    }

    private void SolveConstraints()
    {
        for (int iter = 0; iter < iterations; ++iter)
        {
            foreach(var c in staticConst)
            {
                // any particle with a anchor constrant cannot move, so lock it's position to the anchor's position 
                particles[c.particle].curr = c.position;
            }
       
            foreach(var b in distConst)
            {
                Vector3 x1 = particles[b.partA].curr;
                Vector3 x2 = particles[b.partB].curr;
                Vector3 delta = x2-x1;

                float deltalength = delta.magnitude;
                float diff=(deltalength-b.distance)/deltalength;

                particles[b.partA].curr += (delta*0.5f*diff);
                particles[b.partB].curr -= (delta*0.5f*diff);
            }

            //Check sphere collision
            for (int i = 0; i < numOfParticles; ++i)
            {
                if((sphere_pos - particles[i].curr).magnitude < sphere_rad)
                {
                particles[i].curr = sphere_pos + (particles[i].curr - sphere_pos).normalized * sphere_rad;
                }
            }
        }    
    }   
}

[RequireComponent(typeof(MeshFilter))]
public class VerletCloth : MonoBehaviour {

    //Set number of rows and columns for cloth.
    [SerializeField, Range(1, 256)] public int rows = 3, columns = 3;
    
    //Gravity
    public float gravity = -9.8f;

    //Vertices & Mesh
    Vector3[] vertices;
    Vector2[] UV;
    int[] triangles;
    Mesh mesh;
    //float z = 0;
    VerletSim vSim = new VerletSim();

   

    protected void Start()
    {
        //Uncomment or comment to swap between static and moving collision.
        vSim.CreateCollisionSphere();

        vSim.Initialise(rows*columns);
        // Set initial positions for particles
        int particleIndex = 0;
        for (int y = 0; y < rows; ++y)
        {
            for (int x = 0; x < columns; ++x) 
            {
                vSim.particles[particleIndex].curr = vSim.particles[particleIndex].prev = new Vector3(x, y, y);
                particleIndex++;
            }
        }
        // Add top constrants
        particleIndex = columns * (rows-1); //pick the top row of particles and fix it in place
        for (int x = 0; x < columns; ++x)
        {
            VerletAnchorConstraint a;
            a.particle = particleIndex;
            a.position = new Vector3(x, rows-1, 0);
            vSim.staticConst.Add(a);
            particleIndex++;
        }

        //add distance constraints between each particle in the cloth.
            for (int x = 0; x < rows; ++x)
            {    
                for (int i = 0; i < columns; ++i)
                { 
                //Create Vertical constraint (UP)
                    //Remove '&& False' to double most constraints and create a more rigid cloth.
                    //Add '&& False' to only create single constraints and create a uniform cloth.
                    if(x < rows-1 /*&& false*/)//stop constraints going beyond top row.
                    {
                        VerletDistanceConstraint b;
                        b.distance = 1.0f;
                        b.partA = GetParticleIndex(x, i); 
                        b.partB = GetParticleIndex(x+1, i);
                        vSim.distConst.Add(b);
                    }
                //Create Vertical constraint (DOWN)
                    if(x > 0)//Stop constraints going below first row.
                    {
                        VerletDistanceConstraint b;
                        b.distance = 1.0f;
                        b.partA = GetParticleIndex(x, i);
                        b.partB = GetParticleIndex(x-1, i);
                        vSim.distConst.Add(b);
                    }
                //Create horizontal constraint (LEFT)
                    if(i > 0)//Stop constraints going behind first column.
                    {
                        VerletDistanceConstraint b;
                        b.distance = 1.0f;
                        b.partA = GetParticleIndex(x, i); 
                        b.partB = GetParticleIndex(x, i-1);
                        vSim.distConst.Add(b);
                    }
                //Create horizontal constraint (RIGHT)
                    //Remove '&& False' to double most constraints and create a more rigid cloth.
                    //Add '&& False' to only create single constraints and create a uniform cloth.
                    if(i < columns - 1 /*&& false*/)//Stop constraints going beyond last column.
                    {
                        VerletDistanceConstraint b;
                        b.distance = 1.0f;
                        b.partA = GetParticleIndex(x, i); 
                        b.partB = GetParticleIndex(x, i+1);  
                        vSim.distConst.Add(b);
                    }  
                   
                }
            }

        GenerateMesh();
        UpdateMesh();
    }

    public int GetParticleIndex(int row, int column) 
    { 
        Debug.Assert(row >= 0 && row < rows);
        Debug.Assert(column >= 0 && column < columns);
        
        return (row*columns)+(column); 
    }

    protected void Update()
    {
        UpdateVertices();
    }

    void FixedUpdate()
    {
        vSim.FixedUpdate();
    }

    void UpdateVertices()
    {
        Vector3[] vertices = new Vector3[rows*columns];
        int data_idx = 0;
        int local_r = rows;
        int local_c = columns;

        for (int y = 0; y < local_r; ++y)
        {
            for (int x = 0; x < local_c; ++x) 
            {
                vertices[data_idx] = vSim.particles[data_idx].curr;
                data_idx++;
            }
        }

        mesh.vertices = vertices;
    }

    void GenerateMesh()
    {
        if (mesh != null) return;

        mesh = new Mesh();
        mesh = GetComponent<MeshFilter>().mesh = mesh;
        mesh.name = "Verlet Cloth";
        mesh.vertices = new Vector3[rows*columns];
        mesh.triangles = new int[rows*columns*6*2];
    }

    void OnDrawGizmos ()
    {
        if (vSim.particles == null) return;
        Gizmos.color = Color.white;
        foreach (var p in vSim.particles) Gizmos.DrawSphere(p.curr, 0.1f);
    }

    void UpdateMesh()
    {
        GenerateMesh();
        UpdateVertices();
    
        int local_r = rows;
        int local_c = columns;
        int data_idx = 0;
        int tri_idx = 0;
        Vector3 vec = Vector3.zero;
        int[] triangles = new int[rows*columns*6*2];
        for (int y = 0; y < local_r-1; ++y)
        {
            for (int x = 0; x < local_c-1; ++x)
            {

                //Front triangles
                triangles[tri_idx++] = data_idx+0;
                triangles[tri_idx++] = data_idx+local_c;
                triangles[tri_idx++] = data_idx+1;

                triangles[tri_idx++] = data_idx+1;
                triangles[tri_idx++] = data_idx+local_c;
                triangles[tri_idx++] = data_idx+local_c+1;

                //Back Triangles
                triangles[tri_idx++] = data_idx+1;
                triangles[tri_idx++] = data_idx+local_c;
                triangles[tri_idx++] = data_idx+0;
                
                triangles[tri_idx++] = data_idx+local_c+1;
                triangles[tri_idx++] = data_idx+local_c;
                triangles[tri_idx++] = data_idx+1;
                
                data_idx++;
                //Debug.Log($"Generate Mesh {data_idx}");
            }
            data_idx++;
        }
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}

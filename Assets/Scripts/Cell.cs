using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

//using UnityEngine.Random as Random;



public class Cell : MonoBehaviour
{
    //Memory
    public double saturation = 0.0;
    public float viewDistance = 0f;
    public float maximumRange = 500;
    public Vector3 currentDirectionality = new Vector3(1f,1f,1f);
    

    protected float forceMultiplier = 20f;

    public Rigidbody rb;


    //Methods
    virtual public void Start() 
    {
        rb = GetComponent<Rigidbody>();
    }

    public Vector3 RandomWeightedDirection(Vector3 pos)
    {
        //outputs a random force direction based on position, keeps it in range

        Vector3 randDirection = Random.insideUnitSphere;

        float strength = 1f;

        if (Vector3.Magnitude(pos) > maximumRange && Vector3.Dot(randDirection, -Vector3.Normalize(pos)) < 0)
        {
            strength = Vector3.Dot(randDirection, -Vector3.Normalize(pos)) + 0.5f;
            strength = Mathf.Max(0f, strength);
        }

        currentDirectionality = Vector3.Normalize((currentDirectionality * 10f + (randDirection * strength * viewDistance)) / 11);


        return currentDirectionality;
    }

    public void JumpTo(Vector3 pos)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        rb.AddForce((pos - rb.position) * rb.mass * forceMultiplier * viewDistance);
    }

    public void Jump() 
    {
        if(rb == null) rb = GetComponent<Rigidbody>();

        //random jump
        rb.AddForce((RandomWeightedDirection(rb.position)) * rb.mass * forceMultiplier * viewDistance);
    }

    public void Jump(Vector3 directional) 
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        //jump towards..
        rb.AddForce(Vector3.Normalize(directional) * rb.mass * forceMultiplier * viewDistance);
    }

    virtual public void Think(List<Colony.Directional> view){}


}













public class Food : Cell
{
    override public void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    override public void Think(List<Colony.Directional> view)
    {
        Debug.Log("Food should not be thinking");
    }
}















public class BasicGuy : Cell
{
    private Vector3 forceDirectional = new Vector3();
    public List<Vector3> foodDirections = new List<Vector3>();
    private List<Cell> friends = new List<Cell>();

    override public void Start()
    {
        rb = GetComponent<Rigidbody>();

        saturation = 1.0;
        viewDistance = Random.Range(25,50);
    }

    void OnCollisionEnter(Collision collision)
    {
        Cell cellType = collision.collider.GetComponent<Cell>();

        //whitelist edible types
        if (cellType is Food)
        {
            collision.collider.gameObject.SetActive(false);
        }
    }

    override public void Think(List<Colony.Directional> view)
    {
        if (view.Count > 0)
        {
            //create a list with all the food in view
            foodDirections.Clear();
            friends.Clear();

            for (int i = 0; i < view.Count; i++)
            {
                if( view[i].cellInformation is Food) foodDirections.Add(view[i].directional);
                if (view[i].cellInformation is BasicGuy && Vector3.Magnitude(view[i].directional) < viewDistance * 0.5f)
                    friends.Add(view[i].cellInformation);
            }

            foodDirections.OrderByDescending(v => Mathf.Abs(Vector3.Magnitude(v))).ToList();
            friends.OrderByDescending(v => Mathf.Abs(Vector3.Magnitude(v.rb.position - rb.position))).ToList();

            if (foodDirections.Count > 0)
            {
                Jump(foodDirections[0]);
            }
            else if (friends.Count > 0)
            {
                Jump(Vector3.Normalize(friends[0].rb.velocity));
            }
            else
            {
                Jump();
            }
        }
        else
        {
            Jump();
        }
    }
}












public class SpecialGuy : Cell 
{
    override public void Start() 
    {
        rb = GetComponent<Rigidbody>();

        saturation = 1.0;
        viewDistance = Random.Range(50, 75);
    }

    override public void Think(List<Colony.Directional> view) 
    {
        Debug.Log("SpecialGuy");
    }

}



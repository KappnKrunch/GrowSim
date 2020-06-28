using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spawnTest : MonoBehaviour
{

    public List<GameObject> props = new List<GameObject>();
    public Matrix4x4 trasnformationMatrix = Matrix4x4.identity;
    public int res = 1000;
    public int batchSize = 10;
    public int range = 500;
    public float curve = 1f;

    private int index = 0;

    void Spawn()
    {
        for (int i = 0; i < res; i++)
        {
            GameObject newSpawn = GameObject.CreatePrimitive(PrimitiveType.Cube);

            Vector3 spawnPoint = Random.insideUnitSphere * 100f;

            spawnPoint = trasnformationMatrix.MultiplyPoint3x4(spawnPoint);

            newSpawn.transform.position = spawnPoint;

            props.Add(newSpawn);
        }
    }

    IEnumerator Move()
    {
        while (true)
        {
            for (int i = 0; i < batchSize; i++)
            {
                Vector3 newPos = props[index].transform.position + Random.insideUnitSphere * 100f;

                Vector3 newDirectional = newPos - props[index].transform.position;

                float strength = 1f;

                float distStrength = Mathf.Max(0f, range - Vector3.Magnitude(newPos) + 1f) / range;

                //if ( (Vector3.Magnitude(newPos) + 1f) / range > 1f) strength = 0f;
                
                if(Vector3.Dot(Vector3.Normalize(newDirectional), -Vector3.Normalize(props[index].transform.position)) < 0f && distStrength == 0f)
                    strength *= Vector3.Dot(Vector3.Normalize(newDirectional), -Vector3.Normalize(props[index].transform.position)) + 0.5f;

                strength = Mathf.Max(0f, strength);
                

                props[index].transform.position = props[index].transform.position + (newDirectional * strength);

                props[index].transform.localScale = Vector3.one * Mathf.Pow(1f - distStrength, curve) * 10f;

                index = (index + 1) % props.Count;
            }

            yield return new WaitForSeconds(0.01f);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Spawn();

        StartCoroutine(Move());
    }
}

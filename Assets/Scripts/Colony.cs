using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class Colony : MonoBehaviour
{
    private List<Cell> population = new List<Cell>();

    public int foodSpawnAmount = 30;
    public int initialSpawnAmount = 250;

    public Material basicCellMaterial;

    public struct Directional
    {
        public Vector3 directional;
        public Cell cellInformation;
    }
    

    IEnumerator FrameManager(int parallelThreadCount)
    {
        PlaceBasicCell();

        List<Thread> parallelThreads = new List<Thread>();

        int lastThreadLength = population.Count % parallelThreadCount;
        if (lastThreadLength > 0) parallelThreadCount++;

        while (true)
        {
            FoodPlacement(); //add new food

            for (int i = 0; i < parallelThreadCount; i++)
            {
                //Thread newThread = new Thread(Frame(i, parallelThreadCount));
                //parallelThreads.Add();
            }

            yield return new WaitForSeconds(10f);

            StopCoroutine("Frame");

            RemoveEatenFood();

            yield return new WaitForSeconds(0.1f);

        }
    }

    void Frame(int threadIndex, int parallelThreads)
    {
        Cell thisCell;
        Vector3 thisPos;
        float viewDist;

        Cell thatCell;
        Vector3 thatPos;
        Vector3 theDifference;

        List<Directional> directionals = new List<Directional>();

        int lastThreadLength;
        int iterationsPerThread;

        int startIndex;
        int endIndex;

        while (true)
        {
            lastThreadLength = population.Count % parallelThreads;
            iterationsPerThread = (population.Count - lastThreadLength) / parallelThreads;

            if (lastThreadLength > 0) parallelThreads++;

            startIndex = threadIndex * iterationsPerThread;
            endIndex = (threadIndex + 1) * iterationsPerThread;

            if (endIndex > population.Count) endIndex = population.Count;

            //i * j so every element can interact with every element
            for (int i = startIndex; i < endIndex; i++)
            {
                thisCell = population[i];

                if (thisCell is Food) continue; //Food doesn't need to think
                else Thread.Sleep(1);

                thisPos = thisCell.transform.position;
                viewDist = thisCell.viewDistance;

                directionals.Clear();
                Directional directionalHolder;

                //thisCell looks at every other cell
                for (int j = 0; j < population.Count; j++)
                {
                    if (i == j) continue; //shouldn't check itself
                    if (!population[j].gameObject.activeSelf) continue;

                    thatCell = population[j];
                    thatPos = thatCell.transform.position;

                    theDifference = thatPos - thisPos;

                    if (Vector3.Magnitude(theDifference) > viewDist) continue;

                    directionalHolder.directional = theDifference;
                    directionalHolder.cellInformation = thatCell;

                    directionals.Add(directionalHolder);
                }

                if (thisCell is BasicGuy) ((BasicGuy) thisCell).Think(directionals);
                if (thisCell is SpecialGuy) ((SpecialGuy) thisCell).Think(directionals);
            }
        }
    }

    public Vector3 FourierPositionalAtTime(List<float> fourierCoefficients, float time)
    {
        Vector3 positional = Vector3.zero;

        for (float i = 0f; i < fourierCoefficients.Count; i++)
        {
            positional.x += fourierCoefficients[(int)i] * Mathf.Sin((i+1f) * 2f * Mathf.PI * time);
            positional.z += fourierCoefficients[(int)i] * Mathf.Sin((i+1f) * Mathf.PI * time);
            positional.y += fourierCoefficients[(int)i] * Mathf.Cos((i+1f) * 2f * Mathf.PI * time);
        }

        return positional * 500;
    }

    private void RemoveEatenFood() 
    {
        List<Cell> culledPopulation = new List<Cell>();

        for (int i = 0; i < population.Count; i++) 
        {
            if (population[i].gameObject.activeSelf) 
            {
                culledPopulation.Add(population[i]);
            }
            else 
            {
                Destroy(population[i].gameObject);
            }
        }

        population.Clear();

        for (int i = 0; i < culledPopulation.Count; i++) 
        {
            population.Add(culledPopulation[i]);
        }

        culledPopulation.Clear();
    }

    void FoodPlacement()
    {
        List<float> foodFunction = new List<float>();
        foodFunction.Add(0f);
        foodFunction.Add(1f / 8);
        foodFunction.Add(0f);
        foodFunction.Add(1f / 11);
        foodFunction.Add(0f);
        foodFunction.Add(1f / 5);

        for (int i = 0; i < foodSpawnAmount; i++)
        {
            GameObject spawned = GameObject.CreatePrimitive(PrimitiveType.Cube);

            spawned.transform.position = FourierPositionalAtTime(foodFunction, Random.value) + Random.insideUnitSphere*10f;
            spawned.transform.rotation = Random.rotation;

            Food newCell = spawned.AddComponent<Food>();
            spawned.transform.localScale = Vector3.one;// * 0.25f;

            population.Add(newCell);

            //Rigidbody rb = spawned.AddComponent<Rigidbody>();
            //rb.drag = 1.25f;
            //rb.angularDrag = 0.5f;
            //rb.useGravity = false;
        }
    }

    void PlaceBasicCell()
    {
        Vector3 currentColor = Vector3.zero;

        for (int i = 0; i < initialSpawnAmount; i++) 
        {
            GameObject newCellObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            newCellObj.transform.position = Random.insideUnitSphere * Random.Range(0f, 100f);

            Cell newCell = newCellObj.AddComponent<BasicGuy>();
            newCellObj.transform.localScale = Vector3.Scale(newCellObj.transform.localScale * 2f, Random.insideUnitSphere);
            population.Add(newCell);

            Rigidbody rb = newCellObj.AddComponent<Rigidbody>();
            rb.drag = 1.25f;
            rb.angularDrag = 0.5f;
            rb.useGravity = false;

            currentColor = (currentColor * 5f + (new Vector3(Random.value, Random.value, Random.value))) / 6;

            Vector4 newCellColor = currentColor;
            newCellColor.w = 1f;

            newCellObj.GetComponent<MeshRenderer>().sharedMaterial = basicCellMaterial;

            newCell.GetComponent<MeshRenderer>().material.color = newCellColor;
        }
    }

    void Start()
    {
        StartCoroutine(FrameManager(50));
    }

}

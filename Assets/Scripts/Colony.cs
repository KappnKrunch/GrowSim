using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal.VersionControl;
using UnityEngine;
using Random = UnityEngine.Random;

public class Colony : MonoBehaviour
{
    public List<Cell> population = new List<Cell>();

    public int foodSpawnAmount = 30;
    public int initialSpawnAmount = 250;

    public Material basicCellMaterial;

    //debug compute shader output
    public Material computeShaderOutputMaterial;
    public ComputeShader computeShader;

    public struct Directional
    {
        public Vector3 directional;
        public Cell cellInformation;
    }
    

    IEnumerator FrameManager()
    {
        PlaceBasicCell();

        int simulationFrames = 2000;

        while (true)
        {
            FoodPlacement(); //add new food

            StartCoroutine(Frame());

            RemoveEatenFood();

            yield return new WaitForSeconds(1f);

        }
    }

    Vector4[] GenerateComputedDistances(int bufferSize)
    {
        //create the buffers
        //fill position buffers
        Vector4[] positionsArray = new Vector4[bufferSize];
        Vector4 newVector;
        for (int i = 0; i < bufferSize; ++i) 
        {
            if (i < population.Count) 
            {
                //valid cell operations
                newVector = population[i].transform.position;
                newVector.w = 0f;

                if (population[i] is BasicGuy) newVector.w = 1f;
                if (population[i] is SpecialGuy) newVector.w = 2f;

                //positions. = population[i].transform.position;
                positionsArray[i] = newVector;
            }
            else 
            {
                //empty space to make the buffer fit evenly
                newVector = Vector4.zero;
                newVector.w = -1f;
                positionsArray[i] = newVector;
            }
        }

        int float4Stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4));

        ComputeBuffer positionsBuffer = new ComputeBuffer(bufferSize, float4Stride, ComputeBufferType.Default);
        positionsBuffer.SetData(positionsArray); //fill the buffer with data from the array

        //Create output directional buffer
        ComputeBuffer directionalsBuffer = new ComputeBuffer(bufferSize, float4Stride, ComputeBufferType.Default);

        //attach buffers to compuet shader
        int kernelIndex = computeShader.FindKernel("CSMain");

        computeShader.SetBuffer(kernelIndex, "Positions", positionsBuffer);

        

        computeShader.SetInt("resolution", bufferSize);

        //debug render texture output
        /*
        RenderTexture renderTetxure;

        renderTetxure = new RenderTexture(bufferSize, bufferSize, 1);
        renderTetxure.enableRandomWrite = true;
        renderTetxure.useMipMap = false;
        renderTetxure.Create();

        computeShaderOutputMaterial.SetTexture("_MainTex", renderTetxure);

        computeShader.SetTexture(kernelIndex,"ResultTexture",renderTetxure);

        */
        //run the compute shader

        computeShader.Dispatch(kernelIndex, 16, 16, 1);

        computeShader.SetBuffer(kernelIndex, "Result", directionalsBuffer);


        //get the processed data


        Vector4[] finalDirectionals = new Vector4[bufferSize*bufferSize];
        //directionalsBuffer.GetData(finalDirectionals);
        

        return finalDirectionals;
    }

    IEnumerator Frame()
    {
        //first determine the size of the buffer, have it be a least square
        int bufferSize = 2;
        while (Mathf.Pow(bufferSize, 2) < population.Count) bufferSize++;
        bufferSize = (int)Mathf.Pow(bufferSize, 2);

        Vector4[] generatedDistances = GenerateComputedDistances(bufferSize);

        List<Directional> currentView = new List<Directional>();

        for (int i = 0; i < population.Count; i++)
        {
            if (generatedDistances[i].w == -1f) continue; //food doesn't think, see shader code for magic number
            else yield return new WaitForEndOfFrame();

            //Debug.Log(generatedDistances[i]);

            currentView.Clear();

            for (int j = 0; j < population.Count; j++)
            {
                if (i == j) continue;
                Vector4 directionToAdjacentCell = generatedDistances[i + j*bufferSize];

                Directional currDirectional = new Directional();
                currDirectional.cellInformation = population[j];
                currDirectional.directional = directionToAdjacentCell;

                currentView.Add(currDirectional);
            }

            Cell currentCell = population[i];

            //Debug.Log(generatedDistances[i]);
            if (currentCell is BasicGuy) ((BasicGuy)currentCell).Think(currentView);
            if (currentCell is SpecialGuy) ((SpecialGuy)currentCell).Think(currentView);

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
        StartCoroutine(FrameManager());
    }

}

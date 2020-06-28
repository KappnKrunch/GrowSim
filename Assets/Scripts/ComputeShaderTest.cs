using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ComputeShaderTest : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material mat;

    void OnEnable()
    {
        RenderTexture renderTetxure;

        renderTetxure = new RenderTexture(256, 256, 1);
        renderTetxure.enableRandomWrite = true;
        renderTetxure.useMipMap = true;
        renderTetxure.Create();
        renderTetxure.enableRandomWrite = true;
        //renderTetxure.useDynamicScale = true;

        mat.SetTexture("_MainTex", renderTetxure);
        //mat.

        var kernelIndex = computeShader.FindKernel("CSMain");

        computeShader.SetTexture(kernelIndex, "Result", renderTetxure);

        computeShader.Dispatch(kernelIndex, 32, 32, 1);
    }
}

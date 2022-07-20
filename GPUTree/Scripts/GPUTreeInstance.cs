using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class GPUTreeInstance
{
    public class TreeData
    {
        public Mesh mesh;
        public Material[] materials;

        public MaterialPropertyBlock mpb;

        public void Dispose()
        {
            mesh = null;
            mpb = null;
            materials = null;
        }
    }

    static int cameraPositionId = Shader.PropertyToID("cameraPosition");
    static int cameraDirectionId = Shader.PropertyToID("cameraDirection");
    static int cameraHalfFovId = Shader.PropertyToID("cameraHalfFov");
    static int matrixVPId = Shader.PropertyToID("matrix_VP");
    static int bufferWithArgsId = Shader.PropertyToID("bufferWithArgs");

    public ComputeShader shader;

    List<TreeData> datas;
    ComputeBuffer bufferWithArgs;
    uint[] args;
    int ShaderId;

    Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

    int count;

    internal GPUTreeInstance(TreeInfo info, ComputeShader cs)
    {
        if (info.lods.Count == 0)
            return;

        shader = ComputeShader.Instantiate<ComputeShader>(cs);
        datas = new List<TreeData>();
        count = info.positions.Length;


        int w = (count >= 1024) ? 1024 : (count % 1024);
        int h = count / 1024;
        h = math.select(w, h + 1, count % 1024 > 0);

        RenderTexture rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat);
        rt.dimension = TextureDimension.Tex2DArray;
        //rt.depth = info.lods.Count;
        rt.volumeDepth = info.lods.Count;
        rt.enableRandomWrite = true;
        rt.Create();

        args = new uint[info.lods.Count * 5];
        for (int i = 0; i < info.lods.Count; i++)
        {
            TreeLOD lod = info.lods[i];
            TreeData tree = new TreeData();
            tree.mesh = lod.mesh;
            tree.materials = lod.materials;

            int offset = i * 5;
            args[offset + 0] = (uint)tree.mesh.GetIndexCount(0);
            args[offset + 1] = 0;
            args[offset + 2] = (uint)tree.mesh.GetIndexStart(0);
            args[offset + 3] = (uint)tree.mesh.GetBaseVertex(0);
            args[offset + 4] = 0;

            tree.mpb = new MaterialPropertyBlock();
            tree.mpb.SetTexture("_visibleTexture", rt);
            tree.mpb.SetInt("lod", i);

            datas.Add(tree);
        }

        ShaderId = shader.FindKernel("GPUTreeCulling");

        ComputeBuffer positionBuffer = new ComputeBuffer(count, 4 * 4);
        positionBuffer.SetData(info.positions);
        shader.SetBuffer(ShaderId, "positionBuffer", positionBuffer);

        shader.SetTexture(ShaderId, "visibleTexture", rt);
        shader.SetInt("textureWidth", w);
        shader.SetFloats("LODDistances", new float[4] { 25f, 50f, 100f, 800f });
        shader.SetInt("count", count);
        shader.SetInt("lodCount", datas.Count);

        bufferWithArgs = new ComputeBuffer(5 * datas.Count, sizeof(uint), ComputeBufferType.IndirectArguments);
        bufferWithArgs.SetData(args);
        shader.SetBuffer(ShaderId, "bufferWithArgs", bufferWithArgs);
    }

    public void Run()
    {

        shader.SetVector(cameraPositionId, Camera.main.transform.position);
        shader.SetVector(cameraDirectionId, Camera.main.transform.forward);
        shader.SetFloat(cameraHalfFovId, Camera.main.fieldOfView / 2);
        var m = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false) * Camera.main.worldToCameraMatrix;
        shader.SetMatrix(matrixVPId, m);

        for (int i = 0; i < datas.Count; i++)
        {
            args[1 + i * 5] = 0;
        }
        bufferWithArgs.SetData(args);
        shader.SetBuffer(ShaderId, bufferWithArgsId, bufferWithArgs);
        shader.Dispatch(ShaderId, count / 4, 1, 1);

        for (int i = 0; i < datas.Count; i++)
        {
            TreeData data = datas[i];
            Graphics.DrawMeshInstancedIndirect(data.mesh, 0, data.materials[0], bounds, bufferWithArgs, i * 5 * 4, data.mpb, ShadowCastingMode.On, false);
        }
    }
    public void Destory()
    {
        if (datas != null)
        {
            for (int i = 0; i < datas.Count; i++)
            {
                datas[i].Dispose();
            }
            datas.Clear();
        }
        if (bufferWithArgs != null)
        {
            bufferWithArgs.Dispose();
            bufferWithArgs = null;
        }

        shader = null;
    }
}


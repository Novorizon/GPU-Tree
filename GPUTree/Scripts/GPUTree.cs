using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class TreeLOD
{
    public Mesh mesh;
    public Material[] materials;
}

[Serializable]
public class TreeInfo : ScriptableObject
{
    public List<TreeLOD> lods;
    public float4[] positions;
}



public class GPUTree : MonoBehaviour
{
    public ScriptableObject[] trees;
    List<GPUTreeInstance> instances;

    public ComputeShader shader;

    bool enable = false;

    void OnEnable()
    {
        if (trees != null && trees.Length > 0)
        {
            Terrain terrain = GetComponent<Terrain>();
            if (terrain != null)
                terrain.drawTreesAndFoliage = false;

            instances = new List<GPUTreeInstance>();
            for (int i = 0; i < trees.Length; i++)
            {
                TreeInfo info = trees[i] as TreeInfo;

                GPUTreeInstance instance = new GPUTreeInstance(info, shader);
                instances.Add(instance);
            }
            enable = true;
        }
    }

    void Update()
    {
        if (!enable)
            return;

        for (int i = 0; i < instances.Count; i++)
        {
            instances[i].Run();
        }
    }

    private void OnDestroy()
    {
        if (instances != null)
        {
            for (int i = 0; i < instances.Count; i++)
            {
                instances[i].Destory();
            }
            instances.Clear();
        }
    }
}

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GPUTree))]
public class GPUTreeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GPUTree terrainTree = (GPUTree)target;
        if (GUILayout.Button("Generate"))
        {
            GenerateTreeData(terrainTree);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void GenerateTreeData(GPUTree terrainTree)
    {
        Terrain terrain = Selection.activeGameObject.GetComponent<Terrain>();
        if (terrain)
        {
            int length = terrain.terrainData.treePrototypes.Length;
            terrainTree.trees = new ScriptableObject[length];
            List<float4>[] data = new List<float4>[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = new List<float4>();
            }

            float3 size = terrain.terrainData.size;
            float3 Position = terrain.GetPosition();
            TreeInstance[] treeInstances = terrain.terrainData.treeInstances;
            for (int i = 0; i < treeInstances.Length; i++)
            {
                TreeInstance treeInstance = treeInstances[i];

                float3 position = treeInstance.position * size + Position;

                int heightScale = (int)math.clamp(treeInstance.heightScale * 100, 0, 1023);
                int widthScale = (int)math.clamp(treeInstance.widthScale * 100, 0, 1023);
                int rotation = (int)math.clamp(treeInstance.rotation / math.PI * 180, 0, 359) / 45;
                int extension = (rotation << 20) | (heightScale << 10) | widthScale;
                data[treeInstance.prototypeIndex].Add(new float4(position, extension));
            }

            for (int i = 0; i < length; i++)
            {
                TreeInfo info = CreateInstance<TreeInfo>();
                TreePrototype prototype = terrain.terrainData.treePrototypes[i];

                GameObject gameObject = prototype.prefab;
                LODGroup LODGroup = gameObject.GetComponent<LODGroup>();
                if (LODGroup != null)
                {
                    LOD[] lods = LODGroup.GetLODs();
                    info.lods = new List<TreeLOD>();

                    for (int j = 0; j < lods.Length; j++)
                    {
                        LOD lod = lods[j];

                        if (lod.renderers.Length == 0)
                            continue;

                        TreeLOD treeLOD = new TreeLOD();
                        GameObject lodGO = lod.renderers[0].gameObject;
                        MeshFilter meshFilter = lodGO.GetComponent<MeshFilter>();
                        treeLOD.mesh = meshFilter.sharedMesh;

                        MeshRenderer meshRender = lodGO.GetComponent<MeshRenderer>();
                        List<Material> materials = new List<Material>();
                        meshRender.GetSharedMaterials(materials);
                        treeLOD.materials = materials.ToArray();

                        info.lods.Add(treeLOD);
                    }
                }
                else
                {
                    info.lods = new List<TreeLOD> { };
                    TreeLOD treeLOD = new TreeLOD();
                    MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
                    treeLOD.mesh = meshFilter.sharedMesh;

                    MeshRenderer meshRender = gameObject.GetComponent<MeshRenderer>();
                    List<Material> materials = new List<Material>();
                    meshRender.GetSharedMaterials(materials);
                    treeLOD.materials = materials.ToArray();

                    info.lods.Add(treeLOD);
                }


                info.positions = data[i].ToArray();
                terrainTree.trees[i] = info;
                string path = "Assets/Settings/";
                string file = path + gameObject.name + ".asset";
                File.Delete(file);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                AssetDatabase.CreateAsset(info, file);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

    }
}
#endif
// Created by 月北(ybwork-cn) https://github.com/ybwork-cn/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ybwork.ECSVertexAnimation.Editor
{
    /// <summary>
    /// 保存需要烘焙的动画的相关数据
    /// </summary>
    internal struct AnimData
    {
        public readonly int MapWidth;
        public readonly int[] MapWidths;
        public readonly List<AnimationState> AnimationClips;
        public readonly string Name;

        private readonly Animation Animation;
        private readonly SkinnedMeshRenderer[] Skins;

        public AnimData(VertexAnimationMapCreator baker)
        {
            Animation anim = baker.GetComponentInChildren<Animation>();
            Skins = baker.GetComponentsInChildren<SkinnedMeshRenderer>();

            MapWidths = Skins.Select(render => render.sharedMesh.vertexCount).ToArray();
            MapWidth = Mathf.NextPowerOfTwo(MapWidths.Sum());
            AnimationClips = new List<AnimationState>(anim.Cast<AnimationState>());
            Animation = anim;
            Name = baker.name;
        }

        #region METHODS

        public void AnimationPlay(string animName)
        {
            Animation.Play(animName);
        }

        public void SampleAnimAndBakeMesh(ref Mesh[] meshes)
        {
            SampleAnim();
            BakeMesh(ref meshes);
        }

        private void SampleAnim()
        {
            if (Animation == null)
            {
                Debug.LogError("animation is null!!");
                return;
            }

            Animation.Sample();
        }

        private void BakeMesh(ref Mesh[] meshes)
        {
            meshes = new Mesh[Skins.Length];
            for (int i = 0; i < Skins.Length; i++)
            {
                Mesh mesh = new Mesh();
                Skins[i].BakeMesh(mesh);
                meshes[i] = mesh;
            }
        }

        #endregion
    }

    /// <summary>
    /// 烘焙后的数据
    /// </summary>
    internal struct BakedData
    {
        public readonly string Name;
        public readonly float AnimLen;
        public readonly Texture2D AnimMap;
        public readonly Texture2D AnimMapNormal;

        public BakedData(string name, float animLen, Texture2D animMap, Texture2D animMapNormal)
        {
            Name = name;
            AnimLen = animLen;
            AnimMap = animMap;
            AnimMapNormal = animMapNormal;
        }
    }

    internal static class VertexAnimationEditor
    {
        private static Shader URPShader => Shader.Find("ybwork/URP/ECSVertexAnimation");
        private static int AnimMap => Shader.PropertyToID("_AnimMap");
        private static int AnimMapNormal => Shader.PropertyToID("_AnimMapNormal");
        private static int AnimLen => Shader.PropertyToID("_AnimLen");

        public static void Save(VertexAnimationMapCreator baker)
        {
            List<BakedData> datas = Bake(baker);
            var textures = datas.Select(data => SaveAsAsset(baker, data)).ToArray();

            string folderPath = CreateFolder(baker);
            Mesh mesh = CombineMeshes(baker.GetComponentsInChildren<SkinnedMeshRenderer>());
            AssetDatabase.CreateAsset(mesh, Path.Combine(folderPath, $"{baker.name}.mesh"));

            for (int i = 0; i < baker.Materials.Count; i++)
            {
                string name = (i + 1).ToString();
                Material[] materials = Enumerable.Range(0, datas.Count)
                    .Select(index => SaveAsMat(baker, name, datas[index], baker.Materials[i], textures[index].vertex, textures[index].normal))
                    .OrderBy(material => material.name)
                    .ToArray();
                string path = Path.Combine(folderPath, $"{baker.name}_{name}.prefab");
                SaveAsPrefab(path, mesh, materials);
            }
        }

        private static List<BakedData> Bake(VertexAnimationMapCreator baker)
        {
            AnimData animData = new AnimData(baker);

            List<BakedData> bakedDataList = new();

            //每一个动作都生成一个动作图
            foreach (AnimationState animationState in animData.AnimationClips)
            {
                if (!animationState.clip.legacy)
                {
                    Debug.LogError(string.Format($"{animationState.clip.name} is not legacy!!"));
                    continue;
                }
                bakedDataList.Add(BakePerAnimClip(animData, animationState));
            }
            return bakedDataList;
        }

        private static BakedData BakePerAnimClip(AnimData animData, AnimationState curAnim)
        {
            float sampleTime = 0;
            int curClipFrame = Mathf.ClosestPowerOfTwo((int)(curAnim.clip.frameRate * curAnim.length + 1));
            float perFrameTime = curAnim.length / (curClipFrame - 1);

            Texture2D animMap = new Texture2D(animData.MapWidth, curClipFrame, TextureFormat.RGBAHalf, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                name = curAnim.name
            };
            Texture2D animMapNormal = new Texture2D(animData.MapWidth, curClipFrame, TextureFormat.RGBAHalf, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                name = curAnim.name
            };
            animData.AnimationPlay(curAnim.name);

            Mesh[] bakedMeshes = new Mesh[animData.MapWidths.Length];
            for (var i = 0; i < curClipFrame; i++)
            {
                curAnim.time = sampleTime;

                animData.SampleAnimAndBakeMesh(ref bakedMeshes);

                var vertices = bakedMeshes.SelectMany(item => item.vertices).ToArray();
                var normals = bakedMeshes.SelectMany(item => item.normals).ToArray();
                for (int j = 0; j < vertices.Length; j++)
                {
                    animMap.SetPixel(j, i, new Color(vertices[j].x, vertices[j].y, vertices[j].z));
                    animMapNormal.SetPixel(j, i, new Color(normals[j].x, normals[j].y, normals[j].z));
                }

                sampleTime += perFrameTime;
            }
            animMap.Apply();
            animMapNormal.Apply();

            return new BakedData(animMap.name, curAnim.clip.length, animMap, animMapNormal);
        }

        private static (Texture2D vertex, Texture2D normal) SaveAsAsset(VertexAnimationMapCreator baker, BakedData data)
        {
            string folderPath = CreateFolder(baker, "Textures");
            string path = Path.Combine(folderPath, $"{baker.name}_{data.Name}.asset");
            if (File.Exists(path))
                File.Delete(path);
            AssetDatabase.CreateAsset(data.AnimMap, path);

            folderPath = CreateFolder(baker, "NormalTextures");
            path = Path.Combine(folderPath, $"{baker.name}_{data.Name}.asset");
            if (File.Exists(path))
                File.Delete(path);
            AssetDatabase.CreateAsset(data.AnimMapNormal, path);

            return (data.AnimMap, data.AnimMapNormal);
        }

        private static Material SaveAsMat(VertexAnimationMapCreator baker, string name, BakedData data,
            Material sourceMaterial, Texture2D animMapTexture, Texture2D animMapNormalTexture)
        {
            string folderPath = CreateFolder(baker, "Matrials");
            string path = Path.Combine(folderPath, $"{baker.name}_{name}_{data.Name}.mat");
            if (File.Exists(path))
                File.Delete(path);

            var material = new Material(URPShader);
            material.CopyMatchingPropertiesFromMaterial(sourceMaterial);
            material.SetTexture(AnimMap, animMapTexture);
            material.SetTexture(AnimMapNormal, animMapNormalTexture);
            material.SetFloat(AnimLen, data.AnimLen);
            material.enableInstancing = true;

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void SaveAsPrefab(string path, Mesh mesh, Material[] materials)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                var go = new GameObject();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().material = materials[0];
                go.AddComponent<MaterialChangerComponent>().materials = materials;
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
            }
            else
            {
                MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    meshFilter = prefab.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                    meshRenderer = prefab.AddComponent<MeshRenderer>();
                meshRenderer.material = materials[0];

                MaterialChangerComponent materialChangerComponent = prefab.GetComponent<MaterialChangerComponent>();
                if (materialChangerComponent == null)
                    materialChangerComponent = prefab.AddComponent<MaterialChangerComponent>();
                materialChangerComponent.materials = materials;

                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
        }

        private static Mesh CombineMeshes(SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            CombineInstance[] combine = new CombineInstance[skinnedMeshRenderers.Length];

            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                combine[i].mesh = skinnedMeshRenderers[i].sharedMesh;
                combine[i].transform = skinnedMeshRenderers[i].transform.localToWorldMatrix;
            }

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combine, mergeSubMeshes: true, useMatrices: true);

            return mesh;
        }

        private static string CreateFolder(VertexAnimationMapCreator baker, string subPath = null)
        {
            string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(baker));
            string folderPath = Path.Combine(path, VertexAnimationMapCreator.SubPath);
            string result = folderPath;
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(path, VertexAnimationMapCreator.SubPath);
            }
            if (!string.IsNullOrEmpty(subPath))
            {
                string subFolderPath = Path.Combine(folderPath, subPath);
                result = subFolderPath;
                if (!AssetDatabase.IsValidFolder(subFolderPath))
                {
                    AssetDatabase.CreateFolder(folderPath, subPath);
                }
            }
            return result;
        }

        // 在文件夹上添加右键菜单项
        [MenuItem("Assets/ybwork/CreateAllVertexAnimation", true, 30)]
        private static bool IsValidFolderSelection()
        {
            // 检查当前所选的是不是文件夹
            return AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        [MenuItem("Assets/ybwork/CreateAllVertexAnimation", false, 30)]
        private static void PerformCustomAction()
        {
            // 处理右键菜单的逻辑
            string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            List<(string assetPath, VertexAnimationMapCreator baker)> bakers = new();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { folderPath }); // 查找folderPath中的所有预制体
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                VertexAnimationMapCreator baker = prefab.GetComponent<VertexAnimationMapCreator>();
                if (baker != null)
                {
                    bakers.Add((assetPath, baker));
                }
            }
            for (int i = 0; i < bakers.Count; i++)
            {
                (string assetPath, VertexAnimationMapCreator baker) = bakers[i];
                EditorUtility.DisplayProgressBar("Baking", "Baking AnimMap " + assetPath, (float)i / bakers.Count);
                Save(baker);
                Debug.Log("生成完成:" + assetPath);
            }
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }
    }
}
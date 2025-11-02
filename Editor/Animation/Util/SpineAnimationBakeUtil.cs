
#if SPINE
using Spine;
using Spine.Unity;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Hukiry.AtlasMesh.Editor
{
    [System.Serializable]
    public class FrameData
    {
        public List<Vector3> vertices = new List<Vector3>();
    }

    [System.Serializable]
    public class MeshAnimationData
    {
        public List<FrameData> frames = new List<FrameData>();
    }

    public class SpineAnimationBakeUtil
    {
        private const float frameRate = 30f;

        private static Dictionary<string, Vector3[]> originalVerts = new Dictionary<string, Vector3[]>();

        private static Dictionary<string, float> previousBoneRotation = new Dictionary<string, float>();

        public static void BakeBakeSkeletonDataAsset(List<SkeletonDataAsset> skeletonDataAsset, string dirPath)
        {
            foreach (var item in skeletonDataAsset)
            {
                BakeBakeSkeletonDataAsset(item, dirPath);
            }
        }

        //渲染一个数据
        public static void BakeBakeSkeletonDataAsset(SkeletonDataAsset skeletonDataAsset, string dirPath)
        {
            var goTemp = CreateSkeletonAnimation(skeletonDataAsset);

            SkeletonAnimation skeletonAnimation = goTemp?.GetComponent<SkeletonAnimation>();

            if (skeletonAnimation == null)
            {
                Debug.LogError(" 请指定 SkeletonAnimation 组件！");
                return;
            }

            originalVerts.Clear();

            previousBoneRotation.Clear();

            var saveSkinDir = dirPath + "/" + skeletonDataAsset.name;

            if (!Directory.Exists(saveSkinDir)) Directory.CreateDirectory(saveSkinDir);


            Skeleton skeleton = skeletonAnimation.Skeleton;

            TrackEntry entry = skeletonAnimation.state.GetCurrent(0);

            if (entry == null)
            {
                Debug.LogError(" 当前没有播放动画！" + skeletonDataAsset.name);
                return;
            }

            //create mesh
            Mesh originalMesh;

            List<Vector3> meshVerts;

            string saveMeshPath = CreateSpineMesh(skeletonDataAsset, saveSkinDir, skeleton, out originalMesh, out meshVerts);

            //create animation
            List<ClipInfo> clipInfos = new List<ClipInfo>();

            MeshAnimationData bakeData = CreateClip(skeletonAnimation, ref clipInfos);

            //create texture
            bool isSuccesful = true;

            int texWidth = originalMesh.vertexCount;

            int texHeight = bakeData.frames.Count;

            Texture2D matrixTexture = new Texture2D(originalMesh.vertexCount, bakeData.frames.Count, TextureFormat.RGBAHalf, false);

            matrixTexture.filterMode = FilterMode.Point;

            for (int j = 0; j < texHeight; j++)
            {
                var frameData = bakeData.frames[j];

                Quaternion rotQ = Quaternion.Euler(0, 0, frameData.vertices[0].z);

                for (int i = 0; i < texWidth; i++)
                {
                    if (i < frameData.vertices.Count)
                    {
                        Vector3 offset = new Vector3(frameData.vertices[i].x, frameData.vertices[i].y, 0);

                        Vector3 lastVertex = meshVerts[i] + rotQ * offset;

                        matrixTexture.SetPixel(i, j, new Color(lastVertex.x, lastVertex.y, lastVertex.z, 1));
                    }
                    else
                    {
                        isSuccesful = false;

                        matrixTexture.SetPixel(i, j, new Color(meshVerts[i].x, meshVerts[i].y, meshVerts[i].z, 1));
                    }
                }
            }
            matrixTexture.Apply();


            //create data
            var saveAniTexPath = Path.Combine(saveSkinDir, $"{ skeletonDataAsset.name}_clip.asset");

            matrixTexture.name = $"{ skeletonDataAsset.name}_clip";

            AssetDatabase.CreateAsset(matrixTexture, saveAniTexPath);

            var saveTexData = Path.Combine(saveSkinDir, $"{ skeletonDataAsset.name}_texture.png");
            var sharedMaterial = goTemp.GetComponent<MeshRenderer>()?.sharedMaterial;

            if (sharedMaterial)
            {
                var destTexPath = AssetDatabase.GetAssetPath(sharedMaterial.mainTexture.GetInstanceID());

                File.Copy(destTexPath, saveTexData, true);
            }

            AssetDatabase.Refresh();

            if (isSuccesful)
            {
                // 创建 asset 数据
                var asset = ScriptableObject.CreateInstance<AnimationDataAsset>();

                asset.clipTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(saveAniTexPath);

                asset.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(saveMeshPath);

                asset.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(saveTexData);

                asset.isAnimation = clipInfos.Count > 0;

                asset.isSpine = true;

                asset.clips = clipInfos;

                var saveAnimationData = Path.Combine(saveSkinDir, $"{skeletonDataAsset.name}.asset");

                AssetDatabase.CreateAsset(asset, saveAnimationData);
            }

            goTemp.SetActive(false);

            GameObject.DestroyImmediate(skeletonAnimation);

            GameObject.DestroyImmediate(goTemp);

            AssetDatabase.Refresh();

            Debug.Log($" 烘焙完成：{dirPath}-----------------{skeletonDataAsset.name}");
        }

        private static GameObject CreateSkeletonAnimation(SkeletonDataAsset skeletonDataAsset)
        {
            GameObject go = new GameObject("temp", typeof(MeshFilter), typeof(MeshRenderer));

            SkeletonAnimation skeletonAnimation = go.AddComponent<SkeletonAnimation>();

            skeletonAnimation.skeletonDataAsset = skeletonDataAsset;

            //set default skin
            skeletonAnimation.initialSkinName = skeletonDataAsset.GetSkeletonData(false).Skins.ToArray()[0].Name;

            //set default animation
            skeletonAnimation.AnimationName = skeletonDataAsset.GetSkeletonData(false).Animations.ToArray()[0].Name;

            go.hideFlags = HideFlags.HideAndDontSave;

            go.GetComponent<Renderer>().enabled = true;

            return go;
        }

        /*
         * 后期修改，每个动画的顶点可能不同（可能需要缓存三角面数据）
         */
        private static MeshAnimationData CreateClip(SkeletonAnimation skeletonAnimation, ref List<ClipInfo> clipInfos)
        {
            clipInfos.Clear();

            Skeleton skeleton = skeletonAnimation.Skeleton;

            var Animations = skeletonAnimation.skeletonDataAsset.GetSkeletonData(false).Animations.ToArray();//动画集合

            int Length = Animations.Length;

            int offsetFrame = 0;

            MeshAnimationData bakeData = new MeshAnimationData();

            for (int k = 0; k < Length; k++)
            {
                var AnimationName = Animations[k].Name;

                skeletonAnimation.AnimationName = AnimationName;

                TrackEntry entry = skeletonAnimation.state.GetCurrent(0);

                Spine.Animation anim = entry.Animation;

                float duration = anim.Duration;

                int totalFrames = Mathf.CeilToInt(duration * frameRate);

                if (totalFrames > 0)
                {
                    ClipInfo clip = new ClipInfo();

                    clip.frameCount = totalFrames;

                    clip.startFrame = offsetFrame;

                    clip.index = k;

                    clip.name = AnimationName;

                    clipInfos.Add(clip);

                    offsetFrame += totalFrames;
                }

                // === 烘焙每帧 ===
                for (int i = 0; i < totalFrames; i++)
                {
                    float time = i / frameRate;

                    anim.Apply(skeleton, time, time, false, null, 1, MixBlend.Setup, MixDirection.In);

                    skeleton.UpdateWorldTransform(Skeleton.Physics.Update);

                    FrameData frame = new FrameData();

                    foreach (Slot slot in skeleton.Slots)
                    {
                        var attach = slot.Attachment;

                        if (attach == null) continue;

                        string attachKey = slot.Data.Name + "_" + attach.Name;

                        Vector3[] origVerts;

                        if (!originalVerts.TryGetValue(attachKey, out origVerts))
                            continue;

                        float[] worldVerts;

                        if (attach is Spine.MeshAttachment meshAttachment)
                        {
                            worldVerts = new float[meshAttachment.WorldVerticesLength];

                            meshAttachment.ComputeWorldVertices(slot, 0, meshAttachment.WorldVerticesLength, worldVerts, 0, 2); // <<< 修改

                            for (int v = 0; v < origVerts.Length; v++)
                            {
                                Vector3 basePos = origVerts[v];
                                Vector3 worldPos = new Vector3(worldVerts[v * 2], worldVerts[v * 2 + 1], 0);

                                Vector3 offset = worldPos - basePos; // <<< 修改

                                frame.vertices.Add(new Vector3(offset.x, offset.y, 0));
                            }
                        }
                        else if (attach is Spine.RegionAttachment region)
                        {
                            worldVerts = new float[8];

                            region.ComputeWorldVertices(slot, worldVerts, 0, 2);

                            // 计算旋转变化量（Δ）
                            Bone bone = slot.Bone;

                            float currentRot = bone.WorldRotationX;

                            float prevRot = previousBoneRotation.TryGetValue(bone.Data.Name, out float last) ? last : currentRot;

                            float deltaRot = Mathf.DeltaAngle(prevRot, currentRot) / skeleton.Slots.Count;

                            previousBoneRotation[bone.Data.Name] = currentRot;

                            // 顶点偏移
                            for (int v = 0; v < origVerts.Length; v++)
                            {
                                Vector3 localPos = new Vector3(worldVerts[v * 2], worldVerts[v * 2 + 1], 0);

                                Vector3 offset = localPos - origVerts[v];

                                frame.vertices.Add(new Vector3(offset.x, offset.y, deltaRot));
                            }
                        }
                        else continue;
                    }

                    bakeData.frames.Add(frame);
                }
            }

            return bakeData;
        }

        private static string CreateSpineMesh(SkeletonDataAsset skeletonDataAsset, string saveSkinDir, Skeleton skeleton, out Mesh originalMesh, out List<Vector3> meshVerts)
        {

            // === 保存原始网格 ===
            originalMesh = new Mesh();

            meshVerts = new List<Vector3>();

            List<int> meshTriangles = new List<int>();

            List<Vector2> meshUVs = new List<Vector2>();

            List<Color> colors = new List<Color>();

            foreach (Slot slot in skeleton.Slots)
            {
                var attach = slot.Attachment;

                string attachKey = slot.Data.Name + "_" + (attach?.Name ?? "null");

                if (attach is MeshAttachment meshAttachment)
                {
                    int vertexCount = meshAttachment.WorldVerticesLength / 2;

                    float[] worldVerts = new float[meshAttachment.WorldVerticesLength];

                    meshAttachment.ComputeWorldVertices(slot, 0, meshAttachment.WorldVerticesLength, worldVerts, 0, 2);

                    Vector3[] localVerts = new Vector3[vertexCount];

                    for (int i = 0; i < vertexCount; i++)
                    {
                        Vector3 w = new Vector3(worldVerts[i * 2], worldVerts[i * 2 + 1], 0f);

                        localVerts[i] = w;

                        colors.Add(meshAttachment.GetColor());
                    }

                    originalVerts[attachKey] = localVerts;

                    int baseIndex = meshVerts.Count;

                    meshVerts.AddRange(localVerts);

                    foreach (var tri in meshAttachment.Triangles)
                        meshTriangles.Add(tri + baseIndex);

                    for (int i = 0; i < vertexCount; i++)
                        meshUVs.Add(new Vector2(meshAttachment.UVs[i * 2], meshAttachment.UVs[i * 2 + 1]));
                }
                else if (attach is RegionAttachment region)
                {
                    float[] verts2D = new float[8];

                    region.ComputeWorldVertices(slot, verts2D, 0, 2);

                    Vector3[] verts = new Vector3[4];

                    for (int i = 0; i < 4; i++)
                    {
                        verts[i] = new Vector3(verts2D[i * 2], verts2D[i * 2 + 1], 0);

                        colors.Add(region.GetColor());
                    }
                    originalVerts[attachKey] = verts;

                    meshVerts.AddRange(verts);

                    int offset = meshVerts.Count - 4;

                    meshTriangles.AddRange(new int[] { offset, offset + 1, offset + 2, offset, offset + 2, offset + 3 });

                    for (int u = 0; u < 4; u++)
                        meshUVs.Add(new Vector2(region.UVs[u * 2], region.UVs[u * 2 + 1]));
                }
            }

            List<Vector2> uv2 = new List<Vector2>();

            for (int i = 0; i < meshVerts.Count; i++)
            {
                uv2.Add(new Vector2(i, 0));
            }

            originalMesh.name = skeletonDataAsset.name;

            originalMesh.vertices = meshVerts.ToArray();

            originalMesh.triangles = meshTriangles.ToArray();

            originalMesh.uv = meshUVs.ToArray();

            originalMesh.SetUVs(1, uv2);

            originalMesh.colors = colors.ToArray();

            originalMesh.RecalculateBounds();

            string saveMeshPath = Path.Combine(saveSkinDir, $"{ skeletonDataAsset.name}_mesh.asset");

            UnityEditor.AssetDatabase.CreateAsset(originalMesh, saveMeshPath);
            return saveMeshPath;
        }
    }
}
#endif
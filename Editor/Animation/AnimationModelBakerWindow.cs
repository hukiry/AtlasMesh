
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Hukiry.AtlasMesh.Editor
{
    public struct TexData
    {
        public half r;
        public half g;
        public half b;
        public half a;
    }

    public class AnimationModelBakerWindow : EditorWindow, IHasCustomMenu
    {
        const bool IsUtilityWindow = true;

        public List<GameObject> modelObject;

        public RuntimeAnimatorController animatorController;

        public string SaveDirPath = "Assets/AtlasMesh/Examples/Model";

        public string SaveTotalPath = "Assets/AtlasMesh/Examples/ModelAtlas";

        public string ModelDataName ="ModelAtlas3D";
        private string projectPath => Application.dataPath.Replace("Assets", "");
#if SPINE
        public Spine.Unity.SkeletonDataAsset skeletonDataAsset;
        [MenuItem("CONTEXT/SkeletonDataAsset/Spine Animation Baking", false, 5000)]
        public static void Init(MenuCommand command)
        {
            AnimationModelBakerWindow window = EditorWindow.GetWindow<AnimationModelBakerWindow>(IsUtilityWindow);

            window.minSize = new Vector2(330f, 530f);

            window.maxSize = new Vector2(600f, 1000f);

            window.titleContent = new GUIContent("Spine Animation Baking");

            window.skeletonDataAsset = command.context as Spine.Unity.SkeletonDataAsset;

            window.Show();
        }
         SerializedProperty soskeletonDataAsset;
#endif

        [MenuItem("Tools/Animation Model Bake Window")]
        static void ShowWindow() => GetWindow<AnimationModelBakerWindow>("Animation Model Bake");

        GUIContent sceneObjectGui;

        SerializedProperty sosceneObject;

        SerializedProperty spanimatorController;

        SerializedProperty sosavePath;

        SerializedProperty someshTex;

        SerializedProperty so_ModelDataName;

        SerializedObject soThis;

        private void OnEnable()
        {
            SerializedObject so = new SerializedObject(this);

            sceneObjectGui = new GUIContent("Model", "模型对象");

            sosceneObject = so.FindProperty(nameof(modelObject));

            spanimatorController = so.FindProperty(nameof(animatorController));

            sosavePath = so.FindProperty(nameof(SaveDirPath));

            so_ModelDataName = so.FindProperty(nameof(ModelDataName));
#if SPINE
            soskeletonDataAsset = so.FindProperty(nameof(skeletonDataAsset));
#endif

            soThis = so;
        }

        private void DrawTitle(string label)
        {
            var alignment = GUI.skin.label.alignment;

            var fontSize = GUI.skin.label.fontSize;

            GUI.skin.label.alignment = TextAnchor.MiddleCenter;

            GUI.skin.label.fontSize = 18;

            GUILayout.Label(label);

            GUI.skin.label.fontSize = fontSize;

            GUI.skin.label.alignment = alignment;
        }

        void OnGUI()
        {
            soThis.Update();

            EditorGUILayout.Space();

            using (new BoxScope())
            {
                DrawTitle("1，烘焙模型");

                EditorGUILayout.PropertyField(sosceneObject, sceneObjectGui, true);

                EditorGUILayout.PropertyField(spanimatorController, true);

                EditorGUILayout.PropertyField(sosavePath);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Bake Clips"))
                    {
                        BakeDynamicModelClips(modelObject);
                    }
                }
            }

            EditorGUILayout.Space();

            var sortlabel = "3，烘焙总数据";
#if SPINE
            using (new BoxScope())
            {
                DrawTitle("2，烘焙Spine动画");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.TextField(SaveDirPath);

                    if (GUILayout.Button("Browse", GUILayout.Width(100)))
                    {
                        string folder = Application.dataPath;

                        if (!string.IsNullOrEmpty(SaveDirPath))
                        {
                            folder = SaveDirPath.Replace(projectPath, "");
                        }

                        string dirPath = EditorUtility.OpenFolderPanel("Unity", folder, "");

                        if (!string.IsNullOrEmpty(dirPath))
                        {
                            SaveDirPath = dirPath.Replace(projectPath, "");
                        }
                        else
                        {
                            SaveDirPath = folder;
                        }
                    }
                }

                EditorGUILayout.PropertyField(soskeletonDataAsset, true);

                if (GUILayout.Button("Bake Spine"))
                {
                    SpineAnimationBakeUtil.BakeBakeSkeletonDataAsset(skeletonDataAsset, SaveDirPath);
                }
            }
#else
            sortlabel = "2，烘焙总数据";
#endif

            using (new BoxScope())
            {
                DrawTitle(sortlabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.TextField(SaveTotalPath);

                    if (GUILayout.Button("Browse", GUILayout.Width(100)))
                    {
                        string folder = Application.dataPath;

                        if (!string.IsNullOrEmpty(SaveTotalPath))
                        {
                            folder = SaveTotalPath.Replace(projectPath, "");
                        }

                        string dirPath = EditorUtility.OpenFolderPanel("Unity", folder, "");

                        if (!string.IsNullOrEmpty(dirPath))
                        {
                            SaveTotalPath = dirPath.Replace(projectPath, "");
                        }
                        else
                        {
                            SaveTotalPath = folder;
                        }
                    }
                }

                //using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(so_ModelDataName);

                    if (GUILayout.Button("Pack All Model Data"))
                    {
                        AnimationModelPackUtil.PackAllAnimationData(SaveTotalPath, so_ModelDataName.stringValue);
                    }
                }
            }
            soThis.ApplyModifiedProperties();
        }

      
        void BakeDynamicModelClips(List<GameObject> modelObjectList)
        {
            foreach (var item in modelObjectList)
            {
                this.BakeDynamicModelClips(item);
            }
        }

        //加载静态模型烘焙----后面加入
        void BakeDynamicModelClips(GameObject modelObject)
        {
            if (modelObject == null)
            {
                Debug.LogError("请指定模型与至少一个动画！");
                return;
            }

            var skinnedMesh = modelObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!skinnedMesh)
            {
                Debug.LogError("未找到 SkinnedMeshRenderer！");
                return;
            }

            Dictionary<string, AnimationClip> animationInfos = new Dictionary<string, AnimationClip>();

            foreach (var clip in animatorController.animationClips)
            {
                animationInfos[clip.name] = clip;
            }

            var bones = skinnedMesh.bones;

            int boneCount = bones.Length;

            int totalFrameCount = 0;

            int index_clip = 0;

            Dictionary<int, ClipInfo> clipInfos = new Dictionary<int, ClipInfo>();

            foreach (var clip in animationInfos.Values)
            {
                if (clip == null) continue;

                int frameCount = Mathf.CeilToInt(clip.length * clip.frameRate);

                clipInfos[index_clip] = new ClipInfo
                {
                    name = clip.name,

                    clip = clip,

                    index = index_clip,

                    startFrame = totalFrameCount,

                    frameCount = frameCount,
                };
                totalFrameCount += frameCount;

                index_clip++;
            }

            int texWidth = boneCount * 4;  // 每根骨骼4列（矩阵4行）

            int texHeight = totalFrameCount; // 每帧一行


            Texture2D matrixTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBAHalf, false);

            matrixTexture.filterMode = FilterMode.Point;

            NativeArray<TexData> matrixTextureData = new NativeArray<TexData>(texWidth * texHeight, Allocator.Temp);

            int frameCursor = 0;

            int index_bone = 0;

            var Count = animationInfos.Values.Count;

            for (int i = 0; i < Count; i++)
            {
                if (!clipInfos.ContainsKey(i)) continue;

                var clip = clipInfos[i].clip;

                if (clip == null) continue;

                int frameCount = Mathf.CeilToInt(clip.length * clip.frameRate);

                for (int f = 0; f < frameCount; f++)
                {
                    float t = f / clip.frameRate;

                    clip.SampleAnimation(modelObject, t);//播放动画

                    for (int index = 0; index < boneCount; index_bone++, index++)
                    {
                        Matrix4x4 m = bones[index].localToWorldMatrix * skinnedMesh.sharedMesh.bindposes[index];

                        var matrix_index = index_bone * 4;

                        for (int row = 0; row < 4; row++)
                        {
                            matrixTextureData[matrix_index + row] = new TexData
                            {
                                r = (half)m.GetRow(row).x,

                                g = (half)m.GetRow(row).y,

                                b = (half)m.GetRow(row).z,

                                a = (half)m.GetRow(row).w,
                            };
                        }
                    }
                }
                frameCursor += frameCount;

                clipInfos[i].clip = null;
            }
            matrixTexture.SetPixelData(matrixTextureData, 0);

            matrixTexture.Apply();

            var mesh = CreateMesh(skinnedMesh.sharedMesh);
          
            string saveModelName = modelObject.name.Replace(' ', '_');

            var arrayNames = modelObject.name.Split('@');

            if (arrayNames.Length >= 2)
            {
                saveModelName = arrayNames[0].Replace(' ', '_');
            }
            var saveSkineDir = SaveDirPath + "/" + saveModelName;

            if (!Directory.Exists(saveSkineDir)) Directory.CreateDirectory(saveSkineDir);


            // 动画贴图数据
            var saveClipPath = Path.Combine(saveSkineDir, $"{saveModelName}_clip.asset");

            matrixTexture.name = $"{saveModelName}_clip";

            AssetDatabase.CreateAsset(matrixTexture, saveClipPath);


            //保存网格
            var saveMeshData = Path.Combine(saveSkineDir, $"{saveModelName}_mesh.asset");

            AssetDatabase.CreateAsset(mesh, saveMeshData);


            //保存贴图
            var saveTexData = Path.Combine(saveSkineDir, $"{saveModelName}_texture.png").Replace(Application.dataPath, "Assets").Replace('\\', '/');

            if (skinnedMesh.sharedMaterial?.mainTexture)
            {
                var destTexPath = AssetDatabase.GetAssetPath(skinnedMesh.sharedMaterial.mainTexture.GetInstanceID());

                File.Copy(destTexPath, saveTexData, true);
            }


            // 创建 asset 数据
            AssetDatabase.Refresh();
            var asset = ScriptableObject.CreateInstance<AnimationDataAsset>();

            asset.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(saveTexData);

            asset.clipTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(saveClipPath);

            asset.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(saveMeshData);

            asset.isAnimation = true;

            asset.isSpine = false;

            asset.clips = clipInfos.Values.ToList();

            var saveAnimationData = Path.Combine(saveSkineDir, $"{saveModelName}.asset");

            AssetDatabase.CreateAsset(asset, saveAnimationData);

            AssetDatabase.Refresh();

            //渲染动画贴图
            MakeReadableTexture.DoTex(AssetDatabase.LoadAssetAtPath<Texture2D>(saveClipPath));

            AssetDatabase.Refresh();

            Debug.Log($"多动画烘焙完成！骨骼数: {boneCount}, 总帧数: {totalFrameCount}, 动画数: {animationInfos.Values.Count}");
        }

        void BakeStaticModelClips(List<GameObject> modelObjectList)
        {
            foreach (var item in modelObjectList)
            {
                this.BakeStaticModelClips(item);
            }
        }

        void BakeStaticModelClips(GameObject modelObject)
        {
            if (modelObject == null)
            {
                Debug.LogError("请指定模型与至少一个动画！");
                return;
            }

            var skinnedMesh = modelObject.GetComponentInChildren<MeshFilter>();

            if (!skinnedMesh)
            {
                Debug.LogError("未找到 SkinnedMeshRenderer！");
                return;
            }

          
            string saveModelName = modelObject.name.Replace(' ', '_');

            var arrayNames = modelObject.name.Split('@');

            if (arrayNames.Length >= 2)
            {
                saveModelName = arrayNames[0].Replace(' ', '_');
            }
            var saveSkineDir = SaveDirPath + "/" + saveModelName;

            if (!Directory.Exists(saveSkineDir)) Directory.CreateDirectory(saveSkineDir);

            //保存网格
            var mesh = CreateMesh(skinnedMesh.sharedMesh);

            var saveMeshData = Path.Combine(saveSkineDir, $"{saveModelName}_mesh.asset");

            AssetDatabase.CreateAsset(mesh, saveMeshData);


            //保存贴图
            var saveTexData = Path.Combine(saveSkineDir, $"{saveModelName}_texture.png").Replace(Application.dataPath, "Assets").Replace('\\', '/');

            var meshRenderer = modelObject.GetComponentInChildren<MeshRenderer>();

            if (meshRenderer.sharedMaterial?.mainTexture)
            {
                var destTexPath = AssetDatabase.GetAssetPath(meshRenderer.sharedMaterial.mainTexture.GetInstanceID());

                File.Copy(destTexPath, saveTexData, true);
            }


            // 创建 asset 数据
            AssetDatabase.Refresh();

            var asset = ScriptableObject.CreateInstance<AnimationDataAsset>();

            if(meshRenderer.sharedMaterial?.mainTexture)

            asset.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(saveTexData);

            asset.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(saveMeshData);

            asset.isAnimation = false;

            asset.isSpine = false;

            asset.speed = 0;

            var saveAnimationData = Path.Combine(saveSkineDir, $"{saveModelName}.asset");

            AssetDatabase.CreateAsset(asset, saveAnimationData);

            AssetDatabase.Refresh();

            Debug.Log($"静态模型烘焙完成！{saveSkineDir}------------{saveModelName}");
        }

        private Mesh CreateMesh(Mesh originalMesh)
        {
            var temp = new Mesh();

            temp.vertices = originalMesh.vertices;

            temp.normals = originalMesh.normals;

            temp.uv = originalMesh.uv;

            temp.tangents = originalMesh.tangents;

            temp.triangles = originalMesh.triangles;

            temp.colors = originalMesh.colors;

            temp.boneWeights = originalMesh.boneWeights;

            temp.bindposes = originalMesh.bindposes;

            temp.RecalculateNormals();

            temp.RecalculateTangents();

            temp.RecalculateBounds();

            temp.Optimize();

            return temp;
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent($"Ping {nameof(AnimationModelBakerWindow)}"), false, () =>
            {
                EditorGUIUtility.PingObject(FindAssetObject<MonoScript>(nameof(AnimationModelBakerWindow)));
            });

        }


        public static T FindAssetObject<T>(string fileName) where T : UnityEngine.Object
        {
            var assetPathArray = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}").Select(x => UnityEditor.AssetDatabase.GUIDToAssetPath(x));

            foreach (var path in assetPathArray)
            {
                var assetName = Path.GetFileNameWithoutExtension(path);

                if (assetName == fileName)
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                }
            }
            return default;
        }

    }

    public static class MakeReadableTexture
    {
        public static void DoTex(Texture2D target)
        {
            var readable = CaptureToReadable(target);

            EditorUtility.CopySerialized(readable, target);

            EditorUtility.SetDirty(target);
        }

        public static Texture2D CaptureToReadable(Texture src)
        {
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;

            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGBHalf,
                                                linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
            Graphics.Blit(src, rt);

            var prev = RenderTexture.active;

            RenderTexture.active = rt;

            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBAHalf, false, linear);

            tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);

            tex.Apply(false, false); 

            RenderTexture.active = prev;

            RenderTexture.ReleaseTemporary(rt);

            return tex;
        }
    }

}
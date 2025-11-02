using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Sprites;
using UnityEngine.U2D;

namespace Hukiry.AtlasMesh.Editor
{
    using Editor = UnityEditor.Editor;
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AtlasDataAsset), true)]
    public class AtlasDataAssetEditor : Editor
    {
        static readonly int SliderHash = "Slider".GetHashCode();

        AtlasDataAsset meshData;

        private string meshName;

        private string spriteName;

        private int[] controlID = new int[4] { 1, 2, 3, 4 };

        private Texture2D TexMesh;

        private Texture2D TexAtlas;

        private Texture2D TexIcon;

        private Texture2D TexSpriteAtlas;

        private Texture TexEye;

        private Texture TexGridY;

        SerializedProperty material;

        SerializedProperty mainTextureArray;

        SerializedProperty mainTextureUv;

        SerializedProperty clipTexture;

        SerializedProperty clipTextureUv;

        SerializedProperty meshLabels;

        SerializedProperty modelInfos;

        private void OnEnable()
        {
            serializedObject.Update();

            TexMesh = EditorGUIUtility.ObjectContent(null, typeof(Mesh)).image as Texture2D;

            TexAtlas = EditorGUIUtility.ObjectContent(null, typeof(UnityEngine.U2D.SpriteAtlas)).image as Texture2D;

            TexIcon = EditorGUIUtility.ObjectContent(null, typeof(Texture2D)).image as Texture2D;

            TexSpriteAtlas = EditorGUIUtility.ObjectContent(null, typeof(SpriteAtlas)).image as Texture2D;

            TexEye = EditorGUIUtility.IconContent("d_MeshRenderer Icon").image;

            TexGridY = EditorGUIUtility.IconContent("d_GridAxisY").image;

            frameScale = 0.5f;

            meshData = target as AtlasDataAsset;

            controlID = new int[4] { 1, 2, 3, 4 };

            material = serializedObject.FindProperty(nameof(material));

            mainTextureArray = serializedObject.FindProperty(nameof(mainTextureArray));

            mainTextureUv = serializedObject.FindProperty(nameof(mainTextureUv));

            clipTexture = serializedObject.FindProperty(nameof(clipTexture));

            clipTextureUv = serializedObject.FindProperty(nameof(clipTextureUv));

            meshLabels = serializedObject.FindProperty(nameof(meshLabels));

            modelInfos = serializedObject.FindProperty(nameof(modelInfos));


            if (meshData.meshLabels.Count <= 0 && meshData.modelInfos.Count > 0)
            {
                List<MeshLabel> labelList = new List<MeshLabel>();

                MeshLabel labelSpine = new MeshLabel() { labelType = MeshLabelType.Spine, labels = new List<string>() };

                MeshLabel labelAnimation = new MeshLabel() { labelType = MeshLabelType.AnimationModel, labels = new List<string>() };

                MeshLabel labelStatic = new MeshLabel() { labelType = MeshLabelType.StaticModel, labels = new List<string>() };
                foreach (var item in meshData.modelInfos)
                {
                    if (item.isSpine)
                    {
                        labelSpine.labels.Add(item.spriteName);
                    }
                    else if (item.isAnimation)
                    {
                        labelAnimation.labels.Add(item.spriteName);
                    }
                    else
                    {
                        labelStatic.labels.Add(item.spriteName);
                    }
                }
                labelList.Add(labelAnimation);

                labelList.Add(labelStatic);

                labelList.Add(labelSpine);

                meshData.meshLabels = labelList;

                EditorUtility.SetDirty(meshData);

                serializedObject.ApplyModifiedProperties();

                AssetDatabase.SaveAssets();
            }

        }

        private static Type GetUnityEditor(string className)
        {
            var type = Type.GetType($"UnityEditor.{className},UnityEditor", false);

            if (type == null)
            {
                type = Type.GetType($"UnityEditor.U2D.{className},UnityEditor", false);
            }

            return type;
        }

        private static MethodInfo m_GetPreviewTexturesMethod;

        private static Texture2D[] SpriteAtlasToTexture(SpriteAtlas spriteAtlas)
        {
            try
            {
                if (m_GetPreviewTexturesMethod == null)
                {
                    System.Type t = GetUnityEditor("SpriteAtlasExtensions");

                    m_GetPreviewTexturesMethod = t.GetMethod("GetPreviewTextures", BindingFlags.NonPublic | BindingFlags.Static);
                }

                if (m_GetPreviewTexturesMethod != null)
                {
                    object textures = m_GetPreviewTexturesMethod.Invoke(null, new object[] { spriteAtlas });

                    return textures as Texture2D[];
                }
            }
            catch { }

            return null;
        }
        /// <summary>
        ///  uv坐标获取
        ///  <code>
        ///     private static Rect GetSpriteUvFromTexture(Sprite sprite){
        ///         
        ///     }
        ///  </code>
        ///  <list type="bullet">1,开启图集打包 </list>
        ///  <list type="bullet">2,运行中 </list>
        ///  <list type="bullet">3,图集的 Texture Type 设置为 "Sprite (2D and UI)" </list>
        ///  <list type="bullet">4,精灵UV，0~1</list>
        /// </summary>
        private static Rect GetSpriteUvFromTexture(Sprite sprite)
        {
            // 计算精灵在当前纹理上的UV范围
            Rect uvRect = new Rect(
                Mathf.Min(sprite.uv[0].x, sprite.uv[1].x, sprite.uv[2].x, sprite.uv[3].x),

                Mathf.Min(sprite.uv[0].y, sprite.uv[1].y, sprite.uv[2].y, sprite.uv[3].y),

                Mathf.Max(sprite.uv[0].x, sprite.uv[1].x, sprite.uv[2].x, sprite.uv[3].x) -
                Mathf.Min(sprite.uv[0].x, sprite.uv[1].x, sprite.uv[2].x, sprite.uv[3].x),

                Mathf.Max(sprite.uv[0].y, sprite.uv[1].y, sprite.uv[2].y, sprite.uv[3].y) -
                Mathf.Min(sprite.uv[0].y, sprite.uv[1].y, sprite.uv[2].y, sprite.uv[3].y)
            );

            return uvRect;
        }

        /// <summary>
        /// 精灵uv坐标转换到世界网格坐标，用于填充网格uv
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
        private static Vector2[] OutSpriteUV(Sprite sp)
        {
            var outerUV = DataUtility.GetOuterUV(sp);

            var spriteArray = new Vector2[4] {

                new Vector2(outerUV.x, outerUV.y),

                new Vector2(outerUV.x, outerUV.w),

                new Vector2(outerUV.z, outerUV.w),

                new Vector2(outerUV.z, outerUV.y),

            };
            return spriteArray;
        }

        /// <summary>
        /// 导出图集纹理为PNG文件
        /// </summary>
        private string ExportAtlasTextureAsPNG(SpriteAtlas atlas)
        {
            string savePath = GetFullPath(atlas, "_tex.png");

            var array = SpriteAtlasToTexture(atlas);

            if (array == null || array.Length == 0) return null;

            var atlasTexture = array[0];

            // 读取纹理像素并保存为PNG
            byte[] pngData = atlasTexture.EncodeToPNG();

            File.WriteAllBytes(savePath, pngData);

            Debug.Log($"图集纹理已导出至：{savePath}");

            return savePath;
        }

        private string GetFullPath(UnityEngine.Object obj, string ext = ".png")
        {
            string assetFolder = AssetDatabase.GetAssetPath(obj);

            assetFolder = assetFolder.Replace(Application.dataPath, "Assets");

            string savePath = assetFolder.Replace(Path.GetExtension(assetFolder), ext);

            return savePath;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(target.name + " (AtlasData)", EditorStyles.whiteLargeLabel);

            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                using (new BoxScope())
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.Space();

                    using (new GUILayout.HorizontalScope())
                    {
                        Rect dropArea = GUILayoutUtility.GetRect(0, 0, GUILayout.Width(150), GUILayout.Height(150));

                        meshData.clipTexture = HandleDragAndDrop(meshData.clipTexture, dropArea, 0, 0);

                        dropArea = GUILayoutUtility.GetRect(0, 0, GUILayout.Width(150), GUILayout.Height(150));

                        meshData.material = HandleDragAndDrop(meshData.material, dropArea, 150, 1);
                    }

                    //EditorGUILayout.ObjectField(material, new GUIContent("Material", TexMaterial));

                    EditorGUILayout.Space();

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                using (new BoxScope())
                {
                    EditorGUILayout.PropertyField(mainTextureArray, true);

                    EditorGUILayout.PropertyField(mainTextureUv, true);

                    EditorGUILayout.PropertyField(clipTextureUv, true);

                    EditorGUILayout.PropertyField(meshLabels, true);

                    EditorGUILayout.PropertyField(modelInfos, true);
                }

                using (new BoxScope())
                {
                    if (GUILayout.Button(new GUIContent("Pack SpriteAtlas", TexSpriteAtlas), GUILayout.Height(30)))
                    {
                        AnimationModelPackUtil.PackTextureSpriteAtlas(target as AtlasDataAsset);
                    }

                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Create Prefab"))
                        {
                            this.CreatePrefabMesh();
                        }
                    }
                }
            }


            serializedObject.ApplyModifiedProperties();
        }


        private void DrawSprite(Rect rect)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                rect.width = 70;

                rect.height = 70;

                AtlasMeshSelector.DrawTiledTexture(rect);

                GUI.DrawTexture(rect, EditorGUIUtility.IconContent("textureCheckerDark").image);

                bool isWarn = true;

                if (this.meshData.mainTextureUv.Count <= 0)
                {
                    return;
                }

                var uv = this.meshData.mainTextureUv[0];

                string nameS = string.IsNullOrEmpty(this.spriteName) ? uv.spriteName : this.spriteName;


                TextureInfo? info = this.meshData.mainTextureUv?.Find(p => p.spriteName == nameS);

                if (info != null && this.meshData.mainTextureArray.Count > 0)
                {
                    isWarn = false;

                    GUI.DrawTextureWithTexCoords(rect, this.meshData.mainTextureArray[info.Value.index], info.Value.GetRect());
                }


                if (isWarn)
                {
                    rect.x += 18;

                    GUI.Label(rect, EditorGUIUtility.IconContent("d_console.warnicon"));

                    rect.x -= 18;
                }

                rect.x += 54;

                rect.y += 54;

                rect.width = 16;

                rect.height = 16;
                //pick sprite
                if (GUI.Button(rect, EditorGUIUtility.IconContent("d_pick")))
                {
                    string spriteName = string.IsNullOrEmpty(this.spriteName) ? "----" : this.spriteName;

                    AtlasMeshSelector.Show(this.meshData, spriteName, 0, (selectedSpriteName) =>
                    {

                        if (selectedSpriteName == null)
                            return;

                        this.spriteName = selectedSpriteName;

                        m_info = this.meshData.mainTextureUv?.Find(p => p.spriteName == this.spriteName);

                        if (previewGameObject != null)
                        {
                            previewGameObject.GetComponent<MeshFilter>().sharedMesh = this.ChangeMeshUV(Instantiate(m_originalMesh), m_info.Value);
                        }

                        requiresRefresh = true;

                        this.Repaint();

                    });
                }
            }

        }

        private void DrawGUITexture(Rect rect, Texture2D tex, Rect uvRect)
        {
            GUI.DrawTextureWithTexCoords(rect, tex, uvRect);
        }

        private void DrawGUISprite(Rect rect, Sprite sprite)
        {
            if (sprite)
            {
                Vector4 uv4 = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);

                Rect uv = new Rect(uv4.x, uv4.y, uv4.z - uv4.x, uv4.w - uv4.y);

                GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            }
        }

        private void CreatePrefabMesh()
        {
            var assetPath = AssetDatabase.GetAssetPath(target.GetInstanceID());

            var savePath = assetPath.Split('.')[0] + ".prefab";

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);

            if (prefab)
            {
                var mesh = prefab.GetComponent<AtlasMesh>();

                if (mesh.AtlasData == null)
                {
                    mesh.AtlasData = meshData;

                    if (meshData.mainTextureUv.Count > 0)
                    {
                        mesh.spriteName = meshData.mainTextureUv[0].spriteName;
                    }
                }

                EditorGUIUtility.PingObject(prefab);

                AssetDatabase.SaveAssets();
            }
            else
            {
                var gameName = Path.GetFileNameWithoutExtension(savePath);

                GameObject rootGo = new GameObject(gameName, typeof(MeshFilter), typeof(MeshRenderer), typeof(AtlasMesh));

                var mesh = rootGo.GetComponent<AtlasMesh>();

                mesh.AtlasData = meshData;

                rootGo.GetComponent<MeshRenderer>().hideFlags = HideFlags.HideInInspector;

                rootGo.GetComponent<MeshFilter>().hideFlags = HideFlags.HideInInspector;

                if (meshData.mainTextureUv.Count > 0)
                {
                    mesh.spriteName = meshData.mainTextureUv[0].spriteName;
                }

                PrefabUtility.SaveAsPrefabAsset(rootGo, savePath, out bool success);

                AssetDatabase.SaveAssets();
                //删除
                GameObject.DestroyImmediate(rootGo);
            }
            AssetDatabase.Refresh();

            //AssetDatabase.AddObjectToAsset(mesh, savePath);

            Debug.Log($"<color=pink>导出完成:</color>{savePath}", AssetDatabase.LoadAssetAtPath<GameObject>(savePath));
        }

        private void OnDisable()
        {
            if (previewGameObject != null)
            {
                GameObject.DestroyImmediate(previewGameObject);

                previewGameObject = null;
            }

            DisposePreviewRenderUtility();
        }

        T HandleDragAndDrop<T>(T draggedTex, Rect dropArea, int offsetX = 0, int index = 0) where T : UnityEngine.Object
        {
            GUI.Box(dropArea, draggedTex ? draggedTex.name : "(Texture2D)", EditorStyles.helpBox);

            Event evt = Event.current;

            if (dropArea.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    // 只允许拖入 Texture2D
                    if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is T)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();

                            draggedTex = DragAndDrop.objectReferences[0] as T;

                            Repaint();
                        }
                        evt.Use();
                    }
                }
            }

            // 计算图标在指定区域内的右下角位置
            Rect iconRect = new Rect(

                dropArea.width - TexIcon.width / 2 + 24 + offsetX,  // 区域右边缘 - 图标宽度 - 偏移

                dropArea.height - TexIcon.height / 2 + 28,  // 区域下边缘 - 图标高度 - 偏移

                TexIcon.width / 2,

                TexIcon.height / 2

            );


            // 绘制图标
            if (GUI.Button(iconRect, ""))
            {
                GUIUtility.GetControlID(FocusType.Passive);

                EditorGUIUtility.ShowObjectPicker<T>(draggedTex, true, "", controlID[index]);
            }

            if (GUI.Button(dropArea, "")) EditorGUIUtility.PingObject(draggedTex);

            if (draggedTex)
            {
                AtlasMeshSelector.DrawTiledTexture(dropArea);

                var tex = draggedTex as Texture2D;

                if (tex)
                {
                    GUI.DrawTexture(dropArea, tex, ScaleMode.ScaleToFit);
                }
                else
                {
                    Texture2D tex1 = AssetPreview.GetMiniThumbnail(draggedTex);

                    if (tex1)
                    {
                        GUI.DrawTexture(dropArea, tex1, ScaleMode.ScaleToFit);
                    }
                }
                //GUI.DrawTexture(iconRect, TexIcon, ScaleMode.ScaleToFit);

                dropArea.height = 20;

                GUI.Label(dropArea, draggedTex.name);
            }
            else
            {
                AtlasMeshSelector.DrawTiledTexture(dropArea);

                //GUI.DrawTexture(iconRect, TexIcon, ScaleMode.ScaleToFit);
            }

            var eventType = EditorGUIUtility.GetObjectPickerControlID() == controlID[index] ? EditorGUIUtility.GetObjectPickerControlID() : 0;

            if (eventType != 0 && Event.current.commandName == "ObjectSelectorUpdated")
            {
                var obj = EditorGUIUtility.GetObjectPickerObject() as T;

                if (obj != draggedTex)
                {
                    draggedTex = obj;

                    Repaint();
                }
            }

            return draggedTex;
        }

        //-----------------------预览

        PreviewRenderUtility previewRenderUtility;

        private Texture previewTexture;

        private bool requiresRefresh = true;

        private GameObject previewGameObject;

        private bool isOrthographic;

        private bool isSkybox;

        private bool isLookMesh = true;

        private bool isLookGrid = true;

        Camera PreviewUtilityCamera
        {
            get
            {
                if (previewRenderUtility == null)
                {
                    return null;
                }

#if UNITY_2017_1_OR_NEWER
                return previewRenderUtility.camera;
#else
			return previewRenderUtility.m_Camera;
#endif
            }
        }

        public float frameScale { get; private set; }

        override public bool HasPreviewGUI()
        {
            if (meshData == null) return false;

            return meshData.mainTextureArray != null && meshData.mainTextureUv != null;
        }

        float rotationX = 0;

        //绘制预览区域
        override public void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (EditorApplication.isPlaying)
            {
                if (meshData.clipTexture)
                {
                    GUI.DrawTexture(r, meshData.clipTexture);
                }

                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(30));

                GUI.Label(rect, "Now is Playing");
                return;
            }

            this.InitPreview(r, background);

            this.DrawMeshToolbar(r);

            this.HandleMouseInput(r, (HorizontalX, VerticalY) =>
            {
                if (previewGameObject != null)
                {
                    rotationX += HorizontalX * 360;

                    previewGameObject.transform.eulerAngles = new Vector3(0, rotationX, 0);
                }
                requiresRefresh = true;
            }
            //,s =>{
            //    if (previewGameObject != null)
            //        previewGameObject.transform.localScale += Vector3.one * s;
            //    requiresRefresh = true;
            //}
            );
        }

        override public GUIContent GetPreviewTitle() { return new GUIContent("Preview"); }
        public override void OnPreviewSettings()
        {
            if (previewTex == null) this.PreviewMesh();

            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(25));

            if (GUI.Button(rect, ""))
            {
                this.PreviewMesh();

                requiresRefresh = true;
            }

            GUI.DrawTexture(rect, previewTex, ScaleMode.StretchToFill);


            if (GUILayout.Button(isSkybox ? "Skybox" : "Depth"))
            {
                isSkybox = !isSkybox;

                DisposePreviewRenderUtility();
            }

            if (!isSkybox)
            {
                rect = EditorGUILayout.GetControlRect(GUILayout.Width(30));

                if (GUI.Button(rect, TexEye))
                {
                    isLookMesh = !isLookMesh;

                    requiresRefresh = true;
                }

                rect = EditorGUILayout.GetControlRect(GUILayout.Width(30));

                if (GUI.Button(rect, TexGridY))
                {
                    isLookGrid = !isLookGrid;

                    requiresRefresh = true;
                }
            }

            if (GUILayout.Button(isOrthographic ? "2D" : "3D"))
            {
                isOrthographic = !isOrthographic;

                DisposePreviewRenderUtility();
            }

            const float SliderWidth = 100;

            const float SliderSnap = 0.01f;

            const float SliderMin = 0f;

            const float SliderMax = 1f;

            float timeScale = GUILayout.HorizontalSlider(frameScale, SliderMin, SliderMax, GUILayout.MaxWidth(SliderWidth));

            timeScale = Mathf.RoundToInt(timeScale / SliderSnap) * SliderSnap;

            if (frameScale != timeScale)
            {
                frameScale = timeScale;

                if (previewGameObject != null)
                {
                    previewGameObject.transform.localScale = Vector3.one * (1.5F - frameScale);
                }

                requiresRefresh = true;
            }

        }

        //----------------endregion

        private GameObject CreateGameObject()
        {
            GameObject go = new GameObject("temp", typeof(MeshFilter), typeof(MeshRenderer));

            var mat = new Material(Shader.Find("Custom/AtlasMesh")); //Instantiate(meshData.material);

            m_info = meshData.mainTextureUv[0];

            mat.mainTexture = meshData.mainTextureArray[m_info.Value.index];

            var originalMesh = Instantiate(Resources.GetBuiltinResource<Mesh>(fbxArray[fbxIndex]));

            go.GetComponent<MeshFilter>().sharedMesh = this.ChangeMeshUV(originalMesh, m_info.Value);

            go.GetComponent<MeshRenderer>().material = mat;

            return go;
        }

        private Mesh ChangeMeshUV(Mesh mesh, TextureInfo atlasUV)
        {
            var mergedMesh = mesh;

            List<Vector2> mergedUVs = new List<Vector2>();

            Vector2[] m_srcUVs = mesh.uv;

            var uv = atlasUV.GetRect();

            for (int index = 0; index < mesh.vertexCount; index++)
            {
                Vector2 originalUV = index < m_srcUVs.Length ? m_srcUVs[index] : Vector2.zero;

                Vector2 finalAtlasUV = new Vector2(
                    uv.x + originalUV.x * uv.width,
                    uv.y + originalUV.y * uv.height
                );

                mergedUVs.Add(finalAtlasUV);
            }

            mergedMesh.SetUVs(0, mergedUVs);

            return mergedMesh;
        }

        private Mesh m_originalMesh;

        private TextureInfo? m_info;

        [SerializeField]
        private Texture2D previewTex;

        private static string[] fbxArray = { "Cube.fbx", "New-Sphere.fbx", "New-Capsule.fbx", "New-Cylinder.fbx", "New-Plane.fbx", "Quad.fbx" };

        private int fbxIndex = 0;
        private void PreviewMesh()
        {
            fbxIndex = (++fbxIndex) % fbxArray.Length;

            m_originalMesh = Resources.GetBuiltinResource<Mesh>(fbxArray[fbxIndex]);

            previewTex = AssetPreview.GetAssetPreview(m_originalMesh);

            if (previewGameObject)
            {
                previewGameObject.GetComponent<MeshFilter>().sharedMesh = this.ChangeMeshUV(Instantiate(m_originalMesh), m_info.Value);
            }
        }

        private static Vector2 lastMousePos;

        public void HandleMouseInput(Rect r, Action<float, float> rotateCall, Action<float> scaleCall = null, float rotationSpeed = 0.001f, float scaleSpeed = 0.02f)
        {
            Event current = Event.current;
            // 检查事件是否在预览区域内
            if (!r.Contains(current.mousePosition))
            {
                return;
            }

            int controlID = GUIUtility.GetControlID(SliderHash, FocusType.Passive);

            switch (current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:

                    lastMousePos = current.mousePosition;

                    GUIUtility.hotControl = controlID;

                    current.Use();

                    break;

                //case EventType.MouseUp:

                //    GUIUtility.hotControl = controlID;

                //    current.Use();

                //    break;

                case EventType.ScrollWheel:

                    float scaleDelta = current.delta.y * scaleSpeed;

                    scaleCall?.Invoke(scaleDelta);

                    GUIUtility.hotControl = controlID;

                    current.Use();

                    break;

                case EventType.MouseDrag:
                    Vector2 delta = current.mousePosition - lastMousePos;

                    rotateCall?.Invoke(-delta.x * rotationSpeed, delta.y * rotationSpeed);

                    lastMousePos = current.mousePosition;

                    GUIUtility.hotControl = controlID;

                    current.Use();

                    break;
            }
        }

        public void DoRenderPreview()
        {
            //创建相机渲染
            if (previewRenderUtility == null)
            {
                previewRenderUtility = new PreviewRenderUtility(true);
                {
                    //设置相机
                    Camera c = this.PreviewUtilityCamera;

                    c.orthographic = isOrthographic;

                    c.cullingMask = 1 << 30;

                    c.nearClipPlane = 0.01f;

                    c.farClipPlane = 1000f;

                    c.orthographicSize = 3f;

                    if (c.orthographic)
                    {
                        c.transform.position = new Vector3(0, 0, -10);
                    }
                    else
                    {
                        c.fieldOfView = 60;

                        c.transform.position = new Vector3(2f, 1.6f, 2f);

                        c.transform.rotation = Quaternion.Euler(24, -134, 0f);
                    }

                    if (isSkybox)
                    {
                        //添加天空盒子
                        c.clearFlags = CameraClearFlags.Skybox;

                        Skybox cameraSkybox = c.gameObject.AddComponent<Skybox>();

                        if (cameraSkybox)
                        {
                            cameraSkybox.material = new Material(Shader.Find("Skybox/Procedural"));
                        }

                        //添加灯光
                        Light dirLight = c.gameObject.AddComponent<Light>();

                        dirLight.type = LightType.Directional;

                        dirLight.renderMode = LightRenderMode.Auto;

                        dirLight.shadows = LightShadows.Soft;

                        dirLight.lightmapBakeType = LightmapBakeType.Realtime;

                        dirLight.cullingMask = 1 << 30;

                        dirLight.intensity = 0.5f; // 光照强度

                        dirLight.range = 10;

                        dirLight.color = Color.white; // 灯光颜色
                    }

                }

                if (previewGameObject != null)
                {
                    GameObject.DestroyImmediate(previewGameObject);

                    previewGameObject = null;
                }
            }
            //创建对象渲染
            if (previewGameObject == null)
            {

                try
                {
                    previewGameObject = this.CreateGameObject();

                    if (previewGameObject != null)
                    {
                        previewGameObject.hideFlags = HideFlags.HideAndDontSave;

                        previewGameObject.layer = 30;

                        previewGameObject.GetComponent<Renderer>().enabled = true;

#if (UNITY_2017_4 || UNITY_2018_1_OR_NEWER)

                        previewRenderUtility.AddSingleGO(previewGameObject);

#endif
                    }
                }
                catch (Exception ex)
                {
                    if (previewGameObject != null)
                    {
                        GameObject.DestroyImmediate(previewGameObject);

                        previewGameObject = null;
                    }
                    Debug.Log(ex);
                }

                requiresRefresh = true;

            }
        }

        void InitPreview(Rect r, GUIStyle background)
        {
            this.DoRenderPreview();

            if (Event.current.type == EventType.Repaint)
            {
                if (requiresRefresh)
                {
                    previewRenderUtility.BeginPreview(r, background);

                    Draw3DMesh(this.PreviewUtilityCamera);

                    this.PreviewUtilityCamera.Render();

                    previewTexture = previewRenderUtility.EndPreview();

                    requiresRefresh = false;
                }

                if (previewTexture != null)
                {
                    GUI.DrawTexture(r, previewTexture, ScaleMode.ScaleToFit, false);
                }
            }

        }

        private void DrawCross(Camera camera)
        {
            Handles.SetCamera(camera);

            Handles.color = new Color(0.3f, 0.3f, 0.3f, 1);

            float cl = 10;

            Handles.DrawLine(new Vector3(-cl, 0), new Vector3(cl, 0));

            Handles.DrawLine(new Vector3(0, cl), new Vector3(0, -cl));

        }

        void Draw3DMesh(Camera camera)
        {
            const float gridSize = 30f;

            const float cellSize = 2f;

            const bool zTest = true;

            if (previewGameObject == null)
            {
                return;
            }

            var transform = previewGameObject.transform;

            Handles.SetCamera(camera);

            if (isLookGrid)
            {

                var originalMatrix = Handles.matrix;

                var localRotation = Quaternion.Euler(transform.eulerAngles);

                var localScale = Matrix4x4.Scale(transform.localScale);

                var positionOffset = Matrix4x4.Translate(transform.position);

                Handles.matrix = positionOffset * Matrix4x4.TRS(Vector3.zero, localRotation, Vector3.one) * localScale;

                Handles.zTest = zTest ? UnityEngine.Rendering.CompareFunction.LessEqual : UnityEngine.Rendering.CompareFunction.Always;

                Handles.color = Color.gray;

                for (float x = -gridSize; x <= gridSize; x += cellSize)
                {

                    Vector3 start = new Vector3(x, 0, -gridSize);

                    Vector3 end = new Vector3(x, 0, gridSize);

                    Handles.DrawLine(start, end);
                }

                for (float z = -gridSize; z <= gridSize; z += cellSize)
                {
                    Vector3 start = new Vector3(-gridSize, 0, z);

                    Vector3 end = new Vector3(gridSize, 0, z);

                    Handles.DrawLine(start, end);
                }

                Handles.matrix = originalMatrix;
            }

            if (isLookMesh)
            {
                DrawMeshWireframe(previewGameObject.GetComponent<MeshFilter>().sharedMesh, transform);
            }
        }

        private void DrawMeshWireframe(Mesh mesh, Transform transform)
        {
            Handles.color = Color.cyan;

            int[] triangles = mesh.triangles;

            Vector3[] vertices = mesh.vertices;


            Quaternion rotation = Quaternion.Euler(transform.localEulerAngles); 

            Matrix4x4 scaleMatrix = Matrix4x4.Scale(transform.localScale);


            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];

                int v1 = triangles[i + 1];

                int v2 = triangles[i + 2];


                Vector3 p0 = rotation * scaleMatrix.MultiplyPoint(vertices[v0]);

                Vector3 p1 = rotation * scaleMatrix.MultiplyPoint(vertices[v1]);

                Vector3 p2 = rotation * scaleMatrix.MultiplyPoint(vertices[v2]);

                Handles.DrawLine(p0, p1);

                Handles.DrawLine(p1, p2);

                Handles.DrawLine(p2, p0);
            }
        }

        void DisposePreviewRenderUtility()
        {
            if (previewRenderUtility != null)
            {
                previewRenderUtility.Cleanup();

                previewRenderUtility = null;
            }
        }

        void DrawMeshToolbar(Rect r)
        {
            AtlasDataAsset data = this.meshData;

            string label = meshName != null ? meshName : "default";

            Rect popRect = new Rect(r);

            popRect.y += 4;

            popRect.x += 4;

            popRect.height = 24;

            popRect.width = 80;

            EditorGUI.DropShadowLabel(popRect, "SpriteAtlas");

            popRect.y += 8;

            popRect.width = 160;

            popRect.x += 100;

            var tex = TexMesh;

            if (data.mainTextureUv.Count <= 0)
            {
                return;
            }

            var uv = data.mainTextureUv[0];

            string nameS = string.IsNullOrEmpty(this.spriteName) ? uv.spriteName : this.spriteName;

            if (GUI.Button(popRect, new GUIContent(nameS, TexAtlas), EditorStyles.popup))
            {
                DropDownShow(popRect, data, (info) =>
               {
                   if (info == null)
                       return;

                   this.spriteName = info.Value.spriteName;

                   m_info = info;

                   if (previewGameObject != null)
                   {
                       previewGameObject.GetComponent<MeshFilter>().sharedMesh = this.ChangeMeshUV(Instantiate(m_originalMesh), m_info.Value);
                   }

                   requiresRefresh = true;

                   this.Repaint();
               });
            }

            r.y = r.height - 50;

            this.DrawSprite(r);
        }

        private void DropDownShow(Rect rect, AtlasDataAsset data, UnityAction<TextureInfo?> callback)
        {
            GenericMenu gm = new GenericMenu();

            foreach (var item in data.mainTextureUv)
            {
                gm.AddItem(new GUIContent(item.spriteName), item.spriteName == this.spriteName, x => callback((TextureInfo)x), item);
            }

            gm.DropDown(rect);
        }
    }

    public class BoxScope : System.IDisposable
    {
        readonly bool indent;

        static GUIStyle boxScopeStyle;
        public static GUIStyle BoxScopeStyle
        {
            get
            {
                if (boxScopeStyle == null)
                {
                    boxScopeStyle = new GUIStyle(EditorStyles.helpBox);

                    RectOffset p = boxScopeStyle.padding; // RectOffset is a class

                    p.right += 6;

                    p.top += 1;

                    p.left += 3;
                }

                return boxScopeStyle;
            }
        }

        public BoxScope(bool indent = true)
        {
            this.indent = indent;

            EditorGUILayout.BeginVertical(BoxScopeStyle);

            EditorGUILayout.Space();

            if (indent)
            {
                EditorGUI.indentLevel++;
            }
        }

        public void Dispose()
        {
            if (indent)
            {
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
        }
    }

}
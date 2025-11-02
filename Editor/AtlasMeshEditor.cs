using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace Hukiry.AtlasMesh.Editor
{

    [CustomEditor(typeof(AtlasMesh))]
    [CanEditMultipleObjects]
    public class AtlasMeshEditor : UnityEditor.Editor
    {
        private SerializedProperty m_AtlasData;

        private SerializedProperty m_MeshLabel;

        private SerializedProperty m_MeshName;

        private SerializedProperty m_AnimationName;

        private SerializedProperty m_isLoop;

        private SerializedProperty m_Speed;

        private SerializedProperty m_SpriteName;

        private SerializedProperty m_Color;

        private SerializedProperty m_isPreview;

        private SerializedProperty m_AlphaCutoff;

        private SerializedProperty m_Cutoff;

        //索引动画
        private int selectIndex;

        private GUIContent[] selectOptions;

        private int[] selectValues;

        private GUIContent selectAnimationLabel;

        private AnimBool animShowType;


        //icon
        private static Texture2D TexMesh;

        private static Texture2D ClipTex;

        private static Texture2D SprieTex;

        private static Texture2D TexAtlasData;

        private static GUIContent guiMeshLabel;

        private AtlasMesh atlasInfo;

        private List<string> meshList = new List<string>();

        private bool isAnimation = false;

        private int lastIndex;

        private string lastMeshName;

        private void OnEnable()
        {
            selectAnimationLabel = new GUIContent("Animation Name");

            ClipTex = EditorGUIUtility.ObjectContent(null, typeof(AnimationClip)).image as Texture2D;

            SprieTex = EditorGUIUtility.ObjectContent(null, typeof(Sprite)).image as Texture2D;

            TexMesh = EditorGUIUtility.ObjectContent(null, typeof(Mesh)).image as Texture2D;

            TexAtlasData = AtlasMeshSymbolsForGroup.FindAssetObject<Texture2D>("icon-AtlasDataAsset");

            guiMeshLabel = EditorGUIUtility.IconContent("AssetLabelIcon");

            guiMeshLabel.text = "Mesh Label";

            m_AtlasData = serializedObject.FindProperty(nameof(m_AtlasData));

            m_MeshName = serializedObject.FindProperty(nameof(m_MeshName));

            m_MeshLabel = serializedObject.FindProperty(nameof(m_MeshLabel));

            m_AnimationName = serializedObject.FindProperty(nameof(m_AnimationName));

            m_SpriteName = serializedObject.FindProperty(nameof(m_SpriteName));

            m_isPreview = serializedObject.FindProperty(nameof(m_isPreview));

            m_isLoop = serializedObject.FindProperty(nameof(m_isLoop));

            m_Speed = serializedObject.FindProperty(nameof(m_Speed));

            m_AlphaCutoff = serializedObject.FindProperty(nameof(m_AlphaCutoff));

            m_Cutoff = serializedObject.FindProperty(nameof(m_Cutoff));

            m_Color = serializedObject.FindProperty(nameof(m_Color));


            animShowType = new AnimBool(m_AtlasData.objectReferenceValue && !string.IsNullOrEmpty(m_SpriteName.stringValue));

            animShowType.valueChanged.AddListener(new UnityAction(base.Repaint));

            atlasInfo = target as AtlasMesh;

            LoadAnimationClip();
        }

        private void LoadAnimationClip()
        {
            isAnimation = false;

            selectOptions = null;

            meshList.Clear();

            if (atlasInfo.AtlasData)
            {
                ModelInfo modelInfo = atlasInfo.AtlasData.modelInfos.Find(p => p.spriteName == atlasInfo.meshName);

                if (modelInfo != null)
                {
                    var len = modelInfo.clips.Count;

                    selectOptions = new GUIContent[len];

                    selectValues = new int[len];

                    for (int i = 0; i < len; i++)
                    {
                        var clip = modelInfo.clips[i];

                        selectOptions[i] = new GUIContent(clip.name, ClipTex);

                        selectValues[i] = clip.index;
                    }

                    isAnimation = modelInfo.isAnimation;
                    //m_Speed.floatValue = modelInfo.speed;
                }

                if (atlasInfo.AtlasData.meshLabels.Count == 0)
                {
                    meshList = atlasInfo.AtlasData.modelInfos.Select(p => p.spriteName).ToList();
                }
                else
                {
                    var labelInfo = atlasInfo.AtlasData.meshLabels.Find(p => (int)p.labelType == m_MeshLabel.enumValueIndex);

                    meshList = labelInfo.labels.ToList();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {

                using (new BoxScope())
                {
                    EditorGUILayout.PropertyField(m_AtlasData, new GUIContent("Atlas Data", TexAtlasData));

                    EditorGUILayout.PropertyField(m_MeshLabel, guiMeshLabel);

                    if (lastIndex != m_MeshLabel.enumValueIndex)
                    {
                        lastIndex = m_MeshLabel.enumValueIndex;

                        atlasInfo.meshLabel = (MeshLabelType)m_MeshLabel.enumValueIndex;

                        LoadAnimationClip();
                    }

                    DrawStringPopup(new GUIContent("Mesh Name"), new GUIContent("---", TexMesh), m_MeshName, meshList);

                    if (lastMeshName != m_MeshName.stringValue)
                    {
                        lastMeshName = m_MeshName.stringValue;

                        LoadAnimationClip();
                    }

                    if (isAnimation)
                    {
                        if (selectOptions != null)
                        {
                            selectIndex = selectOptions.ToList().FindIndex(p => p.text == m_AnimationName.stringValue);

                            if (selectIndex < 0) selectIndex = 0;

                            var index = EditorGUILayout.IntPopup(selectAnimationLabel, selectIndex, selectOptions, selectValues);

                            if (index != selectIndex)
                            {
                                selectIndex = index;

                                m_AnimationName.stringValue = selectOptions[index].text;
                            }
                        }

                        EditorGUILayout.Space();

                        using (new BoxScope())
                        {
                            EditorGUILayout.PropertyField(m_isLoop, new GUIContent("Loop", "Animation"));

                            EditorGUILayout.PropertyField(m_Speed, true);

                            if ((MeshLabelType)m_MeshLabel.enumValueIndex == MeshLabelType.Spine)
                            {

                                EditorGUILayout.PropertyField(m_AlphaCutoff);

                                EditorGUILayout.PropertyField(m_Cutoff, true);
                            }
                        }
                    }
                }

                if (changeCheck.changed)
                {
                    LoadAnimationClip();
                }
            }


            animShowType.target = m_AtlasData.objectReferenceValue;

            if (EditorGUILayout.BeginFadeGroup(animShowType.faded))
            {

                EditorGUI.indentLevel++;

                using (new EditorGUI.DisabledGroupScope(!(m_AtlasData.objectReferenceValue)))
                {
                    if (m_AtlasData.objectReferenceValue)
                    {
                        using (new BoxScope())
                        {
                            EditorGUI.indentLevel++;
                            {
                                DrawSpritePopup(m_AtlasData.objectReferenceValue, m_SpriteName, (selectedSpriteName) =>
                                 {
                                     if (selectedSpriteName == null)
                                         return;

                                     MeshDataConfigAsset.ins.selectSpriteName = selectedSpriteName;

                                     serializedObject.FindProperty("m_SpriteName").stringValue = selectedSpriteName;

                                     serializedObject.FindProperty("m_SpriteName").serializedObject.ApplyModifiedProperties();

                                 }, () =>
                                 {


                                     GUIContent label = new GUIContent(m_SpriteName.stringValue, m_SpriteName.displayName);

                                     Rect rectBtn = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));

                                     rectBtn.x += 2;

                                     rectBtn.width = 25;

                                     var texCopy = EditorGUIUtility.IconContent("d_winbtn_win_restore");

                                     texCopy.tooltip = "Copy";

                                     if (GUI.Button(rectBtn, texCopy))
                                     {
                                         GUIUtility.systemCopyBuffer = m_SpriteName.stringValue;
                                     }

                                     rectBtn.x += 25;

                                     rectBtn.width = 25;

                                     var texPaste = EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow");

                                     texPaste.tooltip = "Pase";

                                     if (GUI.Button(rectBtn, texPaste))
                                     {
                                         m_SpriteName.stringValue = GUIUtility.systemCopyBuffer;
                                     }


                                 });
                            }
                            EditorGUI.indentLevel--;

                            EditorGUILayout.PropertyField(m_Color, true);

                            EditorGUILayout.PropertyField(m_isPreview);
                        }
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();
        }

        public static void DrawSpritePopup(Object atlas, SerializedProperty spriteProperty, UnityAction<string> callback, UnityAction drawGui)
        {
            GUIContent label = new GUIContent(spriteProperty.displayName, spriteProperty.tooltip);

            string spriteName = string.IsNullOrEmpty(spriteProperty.stringValue) ? "----" : spriteProperty.stringValue;

            using (new EditorGUI.DisabledGroupScope(!atlas))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(label);

                    if (GUILayout.Button(new GUIContent(string.IsNullOrEmpty(spriteName) ? "-" : spriteName, SprieTex), "minipopup") && atlas)
                    {
                        AtlasMeshSelector.Show(atlas, spriteName, 0, callback);
                    }

                    drawGui();
                }
            }
        }

        public static void DrawMeshDataPopup<T>(GUIContent label, GUIContent nullLabel, SerializedProperty property, UnityAction<T> onSelect = null, params GUILayoutOption[] option) where T : UnityEngine.Object
        {
            DrawMeshDataPopup(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.popup, option), label, nullLabel, property.objectReferenceValue as T, obj =>
            {
                if (property != null)
                {
                    property.objectReferenceValue = obj;

                    onSelect?.Invoke(obj);

                    property.serializedObject.ApplyModifiedProperties();
                }
            });
        }

        private static void DrawMeshDataPopup<T>(Rect rect, GUIContent label, GUIContent nullLabel, T atlas, UnityAction<T> onSelect = null) where T : UnityEngine.Object
        {
            rect = EditorGUI.PrefixLabel(rect, label);

            if (GUI.Button(rect, atlas ? new GUIContent(atlas.name) : nullLabel, EditorStyles.popup))
            {
                GenericMenu gm = new GenericMenu();

                gm.AddItem(nullLabel, !atlas, () => onSelect(null));

                foreach (string path in AssetDatabase.FindAssets("t:" + typeof(T).Name).Select(x => AssetDatabase.GUIDToAssetPath(x)))
                {
                    string displayName = Path.GetFileNameWithoutExtension(path);

                    gm.AddItem(
                      new GUIContent(displayName),
                      atlas && (atlas.name == displayName),
                      x =>
                      {
                          MeshDataConfigAsset.ins.lastMeshDataPath = (string)x;

                          onSelect(x == null ? null : AssetDatabase.LoadAssetAtPath((string)x, typeof(T)) as T);
                      },
                      path
                  );

                }

                gm.DropDown(rect);
            }
        }

        private static void DrawStringPopup(GUIContent label, GUIContent nullLabel, SerializedProperty property, List<string> atlas)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.popup);
            rect = EditorGUI.PrefixLabel(rect, label);

            if (GUI.Button(rect, !string.IsNullOrEmpty(property.stringValue) ? new GUIContent(property.stringValue, TexMesh) : nullLabel, EditorStyles.popup))
            {
                GenericMenu gm = new GenericMenu();

                gm.AddItem(nullLabel, (atlas == null || atlas.Count == 0), () =>
                {
                    property.stringValue = null;
                    property.serializedObject.ApplyModifiedProperties();

                });

                foreach (string displayName in atlas)
                {

                    gm.AddItem(
                      new GUIContent(displayName), (property.stringValue == displayName),
                      x =>
                      {
                          property.stringValue = (string)x;
                          property.serializedObject.ApplyModifiedProperties();
                      }, displayName);

                }

                gm.DropDown(rect);
            }
        }
    }

    [InitializeOnLoad]
    public class AtlasMeshSymbolsForGroup
    {
        public static Dictionary<int, DefineInfo> defineSymbols = new Dictionary<int, DefineInfo>();
        static AtlasMeshSymbolsForGroup()
        {
            var obj = FindAssetObject<MonoScript>("SkeletonAnimation");

            defineSymbols[1] = new DefineInfo() { isEnable = obj != null, symbols = "SPINE" };

            SymbolsForGroup();

        }

        [System.Serializable]
        public class DefineInfo
        {
            public string symbols;

            public bool isEnable;
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

        private static void SymbolsForGroup()
        {
            BuildTargetGroup curBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            string existSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(curBuildTargetGroup);

            foreach (var item in defineSymbols.Values)
            {
                if (item.isEnable)
                {
                    if (!existSymbols.Contains(item.symbols))
                    {
                        if (string.IsNullOrEmpty(existSymbols))
                            existSymbols = item.symbols;
                        else
                            existSymbols += ";" + item.symbols;
                    }
                }
                else
                {
                    if (existSymbols.Contains(item.symbols))
                    {
                        existSymbols = existSymbols.Replace(";" + item.symbols, "");

                        if (existSymbols.Contains(item.symbols))
                        {
                            existSymbols = existSymbols.Replace(item.symbols, "");
                        }
                    }
                }
            }
            //string.Join(";", defineSymbols.ToArray())
            PlayerSettings.SetScriptingDefineSymbolsForGroup(curBuildTargetGroup, existSymbols);
        }
    }
}
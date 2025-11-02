using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.U2D;
using UnityObject = UnityEngine.Object;

namespace Hukiry.AtlasMesh.Editor
{
    public delegate void OnMeshSelected(string spriteName);

    //窗口选择器
    public class AtlasMeshSelector : ScriptableWizard
    {
        static public AtlasMeshSelector spriteSelector;

        public static void Show<T>(T spriteAtlas, string defaultSpriteName,int index, UnityAction<string> callback) where T : UnityEngine.Object
        {
            if (spriteSelector != null)
            {
                spriteSelector.Close();

                spriteSelector = null;
            }
            AtlasMeshSelector selector = ScriptableWizard.DisplayWizard<AtlasMeshSelector>("Select a Texture");

            if (index == 0)
            {
                selector.atlas = spriteAtlas as AtlasDataAsset;
            }
            else
            {
                selector.spriteAtlas = spriteAtlas as SpriteAtlas;
            }

            selector.mSelectedSpriteName = defaultSpriteName;

            selector.mSpriteSeletectedCallback = callback;
        }

        private AtlasDataAsset atlas = null;

        private SpriteAtlas spriteAtlas = null;

        private UnityEngine.Object mSelectedSprite;

        private string mSelectedSpriteName = "";

        private UnityAction<string> mSpriteSeletectedCallback;

        private Vector2 mScrollPos = Vector2.zero;

        private float mClickTime = 0f;

        private string mSearchText = "", lastSearchText;

        void OnEnable() { spriteSelector = this; }

        void OnDisable() { spriteSelector = null; }

        static Texture2D mBackdropTex;

        private TextureInfo[] spritesUV;

        void OnGUI()
        {

            EditorGUIUtility.labelWidth = 80f;

            if (atlas == null&& spriteAtlas==null)
            {
                GUILayout.Label("No Atlas selected.");
            }
            else
            {
                bool close = false;
                var descName = atlas != null ? atlas.name : spriteAtlas.name;
                if (spritesUV != null) GUILayout.Label(descName + "  [" + spritesUV.Length + "]", "LODLevelNotifyText");

                else GUILayout.Label(descName + " Meshs", "LODLevelNotifyText");

                GUILayout.Space(12f);

                if (Event.current.type == EventType.Repaint)
                {
                    GUI.color = new Color(0f, 0f, 0f, 0.25f);

                    GUI.DrawTexture(new Rect(0f, GUILayoutUtility.GetLastRect().yMin + 6f, Screen.width, 4f), blankTexture);

                    GUI.DrawTexture(new Rect(0f, GUILayoutUtility.GetLastRect().yMin + 6f, Screen.width, 1f), blankTexture);

                    GUI.DrawTexture(new Rect(0f, GUILayoutUtility.GetLastRect().yMin + 9f, Screen.width, 1f), blankTexture);

                    GUI.color = Color.white;
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(84f);

                    mSearchText = EditorGUILayout.TextField("", mSearchText, "SearchTextField");

                    if (GUILayout.Button("", "SearchCancelButton", GUILayout.Width(18f)))
                    {
                        mSearchText = "";

                        MeshDataConfigAsset.ins.searchSpriteName = mSearchText;

                        GUIUtility.keyboardControl = 0;
                    }
                    GUILayout.Space(84f);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10f);

                if (lastSearchText != mSearchText)
                {
                    lastSearchText = mSearchText;

                    this.ResetSpriteUv();

                    if (spritesUV == null) return;

                    List<TextureInfo> searchSprites = new List<TextureInfo>();

                    Array.ForEach(spritesUV, (sprite) =>
                    {
                        if (sprite.spriteName != null && sprite.spriteName.IndexOf(mSearchText) >= 0)
                        {
                            searchSprites.Add(sprite);
                        }
                    });

                    spritesUV = searchSprites.ToArray();

                }

                if (spritesUV == null) return;

                List<TextureInfo> spriteuvList = new List<TextureInfo>();

                spriteuvList = spritesUV.Where(p => p.spriteName != null).ToList();

                if (spriteuvList.Count > 1)
                {
                    spriteuvList.Sort((a, b) => a.spriteName.CompareTo(b.spriteName));
                }

                float size = 80f;

                float padded = size + 10f;

                int columns = Mathf.FloorToInt(Screen.width / padded);

                if (columns < 1) columns = 1;

                int offset = 0;
                Rect rect = new Rect(10f, 0, size, size);

                int Count = spriteuvList.Count;

                mScrollPos = GUILayout.BeginScrollView(mScrollPos);
                {
                    int rows = 1;

                    while (offset < Count)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            int col = 0;

                            rect.x = 10f;

                            for (; offset < Count; ++offset)
                            {
                                TextureInfo spriteUV = spriteuvList[offset];

                                if (spriteUV.spriteName == null) continue;

                                if (GUI.Button(rect, ""))
                                {
                                    if (Event.current.button == 0)
                                    {
                                        float delta = Time.realtimeSinceStartup - mClickTime;

                                        mClickTime = Time.realtimeSinceStartup;

                                        if (mSelectedSpriteName != spriteUV.spriteName)
                                        {
                                            if (mSelectedSprite != null)
                                            {
                                                RegisterUndo("Mesh Selection", mSelectedSprite);
                                            }

                                            mSelectedSpriteName = spriteUV.spriteName;

                                            mSpriteSeletectedCallback?.Invoke(spriteUV.spriteName);

                                            Repaint();


                                        }
                                        else if (delta < 0.5f)
                                        {
                                            close = true;
                                        }
                                    }
                                }

                                if (Event.current.type == EventType.Repaint)
                                {
                                    // On top of the button we have a checkboard grid
                                    DrawTiledTexture(rect, backdropTexture);

                                    if (spriteUV.textureRect == Rect.zero)
                                    {
                                        //draw static atlas
                                        var sprite = this.spriteAtlas.GetSprite(spriteUV.spriteName);

                                        Vector4 uv4 = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);

                                        Rect uv = new Rect(uv4.x, uv4.y, uv4.z - uv4.x, uv4.w - uv4.y);

                                        GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
                                    }
                                    else if(spriteUV.spriteName!=null)
                                    {
                                        GUI.DrawTextureWithTexCoords(rect, atlas.mainTextureArray[spriteUV.index], spriteUV.GetRect());
                                    }

                                    // Draw the selection
                                    if (mSelectedSpriteName == spriteUV.spriteName)
                                    {
                                        DrawOutline(rect, new Color(0.4f, 1f, 0f, 1f));
                                    }

                                }

                                GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);

                                GUI.contentColor = new Color(1f, 1f, 1f, 0.7f);

                                GUI.Label(new Rect(rect.x, rect.y + rect.height, rect.width, 24f), spriteUV.spriteName, "ProgressBarBack");

                                GUI.contentColor = Color.white;

                                GUI.backgroundColor = Color.white;

                                col++;

                                if (col >= columns)
                                {
                                    ++offset;

                                    break;
                                }
                                rect.x += padded;
                            }
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.Space(padded);

                        rect.y += padded + 26;

                        ++rows;
                    }
                    GUILayout.Space(rows * 26);
                }
                GUILayout.EndScrollView();

                if (close)
                {
                    Close();
                }
            }
        }

        private void ResetSpriteUv()
        {

            if (this.spriteAtlas != null)
            {
                Sprite[] sprites = new Sprite[this.spriteAtlas.spriteCount];

                this.spriteAtlas.GetSprites(sprites);

                spritesUV = new TextureInfo[this.spriteAtlas.spriteCount];

                for (int i = 0; i < this.spriteAtlas.spriteCount; i++)
                {
                    var sp = sprites[i];

                    Rect uvRect = new Rect(
                        Mathf.Min(sp.uv[0].x, sp.uv[1].x, sp.uv[2].x, sp.uv[3].x),
                        Mathf.Min(sp.uv[0].y, sp.uv[1].y, sp.uv[2].y, sp.uv[3].y),
                        Mathf.Max(sp.uv[0].x, sp.uv[1].x, sp.uv[2].x, sp.uv[3].x) -
                        Mathf.Min(sp.uv[0].x, sp.uv[1].x, sp.uv[2].x, sp.uv[3].x),
                        Mathf.Max(sp.uv[0].y, sp.uv[1].y, sp.uv[2].y, sp.uv[3].y) -
                        Mathf.Min(sp.uv[0].y, sp.uv[1].y, sp.uv[2].y, sp.uv[3].y)
                    );

                    spritesUV[i] = new TextureInfo()
                    {
                        spriteName = sp.name.Replace("(Clone)", ""),

                        textureRect = Rect.zero
                    };
                }
            }
            else
            {
                spritesUV = atlas.mainTextureUv.ToArray();
            }
        }


        static public void RegisterUndo(string name, params UnityObject[] objects)
        {
            if (objects != null && objects.Length > 0)
            {
                UnityEditor.Undo.RecordObjects(objects, name);

                foreach (UnityObject obj in objects)
                {
                    if (obj == null)
                        continue;

                    EditorUtility.SetDirty(obj);
                }
            }
        }

        static public void DrawTiledTexture(Rect rect)
        {
            DrawTiledTexture(rect, backdropTexture);
        }

        static public void DrawTiledTexture(Rect rect, Texture tex)
        {
            GUI.BeginGroup(rect);
            {
                int width = Mathf.RoundToInt(rect.width);

                int height = Mathf.RoundToInt(rect.height);

                for (int y = 0; y < height; y += tex.height)
                {
                    for (int x = 0; x < width; x += tex.width)
                    {
                        GUI.DrawTexture(new Rect(x, y, tex.width, tex.height), tex);
                    }
                }
            }
            GUI.EndGroup();
        }

        static public Rect ConvertToTexCoords(Rect rect, int width, int height)
        {
            Rect final = rect;

            if (width != 0f && height != 0f)
            {
                final.xMin = rect.xMin / width;

                final.xMax = rect.xMax / width;

                final.yMin = 1f - rect.yMax / height;

                final.yMax = 1f - rect.yMin / height;
            }
            return final;
        }

        static public Texture2D blankTexture
        {
            get
            {
                return EditorGUIUtility.whiteTexture;
            }
        }

        static public Texture2D backdropTexture
        {
            get
            {
                if (mBackdropTex == null) mBackdropTex = CreateCheckerTex(
                    new Color(0.1f, 0.1f, 0.1f, 0.5f),

                    new Color(0.2f, 0.2f, 0.2f, 0.5f));
                return mBackdropTex;
            }
        }

        static Texture2D CreateCheckerTex(Color c0, Color c1)
        {
            Texture2D tex = new Texture2D(16, 16);

            tex.name = "[Generated] Checker Mesh";

            tex.hideFlags = HideFlags.DontSave;

            for (int y = 0; y < 8; ++y) for (int x = 0; x < 8; ++x) tex.SetPixel(x, y, c1);
            for (int y = 8; y < 16; ++y) for (int x = 0; x < 8; ++x) tex.SetPixel(x, y, c0);
            for (int y = 0; y < 8; ++y) for (int x = 8; x < 16; ++x) tex.SetPixel(x, y, c0);
            for (int y = 8; y < 16; ++y) for (int x = 8; x < 16; ++x) tex.SetPixel(x, y, c1);

            tex.Apply();

            tex.filterMode = FilterMode.Point;

            return tex;
        }

        static public void DrawOutline(Rect rect, Color color)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Texture2D tex = blankTexture;

                GUI.color = color;

                GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 1f, rect.height), tex);

                GUI.DrawTexture(new Rect(rect.xMax, rect.yMin, 1f, rect.height), tex);

                GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 1f), tex);

                GUI.DrawTexture(new Rect(rect.xMin, rect.yMax, rect.width, 1f), tex);

                GUI.color = Color.white;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
namespace Hukiry.AtlasMesh.Editor
{
    public class AnimationModelPackUtil
    {
        const int TextureNumber = 20;

        public static void PackTextureSpriteAtlas(AtlasDataAsset animationData)
        {
            List<Texture2D> mainTextureArray = animationData.mainTextureArray;

            var saveSkinDir = AssetDatabase.GetAssetPath(animationData);

            string atlasPath = saveSkinDir.Split(".")[0] + ".spriteatlas";

            SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);

            PackSpriteAtlas(atlasPath, ref spriteAtlas, mainTextureArray);
        }

        public static void PackAllAnimationData(string saveSkinDir, string Name)
        {
            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayProgressBar("pack SpriteAtlas", "begin...", 0.1f);

            if (!Directory.Exists(saveSkinDir)) Directory.CreateDirectory(saveSkinDir);

            var animationList = FindAssetsObject<AnimationDataAsset>();


            List<Texture2D> mainTextureArray = new List<Texture2D>();

            List<Texture2D> clipTexture = new List<Texture2D>();

            List<MeshLabel> meshLabels = new List<MeshLabel>();

            List<ModelInfo> modelInfos = new List<ModelInfo>();

            foreach (var item in animationList)
            {
                if (item.clips.Count > 0)
                {
                    clipTexture.Add(item.clipTexture);
                }

                var importPath = AssetDatabase.GetAssetPath(item.mainTexture);

                ImportTexture2D(importPath);

                if (item.mainTexture == null)
                {
                    Debug.LogError($"Die mainTexture{item.name} ist nicht null", item);
                    continue;
                }

                mainTextureArray.Add(item.mainTexture);

                modelInfos.Add(new ModelInfo()
                {
                    isAnimation = item.isAnimation,

                    isSpine = item.isSpine,

                    mesh = item.mesh,

                    spriteName = item.name,

                    speed = item.speed,

                    clips = item.clips.ToList()
                });
            }

            EditorUtility.DisplayProgressBar("pack SpriteAtlas", "begin...", 0.5f);

            List<Texture2D> textureArray = new List<Texture2D>();

            List<TextureInfo> mainTextureUv = new List<TextureInfo>();

            string atlasPath = Path.Combine(saveSkinDir, Name + ".spriteatlas");

            SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);

            bool isFinsih = PackSpriteAtlas(atlasPath, ref spriteAtlas, mainTextureArray);

            spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);

            EditorUtility.DisplayProgressBar("pack SpriteAtlas", "waitting...", 0.9f);

            if (EditorApplication.isPlaying)
            {
                mainTextureUv = ExportAtlasTextureAsPNG(saveSkinDir, spriteAtlas);

                AssetDatabase.Refresh();

                for (int i = 0; i <= TextureNumber; i++)
                {
                    var saveTexturePath = Path.Combine(saveSkinDir, $"{ Name}_texture{i}.png");

                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(saveTexturePath);

                    if (tex)
                    {
                        TextureImporter importer = TextureImporter.GetAtPath(saveTexturePath) as TextureImporter;

                        importer.isReadable = true;

                        EditorUtility.SetDirty(importer);

                        textureArray.Add(tex);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                Debug.Log("<color=yellow>du musst auf isPlaying die Texture2D exporten</color>", spriteAtlas);
            }

            Texture2D clipTex;

            AnimationAtlasUtil.MergeTexture(clipTexture, out clipTex, out List<TextureInfo> clipTextureUv);

            var saveClipPath = Path.Combine(saveSkinDir, $"{ Name}_clip.asset");

            clipTex.name = $"{Name}_clip";

            AssetDatabase.CreateAsset(clipTex, saveClipPath);

            AssetDatabase.Refresh();

            var savePath = Path.Combine(saveSkinDir, $"{Name}.asset");

            AtlasDataAsset atlasData = AssetDatabase.LoadAssetAtPath<AtlasDataAsset>(savePath);

            if (atlasData == null)
            {
                atlasData = AtlasDataAsset.CreateInstance<AtlasDataAsset>();

                AssetDatabase.CreateAsset(atlasData, savePath);
            }

            MeshLabel labelSpine = new MeshLabel() { labelType = MeshLabelType.Spine, labels = new List<string>() };

            MeshLabel labelAnimation = new MeshLabel() { labelType = MeshLabelType.AnimationModel, labels = new List<string>() };

            MeshLabel labelStatic = new MeshLabel() { labelType = MeshLabelType.StaticModel, labels = new List<string>() };

            foreach (var item in modelInfos)
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

            meshLabels.Add(labelAnimation);

            meshLabels.Add(labelStatic);

            meshLabels.Add(labelSpine);

            atlasData.material = FindAssetObject<Material>("MeshRenderer");

            if (textureArray.Count > 0)
                atlasData.mainTextureArray = textureArray;
            if (mainTextureUv.Count > 0)
                atlasData.mainTextureUv = mainTextureUv;

            atlasData.clipTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(saveClipPath);

            atlasData.clipTextureUv = clipTextureUv;

            atlasData.meshLabels = meshLabels;

            atlasData.modelInfos = modelInfos;

            EditorUtility.SetDirty(atlasData);

            AssetDatabase.SaveAssets();

            EditorUtility.ClearProgressBar();
        }

        private static void ImportTexture2D(string importPath)
        {
            TextureImporter importer = TextureImporter.GetAtPath(importPath) as TextureImporter;

            TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings("DefaultTexturePlatform");
#if UNITY_ANDROID
                if (importer.textureType != TextureImporterType.Sprite) continue;

                importer.name = "Android";

                importer.androidETC2FallbackOverride = AndroidETC2FallbackOverride.UseBuildSettings;

                platformSettings.format = TextureImporterFormat.ASTC_6x6;

                platformSettings.overridden = true;

#elif UNITY_IOS || UNITY_IPHONE
                importer.name = "iPhone";

                platformSettings.format = TextureImporterFormat.ASTC_RGBA_6x6;

                platformSettings.overridden = true;

#elif UNITY_WEBGL && UNITY_2021_3_OR_NEWER
                importer.name = "WebGL";

                platformSettings.name = "WebGL";

                platformSettings.format = TextureImporterFormat.ASTC_8x8;

                platformSettings.overridden = true;
#endif
            BindingFlags bindingFlags = BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic;

            PropertyInfo propertyInfo = importer.GetType().GetProperty("settings", bindingFlags);

            TextureImporterSettings textureImporterSettings = (TextureImporterSettings)propertyInfo.GetValue(importer);

            if (textureImporterSettings.spriteMeshType == SpriteMeshType.Tight)
            {
                textureImporterSettings.textureType = TextureImporterType.Sprite;

                textureImporterSettings.spriteMode = (int)SpriteImportMode.Single;

                textureImporterSettings.spritePixelsPerUnit = 100;

                textureImporterSettings.spriteMeshType = SpriteMeshType.FullRect;

                textureImporterSettings.sRGBTexture = true;

                textureImporterSettings.alphaSource = TextureImporterAlphaSource.FromInput;

                textureImporterSettings.alphaIsTransparency = true;

                textureImporterSettings.wrapMode = TextureWrapMode.Clamp;

                textureImporterSettings.filterMode = FilterMode.Bilinear;

                textureImporterSettings.ApplyTextureType(TextureImporterType.Sprite);
                //textureImporterSettings.aniso = 0;
                propertyInfo.SetValue(importer, textureImporterSettings);


                importer.maxTextureSize = 2048;

                platformSettings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;

                platformSettings.format = TextureImporterFormat.Automatic;

                platformSettings.compressionQuality = 50;

                //importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(platformSettings);

                EditorUtility.SetDirty(importer);
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

        private static bool PackSpriteAtlas(string atlasPath, ref SpriteAtlas atlas, List<Texture2D> texture2s)
        {
            if (atlas == null)
            {
                atlas = new SpriteAtlas();

                SpriteAtlasPackingSettings packingSettings = atlas.GetPackingSettings();

                packingSettings.padding = 2;

                packingSettings.enableTightPacking = true;

                packingSettings.enableRotation = false;

                atlas.SetPackingSettings(packingSettings);

                SpriteAtlasTextureSettings textureSettings = atlas.GetTextureSettings();

                textureSettings.readable = true;

                textureSettings.sRGB = true;

                textureSettings.filterMode = FilterMode.Bilinear;

                atlas.SetTextureSettings(textureSettings);


                TextureImporterPlatformSettings setPlatformSettings = atlas.GetPlatformSettings("DefaultTexturePlatform");

                setPlatformSettings.maxTextureSize = 2048;

                setPlatformSettings.format = TextureImporterFormat.RGBA32;

                atlas.SetPlatformSettings(setPlatformSettings);

                EditorUtility.SetDirty(atlas);

                atlas.Add(texture2s.ToArray());

                AssetDatabase.CreateAsset(atlas, atlasPath);
            }
            else
            {
                atlas.Remove(atlas.GetPackables());

                atlas.Add(texture2s.ToArray());
            }

            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();

            return true;
        }

        //导出纹理+uv
        private static List<TextureInfo> ExportAtlasTextureAsPNG(string savePath, SpriteAtlas atlas)
        {
            List<TextureInfo> textureInfos = new List<TextureInfo>();

            Dictionary<string, List<TextureInfo>> temp = new Dictionary<string, List<TextureInfo>>();

            Sprite[] sprites = new Sprite[atlas.spriteCount];

            atlas.GetSprites(sprites);

            var array = SpriteAtlasToTexture(atlas);

            //收集uv数据
            foreach (var item in sprites)
            {
                if (!temp.ContainsKey(item.texture.name))
                {
                    temp[item.texture.name] = new List<TextureInfo>();
                }

                temp[item.texture.name].Add(new TextureInfo()
                {
                    spriteName = item.name.Replace("_texture(Clone)", ""),

                    textureRect = item.textureRect,

                    uv = GetSpriteTextureUV(item),
                });
            }

            if (array == null || array.Length == 0)
            {
                return textureInfos;
            }

            int Length = array.Length;

            for (int i = 0; i < Length; i++)
            {
                var atlasTexture = array[i];

                //校正纹理索引
                int index = i;

                foreach (var item in temp)
                {
                    if (item.Key == atlasTexture.name)
                    {
                        int Count = item.Value.Count;

                        for (int j = 0; j < Count; j++)
                        {
                            TextureInfo info = item.Value[j];

                            info.index = index;

                            item.Value[j] = info;
                        }
                        break;
                    }
                }

                if (atlasTexture.width < 2048 || atlasTexture.height < 2048)
                {
                    atlasTexture = FixTexture2D2048(atlasTexture);
                }
                var saveTexPath = Path.Combine(savePath, $"{ atlas.name}_texture{i}.png");

                File.WriteAllBytes(saveTexPath, atlasTexture.EncodeToPNG());

                Debug.Log($"图集纹理已导出至：{savePath}");
            }


            foreach (var item in temp.Values)
            {
                textureInfos.AddRange(item);
            }

            return textureInfos;
        }

        private static Texture2D FixTexture2D2048(Texture2D tex)
        {
            Texture2D temp = new Texture2D(2048, 2048, TextureFormat.RGBA32, false);

            temp.filterMode = FilterMode.Bilinear;

            temp.SetPixels(0, 0, tex.width, tex.height, tex.GetPixels());

            temp.Apply();

            return temp;
        }

        //uv坐标 0~1 sprite.textureRect =真实像素的尺寸
        private static Vector4 GetSpriteTextureUV(Sprite sprite)
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

            return new Vector4(uvRect.x, uvRect.y, uvRect.width, uvRect.height);
        }

        private static T FindAssetObject<T>(string fileName) where T : UnityEngine.Object
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

        private static List<T> FindAssetsObject<T>() where T : UnityEngine.Object
        {
            var assetPathArray = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}").Select(x => UnityEditor.AssetDatabase.GUIDToAssetPath(x));

            List<T> temp = new List<T>();

            foreach (var path in assetPathArray)
            {
                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);

                temp.Add(item);
            }
            return temp;
        }

    }
}
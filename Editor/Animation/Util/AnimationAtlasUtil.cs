using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Hukiry.AtlasMesh.Editor
{
    public class AnimationAtlasUtil
    {
        static List<TexturePacker.RectInfo> infoList = new List<TexturePacker.RectInfo>();

        public static void MergeTexture(List<Texture2D> meshTexList, out Texture2D clipTexture, out List<TextureInfo> textureInfos)
        {
            infoList.Clear();

            int len = meshTexList.Count;

            for (int i = 0; i < len; i++)
            {
                if (meshTexList[i])
                {
                    infoList.Add(new TexturePacker.RectInfo
                    {
                        width = meshTexList[i].width,

                        height = meshTexList[i].height,

                        name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(meshTexList[i].GetInstanceID())),

                        tex = meshTexList[i]
                    });
                }
            }

            clipTexture = new Texture2D(1, 1);

            textureInfos = new List<TextureInfo>();

            if (AtlasCore.ins.PackTextures(out TexturePacker.RectInfo mainTex, infoList))
            {
                Texture2D matrixTexture = new Texture2D(mainTex.width, mainTex.height, TextureFormat.RGBAHalf, false);

                matrixTexture.filterMode = FilterMode.Point;

                for (int i = 0; i < infoList.Count; i++)
                {
                    var texArray = infoList[i].tex.GetPixels();

                    var info = infoList[i];

                    matrixTexture.SetPixels(info.x, info.y, info.width, info.height, texArray);
                }
                matrixTexture.Apply();

                clipTexture = matrixTexture;

                foreach (var item in infoList)
                {
                    var spriteName = item.name.Replace("_clip", "");

                    textureInfos.Add(new TextureInfo
                    {
                        spriteName = spriteName,

                        textureRect = new Rect(item.x, item.y, item.width, item.height),

                        index = 0
                    });
                }
            }
        }
    }
}
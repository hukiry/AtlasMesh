using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Hukiry.AtlasMesh
{
    [CreateAssetMenu(fileName = "New AtlasData", menuName = "Hukiry/AtlasMesh/AtlasData")]
    public class AtlasDataAsset : ScriptableObject
    {
        [SerializeField]
        public Material material;

        [Header("----------------Main Texture")]
        [SerializeField]
        public List<Texture2D> mainTextureArray = new List<Texture2D>();

        [SerializeField]
        public List<TextureInfo> mainTextureUv = new List<TextureInfo>();

        [Header("----------------Clip Texture")]
        [SerializeField]
        public Texture2D clipTexture;

        [SerializeField]
        public List<TextureInfo> clipTextureUv = new List<TextureInfo>();

        [Header("----------------Mesh Data")][SerializeField]
        public List<MeshLabel> meshLabels = new List<MeshLabel>();

        [SerializeField]
        public List<ModelInfo> modelInfos = new List<ModelInfo>();

        [HideInInspector]
        public int spriteAtlasID;

        [HideInInspector]
        public string spriteAtlasName;

        [System.NonSerialized]
        private Dictionary<string, TextureInfo> dicClip = new Dictionary<string, TextureInfo>();

        public Vector2 GetClipRect(string spriteName)
        {
            if (dicClip.Count <= 0)
            {
                dicClip = clipTextureUv.ToDictionary(p => p.spriteName);
            }

            if (dicClip.ContainsKey(spriteName))
            {
                if (string.IsNullOrEmpty(dicClip[spriteName].spriteName))
                {
                    dicClip = clipTextureUv.ToDictionary(p => p.spriteName);
                }

                var v = dicClip[spriteName].GetClipVector4(); 

                return new Vector2(v.x, v.y);
            }

            return new Vector2(0, 0);
        }

        [System.NonSerialized]
        private Dictionary<string, ModelInfo> dicModel = new Dictionary<string, ModelInfo>();
        public Vector4 GetAnimationFrame(string spriteName, string aniName, float speed)
        {
            if (dicModel.Count <= 0)
            {
                dicModel = modelInfos.ToDictionary(p => p.spriteName);
            }

            if (dicModel.ContainsKey(spriteName))
            {
                if (string.IsNullOrEmpty(dicModel[spriteName].spriteName))
                {
                    dicModel = modelInfos.ToDictionary(p => p.spriteName);
                }

                return dicModel[spriteName].GetAnimationUv(aniName, speed);
            }

            return new Vector4(0, 30, speed, 1);
        }

        public ModelInfo GetModelInfo(string spriteName)
        {
            GetAnimationFrame(spriteName, "", 0);

            if (dicModel.ContainsKey(spriteName))
            {
                return dicModel[spriteName];
            }

            return null;
        }
    }

    //每一个模型动画 和 网格
    [System.Serializable]
    public class ModelInfo {
        public string spriteName;//ist same texture Name and clip Name

        public Mesh mesh;

        public float speed = 10;//default speed

        public bool isAnimation = true; 

        public bool isSpine;
     
        public List<ClipInfo> clips = new List<ClipInfo>();

        [System.NonSerialized]
        private Dictionary<string, ClipInfo> dicInfo = null;

        //auf die material Dynamic setting
        public Vector4 GetAnimationUv(string aniName, float speed)
        {
            if (dicInfo == null) dicInfo = clips.ToDictionary(p => p.name);

            if (dicInfo.ContainsKey(aniName))
            {
                ClipInfo info = dicInfo[aniName];

                return new Vector4(info.startFrame, info.frameCount, speed, isAnimation ? 1 : 0);
            }
            return new Vector4(0, 30, speed, isAnimation ? 1 : 0);
        }
    }

    public enum MeshLabelType
    {
        AnimationModel,

        StaticModel,

        Spine,
    }

    [System.Serializable]
    public struct TextureInfo
    {
        public string spriteName;
        /// <summary>
        ///  uv纹理坐标0~1之间
        ///  <list>不使用，仅查看</list>
        ///  <list>不使用，后面还需要修复</list>
        /// </summary>
        public Vector4 uv;
        /// <summary>
        ///  real pixel size
        /// </summary>
        public Rect textureRect;

        public int index;//texture index

        public Vector4 GetClipVector4()
        {
            return new Vector4(textureRect.x, textureRect.y, textureRect.width, textureRect.height);
        }
        //0~1
        public Vector4 GetSkinVector4()
        {
            return new Vector4(textureRect.x / 2048f, textureRect.y / 2048f, textureRect.width / 2048f, textureRect.height / 2048f);
        }

        //0~1
        public Rect GetRect()
        {
            return new Rect(textureRect.x/2048f, textureRect.y / 2048f, textureRect.width / 2048f, textureRect.height / 2048f);
        }
    }

    [System.Serializable]
    public struct MeshLabel
    {
        public MeshLabelType labelType;

        [SerializeField]
        public List<string> labels;
    }


    [System.Serializable]
    public class ClipInfo
    {
        public string name;

        public int startFrame;

        public int frameCount;

        [HideInInspector] public int index;//clip index

        [System.NonSerialized]
        [HideInInspector]
        public AnimationClip clip;
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Hukiry.AtlasMesh
{
    [System.Serializable]
    public class AnimationDataAsset : ScriptableObject
    {
        [Tooltip("Animation Texture")]
        public Texture2D clipTexture;

        public Mesh mesh;

        [Tooltip("Model Main Texture")]
        public Texture2D mainTexture;

        //has Animation
        public bool isAnimation;

        public bool isSpine;

        public float speed = 10;

        public List<ClipInfo> clips = new List<ClipInfo>();


        [System.NonSerialized]
        private Dictionary<string, ClipInfo> dicInfo = null;
        //auf die material Dynamic setting
        public Vector4 GetAnimationUv(string aniName)
        {
            if (dicInfo == null) dicInfo = clips.ToDictionary(p => p.name);

            if (dicInfo.ContainsKey(aniName))
            {
                ClipInfo info = dicInfo[aniName];

                return new Vector4(info.startFrame, info.frameCount, speed, isAnimation ? 1 : 0);
            }
            return new Vector4(0, 30, 10, 1);
        }
    }
}

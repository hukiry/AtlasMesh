using UnityEngine;

namespace Hukiry.AtlasMesh
{
    /// <summary>
    /// all data 
    /// <list type="">
    ///     author:hxk
    /// </list> 
    /// </summary>
    public class MeshDataConfigAsset : ScriptableObject
    {
        public string lastMeshDataPath;

        public string selectSpriteName;

        public string searchSpriteName = "";

        private static MeshDataConfigAsset instance;
        public static MeshDataConfigAsset ins
        {
            get
            {
                if (instance == null)
                {
#if UNITY_EDITOR
                    string filePath = GetFilePath(typeof(MeshDataConfigAsset).Name, typeof(MeshDataConfigAsset).Name).Replace(".cs", ".asset");

                    instance = UnityEditor.AssetDatabase.LoadAssetAtPath<MeshDataConfigAsset>(filePath);

                    if (instance == null)
                    {
                        instance = CreateInstance<MeshDataConfigAsset>();

                        instance.name = typeof(MeshDataConfigAsset).Name;

                        UnityEditor.AssetDatabase.CreateAsset(instance, filePath);
                    }
#endif
                }
                return instance;
            }
        }

#if UNITY_EDITOR
        private static string GetFilePath(string assetName, string scriptName = null)
        {
            var assets = UnityEditor.AssetDatabase.FindAssets(assetName);

            string assetPath = null;

            if (assets.Length == 1)
            {
                assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assets[0]);
            }
            else
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(assets[i]);

                    if (System.IO.Path.GetFileNameWithoutExtension(path) == scriptName)
                    {
                        assetPath = path;
                        break;
                    }
                }
            }
            return assetPath;
        }

        private void OnValidate()
        {

        }
#endif
    }
}
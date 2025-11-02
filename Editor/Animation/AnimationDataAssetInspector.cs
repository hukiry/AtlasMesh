using UnityEditor;
using UnityEngine;
namespace Hukiry.AtlasMesh.Editor
{
    [CustomEditor(typeof(AnimationDataAsset))]
    public class AnimationDataAssetInspector : UnityEditor.Editor
    {
        private string projectPath => Application.dataPath.Replace("Assets", "");

        SerializedProperty clipTexture;

        SerializedProperty mesh;

        SerializedProperty mainTexture;
        private void OnEnable()
        {
            serializedObject.Update();

            clipTexture = serializedObject.FindProperty(nameof(clipTexture));

            mesh = serializedObject.FindProperty(nameof(mesh));

            mainTexture = serializedObject.FindProperty(nameof(mainTexture));

            var path = AssetDatabase.GetAssetPath(target.GetInstanceID());

            path = path.Replace(projectPath, "");

            string[] array = path.Split('.');

            if (array.Length == 2)
            {
                var clipPath = array[0] + "_clip.asset";

                var meshPath = array[0] + "_mesh.asset";

                var mainPath = array[0] + "_texture.png";

                if (clipTexture.objectReferenceValue == null)
                    clipTexture.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(clipPath);

                if (mesh.objectReferenceValue == null)
                    mesh.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                if (mainTexture.objectReferenceValue == null)
                    mainTexture.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(mainPath);

                serializedObject.ApplyModifiedProperties();
            }

        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }

}
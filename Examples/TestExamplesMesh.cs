using Hukiry.AtlasMesh;
using System.Collections.Generic;
using UnityEngine;

public class TestExamplesMesh : MonoBehaviour
{
    public AtlasDataAsset meshData;
    private List<GameObject> gameObjects = new List<GameObject>();
    public int Count=1000;
    public List<Mesh> meshes = new List<Mesh>();
    // Start is called before the first frame update
    void Start()
    {
        //for (int i = 0; i < Count; i++)
        //{
        //    var go = new GameObject(i.ToString());
        //    var opt = go.AddComponent<AtlasMesh>();
        //    go.transform.position = new Vector3(Random.Range(-8f, 8f), Random.Range(-4.5f, 4.5f), Random.Range(0, 10f));
        //    opt.AtlasData = meshData;
        //    opt.spriteName = meshData.mainTextureUv[Random.Range(0, meshData.mainTextureUv.Count)].spriteName;
        //    opt.color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1);
        //    go.transform.SetParent(this.transform, true);
        //    go.AddComponent<MeshCollider>().convex = true;
        //    go.AddComponent<Rigidbody>().useGravity = false;
        //    gameObjects.Add(go);

        //}
    }

    private void Update()
    {
        //每隔20毫秒执行一次
        if (Time.frameCount % 10 == 0)
        {
            var array = new Vector3[] { Vector3.up, Vector3.down, Vector3.right, Vector3.left };
            for (int i = 0; i < gameObjects.Count; i++)
            {
                var tf = gameObjects[i].transform;
                tf.localScale = Vector3.one * Random.Range(0.5f, 1.4f);

                tf.position += array[Random.Range(0, 4)].normalized * Time.deltaTime;
            }
        }
    }

}




using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hukiry.AtlasMesh
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class AtlasMesh : MonoBehaviour
    {
        protected struct MeshInfo
        {
            public string name;

            public int vertexStart;   // vertex start index

            public int vertexCount;   // vertex Count

            public int index;//mesh index
        }

        [SerializeField]
        private AtlasDataAsset m_AtlasData;
        public AtlasDataAsset AtlasData
        {
            get => m_AtlasData;
            set
            {
                if (m_AtlasData != value)
                {
                    m_AtlasData = value;
                }
            }
        }

        [SerializeField]
        private MeshLabelType m_MeshLabel;
        public MeshLabelType meshLabel
        {
            get => m_MeshLabel;
            set
            {
                if (value != m_MeshLabel)
                {
                    m_MeshLabel = value;
                    this.CombineMesh();
                }
            }
        }


        [SerializeField]
        private string m_MeshName;
        public string meshName
        {
            get => m_MeshName;
            set
            {
                if (value != m_MeshName)
                {
                    m_MeshName = value;
                    InitComponent();
                    this.ChangeMesh();
                }
            }
        }


        [SerializeField]
        private string m_AnimationName;
        public string animationName
        {
            get => m_AnimationName;
            set
            {
                if (value != m_AnimationName)
                {
                    m_AnimationName = value;
                    InitComponent();
                    this.ChangeClip();
                }
            }
        }

        [SerializeField]
        private string m_SpriteName;
        public string spriteName
        {
            get => m_SpriteName;
            set
            {
                if (value != m_SpriteName)
                {
                    m_SpriteName = value;

                    info = m_AtlasData?.mainTextureUv.Find(p => p.spriteName == this.spriteName);

                    InitComponent();

                    this.ChangeTexture();
                }
            }
        }

        [SerializeField]
        private bool m_isLoop = true;

        [SerializeField][Range(0.1f, 5)]
        private float m_Speed = 1;

        [SerializeField]
        private bool m_AlphaCutoff;

        [SerializeField]
        [Range(0.0001f, 0.85f)]
        private float m_Cutoff = 0.8f;

        [SerializeField]
        private bool m_isPreview;

        [SerializeField]
        [ColorUsage(true)]
        private Color m_Color = Color.white;
        public Color color
        {
            get => m_Color;
            set
            {
                if (value != m_Color)
                {
                    m_Color = value;

                    this.ChangeColor();
                }
            }
        }

  
        [SerializeField]
        private TextureInfo? info;

        //play delay
        private float m_delayPlayTime = 0;


        private bool m_isPreviewComponet;

        private MaterialPropertyBlock propertyBlock;

        private List<MeshInfo> meshInfoList = new List<MeshInfo>();

        private MeshRenderer meshRenderer;

        private MeshFilter meshFilter;

        private static Material m_material;

        private static Dictionary<MeshLabelType, Mesh> dicMesh = new Dictionary<MeshLabelType, Mesh>();

        private static Dictionary<int, Material> dicTexMat = new Dictionary<int, Material>();

        private void InitComponent(bool isFlags=false)
        {
            if (meshRenderer == null)
                meshRenderer = this.GetComponent<MeshRenderer>();

            if (meshFilter == null)
                meshFilter = this.GetComponent<MeshFilter>();

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            if (isFlags)
            {
                meshRenderer.hideFlags = m_isPreviewComponet ? HideFlags.None : HideFlags.HideInInspector;

                meshFilter.hideFlags = m_isPreviewComponet ? HideFlags.None : HideFlags.HideInInspector;
            }
        }
        private void Awake()
        {
            m_delayPlayTime = 0;

            InitComponent(true);
        }

        void Start()
        {
            this.InitComponent(true);

            this.CombineMesh();

            this.ChangeMesh();

            this.ChangeTexture();

            this.ChangeClip();

            this.ChangeColor();
        }

        /// <summary>
        /// merge all mesh and recode vertex number
        /// </summary>
        private void CombineMesh()
        {
            dicMesh.TryGetValue(this.meshLabel, out Mesh combinedMesh);

            if (combinedMesh == null)
            {
                combinedMesh = new Mesh();

                combinedMesh.name = this.meshLabel.ToString() ;
            }

            combinedMesh.Clear();

            List<Vector3> vertices = new List<Vector3>();

            List<int> triangles = new List<int>();

            List<Color> colors = new List<Color>();

            List<Vector3> normals = new List<Vector3>();

            List<Vector4> uvs = new List<Vector4>();

            List<BoneWeight> boneWeights = new List<BoneWeight>();

            List<Matrix4x4> bindposes = new List<Matrix4x4>();

            List<Vector2> uvs2 = new List<Vector2>();

            meshInfoList.Clear();

            int currentVertexOffset = 0;

            int currentTriangleOffset = 0;

            List<ModelInfo> tempModels = new List<ModelInfo>();

            int length = AtlasData.modelInfos.Count;

            MeshLabel label = this.AtlasData.meshLabels.Find(p => p.labelType == this.meshLabel);

            if (label.labels!=null&&label.labels.Count > 0)
            {
                for (int i = 0; i < length; i++)
                {
                    var model = AtlasData.modelInfos[i];

                    if (label.labels.Contains(model.spriteName))
                    {
                        tempModels.Add(model);
                    }
                }
            }
            

            foreach (var item in tempModels)
            {
                var subMesh = item.mesh;

                if (subMesh == null) continue;


                int index = meshInfoList.Count;

                var range = new MeshInfo
                {
                    vertexStart = currentVertexOffset,

                    vertexCount = subMesh.vertexCount,

                    name = item.spriteName,

                    index = index
                };
                meshInfoList.Add(range);

                vertices.AddRange(subMesh.vertices);
              
                for (int i = 0; i < subMesh.uv.Length; i++)
                {
                    var uv = subMesh.uv[i];

                    uvs.Add(new Vector4(uv.x, uv.y, i + currentVertexOffset, index));
                }

                if (subMesh.colors.Length == subMesh.vertexCount)
                {
                    colors.AddRange(subMesh.colors);
                }
                else
                {
                    for (int i = 0; i < subMesh.vertexCount; i++)
                    {
                        colors.Add(Color.white);
                    }
                }

                if (this.meshLabel != MeshLabelType.Spine)
                {
                    normals.AddRange(subMesh.normals);
                }

                if (this.meshLabel == MeshLabelType.AnimationModel)
                {
                    boneWeights.AddRange(subMesh.boneWeights);

                    bindposes.AddRange(subMesh.bindposes);
                }

                int[] subTris = subMesh.triangles;

                for (int i = 0; i < subTris.Length; i++)
                {
                    subTris[i] += currentVertexOffset;
                }

                triangles.AddRange(subTris);


                currentVertexOffset += subMesh.vertexCount;

                currentTriangleOffset += subMesh.triangles.Length;
            }

            combinedMesh.vertices = vertices.ToArray();

            combinedMesh.triangles = triangles.ToArray();

            combinedMesh.colors = colors.ToArray();

            combinedMesh.SetUVs(0, uvs.ToArray());

            if (normals.Count > 0)
            {
                combinedMesh.normals = normals.ToArray();
            }

            if (boneWeights.Count > 0)
            {
                combinedMesh.boneWeights = boneWeights.ToArray();

                combinedMesh.bindposes = bindposes.ToArray();
            }

            if (vertices.Count > 0)
            {
                combinedMesh.RecalculateBounds();

                dicMesh[this.meshLabel] = combinedMesh;

                meshFilter.sharedMesh = dicMesh[this.meshLabel];
            }

            //dicTexMat.TryGetValue(0, out Material material);
            //meshRenderer.sharedMaterial = material;
            meshRenderer.sharedMaterial = m_material;
        }

        private void ChangeMesh()
        {
            if (meshInfoList.Count <= 0)
            {
                this.CombineMesh();

                return;
            }

            var info = meshInfoList.Find(p => p.name == this.meshName);

            if (!string.IsNullOrEmpty(info.name))
            {
                meshRenderer.GetPropertyBlock(propertyBlock);

                propertyBlock.SetInt("_vertexStart", info.vertexStart);

                propertyBlock.SetInt("_vertexCount", info.vertexCount);

                propertyBlock.SetInt("_isSpine", this.meshLabel == MeshLabelType.Spine ? 1 : 0);

                propertyBlock.SetFloat("_Cutoff", this.m_Cutoff);

                propertyBlock.SetInt("_AlphaCutoff", this.m_AlphaCutoff?1:0);

                meshRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ChangeTexture()
        {
            var _MainTex = this.GetTexture2D();

            //dicTexMat.TryGetValue(info.Value.index, out Material material);
            //if (material == null)
            //{
            //    material = new Material(AtlasData.material);

            //    meshRenderer.sharedMaterial = m_material;

            //    material.SetTexture("_MatrixTex", this.AtlasData.clipTexture);
            //}

            //material.mainTexture = _MainTex;

            //dicTexMat[info.Value.index] = material;

            if (m_material == null)
            {
                m_material = new Material(AtlasData.material);

                meshRenderer.sharedMaterial = m_material;

                m_material.SetTexture("_MatrixTex", this.AtlasData.clipTexture);
            }
            m_material.mainTexture = _MainTex;

            var _skineRect = this.GetRectUV();

            meshRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetVector("_skineRect", _skineRect);

            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ChangeClip()
        {
            m_delayPlayTime = 0;

            //animation texture uv, is loop
            var _aniRect = this.AtlasData.GetClipRect(this.meshName);

            //startframe，animation frame，animation speed，ishasAnimation
            var _aniFrame = this.AtlasData.GetAnimationFrame(meshName, animationName, this.m_Speed);

            meshRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetVector("_aniRect", _aniRect);

            propertyBlock.SetVector("_aniFrame", _aniFrame);

            propertyBlock.SetFloat("_isSpine", this.meshLabel == MeshLabelType.Spine?1:0);

            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ChangeColor()
        {
            meshRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetColor("_Color", m_Color);

            meshRenderer.SetPropertyBlock(propertyBlock);
        }


      
        private void Update()
        {
            if (this.meshLabel != MeshLabelType.StaticModel)
            {
                m_delayPlayTime += Time.deltaTime;

                propertyBlock.SetInt("_isLoop", this.m_isLoop ? 1 : 0);

                propertyBlock.SetFloat("_delayTime", m_delayPlayTime);

                meshRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        //图集数组+独立纹理
        private Texture2D GetTexture2D()
        {
            info = m_AtlasData?.mainTextureUv.Find(p => p.spriteName == this.spriteName);

            return m_AtlasData?.mainTextureArray[info.Value.index];
        }

        private Vector4 GetRectUV()
        {
            
            info = m_AtlasData?.mainTextureUv.Find(p => p.spriteName == this.spriteName);
            
            return info.Value.GetSkinVector4();
        }


#if UNITY_EDITOR

        [ContextMenu("Copy AnimationName")]
        private void CopySpriteName()
        {
            GUIUtility.systemCopyBuffer = this.animationName;
        }

        [ContextMenu("Pase AnimationName")]
        private void PasteSpriteName()
        {
            this.animationName = GUIUtility.systemCopyBuffer;
        }

        [ContextMenu("Ping AtlasData")]
        private void PingAtlas()
        {
            UnityEditor.EditorGUIUtility.PingObject(this.AtlasData);
        }

        [ContextMenu("Enable Preview Component")]
        private void EnablePreviewComponent()
        {
            this.m_isPreviewComponet = !this.m_isPreviewComponet;

            this.InitComponent();
        }

        //绘制选中的
        void OnDrawGizmosSelected()
        {

            if (!m_isPreview) return;

            var model = this.AtlasData.GetModelInfo(this.meshName);

            if (model != null)
            {
                Gizmos.color = Color.cyan;

                Gizmos.DrawWireMesh(model.mesh, transform.position, transform.rotation, transform.lossyScale);
            }
        }

        private int intValidate = 0;
        protected void OnValidate()
        {
            if (AtlasData == null) return;

            InitComponent(true);

            if (!gameObject.activeInHierarchy)
            {
                if (intValidate > 0) return;

                intValidate++;
            }

            this.ChangeMesh();

            this.ChangeTexture();

            this.ChangeClip();

            this.ChangeColor();
        }
#endif
    }


}
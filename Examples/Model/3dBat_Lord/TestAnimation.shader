Shader "Unlit/TestAnimation"
{
    Properties
    {   
         _MainTex("Skined Tex", 2D) = "" {}
         _MatrixTex("Martix Data", 2D) = "" {}
        _indexAni("indexAni", Range(0,3)) = 0
    }
    SubShader
    {
        Pass
        {
            //blend SrcAlpha OneMinusSrcAlpha
            Name "TestMeshRenderer"
            ZTest less
            ZWrite on
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_instancing
            // #pragma instancing_options procedural:setup 
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

             struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : Normal;
                float4 color : COLOR;
                float4 uv : TEXCOORD1;
                float4 boneWeights  : BLENDWEIGHTS;//骨骼权重
                uint4 bonesIndexs   : BLENDINDICES;//骨骼索引
            };

            struct v2f
            {
            	float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD1;
                float3 normal : TEXCOORD2;
                int debug : TEXCOORD3;
            };
            

            sampler2D _MatrixTex;
            float4 _MatrixTex_TexelSize;
            sampler2D  _MainTex;
            float4 _MainTex_TexelSize;
            int _indexAni;


            void Change(out int _OffsetFrame, out int _CountFrame)
            {
                if (_indexAni == 0)
                {
                    _OffsetFrame = 0;
                    _CountFrame = 30;
                }

                if (_indexAni == 1)
                {
                    _OffsetFrame = 90;
                    _CountFrame = 26;
                }

                if (_indexAni == 2)
                {
                    _OffsetFrame = 214;
                    _CountFrame = 20;
                }

                if (_indexAni == 3)
                {
                    _OffsetFrame = 234;
                    _CountFrame = 51;
                }
            }

            //贴图
            float4x4 GetBoneMatrixTex(float boneIndex, float animationRow)
            {
                float invW = _MatrixTex_TexelSize.x;
                float invH = _MatrixTex_TexelSize.y;
                int x0Pix = boneIndex * 4.0 + 0.0;
                int x1Pix = boneIndex * 4.0 + 1.0;
                int x2Pix = boneIndex * 4.0 + 2.0;
                int x3Pix = boneIndex * 4.0 + 3.0;
                int _OffsetFrame, _CountFrame;
                Change(_OffsetFrame, _CountFrame);
                int y = _OffsetFrame + animationRow % _CountFrame;
                float4 r0 =  tex2Dlod(_MatrixTex, float4(x0Pix * invW + invW / 2, y * invH + invH / 2, 0, 0));
                float4 r1 =  tex2Dlod(_MatrixTex, float4(x1Pix * invW + invW / 2, y * invH + invH / 2, 0, 0));
                float4 r2 =  tex2Dlod(_MatrixTex, float4(x2Pix * invW + invW / 2, y * invH + invH / 2, 0, 0));
                float4 r3 =  tex2Dlod(_MatrixTex, float4(x3Pix * invW + invW / 2, y * invH + invH / 2, 0, 0));

                return float4x4(r0, r1, r2, r3);
            }

            void SkinVertex(out float4 outPos, out float3 outNormal, appdata v, float animationTime)
            {
                float4 skinnedPos = float4(0, 0, 0, 0);
                float3 skinnedNormal = float3(0, 0, 0);
                float4x4 boneMatrix;
                float4 pos = v.vertex;
                float3 normal = v.normal;
                boneMatrix = GetBoneMatrixTex(v.bonesIndexs.x, animationTime);
                skinnedPos += mul(boneMatrix, pos) * v.boneWeights.x;
                skinnedNormal += mul((float3x3)boneMatrix, normal) * v.boneWeights.x;
                boneMatrix = GetBoneMatrixTex(v.bonesIndexs.y, animationTime);
                skinnedPos += mul(boneMatrix, pos) * v.boneWeights.y;
                skinnedNormal += mul((float3x3)boneMatrix, normal) * v.boneWeights.y;
                boneMatrix = GetBoneMatrixTex(v.bonesIndexs.z, animationTime);
                skinnedPos += mul(boneMatrix, pos) * v.boneWeights.z;
                skinnedNormal += mul((float3x3)boneMatrix, normal) * v.boneWeights.z;
                boneMatrix = GetBoneMatrixTex(v.bonesIndexs.w, animationTime);
                skinnedPos += mul(boneMatrix, pos) * v.boneWeights.w;
                skinnedNormal += mul((float3x3)boneMatrix, normal) * v.boneWeights.w;
                outPos = skinnedPos;
                outNormal = skinnedNormal;
            }

            float4 GetWorldPos(float4 skinnedPos)
            {
                float4 worldPos = float4(TransformObjectToWorld(skinnedPos), 0);
                return TransformWorldToHClip(worldPos);
            }

            v2f vert (appdata v)
            {
                v2f o;
                float vid = v.uv.z;
                float4 skinnedPos = float4(0, 0, 0, 0);
                float3 skinnedNormal = float3(0, 0, 0);
                float animationTime =  _Time.w*1000;
                SkinVertex( skinnedPos,  skinnedNormal, v, animationTime);
                skinnedNormal = normalize(skinnedNormal);
                o.vertex = GetWorldPos(skinnedPos);
                o.uv = v.uv;
                o.normal = skinnedNormal;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {   
                half4 col = tex2D(_MainTex, i.uv);
                Light mainLight = GetMainLight();
                float3 dir = normalize(mainLight.direction);
                col = col * max(0.2, dot(dir, i.normal));   
                return col;
            }
            ENDHLSL
        }
    }
}

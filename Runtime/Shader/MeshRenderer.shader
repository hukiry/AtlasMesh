Shader "Hukiry/MeshRenderer"
{
    Properties
    { 
        [NoScaleOffset]
        _MainTex("MainTex", 2D) = "" {}
    
        [NoScaleOffset]
         _MatrixTex("Martix Data", 2D) = "" {}

    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Fog { Mode Off }
        ZWrite On
        ZTest LEqual
        Blend one OneMinusSrcAlpha

        Pass
        {
            Name "MeshRenderer"

            HLSLPROGRAM
            #pragma vertex vert

            #pragma fragment frag

            #pragma multi_compile_instancing

            #pragma instancing_options assumeuniformscaling

            #include "UnityCG.cginc" 
            // #pragma instancing_options procedural:setup 
			//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : Normal;
                float4 color : COLOR;
                float4 uv : TEXCOORD0;

                float4 boneWeights  : BLENDWEIGHTS;
                uint4 bonesIndexs   : BLENDINDICES;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
            	float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD0;
                float3 normal : Normal;
                float4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            sampler2D _MatrixTex;

            float4 _MatrixTex_TexelSize;

            sampler2D  _MainTex;

            //instance var define
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _skineRect)

                UNITY_DEFINE_INSTANCED_PROP(int2, _aniRect)
                UNITY_DEFINE_INSTANCED_PROP(float4, _aniFrame)

                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)

                UNITY_DEFINE_INSTANCED_PROP(int, _vertexStart)
                UNITY_DEFINE_INSTANCED_PROP(int, _vertexCount)

                UNITY_DEFINE_INSTANCED_PROP(float, _delayTime)

                UNITY_DEFINE_INSTANCED_PROP(int, _isLoop)

                UNITY_DEFINE_INSTANCED_PROP(int, _isSpine)
                UNITY_DEFINE_INSTANCED_PROP(int, _AlphaCutoff)
                UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
            UNITY_INSTANCING_BUFFER_END(Props)


            struct TransformData
            {
                float4 vertex;
                float3 normal;
                float3 position;
                float3 rotate;
                float3 scale;
            };

            /*  model space transform, Scale,rotate,
                TransformData t;
                t.vertex = skinnedPos;
                t.normal = skinnedNormal;
                t.position = 0;//用户输入
                t.rotate = 0;//用户输入
                t.scale = 1;//用户输入
                TransformObjectVertex(out float4 vertex, out float3 normal, TransformData v)
            */
            void TransformObjectVertex(out float4 vertex, out float3 normal, TransformData v)
            {
                vertex = v.vertex;

                normal = v.normal;

                //scale 
                float4x4 scale = float4x4(v.scale.x, 0, 0, 0,
                    0, v.scale.y, 0, 0,
                    0, 0, v.scale.z, 0,
                    0, 0, 0, 1);

                vertex = mul(scale, vertex);

                //rotate
                float4x4 rotateX = float4x4(1, 0, 0, 0,
                    0, cos(v.rotate.x), -sin(v.rotate.x), 0,
                    0, sin(v.rotate.x), cos(v.rotate.x), 0,
                    0, 0, 0, 1);

                float4x4 rotateY = float4x4(
                    cos(v.rotate.y), 0, sin(v.rotate.y), 0,
                    0, 1, 0, 0,
                    -sin(v.rotate.y), 0, cos(v.rotate.y), 0,
                    0, 0, 0, 1);

                float4x4 rotateZ = float4x4(
                    cos(v.rotate.z), -sin(v.rotate.z), 0, 0,
                    sin(v.rotate.z), cos(v.rotate.z), 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1);

                vertex = mul(rotateX, vertex);
                vertex = mul(rotateY, vertex);
                vertex = mul(rotateZ, vertex);

                // position 
                vertex += float4(v.position, 0);

                normal = mul((float3x3)rotateX, normal);
                normal = mul((float3x3)rotateY, normal);
                normal = mul((float3x3)rotateZ, normal);

                normal = normalize(normal);
            }


            float3 GetVertexAnimationFromTex(int animationRow, int id)
            {
                float invW = _MatrixTex_TexelSize.x;

                float invH = _MatrixTex_TexelSize.y;
                //texture uv
                int2 aniRect = UNITY_ACCESS_INSTANCED_PROP(Props, _aniRect);
                //start Frame，current animation frame 
                float4 aniFrame = UNITY_ACCESS_INSTANCED_PROP(Props, _aniFrame);
                //Texture width
                int vertexCount = UNITY_ACCESS_INSTANCED_PROP(Props, _vertexCount);

                int y = aniRect.y + aniFrame.x + animationRow % aniFrame.y;

                int x = aniRect.x + id% vertexCount;

                float4 col = tex2Dlod(_MatrixTex, float4(x * invW + 0.5 * invW, y * invH + 0.5 * invH, 0, 0));

                return col.xyz;
            }

            float4x4 GetBoneMatrixTex(float boneIndex, int animationRow, appdata v)
            {
                float invW = _MatrixTex_TexelSize.x;

                float invH = _MatrixTex_TexelSize.y;

                int x0Pix = boneIndex * 4.0 + 0.0;

                int x1Pix = boneIndex * 4.0 + 1.0;

                int x2Pix = boneIndex * 4.0 + 2.0;

                int x3Pix = boneIndex * 4.0 + 3.0;

                int2 aniRect = UNITY_ACCESS_INSTANCED_PROP(Props, _aniRect);

                float4 aniFrame = UNITY_ACCESS_INSTANCED_PROP(Props, _aniFrame);
                
                int y = aniRect.y + aniFrame.x + animationRow % aniFrame.y;

                int x = aniRect.x * invH;

                float4 r0 =  tex2Dlod(_MatrixTex, float4(x + x0Pix * invW + invW / 2, y * invH + invH / 2, 0, 0));

                float4 r1 =  tex2Dlod(_MatrixTex, float4(x + x1Pix * invW + invW / 2, y * invH + invH / 2, 0, 0));

                float4 r2 =  tex2Dlod(_MatrixTex, float4(x + x2Pix * invW + invW / 2, y * invH + invH / 2, 0, 0));

                float4 r3 =  tex2Dlod(_MatrixTex, float4(x + x3Pix * invW + invW / 2, y * invH + invH / 2, 0, 0));

                return float4x4(r0, r1, r2, r3);
            }

            void SkinVertex(out float4 outSkinVertex, out float3 outNormal, appdata v, int animationTime)
            {
                float4 skinnedPos = float4(0, 0, 0, 0);

                float3 skinnedNormal = float3(0, 0, 0);

                float4x4 boneMatrix;

                float4 pos = v.vertex;

                float3 normal = v.normal;

                //Bone 0
                boneMatrix = GetBoneMatrixTex(v.bonesIndexs.x, animationTime, v);

                skinnedPos += mul(boneMatrix, pos) * v.boneWeights.x;

                skinnedNormal += mul((float3x3)boneMatrix, normal) * v.boneWeights.x;

                //Bone 1
                boneMatrix = GetBoneMatrixTex(v.bonesIndexs.y, animationTime, v);

                skinnedPos += mul(boneMatrix, pos) * v.boneWeights.y;

                skinnedNormal += mul((float3x3)boneMatrix, normal) * v.boneWeights.y;

                //Bone 2
                boneMatrix = GetBoneMatrixTex(v.bonesIndexs.z, animationTime, v);

                skinnedPos += mul(boneMatrix, pos) * v.boneWeights.z;

                skinnedNormal += mul((float3x3)boneMatrix, normal) * v.boneWeights.z;

                //Bone 3
                boneMatrix = GetBoneMatrixTex(v.bonesIndexs.w, animationTime, v);

                skinnedPos += mul(boneMatrix, pos) * v.boneWeights.w;

                skinnedNormal += mul((float3x3)boneMatrix, normal) * v.boneWeights.w;

                outSkinVertex = skinnedPos;

                outNormal = skinnedNormal;
            }

            //模型空间到世界空间
            float3 VertexObjectToWorld(float3 positionOS)
            {
                #if defined(SHADER_STAGE_RAY_TRACING)
                     return mul(ObjectToWorld3x4(), float4(positionOS, 1.0)).xyz;
                #else
                     return mul(UNITY_MATRIX_M, float4(positionOS, 1.0)).xyz;
                #endif
            }

            int GetSpineFrameIndex(float speed)
            {
                float frameRate = 30;

                float delayTime = UNITY_ACCESS_INSTANCED_PROP(Props, _delayTime);

                delayTime = delayTime * speed;

                int totalFrame = UNITY_ACCESS_INSTANCED_PROP(Props, _aniFrame).y;

                int isLoop = UNITY_ACCESS_INSTANCED_PROP(Props, _isLoop);
                
                float frameTime = 1.0 / frameRate;//pro frame time

                float totalDuration = totalFrame * frameTime;

                if (delayTime > totalDuration)
                {
                    if (isLoop)
                        delayTime %= totalDuration;
                    else
                        delayTime = totalDuration - frameTime;
                }
                return ceil(delayTime * frameRate);
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float vid = v.uv.z;

                int vertexStart = UNITY_ACCESS_INSTANCED_PROP(Props, _vertexStart);

                int vertexCount = UNITY_ACCESS_INSTANCED_PROP(Props, _vertexCount);

                if (vid< vertexStart || vid>= vertexStart+vertexCount)
                {
                    o.vertex = 0;

                    return o;
                }

                float4 skinnedPos = float4(0, 0, 0, 0);

                float3 skinnedNormal = float3(0, 0, 0);

                float4 aniFrame = UNITY_ACCESS_INSTANCED_PROP(Props, _aniFrame);

                if (aniFrame.w == 1)
                {
                    int isSpine = UNITY_ACCESS_INSTANCED_PROP(Props, _isSpine);

                    int animationTime = GetSpineFrameIndex(aniFrame.z);

                    if (isSpine)
                    {
                        int vertexIndex = v.uv.z -vertexStart;

                        v.vertex.xyz = GetVertexAnimationFromTex(animationTime, vertexIndex);

                        o.vertex = UnityObjectToClipPos(v.vertex);
                    }
                    else {
                        SkinVertex(skinnedPos, skinnedNormal, v, animationTime);

                        skinnedNormal = normalize(skinnedNormal);

                        //float4 worldPos = float4(TransformObjectToWorld(skinnedPos), 0); //Unity2022 above
                        float4 worldPos = float4(VertexObjectToWorld(skinnedPos), 0);
                       
                        o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos.xyz, 1.0));//world space to view project ray
                        //o.vertex = TransformWorldToHClip(worldPos); //Unity2022 above
                    }
                }
                else
                {
                    skinnedNormal = v.normal;

                    o.vertex = UnityObjectToClipPos(v.vertex);
                }

                float4 skineRect = UNITY_ACCESS_INSTANCED_PROP(Props, _skineRect);

                o.uv = float3(skineRect.x + v.uv.x * skineRect.z,  skineRect.y + v.uv.y * skineRect.w, v.uv.w);

                o.normal = skinnedNormal;

                o.color = v.color;

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {   
                UNITY_SETUP_INSTANCE_ID(i);

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                half4 col = tex2D(_MainTex, i.uv)* color;
              
                int isSpine = UNITY_ACCESS_INSTANCED_PROP(Props, _isSpine);

                if (isSpine)
                {
                    float cutoff = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutoff);

                    int AlphaCutoff = UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaCutoff);

                    if (AlphaCutoff)
                    {
                        color.rgb *= i.color.a;
                    }

                    clip(col.a - cutoff);

                    return (col*i.color);
                }
                else
                {
                    //Light mainLight = GetMainLight();//Unity2022 above
                    //float3 dir = normalize(mainLight.direction);//Unity2022 above
                    float3 dir = normalize(_WorldSpaceLightPos0.xyz);

                    col = col * max(0.2, dot(dir, i.normal));

                    return float4(col.rgb, color.a);
                }
              
            }
            ENDHLSL
        }
    }

    FallBack "Transparent/VertexLit"
}

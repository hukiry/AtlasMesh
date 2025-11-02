Shader "Spine/SkeletonAtlasMesh" {
	Properties {
				
		[NoScaleOffset] _MainTex ("Main Texture", 2D) = "black" {}
		[NoScaleOffset]
		 _MatrixTex("Martix Data", 2D) = "" {}

		[Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 0
		_Cutoff("Shadow alpha cutoff", Range(0,1)) = 0.5
	}

	SubShader {
		
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			Name "Normal"

			CGPROGRAM
			#pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_instancing
			#pragma instancing_options assumeuniformscaling
			#include "UnityCG.cginc"

			struct VertexInput {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uvIndex : TEXCOORD1;
				float4 vertexColor : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 vertexColor : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			sampler2D _MatrixTex;

			float4 _MatrixTex_TexelSize;

			sampler2D _MainTex;

			float _Cutoff;

			float3 GetVertexAnimationFromTex(int animationRow, int id)
			{
				float invW = _MatrixTex_TexelSize.x;

				float invH = _MatrixTex_TexelSize.y;

				uint x = id % _MatrixTex_TexelSize.z;

				uint y = animationRow % 25; //_MatrixTex_TexelSize.w;

				float4 col = tex2Dlod(_MatrixTex, float4(x * invW + 0.5 * invW, y * invH + 0.5 * invH, 0, 0));

				return col.xyz;
			}

			VertexOutput vert (VertexInput v) {

				VertexOutput o;

				UNITY_SETUP_INSTANCE_ID(v);

				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float animationTime = floor(_Time.w * 10);

				if (_MatrixTex_TexelSize.w > 0)
				{
					float3 pos = GetVertexAnimationFromTex(animationTime, v.uvIndex.x);

					v.vertex.xyz = pos;
				}

				o.pos = UnityObjectToClipPos(v.vertex);

				o.uv = v.uv;

				o.vertexColor = v.vertexColor;

				return o;
			}

			float4 frag (VertexOutput i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);

				float4 texColor = tex2D(_MainTex, i.uv);

				#if defined(_STRAIGHT_ALPHA_INPUT)

					texColor.rgb *= i.vertexColor.a;

				#endif

				clip(texColor.a - _Cutoff);

				return (texColor * i.vertexColor);
			}
			ENDCG
		}

	}
	FallBack "Transparent/VertexLit"
}
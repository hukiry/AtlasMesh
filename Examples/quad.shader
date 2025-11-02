Shader "Unlit/quad"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _row("_row", Range(0,256)) =2
        _cell("_cell", Range(0,256)) = 2
            _rect("_rect",vector)=(0,0,0,0)
    }
    SubShader
    {
        // 透明渲染核心标签：控制渲染顺序和类型
         Tags {
             "RenderType" = "Transparent"   // 标记为透明物体，用于后处理和SRP筛选
             "Queue" = "Transparent"        // 透明队列（在不透明物体后渲染，确保叠加正确）
             "IgnoreProjector" = "True"     // 忽略投影器（避免透明物体被投影异常影响）
         }
         LOD 100  // 简化渲染级别（低配置设备也能运行）

         Cull Off
         ZWrite Off  // 关闭深度写入（半透明物体不遮挡后续透明物体，符合透明渲染逻辑）
         Blend SrcAlpha OneMinusSrcAlpha  // 标准透明混合：源色*源Alpha + 目标色*(1-源Alpha)

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float _row;

            float _cell;

            float4 _rect;

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                 fixed4 col = tex2D(_MainTex, i.uv);
                //float x = ceil( i.uv.x/(1/_row));
                //float y = ceil(i.uv.y / (1 / _cell));
                //col.a = x%2==0|| y%2==0?0:1;


      
            //for (int k = 1;k < _row; k++)
            //{
            //    for (int j = 1;j < _cell; j++)
            //    {
            //        float x = 1 / _row * k+(k-1)*0.001;
            //        float y = 1 / _cell * j + (j-1) * 0.001;
            //        if (length(i.uv - float2(x, y)) < _rect.x)
            //            col.a = 0;
            //    }
            //}

                //// apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                //bool x = i.uv.x >= _rect.x && (i.uv.x <= _rect.x + _rect.z);
                //bool y = i.uv.y >= _rect.y && (i.uv.y <= _rect.y + _rect.w);
                //if (x || y)
                //{
                //    col.a = 0;
                //   
                //}

            //float len = length(i.uv - _rect.xy);
            //float w = 0.1;
            //    if (len <= _rect.z-w)
            //    {
            //        col.a = 0;
            //    }

            //    else if (len <= _rect.z)
            //    {
            //        col.a =(w-(_rect.z - len))*(1/w);

            //    }

              
                return col;
            }
            ENDCG
        }
    }
}

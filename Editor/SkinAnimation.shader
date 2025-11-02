Shader "Custom/AtlasMesh"
{
    Properties
    {
        _BaseColor("全局颜色", Color) = (1,1,1,1)  // 全局基础叠加色（与C#脚本的globalBaseColor对应）
        _MainTex("图集纹理", 2D) = "white" {}       // 自动合并后的单张图集纹理（C#脚本赋值）
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
            #pragma vertex vert  // 顶点着色器入口
            #pragma fragment frag  // 片段着色器入口
            #include "UnityCG.cginc"  // 引入Unity基础CG工具库

            // ------------------------------ 输入顶点数据结构 ------------------------------
            // 存储从C#脚本传递的网格原始数据
            struct appdata
            {
                float4 vertex : POSITION;  // 模型空间顶点位置
                float2 uv : TEXCOORD0;     // 适配图集的最终UV（C#脚本计算后传入）
                float4 color : COLOR;      // 顶点颜色（RGB：网格颜色，A：显示/隐藏控制）
            };

            // ------------------------------ 输出片段数据结构 ------------------------------
            // 从顶点着色器传递到片段着色器的数据
            struct v2f
            {
                float2 uv : TEXCOORD0;     // 传递图集UV到片段着色器
                float4 color : TEXCOORD1;  // 传递顶点颜色到片段着色器
                float4 pos : SV_POSITION;  // 裁剪空间顶点位置（最终屏幕坐标）
            };

            // ------------------------------ 外部参数声明 ------------------------------
            uniform float4 _BaseColor;    // 全局基础颜色（对应Properties中的_BaseColor）
            uniform sampler2D _MainTex;   // 图集纹理采样器（对应Properties中的_MainTex）
            uniform float4 _MainTex_ST;   // 纹理缩放偏移（Unity自动生成，用于UV缩放）

            // ------------------------------ 顶点着色器 ------------------------------
            v2f vert(appdata v)
            {
                v2f o;
                // 1. 模型空间→裁剪空间：将顶点位置转换为屏幕显示坐标
                o.pos = UnityObjectToClipPos(v.vertex);
                // 2. UV处理：应用纹理缩放偏移（支持在Inspector中调整纹理缩放）
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // 3. 传递顶点颜色（直接传递，无修改）
                o.color = v.color;
                return o;
            }

            // ------------------------------ 片段着色器 ------------------------------
            fixed4 frag(v2f i) : SV_Target
            {
                // 1. 显示控制：顶点颜色Alpha < 0.1时，直接裁剪该像素（完全隐藏）
                //clip(i.color.a - 0.1);

                // 2. 图集纹理采样：根据适配后的UV获取纹理颜色（包含Alpha通道）
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // 3. 最终颜色计算：全局色 × 顶点色(RGB) × 纹理色（透明度=纹理Alpha × 顶点Alpha）
                fixed4 finalColor;
                finalColor.rgb = _BaseColor.rgb * i.color.rgb * texColor.rgb;  // RGB通道叠加
                finalColor.a = texColor.a * i.color.a;                        // Alpha通道：纹理透明 + 显示控制

                return finalColor;  // 输出最终颜色到屏幕
            }
            ENDCG
        }
    }
        // 降级策略：当设备不支持当前Shader时，自动使用Unity内置透明Shader
    FallBack "Transparent/VertexLit"
}
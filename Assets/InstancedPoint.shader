// Assets/Shaders/InstancedPoint.shader
Shader "Custom/InstancedPoint"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _MinY ("Min Y", Float) = 0
        _MaxY ("Max Y", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _COLOR_BY_HEIGHT

            #include "UnityCG.cginc"

            // 用自定义名字，避免与引擎内部缓冲重名
            UNITY_INSTANCING_BUFFER_START(Props)
                // 颜色用 half4，和 Color 属性保持一致，避免类型不匹配
                UNITY_DEFINE_INSTANCED_PROP(half4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _MinY)
                UNITY_DEFINE_INSTANCED_PROP(float, _MaxY)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos  : SV_POSITION;
                float3 wpos : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float4 wpos = mul(unity_ObjectToWorld, v.vertex);
                o.wpos = wpos.xyz;
                o.pos  = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half3 HeightGradient(float y, float minY, float maxY)
            {
                float t = saturate((y - minY) / max(1e-6, (maxY - minY)));
                half3 c1 = half3(0.1, 0.4, 1.0);
                half3 c2 = half3(0.0, 1.0, 1.0);
                half3 c3 = half3(1.0, 1.0, 0.0);
                half3 c4 = half3(1.0, 0.2, 0.1);
                return (t < 0.33) ? lerp(c1, c2, t/0.33) :
                       (t < 0.66) ? lerp(c2, c3, (t-0.33)/0.33) :
                                    lerp(c3, c4, (t-0.66)/0.34);
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                half4 baseCol = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);

                #ifdef _COLOR_BY_HEIGHT
                    float minY = UNITY_ACCESS_INSTANCED_PROP(Props, _MinY);
                    float maxY = UNITY_ACCESS_INSTANCED_PROP(Props, _MaxY);
                    return half4(HeightGradient(i.wpos.y, minY, maxY), 1.0h);
                #else
                    return baseCol;
                #endif
            }
            ENDCG
        }
    }
}

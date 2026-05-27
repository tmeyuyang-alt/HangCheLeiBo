Shader "Custom/FresnelXRayTransparent"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.05, 0.75, 1.0, 0.18)
        _FresnelColor ("Fresnel Color", Color) = (0.0, 0.95, 1.0, 1.0)
        _HiddenColor ("Hidden XRay Color", Color) = (0.15, 0.55, 1.0, 0.35)
        _FresnelPower ("Fresnel Power", Range(0.25, 8)) = 2.5
        _FresnelIntensity ("Fresnel Intensity", Range(0, 8)) = 2.0
        _Alpha ("Visible Alpha", Range(0, 1)) = 0.35
        _HiddenAlpha ("Hidden Alpha", Range(0, 1)) = 0.45
        _RimMin ("Rim Min", Range(0, 1)) = 0.0
        _RimMax ("Rim Max", Range(0, 1)) = 1.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-100"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100
        Cull [_Cull]
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "XRayHidden"
            Tags { "LightMode" = "Always" }
            ZTest Greater

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragHidden
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _HiddenColor;
            float _FresnelPower;
            float _FresnelIntensity;
            float _HiddenAlpha;
            float _RimMin;
            float _RimMax;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 fragHidden(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 normalDir = normalize(i.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float fresnel = 1.0 - saturate(dot(normalDir, viewDir));
                fresnel = smoothstep(_RimMin, _RimMax, fresnel);
                fresnel = pow(fresnel, _FresnelPower) * _FresnelIntensity;

                fixed4 col = _HiddenColor;
                col.rgb *= 1.0 + fresnel;
                col.a = saturate(_HiddenAlpha * (0.35 + fresnel));
                return col;
            }
            ENDCG
        }

        Pass
        {
            Name "VisibleFresnel"
            Tags { "LightMode" = "Always" }
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragVisible
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            fixed4 _FresnelColor;
            float _FresnelPower;
            float _FresnelIntensity;
            float _Alpha;
            float _RimMin;
            float _RimMax;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 fragVisible(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 normalDir = normalize(i.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float fresnel = 1.0 - saturate(dot(normalDir, viewDir));
                fresnel = smoothstep(_RimMin, _RimMax, fresnel);
                fresnel = pow(fresnel, _FresnelPower);

                fixed4 col;
                col.rgb = _BaseColor.rgb + _FresnelColor.rgb * fresnel * _FresnelIntensity;
                col.a = saturate(_Alpha + fresnel * _FresnelColor.a);
                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}

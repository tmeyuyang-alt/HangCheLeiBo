Shader "Hik/PointCloudBillboard"
{
    Properties
    {
        _Tint("Tint", Color) = (1,1,1,1)
        _PointSize("Point Size (px)", Float) = 3
        _SizeScaleWithDistance("Size Scale With Distance", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }
        Cull Off ZWrite On ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex   VS
            #pragma geometry GS
            #pragma fragment PS
            #include "UnityCG.cginc"

            StructuredBuffer<float4> _PointBuffer; // xyz + rgba(uint) 共享 16 字节
            float4 _Tint;
            float _PointSize;
            float _SizeScaleWithDistance;

            struct v2g { float3 wpos : TEXCOORD0; uint vid : TEXCOORD1; };
            struct g2f { float4 pos : SV_POSITION; float4 col : COLOR0; };

            v2g VS(uint id : SV_VertexID)
            {
                v2g o;
                float4 data = _PointBuffer[id];
                o.wpos = data.xyz;
                o.vid = id; // 通过 TEXCOORD1 传给 GS（不要在 GS 用 SV_VertexID）
                return o;
            }

            [maxvertexcount(4)]
            void GS(point v2g i[1], inout TriangleStream<g2f> triStream)
            {
                // 从 StructuredBuffer 取出颜色与位置
                uint idx = i[0].vid;
                float4 data = _PointBuffer[idx];
                uint rgba = asuint(data.w);
                float a = ((rgba >> 24) & 255) / 255.0;
                float b = ((rgba >> 16) & 255) / 255.0;
                float g = ((rgba >> 8)  & 255) / 255.0;
                float r = ((rgba >> 0)  & 255) / 255.0;
                float4 col = _Tint * float4(r,g,b,a);

                float3 wpos = i[0].wpos;
                float4 p = UnityWorldToClipPos(wpos);

                float2 fb = _ScreenParams.xy; // frame buffer size
                float sizePx = _PointSize;
                if (_SizeScaleWithDistance > 0)
                {
                    float3 camPos = _WorldSpaceCameraPos;
                    float dist = max(0.001, distance(wpos, camPos));
                    sizePx *= (1 + dist * _SizeScaleWithDistance);
                }

                float2 sizeNDC = sizePx * 2.0 / fb; // 像素 -> NDC
                float2 sizeCS = sizeNDC * p.w;      // NDC -> ClipSpace

                float2 offs[4] = {
                    float2(-sizeCS.x, -sizeCS.y),
                    float2(-sizeCS.x,  sizeCS.y),
                    float2( sizeCS.x,  sizeCS.y),
                    float2( sizeCS.x, -sizeCS.y)
                };

                g2f o;
                float4 p0 = float4(p.x + offs[0].x, p.y + offs[0].y, p.z, p.w);
                float4 p1 = float4(p.x + offs[1].x, p.y + offs[1].y, p.z, p.w);
                float4 p2 = float4(p.x + offs[2].x, p.y + offs[2].y, p.z, p.w);
                float4 p3 = float4(p.x + offs[3].x, p.y + offs[3].y, p.z, p.w);

                o.col = col; o.pos = p0; triStream.Append(o);
                o.col = col; o.pos = p1; triStream.Append(o);
                o.col = col; o.pos = p2; triStream.Append(o);
                triStream.RestartStrip();
                o.col = col; o.pos = p2; triStream.Append(o);
                o.col = col; o.pos = p3; triStream.Append(o);
                o.col = col; o.pos = p0; triStream.Append(o);
                triStream.RestartStrip();
            }

            fixed4 PS(g2f i) : SV_Target { return i.col; }
            ENDCG
        }
    }
    FallBack Off
}
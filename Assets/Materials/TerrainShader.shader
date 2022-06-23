Shader "Custom/TerrainShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Layer0 ("Layer 0 (RGB)", 2D) = "white" {}
        _Layer1 ("Layer 1 (RGB)", 2D) = "white" {}
        _Heightmap ("Heightmap (RGB)", 2D) = "white" {}
        _SlopeOffset ("Slope Offset", Range(-1,1)) = 0.0
        _SlopeFade ("Slope Fade", Range(0,1)) = 0.1
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Size ("Size", Float) = 0.0
        _Height("Height", Range(0, 1000)) = 200.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _Layer0;
        float4 _Layer0_ST;
        sampler2D _Layer1;
        float4 _Layer1_ST;
        sampler2D _Heightmap;
        
        struct Input
        {
            float3 normal;
	        float3 worldPos;
        };

        half _SlopeOffset;
        half _SlopeFade;
        half _Glossiness;
        half _Metallic;
        half _Size;
        half _Height;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float2 getHeightmapUV(float2 pos) {
            float texel = 1.0 / _Size;
            float2 uv = ((pos * texel) + float2(texel, texel) * 2) * (1.0 - texel * 4.0);
            return uv;
        }

        float3 getHeightmapNormal(float2 pos) {
            float a = tex2Dlod(_Heightmap, float4(getHeightmapUV(pos.xy + float2(0, -1)), 0, 0)).r * _Height;
            float b = tex2Dlod(_Heightmap, float4(getHeightmapUV(pos.xy + float2(-1, 0)), 0,0)).r* _Height;
            float c = tex2Dlod(_Heightmap, float4(getHeightmapUV(pos.xy + float2(1, 0)), 0,0)).r* _Height;
            float d = tex2Dlod(_Heightmap, float4(getHeightmapUV(pos.xy + float2(0, 1)), 0,0)).r* _Height;
            return normalize(float3(b - c, 2.0, a - d));
        }

        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            float3 pos = mul(unity_ObjectToWorld, v.vertex);
            fixed4 heightmap = tex2Dlod(_Heightmap, float4(getHeightmapUV(pos.xz + float2(0, 0)), 0, 0));
            v.vertex.y = heightmap.x * _Height;
            v.normal = getHeightmapNormal(pos.xz);
            o.normal = v.normal;
            o.worldPos = pos;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.worldPos.xz * 0.1;
            fixed4 layer0 = tex2D (_Layer0, uv * _Layer0_ST.xy) * _Color;
            fixed4 layer1 = tex2D (_Layer1, uv * _Layer1_ST.xy) * _Color;
            float slope = smoothstep(1.0 * _SlopeFade, 0.0, IN.normal.y + _SlopeOffset);
            o.Albedo = lerp(layer0.rgb, layer1.rgb, slope);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

    Shader "Hidden/TerrainEngine/ChangeHeight" {

    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "Packages/com.unity.terrain-tools/Shaders/TerrainTools.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

            sampler2D _BrushTex;
            sampler2D _FilterTex;

            float4 _BrushParams;
            #define BRUSH_STRENGTH      (_BrushParams[0])
            #define BRUSH_TARGETHEIGHT  (_BrushParams[1])
            #define kMaxHeight          (32766.0f/65535.0f)

            struct appdata_t {
                float4 vertex : POSITION;
                float2 pcUV : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 pcUV : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pcUV = v.pcUV;
                return o;
            }


        ENDCG

        Pass    // 0 raise/lower heights
        {
            Name "Raise/Lower Heights"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment RaiseHeight

            float4 RaiseHeight(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float brushShape = oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));

                return PackHeightmap(clamp(BRUSH_STRENGTH * brushShape, 0, kMaxHeight));
            }
            ENDCG
        }


    }
    Fallback Off
}

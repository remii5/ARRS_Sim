Shader "Hidden/DepthOnly"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float depth : DEPTH;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.depth = o.pos.z / o.pos.w;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normalize depth (0 to 1)
                float d = saturate(i.depth);
                return fixed4(d, d, d, 1);
            }
            ENDCG
        }
    }
}

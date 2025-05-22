Shader "Custom/LineRotateUV"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Rotation ("UV Rotation (Radians)", Float) = 0
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Rotation;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;

                // Rotate UV around (0.5, 0.5)
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                uv -= 0.5;

                float cosR = cos(_Rotation);
                float sinR = sin(_Rotation);

                float2x2 rot = float2x2(cosR, -sinR, sinR, cosR);
                uv = mul(rot, uv);

                uv += 0.5;
                o.uv = uv;

                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
}

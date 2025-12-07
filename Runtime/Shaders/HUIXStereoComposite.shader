/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Stereo Composite Shader - Combines left and right eye renders
 */

Shader "HUIX/StereoComposite"
{
    Properties
    {
        _LeftTex ("Left Eye Texture", 2D) = "white" {}
        _RightTex ("Right Eye Texture", 2D) = "white" {}
        _SplitPosition ("Split Position", Range(0, 1)) = 0.5
        _BorderWidth ("Border Width", Range(0, 0.01)) = 0.002
        _BorderColor ("Border Color", Color) = (0, 0, 0, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
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
            
            sampler2D _LeftTex;
            sampler2D _RightTex;
            float _SplitPosition;
            float _BorderWidth;
            fixed4 _BorderColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float splitPos = _SplitPosition;
                float halfBorder = _BorderWidth * 0.5;
                
                // Check if we're in the border region
                if (abs(i.uv.x - splitPos) < halfBorder)
                {
                    return _BorderColor;
                }
                
                // Left eye (left side of screen)
                if (i.uv.x < splitPos)
                {
                    float2 leftUV = float2(i.uv.x / splitPos, i.uv.y);
                    return tex2D(_LeftTex, leftUV);
                }
                // Right eye (right side of screen)
                else
                {
                    float2 rightUV = float2((i.uv.x - splitPos) / (1.0 - splitPos), i.uv.y);
                    return tex2D(_RightTex, rightUV);
                }
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}


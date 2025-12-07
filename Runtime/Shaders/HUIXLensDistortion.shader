/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Lens Distortion Shader - Corrects barrel distortion and chromatic aberration
 */

Shader "HUIX/LensDistortion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DistortionK1 ("Distortion K1", Range(0, 1)) = 0.34
        _DistortionK2 ("Distortion K2", Range(0, 1)) = 0.55
        _ChromaticRed ("Chromatic Red", Range(-0.1, 0.1)) = -0.006
        _ChromaticGreen ("Chromatic Green", Range(-0.1, 0.1)) = 0
        _ChromaticBlue ("Chromatic Blue", Range(-0.1, 0.1)) = 0.014
        _EyeOffset ("Eye Offset", Float) = 0
        _ScreenCenter ("Screen Center", Vector) = (0.5, 0.5, 0, 0)
        _Brightness ("Brightness", Range(0.5, 2)) = 1
        _Contrast ("Contrast", Range(0.5, 2)) = 1
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
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _DistortionK1;
            float _DistortionK2;
            float _ChromaticRed;
            float _ChromaticGreen;
            float _ChromaticBlue;
            float _EyeOffset;
            float4 _ScreenCenter;
            float _Brightness;
            float _Contrast;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            // Apply barrel distortion
            float2 ApplyDistortion(float2 uv, float2 center, float k1, float k2)
            {
                float2 offset = uv - center;
                float r2 = dot(offset, offset);
                float r4 = r2 * r2;
                
                float distortion = 1.0 + k1 * r2 + k2 * r4;
                
                return center + offset * distortion;
            }
            
            // Apply inverse barrel distortion for correction
            float2 ApplyInverseDistortion(float2 uv, float2 center, float k1, float k2)
            {
                float2 offset = uv - center;
                float r2 = dot(offset, offset);
                float r4 = r2 * r2;
                
                // Inverse distortion approximation
                float distortion = 1.0 - k1 * r2 - k2 * r4;
                
                return center + offset * distortion;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(0.5 + _EyeOffset, 0.5);
                
                // Calculate distorted UVs for each color channel (chromatic aberration correction)
                float2 uvR = ApplyInverseDistortion(i.uv, center, _DistortionK1 + _ChromaticRed, _DistortionK2);
                float2 uvG = ApplyInverseDistortion(i.uv, center, _DistortionK1 + _ChromaticGreen, _DistortionK2);
                float2 uvB = ApplyInverseDistortion(i.uv, center, _DistortionK1 + _ChromaticBlue, _DistortionK2);
                
                // Check if UVs are in bounds
                float inBoundsR = step(0, uvR.x) * step(uvR.x, 1) * step(0, uvR.y) * step(uvR.y, 1);
                float inBoundsG = step(0, uvG.x) * step(uvG.x, 1) * step(0, uvG.y) * step(uvG.y, 1);
                float inBoundsB = step(0, uvB.x) * step(uvB.x, 1) * step(0, uvB.y) * step(uvB.y, 1);
                
                // Sample texture for each channel
                fixed r = tex2D(_MainTex, uvR).r * inBoundsR;
                fixed g = tex2D(_MainTex, uvG).g * inBoundsG;
                fixed b = tex2D(_MainTex, uvB).b * inBoundsB;
                
                fixed4 col = fixed4(r, g, b, 1.0);
                
                // Apply brightness and contrast
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;
                col.rgb *= _Brightness;
                
                // Vignette effect (optional, helps hide edge distortion)
                float2 vignette = i.uv - center;
                float vignetteIntensity = 1.0 - saturate(length(vignette) * 1.5);
                vignetteIntensity = smoothstep(0.0, 1.0, vignetteIntensity);
                
                col.rgb *= vignetteIntensity;
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}


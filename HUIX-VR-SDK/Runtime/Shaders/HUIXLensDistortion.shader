/*
 * HUIX-VR-SDK-PHONE
 * Lens Distortion Shader
 * 
 * Barrel distortion to correct for VR lens distortion
 * Same algorithm as Google Cardboard SDK
 */

Shader "HUIX/VR/LensDistortion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _K1 ("Distortion K1", Float) = 0.441
        _K2 ("Distortion K2", Float) = 0.156
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        LOD 100
        
        Cull Off
        ZWrite Off
        ZTest Always
        
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
            float _K1;
            float _K2;
            float4 _Center;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            // Barrel distortion function
            // Uses polynomial radial distortion same as Cardboard SDK
            // p' = p * (1 + K1*r^2 + K2*r^4)
            float2 DistortUV(float2 uv, float2 center)
            {
                // Convert UV to centered coordinates (-0.5 to 0.5)
                float2 centered = uv - center;
                
                // Calculate radius squared
                float r2 = dot(centered, centered);
                float r4 = r2 * r2;
                
                // Apply polynomial distortion
                float distortionFactor = 1.0 + _K1 * r2 + _K2 * r4;
                
                // Apply distortion and convert back to UV space
                float2 distorted = centered * distortionFactor + center;
                
                return distorted;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Apply barrel distortion
                float2 distortedUV = DistortUV(i.uv, _Center.xy);
                
                // Check if UV is out of bounds (creates vignette effect)
                if (distortedUV.x < 0.0 || distortedUV.x > 1.0 || 
                    distortedUV.y < 0.0 || distortedUV.y > 1.0)
                {
                    return fixed4(0, 0, 0, 1); // Black for out of bounds
                }
                
                // Sample the texture with distorted coordinates
                fixed4 col = tex2D(_MainTex, distortedUV);
                
                return col;
            }
            ENDCG
        }
    }
    
    // Fallback for older hardware
    Fallback "Unlit/Texture"
}


Shader "Custom/HighlightShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _HighlightColor ("Highlight Color", Color) = (1, 1, 0, 1)
        _HoverIntensity ("Hover Intensity", Range(0, 1)) = 0.3
        _GrabIntensity ("Grab Intensity", Range(0, 1)) = 0.8
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 1.0
        _FresnelPower ("Fresnel Power", Range(0, 10)) = 3.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Geometry" 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            // 材质属性
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _HighlightColor;
                float _HoverIntensity;
                float _GrabIntensity;
                float _EmissionStrength;
                float _FresnelPower;
                float _Smoothness;
            CBUFFER_END
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // 采样纹理
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // 计算高亮强度（Hover和Grab的最大值）
                float highlightIntensity = max(_HoverIntensity, _GrabIntensity);
                
                // 菲涅尔效果
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                float fresnel = 1.0 - saturate(dot(normal, viewDir));
                fresnel = pow(fresnel, _FresnelPower);
                
                // 混合基础颜色和高亮颜色
                half4 baseColor = texColor * _BaseColor;
                half4 highlightColor = _HighlightColor * highlightIntensity;
                half4 finalColor = lerp(baseColor, highlightColor, fresnel * highlightIntensity);
                
                // 计算自发光
                float emission = fresnel * _EmissionStrength * highlightIntensity;
                finalColor.rgb += emission * highlightColor.rgb;
                
                // 光照计算
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normal;
                lightingInput.viewDirectionWS = viewDir;
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                SurfaceData surfaceInput = (SurfaceData)0;
                surfaceInput.albedo = finalColor.rgb;
                surfaceInput.alpha = finalColor.a;
                surfaceInput.smoothness = _Smoothness * (1.0 + highlightIntensity * 0.5);
                surfaceInput.specular = 0.5;
                surfaceInput.emission = emission * highlightColor.rgb;
                
                // 应用光照
                half4 litColor = UniversalFragmentPBR(lightingInput, surfaceInput);
                
                return litColor;
            }
            ENDHLSL
        }
        
        // 简化的阴影投射Pass
        Pass
        {
            Name "ShadowCaster"
            
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            float3 _LightDirection;
            float3 _LightPosition;
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END
            
            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return positionCS;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.positionCS = GetShadowPositionHClip(input);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            
            ENDHLSL
        }
        
        // 深度Only Pass（用于深度纹理）
        Pass
        {
            Name "DepthOnly"
            
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 position     : POSITION;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                output.uv = input.texcoord;
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
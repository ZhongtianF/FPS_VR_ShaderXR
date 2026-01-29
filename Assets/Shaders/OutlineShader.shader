Shader "Custom/OutlineShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0, 1, 0, 1)
        _OutlineThickness ("Outline Thickness", Range(0, 0.1)) = 0.05
        _OutlineIntensity ("Outline Intensity", Range(0, 2)) = 1.0
        _OutlineAlpha ("Outline Alpha", Range(0, 1)) = 0.8
        [Toggle]_OutlineEnabled ("Outline Enabled", Float) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+100" 
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        // 正常渲染Pass（保持原有外观）
        Pass
        {
            Name "ForwardLit"
            
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Cull Back
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
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
                float3 positionWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
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
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // 采样纹理
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // 光照计算
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalize(input.normalWS);
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                
                #if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
                    lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    lightingInput.shadowCoord = float4(0, 0, 0, 0);
                #endif
                
                SurfaceData surfaceInput = (SurfaceData)0;
                surfaceInput.albedo = texColor.rgb;
                surfaceInput.alpha = texColor.a;
                surfaceInput.smoothness = 0.5;
                surfaceInput.specular = 0.5;
                
                // 应用光照
                half4 color = UniversalFragmentPBR(lightingInput, surfaceInput);
                
                return color;
            }
            ENDHLSL
        }
        
        // 轮廓Pass（单独渲染）
        Pass
        {
            Name "Outline"
            
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }
            
            Cull Front
            ZWrite Off
            ZTest Greater
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
                float _OutlineIntensity;
                float _OutlineAlpha;
                float _OutlineEnabled;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 法线外扩效果
                float3 normalOS = normalize(input.normalOS);
                float3 positionOS = input.positionOS.xyz + normalOS * _OutlineThickness * _OutlineEnabled;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                output.positionHCS = vertexInput.positionCS;
                
                // 计算世界空间法线和视线方向
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // 如果轮廓未启用，返回透明
                if (_OutlineEnabled < 0.5)
                {
                    return half4(0, 0, 0, 0);
                }
                
                // 计算轮廓颜色
                half4 outlineColor = _OutlineColor * _OutlineIntensity;
                outlineColor.a = _OutlineAlpha;
                
                // 使用菲涅尔效果增强边缘
                float3 viewDir = normalize(input.viewDirWS);
                float3 normal = normalize(input.normalWS);
                float fresnel = 1.0 - saturate(dot(viewDir, normal));
                fresnel = pow(fresnel, 3.0);
                
                outlineColor.rgb *= (1.0 + fresnel * 0.5);
                
                return outlineColor;
            }
            ENDHLSL
        }
        
        // 阴影Pass
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
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
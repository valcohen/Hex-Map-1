﻿Shader "Custom/Terrain" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Terrain Texture Array", 2DArray) = "white" {}
        _GridTex ("Grid Texture", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.5 target, to enable texture arrays
		#pragma target 3.5

        #pragma multi_compile _ GRID_ON
       
        #include "HexCellData.cginc"

		UNITY_DECLARE_TEX2DARRAY(_MainTex);

		struct Input {
            float4 color : COLOR;
            float3 worldPos;
            float3 terrain;
		};

        void vert (inout appdata_full v, out Input data) {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            float4 cell0 = GetCellData(v, 0);
            float4 cell1 = GetCellData(v, 1);
            float4 cell2 = GetCellData(v, 2);

            data.terrain.x = cell0.w;
            data.terrain.y = cell0.w;
            data.terrain.z = cell0.w;
        }

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
        sampler2D _GridTex;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

        float4 GetTerrainColor (Input IN, int index) {
            float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]);
            float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw);
            return c * IN.color[index];
        }

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = 
                GetTerrainColor(IN, 0) +
                GetTerrainColor(IN, 1) +
                GetTerrainColor(IN, 2);

            fixed4 grid = 1;
            #if defined(GRID_ON)
                float2 gridUV = IN.worldPos.xz;
                gridUV.x *= 1 / (4 * 8.66025404);   // inner radius = 5 * sqrt(3), * 4 to move 2 cells right
                gridUV.y *= 1 / (2 * 15.0);         // fwd dist = 15, 2x to move 2 cells up
                grid = tex2D(_GridTex, gridUV);
            #endif

			o.Albedo = c.rgb * grid * _Color;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

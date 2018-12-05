Shader "Custom/Feature" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
        [NoScaleOffset] _GridCoordinates ("Grid Coordinates", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, transparent, no shadows
		#pragma surface surf Standard alpha vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

        #include "HexCellData.cginc"

		sampler2D _MainTex, _GridCoordinates;

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

        struct Input {
            float2 uv_MainTex;
            float  visibility;
        };

        void vert (inout appdata_full v, out Input data) {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            float3 pos = mul(unity_ObjectToWorld, v.vertex);

            float4 gridUV = float4(pos.xz, 0, 0);
            gridUV.x *= 1 / (4 * 8.66025404);   // inner radius = 5 * sqrt(3), * 4 to move 2 cells right
            gridUV.y *= 1 / (2 * 15.0);         // fwd dist = 15, 2x to move 2 cells up

            // find the 2x2 cell patch we're in by taking floor of UV coords
            // to get coords of current cell, add offsets stored in texture
            float2 cellDataCoordinates = 
                floor(gridUV.xy) + tex2Dlod(_GridCoordinates, gridUV).rg;

            // because grid patch is 2x2 and offsets are halved, double the result
            cellDataCoordinates *= 2;

            data.visibility = GetCellData(cellDataCoordinates).x;
            data.visibility = lerp(0.25, 1, data.visibility);
        }

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb * IN.visibility;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

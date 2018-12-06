Shader "Custom/Feature" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Specular ("Specular", Color) = (0.2, 0.2, 0.2)
        _BackgroundColor ("BackgroundColor", Color) = (0, 0, 0)
        [NoScaleOffset] _GridCoordinates ("Grid Coordinates", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
		LOD 200

		CGPROGRAM
		// Physically based Specular lighting model, transparent, no shadows
		#pragma surface surf StandardSpecular alpha vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

        // create shader variant HEX_MAP_EDIT_MODE for when keyword is defined
        #pragma multi_compile _ HEX_MAP_EDIT_MODE

        #include "HexCellData.cginc"

		sampler2D _MainTex, _GridCoordinates;

        half _Glossiness;
        fixed3 _Specular;
        fixed4 _Color;
        half3 _BackgroundColor;

        struct Input {
            float2 uv_MainTex;
            float2 visibility;  // visibility, explored
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

            float4 cellData = GetCellData(cellDataCoordinates);

            // set visibility
            data.visibility.x = cellData.x;
            data.visibility.x = lerp(0.25, 1, data.visibility.x);

            // set exploration
            data.visibility.y = cellData.y;
        }

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            float explored = IN.visibility.y;
			o.Albedo = c.rgb * (IN.visibility.x * explored);
            o.Specular = _Specular * explored;
			o.Smoothness = _Glossiness;
            o.Occlusion = explored;
            o.Emission = _BackgroundColor * (1 - explored);
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

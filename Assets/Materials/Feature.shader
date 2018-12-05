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

            data.visibility = 1;
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

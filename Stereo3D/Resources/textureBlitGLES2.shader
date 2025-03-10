Shader "Unlit/textureBlit GLES2"
{
    Properties
    {
       [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
	   _ShiftX ("ShiftX", Float) = 0
	   _ShiftY ("ShiftY", Float) = 0
	   _Clockwise ("Clockwise", Int) = 0
    }
    SubShader
    {
		ZWrite Off
		ZTest Always
		Cull Off

        //Tags { "RenderType"="Opaque" }

        // Tags
        // {
        //     "Queue"="Transparent"
        //     "IgnoreProjector"="True"
        //     "RenderType"="Transparent"
        //     "PreviewType"="Plane"
        //     "CanUseSpriteAtlas"="True"
        // }

        //LOD 100
        Blend One OneMinusSrcAlpha

        Pass //0 no shift
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma only_renderers gles

			struct vertexDataInput
			{
				float3 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
			int _Clockwise;

			v2f vert(vertexDataInput i)
            {
                v2f o;

				o.vertex = float4(i.pos, 1);
				o.uv = i.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }

        Pass //1 shift X & Y
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma only_renderers gles
            //#pragma target 5.0
            // make fog work
            //#pragma multi_compile_fog

            //#include "UnityCG.cginc"

            // struct appdata
            // {
            //     float4 vertex : POSITION;
            //     float2 uv : TEXCOORD0;
            // };

			struct vertexDataInput
			{
				float3 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                //UNITY_FOG_COORDS(1)
                //float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            //float4 _MainTex_ST;
			//Texture2D  _MainTex;
			//SamplerState clamp_point_sampler;
            float _ShiftX;
            float _ShiftY;
			int _Clockwise;

            //v2f vert (appdata v)
			v2f vert(vertexDataInput i)
            {
                v2f o;

				o.vertex = float4(i.pos, 1);
                o.uv.x = i.uv.x + _ShiftX;
                o.uv.y = i.uv.y + _ShiftY;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
				//fixed4 col = _MainTex.Sample(clamp_point_sampler, i.uv);
                //fixed4 col = fixed4(1, 1, 1, 0);
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        Pass //2 shift X
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma only_renderers gles
            //#pragma target 5.0
            // make fog work
            //#pragma multi_compile_fog

            //#include "UnityCG.cginc"

    //         struct appdata
    //         {
    //             float4 vertex : POSITION;
    //             float2 uv : TEXCOORD0;
				// //float4 color : COLOR0;
				// //float3 color : COLOR0;
    //         };

			struct vertexDataInput
			{
				float3 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                //UNITY_FOG_COORDS(1)
                //float4 vertex : SV_POSITION;
				//float4 color : COLOR0;
				//float3 color : COLOR0;
            };

            sampler2D _MainTex;
            //float4 _MainTex_ST;
			//Texture2D  _MainTex;
			//SamplerState clamp_point_sampler;
            float _ShiftX;
			int _Clockwise;

            //v2f vert (appdata v)
			v2f vert(vertexDataInput i)
            {
                v2f o;

				o.vertex = float4(i.pos, 1);
                o.uv.x = i.uv.x - _ShiftX;
				o.uv.y = i.uv.y;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
				//fixed4 col = _MainTex.Sample(clamp_point_sampler, i.uv);
                //fixed4 col = fixed4(1, 1, 1, 0);
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
                //return i.color;
                //return fixed4(i.color, 1);
            }
            ENDCG
        }
    }
}

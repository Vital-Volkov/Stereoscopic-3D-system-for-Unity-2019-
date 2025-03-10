Shader "Unlit/textureBlit"
{
    Properties
    {
       [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
	   _ShiftX ("ShiftX", Float) = 0
	   _ShiftY ("ShiftY", Float) = 0
	   _Clockwise ("Clockwise", Int) = 0
	   _FlipX ("FlipX", Int) = 0
	   _FlipY ("FlipY", Int) = 0
    }
    SubShader
    {
		ZWrite Off
		ZTest Always
		//Cull Off

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
			#pragma exclude_renderers gles

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
			int _Clockwise;
			int _FlipX;
			int _FlipY;

			v2f vert(uint vertexID : SV_VertexID)
            {
                v2f o;
				float2 pos;
				float2 uv;

				if (_Clockwise == 1)
				{
					//clockwise
					if (vertexID == 0)
					{
						pos = float2(-1, -1);
						uv = float2(0, 0);
						//uv = float2(1, 0);
					}
					else
						if (vertexID == 1)
						{
							pos = float2(-1, 1);
							uv = float2(0, 1);
							//uv = float2(1, 1);
						}
						else
							if (vertexID == 2)
							{
								pos = float2(1, 1);
								uv = float2(1, 1);
								//uv = float2(0, 1);
							}
							else
								if (vertexID == 3)
								{
									pos = float2(1, -1);
									uv = float2(1, 0);
									//uv = float2(0, 0);
								}
				}
				else
					//counterclockwise
					if (vertexID == 0)
					{
						pos = float2(-1, -1);
						uv = float2(0, 1);
					}
					else
						if (vertexID == 1)
						{
							pos = float2(1, -1);
							uv = float2(1, 1);
						}
						else
							if (vertexID == 2)
							{
								pos = float2(1, 1);
								uv = float2(1, 0);
							}
							else
								if (vertexID == 3)
								{
									pos = float2(-1, 1);
									uv = float2(0, 0);
								}

				if (_FlipX != 0)
					//uv.x = uv.x * -1 + 1;
					uv.x = 1 - uv.x;

				if (_FlipY != 0)
					//uv.y = uv.y * -1 + 1;
					uv.y = 1 - uv.y;

				o.vertex = float4(pos, 1, 1);
				o.uv = uv;

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
			#pragma exclude_renderers gles
            //#pragma target 5.0
            // make fog work
            //#pragma multi_compile_fog

            //#include "UnityCG.cginc"

            // struct appdata
            // {
            //     float4 vertex : POSITION;
            //     float2 uv : TEXCOORD0;
            // };

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
			int _FlipX;
			int _FlipY;

            //v2f vert (appdata v)
			v2f vert(uint vertexID : SV_VertexID)
            {
                v2f o;

                // o.vertex = UnityObjectToClipPos(v.vertex);
                // //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // o.uv = v.uv;
                // //UNITY_TRANSFER_FOG(o,o.vertex);

				//uv = float2(
				//	vertexID & 1 ? 0 : 1,  // x: 0 | 1 | 0 | 1
				//	vertexID & 2 ? 1 : 0); // y: 1 | 1 | 0 | 0

				float2 pos;
				float2 uv;

				if (_Clockwise == 1)
				{
					//clockwise
					if (vertexID == 0)
					{
						pos = float2(-1, -1);
						uv = float2(0, 0);
						//uv = float2(1, 0);
					}
					else
						if (vertexID == 1)
						{
							pos = float2(-1, 1);
							uv = float2(0, 1);
							//uv = float2(1, 1);
						}
						else
							if (vertexID == 2)
							{
								pos = float2(1, 1);
								uv = float2(1, 1);
								//uv = float2(0, 1);
							}
							else
								if (vertexID == 3)
								{
									pos = float2(1, -1);
									uv = float2(1, 0);
									//uv = float2(0, 0);
								}
				}
				else
					//counterclockwise
					if (vertexID == 0)
					{
						pos = float2(-1, -1);
						uv = float2(0, 1);
					}
					else
						if (vertexID == 1)
						{
							pos = float2(1, -1);
							uv = float2(1, 1);
						}
						else
							if (vertexID == 2)
							{
								pos = float2(1, 1);
								uv = float2(1, 0);
							}
							else
								if (vertexID == 3)
								{
									pos = float2(-1, 1);
									uv = float2(0, 0);
								}

				if (_FlipX != 0)
					//uv.x = uv.x * -1 + 1;
					uv.x = 1 - uv.x;

				if (_FlipY != 0)
					//uv.y = uv.y * -1 + 1;
					uv.y = 1 - uv.y;

				//Output output;
				//output.uv = float2(
				//	vertexID & 1 ? 0 : 1,  // x: 0 | 1 | 0 | 1
				//	vertexID & 2 ? 1 : 0); // y: 1 | 1 | 0 | 0
				//output.color = float4(output.uv, 1, 1);
				//output.pos = float4(output.uv, 0, 1);
				//return output;

				// //color = float3(uv, 1);
				// //color = float3(1, 1, 1);
				// //return float4(uv * 2 - 1, 0, 1);
				// return float4(pos, 0, 1);

                uv.x = uv.x + _ShiftX;
                uv.y = uv.y + _ShiftY;

				o.vertex = float4(pos, 1, 1);
				o.uv = uv;

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
			#pragma exclude_renderers gles
            //#pragma target 5.0
            // make fog work
            //#pragma multi_compile_fog

            //#include "UnityCG.cginc"

    //         struct appdata
    //         {
    //             float4 vertex : POSITION;
    //             float2 uv : TEXCOORD0;
    //             uint vertexID : SV_VertexID;
				// //float4 color : COLOR0;
				// //float3 color : COLOR0;
    //         };

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
			int _FlipX;
			int _FlipY;

            //v2f vert (appdata v)
			v2f vert(uint vertexID : SV_VertexID)
            {
                v2f o;

                // o.vertex = UnityObjectToClipPos(v.vertex);
                // //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // o.uv = v.uv;
                // //UNITY_TRANSFER_FOG(o,o.vertex);

				//uv = float2(
				//	vertexID & 1 ? 0 : 1,  // x: 0 | 1 | 0 | 1
				//	vertexID & 2 ? 1 : 0); // y: 1 | 1 | 0 | 0

				float2 pos;
				float2 uv;

				if (_Clockwise == 1)
				{
					//clockwise
					if (vertexID == 0)
					{
						pos = float2(-1, -1);
						uv = float2(0, 0);
						//uv = float2(1, 0);
					}
					else
						if (vertexID == 1)
						{
							pos = float2(-1, 1);
							uv = float2(0, 1);
							//uv = float2(1, 1);
						}
						else
							if (vertexID == 2)
							{
								pos = float2(1, 1);
								uv = float2(1, 1);
								//uv = float2(0, 1);
							}
							else
								if (vertexID == 3)
								{
									pos = float2(1, -1);
									uv = float2(1, 0);
									//uv = float2(0, 0);
								}
				}
				else
					//counterclockwise
					if (vertexID == 0)
					{
						pos = float2(-1, -1);
						uv = float2(0, 1);
					}
					else
						if (vertexID == 1)
						{
							pos = float2(1, -1);
							uv = float2(1, 1);
						}
						else
							if (vertexID == 2)
							{
								pos = float2(1, 1);
								uv = float2(1, 0);
							}
							else
								if (vertexID == 3)
								{
									pos = float2(-1, 1);
									uv = float2(0, 0);
								}

				// if (vertexID == 0)
				// //if (v.vertexID == 0)
				// {
				// 	pos = float2(-1, -1);
				// 	uv = float2(0, 1);
				// 	//o.color = float4(1, 1, 1, 1);
				// 	//o.color = float3(1, 1, 1);
				// }
				// else
				// 	if (vertexID == 1)
				// 	//if (v.vertexID == 1)
				// 	{
				// 		pos = float2(1, -1);
				// 		uv = float2(1, 1);
				// 		//o.color = float4(1, 0, 0, 1);
				// 		//o.color = float3(1, 0, 0);
				// 	}
				// 	else
				// 		if (vertexID == 2)
				// 		//if (v.vertexID == 2)
				// 		{
				// 			pos = float2(1, 1);
				// 		    uv = float2(1, 0);
				// 			//o.color = float4(0, 1, 0, 1);
				// 			//o.color = float3(0, 1, 0);
				// 		}
				// 		else
				// 			if (vertexID == 3)
				// 			//if (v.vertexID == 3)
				// 			{
				// 		        pos = float2(-1, 1);
				// 	            uv = float2(0, 0);
				// 				//o.color = float4(0, 0, 1, 1);
				// 				//o.color = float3(0, 0, 1);
				// 			}

				if (_FlipX != 0)
					//uv.x = uv.x * -1 + 1;
					uv.x = 1 - uv.x;

				if (_FlipY != 0)
					//uv.y = uv.y * -1 + 1;
					uv.y = 1 - uv.y;

				//Output output;
				//output.uv = float2(
				//	vertexID & 1 ? 0 : 1,  // x: 0 | 1 | 0 | 1
				//	vertexID & 2 ? 1 : 0); // y: 1 | 1 | 0 | 0
				//output.color = float4(output.uv, 1, 1);
				//output.pos = float4(output.uv, 0, 1);
				//return output;

				// //color = float3(uv, 1);
				// //color = float3(1, 1, 1);
				// //return float4(uv * 2 - 1, 0, 1);
				// return float4(pos, 0, 1);

                uv.x = uv.x - _ShiftX;

				o.vertex = float4(pos, 1, 1);
				o.uv = uv;

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

   //      Pass //3 flip X Y
   //      {
   //          CGPROGRAM
   //          #pragma vertex vert
   //          #pragma fragment frag
			// #pragma exclude_renderers gles
   //          //#pragma target 5.0
   //          // make fog work
   //          //#pragma multi_compile_fog

   //          //#include "UnityCG.cginc"

   //  //         struct appdata
   //  //         {
   //  //             float4 vertex : POSITION;
   //  //             float2 uv : TEXCOORD0;
   //  //             uint vertexID : SV_VertexID;
			// 	// //float4 color : COLOR0;
			// 	// //float3 color : COLOR0;
   //  //         };

   //          struct v2f
   //          {
   //              float4 vertex : SV_POSITION;
   //              float2 uv : TEXCOORD0;
   //              //UNITY_FOG_COORDS(1)
   //              //float4 vertex : SV_POSITION;
			// 	//float4 color : COLOR0;
			// 	//float3 color : COLOR0;
   //          };

   //          sampler2D _MainTex;
   //          //float4 _MainTex_ST;
			// //Texture2D  _MainTex;
			// //SamplerState clamp_point_sampler;
   //          float _ShiftX;
			// int _Clockwise;

   //          //v2f vert (appdata v)
			// v2f vert(uint vertexID : SV_VertexID)
   //          {
   //              v2f o;

   //              // o.vertex = UnityObjectToClipPos(v.vertex);
   //              // //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
   //              // o.uv = v.uv;
   //              // //UNITY_TRANSFER_FOG(o,o.vertex);

			// 	//uv = float2(
			// 	//	vertexID & 1 ? 0 : 1,  // x: 0 | 1 | 0 | 1
			// 	//	vertexID & 2 ? 1 : 0); // y: 1 | 1 | 0 | 0

			// 	float2 pos;
			// 	float2 uv;

			// 	if (_Clockwise == 1)
			// 	{
			// 		//clockwise
			// 		if (vertexID == 0)
			// 		{
			// 			pos = float2(-1, -1);
			// 			uv = float2(0, 0);
			// 			//uv = float2(1, 0);
			// 		}
			// 		else
			// 			if (vertexID == 1)
			// 			{
			// 				pos = float2(-1, 1);
			// 				uv = float2(0, 1);
			// 				//uv = float2(1, 1);
			// 			}
			// 			else
			// 				if (vertexID == 2)
			// 				{
			// 					pos = float2(1, 1);
			// 					uv = float2(1, 1);
			// 					//uv = float2(0, 1);
			// 				}
			// 				else
			// 					if (vertexID == 3)
			// 					{
			// 						pos = float2(1, -1);
			// 						uv = float2(1, 0);
			// 						//uv = float2(0, 0);
			// 					}
			// 	}
			// 	else
			// 		//counterclockwise
			// 		if (vertexID == 0)
			// 		{
			// 			pos = float2(-1, -1);
			// 			uv = float2(0, 1);
			// 		}
			// 		else
			// 			if (vertexID == 1)
			// 			{
			// 				pos = float2(1, -1);
			// 				uv = float2(1, 1);
			// 			}
			// 			else
			// 				if (vertexID == 2)
			// 				{
			// 					pos = float2(1, 1);
			// 					uv = float2(1, 0);
			// 				}
			// 				else
			// 					if (vertexID == 3)
			// 					{
			// 						pos = float2(-1, 1);
			// 						uv = float2(0, 0);
			// 					}

			// 	// if (_FlipX != 0)
			// 	// 	//uv.x = uv.x * -1 + 1;
			// 	uv.x = 1 - uv.x;

			// 	// if (_FlipY != 0)
			// 	// 	//uv.y = uv.y * -1 + 1;
			// 	// 	uv.y = 1 - uv.y;

			// 	//Output output;
			// 	//output.uv = float2(
			// 	//	vertexID & 1 ? 0 : 1,  // x: 0 | 1 | 0 | 1
			// 	//	vertexID & 2 ? 1 : 0); // y: 1 | 1 | 0 | 0
			// 	//output.color = float4(output.uv, 1, 1);
			// 	//output.pos = float4(output.uv, 0, 1);
			// 	//return output;

			// 	// //color = float3(uv, 1);
			// 	// //color = float3(1, 1, 1);
			// 	// //return float4(uv * 2 - 1, 0, 1);
			// 	// return float4(pos, 0, 1);

   //              //uv.x = uv.x - _ShiftX;

			// 	o.vertex = float4(pos, 1, 1);
			// 	o.uv = uv;

   //              return o;
   //          }

   //          fixed4 frag (v2f i) : SV_Target
   //          {
   //              // sample the texture
   //              fixed4 col = tex2D(_MainTex, i.uv);
			// 	//fixed4 col = _MainTex.Sample(clamp_point_sampler, i.uv);
   //              //fixed4 col = fixed4(1, 1, 1, 0);
   //              // apply fog
   //              //UNITY_APPLY_FOG(i.fogCoord, col);
   //              return col;
   //              //return i.color;
   //              //return fixed4(i.color, 1);
   //          }
   //          ENDCG
   //      }

		// //pass3 flip for Method.Two_Displays_MirrorX & Method.Two_Displays_MirrorY
		// Pass
		// {
		// 	CGPROGRAM
		// 	#pragma vertex vert
		// 	#pragma fragment frag
		// 	#pragma exclude_renderers gles

		// 	struct vertexDataOutput
		// 	{
		// 		float4 pos : SV_POSITION;
		// 		float2 uv : TEXCOORD0;
		// 	};

		// 	// int _FlipX;
		// 	// int _FlipY;

		// 	//Output main(uint vertexID : SV_VertexID)
		// 	//float4 vert(uint vertexID : SV_VertexID, out float3 color : COLOR, out float2 uv : TEXCOORD0) : SV_POSITION
		// 	//float4 vert(uint vertexID : SV_VertexID, out float2 uv : TEXCOORD0) : SV_POSITION
		// 	vertexDataOutput vert(uint vertexID : SV_VertexID)
		// 	{
		// 		vertexDataOutput o;

		// 		//uv = float2(
		// 		//	vertexID & 1 ? 0 : 1,  // x: 0 | 1 | 0 | 1
		// 		//	vertexID & 2 ? 1 : 0); // y: 1 | 1 | 0 | 0

		// 		float2 pos;
		// 		float2 uv;

		// 		if (vertexID == 0)
		// 		//if (v.vertexID == 0)
		// 		{
		// 			pos = float2(-1, -1);
		// 			uv = float2(0, 1);
		// 			//o.color = float4(1, 1, 1, 1);
		// 			//o.color = float3(1, 1, 1);
		// 		}
		// 		else
		// 			if (vertexID == 1)
		// 			//if (v.vertexID == 1)
		// 			{
		// 				pos = float2(1, -1);
		// 				uv = float2(1, 1);
		// 				//o.color = float4(1, 0, 0, 1);
		// 				//o.color = float3(1, 0, 0);
		// 			}
		// 			else
		// 				if (vertexID == 2)
		// 				//if (v.vertexID == 2)
		// 				{
		// 					pos = float2(1, 1);
		// 				    uv = float2(1, 0);
		// 					//o.color = float4(0, 1, 0, 1);
		// 					//o.color = float3(0, 1, 0);
		// 				}
		// 				else
		// 					if (vertexID == 3)
		// 					//if (v.vertexID == 3)
		// 					{
		// 				        pos = float2(-1, 1);
		// 			            uv = float2(0, 0);
		// 						//o.color = float4(0, 0, 1, 1);
		// 						//o.color = float3(0, 0, 1);
		// 					}

		// 		// if (_FlipX != 0)
		// 		// 	//uv.x = uv.x * -1 + 1;
		// 		// 	uv.x = 1 - uv.x;

		// 		// if (_FlipY != 0)
		// 			//uv.y = uv.y * -1 + 1;
		// 			uv.y = 1 - uv.y;

		// 		//Output output;
		// 		//output.uv = float2(
		// 		//	vertexID & 1 ? 0 : 1,  // x: 0 | 1 | 0 | 1
		// 		//	vertexID & 2 ? 1 : 0); // y: 1 | 1 | 0 | 0
		// 		//output.color = float4(output.uv, 1, 1);
		// 		//output.pos = float4(output.uv, 0, 1);
		// 		//return output;

		// 		// //color = float3(uv, 1);
		// 		// //color = float3(1, 1, 1);
		// 		// //return float4(uv * 2 - 1, 0, 1);
		// 		// return float4(pos, 0, 1);

		// 		o.pos = float4(pos, 0, 1);
		// 		o.uv = uv;

		// 		return o;
		// 	}

		// 	// //struct Input
		// 	// //{
		// 	// //	float4 pos : SV_POSITION;
		// 	// //	float2 coords : TEXCOORD0;
		// 	// //	float4 color : COLOR;
		// 	// //};

		// 	// //Texture2D SimpleTexture : register(t0);
		// 	// Texture2D _MainTex;
		// 	// SamplerState clamp_point_sampler : register(s0);

		// 	// //float4 main(Input input) : SV_TARGET
		// 	// //float4 main(float4 pos : SV_POSITION, float3 color : COLOR) : SV_TARGET
		// 	// //float4 frag(float4 pos : SV_POSITION, float3 color : COLOR, float2 uv : TEXCOORD0) : SV_TARGET
		// 	// float4 frag(float4 pos : SV_POSITION, float2 uv : TEXCOORD0) : SV_TARGET
		// 	// {
		// 	// 	//return float4(color, 1);
		// 	// 	//return input.color;
		// 	// 	return _MainTex.Sample(clamp_point_sampler, uv);
		// 	// }

  //           sampler2D _MainTex;

  //           fixed4 frag (v2f i) : SV_Target
  //           {
  //               // sample the texture
  //               fixed4 col = tex2D(_MainTex, i.uv);
		// 		//fixed4 col = _MainTex.Sample(clamp_point_sampler, i.uv);
  //               //fixed4 col = fixed4(1, 1, 1, 0);
  //               // apply fog
  //               //UNITY_APPLY_FOG(i.fogCoord, col);
  //               return col;
  //               //return i.color;
  //               //return fixed4(i.color, 1);
  //           }
		// 	ENDCG
		// }
    }
}

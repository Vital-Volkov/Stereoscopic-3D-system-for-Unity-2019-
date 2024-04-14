Shader "Stereo3D Screen Quad" 
{
	Properties 
	{
	   [NoScaleOffset] _MainTex ("Main Texture", 2D) = "white" {} //Graphics.Blit require this to work correct and not correct with _RightTex
	   [NoScaleOffset] _LeftTex ("Left Texture", 2D) = "white" {}
	   [NoScaleOffset] _RightTex ("Right Texture", 2D) = "white" {}

	   _LeftCol ("Left Color", Color) = (1, 0, 0)
	   _RightCol ("Right Color", Color) = (0, 1, 1)

	   _Columns ("Columns", Int) = 1
	   _Rows ("Rows", Int) = 1
	   _OddFrame ("OddFrame", Int) = 0
	   _FlipX ("FlipX", Int) = 0
	   _FlipY ("FlipY", Int) = 0
	}

	SubShader 
	{
		ZWrite Off
		ZTest Always

		//pass0 Interleaved Row
		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
		
			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertData
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertData vert(uint vertexID : SV_VertexID)
			{
				vertData o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			int _Rows;
			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			//Texture2DMS<fixed4, 8>  _LeftTex;
			//Texture2DMS<fixed4, 8>  _RightTex;
			SamplerState clamp_point_sampler;

			fixed4 frag( vertData i ) : SV_Target
			{
				fixed4 left = _LeftTex.Sample(clamp_point_sampler, i.uv);
				fixed4 right = _RightTex.Sample(clamp_point_sampler, i.uv);

				//fixed4 left = _LeftTex.Load(i.pos, 0);
				//fixed4 right = _RightTex.Load(i.pos, 0);

				//for (int j = 1; j < 8; j++)
				//{
				//	left += _LeftTex.Load(i.pos, j);
				//	right += _RightTex.Load(i.pos, j);
				//}

				//left /= 8;
				//right /= 8;

				uint row = i.uv.y * _Rows;
				//uint odd = row - row / 2 * 2;
				uint odd = row & 1 ? 1 : 0;
				return odd == 0 ? left : right;
				//return row & 1 ? right : left;
			}
			ENDCG
		}

		//pass1 Interleaved Column
		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
		
			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertData
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertData vert(uint vertexID : SV_VertexID)
			{
				vertData o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			int _Columns;
			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			SamplerState clamp_point_sampler;

			fixed4 frag( vertData i ) : SV_Target
			{
				fixed4 left = _LeftTex.Sample(clamp_point_sampler, i.uv);
				fixed4 right = _RightTex.Sample(clamp_point_sampler, i.uv);

				uint column = i.uv.x * _Columns;
				//uint odd = column - column / 2 * 2;
				uint odd = column & 1 ? 1 : 0;
				return odd == 0 ? left : right;
				//return column & 1 ? right : left;
			}
			ENDCG
		}

		//pass2 Interleaved Checkerboard
		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
		
			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertData
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertData vert(uint vertexID : SV_VertexID)
			{
				vertData o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			int _Columns;
			int _Rows;
			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			SamplerState clamp_point_sampler;

			fixed4 frag( vertData i ) : SV_Target
			{
				fixed4 left = _LeftTex.Sample(clamp_point_sampler, i.uv);
				fixed4 right = _RightTex.Sample(clamp_point_sampler, i.uv);

				uint row = i.uv.y * _Rows;
				uint column = i.uv.x * _Columns;
				//uint oddRow = row - row / 2 * 2;
				uint oddRow = row & 1 ? 1 : 0;
				//uint oddColumn = column - column / 2 * 2;
				uint oddColumn = column & 1 ? 1 : 0;
				//uint switcher = oddRow + oddColumn - 2 * oddRow * oddColumn;

				//return switcher == 0 ? left : right;
				return oddRow ^ oddColumn ? left : right;
			}
			ENDCG
		}
   
		//pass3 Side By Side
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertData
			{
				float4 pos : SV_POSITION;
				float2 uv : uv;
			};

			vertData vert(uint vertexID : SV_VertexID)
			{
				vertData o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			SamplerState repeat_point_sampler;

			fixed4 frag(vertData i) : SV_Target
			{
				return i.uv.x < 1 ? _LeftTex.Sample(repeat_point_sampler, i.uv) : _RightTex.Sample(repeat_point_sampler, i.uv);
			}
			ENDCG
		}

		//pass4 Over Under
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertData
			{
				float4 pos : SV_POSITION;
				float2 uv : uv;
			};

			vertData vert(uint vertexID : SV_VertexID)
			{
				vertData o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			SamplerState repeat_point_sampler;

			fixed4 frag(vertData i) : SV_Target
			{
				return i.uv.y < 1 ? _LeftTex.Sample(repeat_point_sampler, i.uv) : _RightTex.Sample(repeat_point_sampler, i.uv);
			}
			ENDCG
		}

		//pass5 Sequential
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertData
			{
				float4 pos : SV_POSITION;
				float2 uv : uv;
			};

			vertData vert(uint vertexID : SV_VertexID)
			{
				vertData o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			int _OddFrame;
			SamplerState repeat_point_sampler;

			fixed4 frag(vertData i) : SV_Target
			{
				return _OddFrame == 0 ? _LeftTex.Sample(repeat_point_sampler, i.uv) : _RightTex.Sample(repeat_point_sampler, i.uv);
			}
			ENDCG
		}

		//pass6 Anaglyph
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertData
			{
				float4 pos : SV_POSITION;
				float2 uv : uv;
			};

			vertData vert(uint vertexID : SV_VertexID)
			{
				vertData o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			SamplerState clamp_point_sampler;
			fixed4 _LeftCol;
			fixed4 _RightCol;

			fixed4 frag(vertData i) : SV_Target
			{
				fixed4 leftColor = _LeftTex.Sample(clamp_point_sampler, i.uv) * _LeftCol;
				fixed4 rightColor = _RightTex.Sample(clamp_point_sampler, i.uv) * _RightCol;
				return leftColor + rightColor;
			}
			ENDCG
		}

		//pass7 flip for Method.Two_Displays_MirrorX & Method.Two_Displays_MirrorY
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			int _FlipX;
			int _FlipY;

			//float4 vert(uint id : SV_VertexID, out float3 color : COL, out float2 uv : TEX) : SV_POSITION
			float4 vert(uint id : SV_VertexID, out float2 uv : TEX) : SV_POSITION
			//Output main(uint id : SV_VertexID)
			{
				//uv = float2(
				//	id & 1 ? 0 : 1,  // x: 0 | 1 | 0 | 1
				//	id & 2 ? 1 : 0); // y: 1 | 1 | 0 | 0

				float2 pos;

				if (id == 0)
				{
					pos = float2(-1, -1);
					uv = float2(0, 0);
					//uv = float2(1, 0);
				}
				else
					if (id == 1)
					{
						pos = float2(-1, 1);
						uv = float2(0, 1);
						//uv = float2(1, 1);
					}
					else
						if (id == 2)
						{
							pos = float2(1, 1);
							uv = float2(1, 1);
							//uv = float2(0, 1);
						}
						else
							if (id == 3)
							{
								pos = float2(1, -1);
								uv = float2(1, 0);
								//uv = float2(0, 0);
							}

				if (_FlipX != 0)
					uv.x = uv.x * -1 + 1;

				if (_FlipY != 0)
					uv.y = uv.y * -1 + 1;

				//color = float3(uv, 1);
				//color = float3(1, 1, 1);
				//return float4(uv * 2 - 1, 0, 1);
				return float4(pos, 0, 1);

				//Output output;
				//output.uv = float2(
				//	id & 1 ? 0 : 1,  // x: 0 | 1 | 0 | 1
				//	id & 2 ? 1 : 0); // y: 1 | 1 | 0 | 0
				//output.color = float4(output.uv, 1, 1);
				//output.pos = float4(output.uv, 0, 1);
				//return output;
			}

			//struct Input
			//{
			//	float4 pos : SV_POSITION;
			//	float2 coords : TEX;
			//	float4 color : COL;
			//};

			//Texture2D SimpleTexture : register(t0);
			Texture2D _MainTex;
			SamplerState clamp_point_sampler : register(s0);

			//float4 main(Input input) : SV_TARGET
			//float4 main(float4 pos : SV_POSITION, float3 color : COL) : SV_TARGET
			//float4 frag(float4 pos : SV_POSITION, float3 color : COL, float2 uv : TEX) : SV_TARGET
			float4 frag(float4 pos : SV_POSITION, float2 uv : TEX) : SV_TARGET
			{
				//return float4(color, 1);
				//return input.color;
				return _MainTex.Sample(clamp_point_sampler, uv);
			}
			ENDCG
		}
	}   
} 

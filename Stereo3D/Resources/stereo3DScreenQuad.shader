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
	   _FirstColumn ("FirstColumn", Int) = 0
	   _Rows ("Rows", Int) = 1
	   _FirstRow ("FirstRow", Int) = 0
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
			#pragma exclude_renderers gles
		
			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(uint vertexID : SV_VertexID)
			{
				vertexDataOutput o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];

				//o.pos = float4(0, 0, 0, 1);

				//if (vertexID == 0)
				//{
				//	o.pos.xy = float2(-1, -1);
				//	o.uv = float2(0, 0);
				//}
				//else
				//	if (vertexID == 1)
				//	{
				//		o.pos.xy = float2(-1, 1);
				//		o.uv = float2(0, 1);
				//	}
				//	else
				//		if (vertexID == 2)
				//		{
				//			o.pos.xy = float2(1, 1);
				//			o.uv = float2(1, 1);
				//		}
				//		else
				//			if (vertexID == 3)
				//			{
				//				o.pos.xy = float2(1, -1);
				//				o.uv = float2(1, 0);
				//			}

				return o;
			}

			int _Rows;
			int _FirstRow;
			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			//Texture2DMS<float4, 8>  _LeftTex;
			//Texture2DMS<float4, 8>  _RightTex;
			SamplerState clamp_point_sampler;

			float4 frag(vertexDataOutput i) : SV_Target
			{
				float4 left = _LeftTex.Sample(clamp_point_sampler, i.uv);
				float4 right = _RightTex.Sample(clamp_point_sampler, i.uv);

				//float4 left = _LeftTex.Load(i.pos, 0);
				//float4 right = _RightTex.Load(i.pos, 0);

				//for (int j = 1; j < 8; j++)
				//{
				//	left += _LeftTex.Load(i.pos, j);
				//	right += _RightTex.Load(i.pos, j);
				//}

				//left /= 8;
				//right /= 8;

				//uint row = i.uv.y * _Rows;
				uint row = i.uv.y * _Rows + _FirstRow;
				//uint row = (1 - i.uv.y) * _Rows + _FirstRow;
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
			#pragma exclude_renderers gles
		
			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(uint vertexID : SV_VertexID)
			{
				vertexDataOutput o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			int _Columns;
			int _FirstColumn;
			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			SamplerState clamp_point_sampler;

			float4 frag(vertexDataOutput i) : SV_Target
			{
				float4 left = _LeftTex.Sample(clamp_point_sampler, i.uv);
				float4 right = _RightTex.Sample(clamp_point_sampler, i.uv);

				//uint column = i.uv.x * _Columns;
				uint column = i.uv.x * _Columns + _FirstColumn;
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
			#pragma exclude_renderers gles
		
			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(uint vertexID : SV_VertexID)
			{
				vertexDataOutput o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			int _Columns;
			int _FirstColumn;
			int _Rows;
			int _FirstRow;
			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			SamplerState clamp_point_sampler;

			float4 frag(vertexDataOutput i) : SV_Target
			{
				float4 left = _LeftTex.Sample(clamp_point_sampler, i.uv);
				float4 right = _RightTex.Sample(clamp_point_sampler, i.uv);

				// uint row = i.uv.y * _Rows;
				// uint column = i.uv.x * _Columns;
				uint row = i.uv.y * _Rows + _FirstRow;
				uint column = i.uv.x * _Columns + _FirstColumn;
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
			#pragma exclude_renderers gles

			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(uint vertexID : SV_VertexID)
			{
				vertexDataOutput o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			SamplerState repeat_point_sampler;

			float4 frag(vertexDataOutput i) : SV_Target
			{
				//return i.uv.x < 1 ? _LeftTex.Sample(repeat_point_sampler, i.uv) : _RightTex.Sample(repeat_point_sampler, i.uv);
				return i.uv.x < 1 ? _LeftTex.Sample(repeat_point_sampler, i.uv) : _RightTex.Sample(repeat_point_sampler, i.uv - float2(1, 0));
			}
			ENDCG
		}

		//pass4 Over Under
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma exclude_renderers gles

			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(uint vertexID : SV_VertexID)
			{
				vertexDataOutput o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			SamplerState repeat_point_sampler;

			float4 frag(vertexDataOutput i) : SV_Target
			{
				//return i.uv.y < 1 ? _LeftTex.Sample(repeat_point_sampler, i.uv) : _RightTex.Sample(repeat_point_sampler, i.uv);
				return i.uv.y < 1 ? _LeftTex.Sample(repeat_point_sampler, i.uv) : _RightTex.Sample(repeat_point_sampler, i.uv - float2(0, 1));
			}
			ENDCG
		}

		//pass5 Sequential
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma exclude_renderers gles

			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(uint vertexID : SV_VertexID)
			{
				vertexDataOutput o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			int _OddFrame;
			SamplerState repeat_point_sampler;

			float4 frag(vertexDataOutput i) : SV_Target
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
			#pragma exclude_renderers gles

			StructuredBuffer<float2> verticesPosBuffer;
			StructuredBuffer<float2> verticesUVBuffer;

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(uint vertexID : SV_VertexID)
			{
				vertexDataOutput o;
				o.pos = float4(verticesPosBuffer[vertexID], 0, 1);
				o.uv = verticesUVBuffer[vertexID];
				return o;
			}

			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			SamplerState clamp_point_sampler;
			float4 _LeftCol;
			float4 _RightCol;

			float4 frag(vertexDataOutput i) : SV_Target
			{
				float4 leftColor = _LeftTex.Sample(clamp_point_sampler, i.uv) * _LeftCol;
				float4 rightColor = _RightTex.Sample(clamp_point_sampler, i.uv) * _RightCol;
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
			#pragma exclude_renderers gles

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			int _FlipX;
			int _FlipY;

			//Output main(uint vertexID : SV_VertexID)
			//float4 vert(uint vertexID : SV_VertexID, out float3 color : COLOR, out float2 uv : TEXCOORD0) : SV_POSITION
			//float4 vert(uint vertexID : SV_VertexID, out float2 uv : TEXCOORD0) : SV_POSITION
			vertexDataOutput vert(uint vertexID : SV_VertexID)
			{
				vertexDataOutput o;

				//uv = float2(
				//	vertexID & 1 ? 0 : 1,  // x: 0 | 1 | 0 | 1
				//	vertexID & 2 ? 1 : 0); // y: 1 | 1 | 0 | 0

				float2 pos;
				float2 uv;

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

				o.pos = float4(pos, 0, 1);
				o.uv = uv;

				return o;
			}

			//struct Input
			//{
			//	float4 pos : SV_POSITION;
			//	float2 coords : TEXCOORD0;
			//	float4 color : COLOR;
			//};

			//Texture2D SimpleTexture : register(t0);
			Texture2D _MainTex;
			SamplerState clamp_point_sampler : register(s0);

			//float4 main(Input input) : SV_TARGET
			//float4 main(float4 pos : SV_POSITION, float3 color : COLOR) : SV_TARGET
			//float4 frag(float4 pos : SV_POSITION, float3 color : COLOR, float2 uv : TEXCOORD0) : SV_TARGET
			float4 frag(float4 pos : SV_POSITION, float2 uv : TEXCOORD0) : SV_TARGET
			{
				//return float4(color, 1);
				//return input.color;
				return _MainTex.Sample(clamp_point_sampler, uv);
			}
			ENDCG
		}
	}   
} 

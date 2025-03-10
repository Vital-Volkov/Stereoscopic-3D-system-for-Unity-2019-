Shader "Stereo3D Screen Quad GLES2" //for GLES2 with vertex positions on input and no SV_VertexID
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
			#pragma only_renderers gles

			struct vertexDataInput
			{
				float3 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(vertexDataInput i)
			{
				vertexDataOutput o;
				o.pos = float4(i.pos, 1);
				o.uv = i.uv;

				return o;
			}

			int _Rows;
			int _FirstRow;
			Texture2D  _LeftTex;
			Texture2D  _RightTex;
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
			#pragma only_renderers gles

			struct vertexDataInput
			{
				float3 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(vertexDataInput i)
			{
				vertexDataOutput o;
				// o.pos = float4(pos, 1);
				// o.uv = uv;
				o.pos = float4(i.pos, 1);
				o.uv = i.uv;

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
			#pragma only_renderers gles
		
			struct vertexDataInput
			{
				float3 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(vertexDataInput i)
			{
				vertexDataOutput o;
				o.pos = float4(i.pos, 1);
				o.uv = i.uv;

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
			#pragma only_renderers gles

			struct vertexDataInput
			{
				float3 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(vertexDataInput i)
			{
				vertexDataOutput o;
				o.pos = float4(i.pos, 1);
				o.uv = i.uv;

				return o;
			}

			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			//SamplerState repeat_point_sampler;
			SamplerState clamp_point_sampler;

			float4 frag(vertexDataOutput i) : SV_Target
			{
				//return i.uv.x < 1 ? _LeftTex.Sample(repeat_point_sampler, i.uv) : _RightTex.Sample(repeat_point_sampler, i.uv - float2(1, 0));
				return i.uv.x < 0.5f ? _LeftTex.Sample(clamp_point_sampler, float2(i.uv.x * 2, i.uv.y)) : _RightTex.Sample(clamp_point_sampler, float2(i.uv.x * 2 - 1, i.uv.y));
			}
			ENDCG
		}

		//pass4 Over Under
		Pass
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

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(vertexDataInput i)
			{
				vertexDataOutput o;
				o.pos = float4(i.pos, 1);
				o.uv = i.uv;

				return o;
			}

			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			//SamplerState repeat_point_sampler;
			SamplerState clamp_point_sampler;

			float4 frag(vertexDataOutput i) : SV_Target
			{
				//return i.uv.y < 1 ? _LeftTex.Sample(repeat_point_sampler, i.uv) : _RightTex.Sample(repeat_point_sampler, i.uv - float2(0, 1));
				return i.uv.y < 0.5f ? _LeftTex.Sample(clamp_point_sampler, float2(i.uv.x, i.uv.y * 2)) : _RightTex.Sample(clamp_point_sampler, float2(i.uv.x, i.uv.y * 2 - 1));
			}
			ENDCG
		}

		//pass5 Sequential
		Pass
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

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(vertexDataInput i)
			{
				vertexDataOutput o;
				o.pos = float4(i.pos, 1);
				o.uv = i.uv;

				return o;
			}

			Texture2D  _LeftTex;
			Texture2D  _RightTex;
			int _OddFrame;
			SamplerState clamp_point_sampler;

			float4 frag(vertexDataOutput i) : SV_Target
			{
				return _OddFrame == 0 ? _LeftTex.Sample(clamp_point_sampler, i.uv) : _RightTex.Sample(clamp_point_sampler, i.uv);
			}
			ENDCG
		}

		//pass6 Anaglyph
		Pass
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

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(vertexDataInput i)
			{
				vertexDataOutput o;
				o.pos = float4(i.pos, 1);
				o.uv = i.uv;

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

		//pass7 Method.Two_Displays & Method.Two_Displays_MirrorX & Method.Two_Displays_MirrorY
		Pass
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

			struct vertexDataOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertexDataOutput vert(vertexDataInput i)
			{
				vertexDataOutput o;
				o.pos = float4(i.pos, 1);;
				o.uv = i.uv;

				return o;
			}

			Texture2D _MainTex;
			SamplerState clamp_point_sampler;

			float4 frag(vertexDataOutput i) : SV_TARGET
			{
				return _MainTex.Sample(clamp_point_sampler, i.uv);
			}
			ENDCG
		}
	}   
} 

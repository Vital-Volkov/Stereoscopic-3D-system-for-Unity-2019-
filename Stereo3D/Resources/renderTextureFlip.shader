Shader "Render Texture Flip" 
{
	Properties 
	{
	   [NoScaleOffset] _MainTex ("Render Texture", 2D) = "white" {}
	   //[NoScaleOffset] _RightTex ("Right Texture", 2D) = "white" {}

	   //_LeftCol ("Left Color", Color) = (1, 0, 0)
	   //_RightCol ("Right Color", Color) = (0, 1, 1)

	   _FlipX ("FlipX", Int) = 0
	   _FlipY ("FlipY", Int) = 0
	}

	SubShader 
	{
		Pass
		{
			ZWrite Off ZTest Always

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

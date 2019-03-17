#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_3
#endif

matrix WorldViewProjection;
float4 Color;
float2 ScreenSize;
float StrokeHalfWidth;

float ComputeAlphaFromCurveCoord(in float4 t, in float4 tx, in float4 ty)
{
	float f;
	float2 gradf;

	if (t.w != 0)
	{
		f = t.w * (t.r * t.r - t.g * t.b);
		gradf.x = t.w * (2 * t.r * tx.r - t.g * tx.b - tx.g * t.b);
		gradf.y = t.w * (2 * t.r * ty.r - t.g * ty.b - ty.g * t.b);
	}
	else
	{
		f = t.r * t.r * t.r - t.g * t.b;
		gradf.x = 3 * t.r * t.r * tx.r - tx.g * t.b - t.g * tx.b;
		gradf.y = 3 * t.r * t.r * ty.r - ty.g * t.b - t.g * ty.b;
	}

	return saturate(0.5 - f / length(gradf));
}

struct VertexShaderInput
{
	float4 Position : POSITION0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);

	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	return Color;
}

struct CurveInput
{
	float4 Position : POSITION0;
	float4 CurveCoord : TEXCOORD0;
};

struct CurveOutput
{
	float4 Position : SV_POSITION;
	float4 CurveCoord : TEXCOORD0;
};

CurveOutput CurveVS(in CurveInput input)
{
	CurveOutput output = (CurveOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.CurveCoord = input.CurveCoord;

	return output;
}

float4 CurvePS(CurveOutput input) : COLOR
{
	float alph = ComputeAlphaFromCurveCoord(input.CurveCoord, ddx(input.CurveCoord), ddy(input.CurveCoord));
	//clip(alph == 0 ? -1 : 1);
	return alph * Color;
}

struct DoubleCurveInput
{
	float4 Position : POSITION0;
	float4 CurveCoord1 : TEXCOORD0;
	float4 CurveCoord2 : TEXCOORD1;
};

struct DoubleCurveOutput
{
	float4 Position : SV_POSITION;
	float4 CurveCoord1 : TEXCOORD0;
	float4 CurveCoord2 : TEXCOORD1;
};

DoubleCurveOutput DoubleCurveVS(in DoubleCurveInput input)
{
	DoubleCurveOutput output = (DoubleCurveOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.CurveCoord1 = input.CurveCoord1;
	output.CurveCoord2 = input.CurveCoord2;

	return output;
}

float4 DoubleCurvePS(DoubleCurveOutput input) : COLOR
{
	// Check for a disjoint union first
	float4 cc1 = input.CurveCoord1;
	bool dj = cc1.w >= 2.0;
	if (dj) cc1.w -= 4.0;

	float alph1 = ComputeAlphaFromCurveCoord(cc1, ddx(input.CurveCoord1), ddy(input.CurveCoord1));
	float alph2 = ComputeAlphaFromCurveCoord(input.CurveCoord2, ddx(input.CurveCoord2), ddy(input.CurveCoord2));

	//clip(alph1 == 0 || alph2 == 0 ? -1 : 1);
	if (dj) return (alph1 + alph2 - alph1 * alph2) * Color;
	else return alph1 * alph2 * Color;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};

technique CurveDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL CurveVS();
		PixelShader = compile PS_SHADERMODEL CurvePS();
	}
};

technique DoubleCurveDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL DoubleCurveVS();
		PixelShader = compile PS_SHADERMODEL DoubleCurvePS();
	}
};
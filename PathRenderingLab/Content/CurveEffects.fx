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

void DeriveCurveFunctions(in float4 t, in float4 tx, in float4 ty, out float f, out float2 gradf)
{
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
}

float4 CurvePS(CurveOutput input) : COLOR
{
	float4 t = input.CurveCoord;
	float4 tx = ddx(input.CurveCoord);
	float4 ty = ddy(input.CurveCoord);

	float f;
	float2 gradf;

	DeriveCurveFunctions(t, tx, ty, f, gradf);

	float alph = saturate(0.5 - f / length(gradf));
	clip(alph);

	return alph * Color;
}

struct StrokeInput
{
	float4 Position : POSITION0;
	float4 CurveCoord : TEXCOORD0;
	float4 CurveCoordX : TEXCOORD1;
	float4 CurveCoordY : TEXCOORD2;
};

struct StrokeOutput
{
	float4 Position : SV_POSITION;
	float4 CurveCoord : TEXCOORD0;
	float4 CurveCoordX : TEXCOORD1;
	float4 CurveCoordY : TEXCOORD2;
};

StrokeOutput StrokeVS(in StrokeInput input)
{
	StrokeOutput output = (StrokeOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.CurveCoord = input.CurveCoord;
	output.CurveCoordX = input.CurveCoordX;
	output.CurveCoordY = input.CurveCoordY;

	return output;
}

float4 StrokePS(StrokeOutput input) : COLOR
{
	float4 t = input.CurveCoord;
	float4 txw = ddx(input.CurveCoord);
	float4 tyw = ddy(input.CurveCoord);
	float4 txa = input.CurveCoordX;
	float4 tya = input.CurveCoordY;

	float f;
	float2 gradfw, gradfa, gradfaxw, gradfayw;
	DeriveCurveFunctions(t, txw, tyw, f, gradfw);
	DeriveCurveFunctions(t, txa, tya, f, gradfa);

	if (t.w != 0)
	{
		gradfaxw.x = t.w * (2 * txw.r * txa.r - txw.g * txa.b - txw.b * txa.g);
		gradfaxw.y = t.w * (2 * txw.r * tya.r - txw.g * tya.b - txw.b * tya.g);
		gradfayw.x = t.w * (2 * tyw.r * txa.r - tyw.g * txa.b - tyw.b * txa.g);
		gradfayw.y = t.w * (2 * tyw.r * tya.r - tyw.g * tya.b - tyw.b * tya.g);
	}
	else
	{
		gradfaxw.x = 6 * t.r * txw.r * txa.r - txw.g * txa.b - txw.b * txa.g;
		gradfaxw.y = 6 * t.r * txw.r * tya.r - txw.g * tya.b - txw.b * tya.g;
		gradfayw.x = 6 * t.r * tyw.r * txa.r - tyw.g * txa.b - tyw.b * txa.g;
		gradfayw.y = 6 * t.r * tyw.r * tya.r - tyw.g * tya.b - tyw.b * tya.g;
	}

	float2 u = float2(dot(gradfa, gradfaxw), dot(gradfa, gradfayw));

	float lfa = length(gradfa);
	float g = abs(f) / lfa - StrokeHalfWidth;
	float2 gradgw = sign(f) * (gradfw - u * f / (2 * lfa * lfa)) / lfa;

	float alph = saturate(0.5 - g / length(gradgw));
	//clip(-g);

	return alph * Color;
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

technique CurveStroke
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL StrokeVS();
		PixelShader = compile PS_SHADERMODEL StrokePS();
	}
};
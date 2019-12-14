
float2 Focus, CF;
float a, rfr, fr2;
sampler ColorRamp : register(s0);

float4 ComputeColor(float2 u, float2 dudx, float2 dudy)
{
	float2 uf = u - Focus;
	float b = dot(uf, CF) + rfr;
	float c = dot(uf, uf) - fr2;

	float dbdx = dot(dudx, CF);
	float dcdxh = dot(dudx, uf);
	
	float dbdy = dot(dudy, CF);
	float dcdyh = dot(dudy, uf);

	float D = b * b - a * c;
	float sqrtD = sqrt(D);
	clip(D <= 0.0f ? -1 : 1);

	float q = b + sqrtD;
	float dqdx = dbdx + (b * dbdx - a * dcdxh) / sqrtD;
	float dqdy = dbdy + (b * dbdy - a * dcdyh) / sqrtD;

	float2 p = float2(c / q, 0.0);
	float2 dpdx = float2((2 * dcdxh * q - c * dqdx) / (q * q), 0.0);
	float2 dpdy = float2((2 * dcdyh * q - c * dqdy) / (q * q), 0.0);

	return tex2D(ColorRamp, p, dpdx, dpdy);
}

#include "CurveEffects.fxinc"

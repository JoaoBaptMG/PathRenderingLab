
float2 Origin, Direction;
sampler ColorRamp : register(s0);

float4 ComputeColor(float2 u, float2 dudx, float2 dudy)
{
	float2 p = float2(dot(u - Origin, Direction) / dot(Direction, Direction), 0.0);
	float2 dpdx = float2(dot(dudx, Direction) / dot(Direction, Direction), 0.0);
	float2 dpdy = float2(dot(dudy, Direction) / dot(Direction, Direction), 0.0);

	return tex2D(ColorRamp, p, dpdx, dpdy);
}

#include "CurveEffects.fxinc"

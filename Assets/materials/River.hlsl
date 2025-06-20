#include "Water.hlsl"
#include "HexCellData.hlsl"

void GetVertexDataRiver_float(
    float3 Indices,
    float3 Weights,
    out float Visibility
) {
    float4 cell0 = GetCellData(Indices.x);
    float4 cell1 = GetCellData(Indices.y);

    Visibility = cell0.x * Weights.x + cell1.x * Weights.y;
    Visibility = lerp(0.25, 1, Visibility);
}


void FgetFragmentDataRiver_float(
    UnityTexture2D NoiseTexture,
    float2 RiverUV, 
    float4 Color, 
    float Time,
    float Visibility,
    out float3 BaseColor,
    out float Alpha) {
    float river = River(RiverUV, Time, NoiseTexture);
    float4 c = saturate(Color + river);
    BaseColor = c.rgb * Visibility;
    Alpha = c.a;
}
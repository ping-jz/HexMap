#include "Water.hlsl"
#include "HexCellData.hlsl"


void GetVertexDataWaterShore_float(
    float3 Indices,
    float3 Weights,
    out float Visibility
) {
    float4 cell0 = GetCellData(Indices.x);
    float4 cell1 = GetCellData(Indices.y);
    float4 cell2 = GetCellData(Indices.z);

    Visibility = cell0.x * Weights.x + cell1.x * Weights.y + cell2.x * Weights.z;
    Visibility = lerp(0.25, 1, Visibility);
}

void FgetFragmentDataWaterShore_float(
    UnityTexture2D NoiseTexture,
    float3 WorldPosition,
    float4 Color, 
    float2 ShoreUV,
    float Time,
    float Visibility,
    out float3 BaseColor,
    out float Alpha) {

    float shore = ShoreUV.y;
    float foam = Foam(shore, WorldPosition.xz, Time, NoiseTexture);
    float waves = Waves(WorldPosition.xz, Time, NoiseTexture);
    waves *= 1 - shore;
    
    float4 c = saturate(Color + max(foam, waves));
    BaseColor = c.rgb * Visibility;
    Alpha = c.a;
}
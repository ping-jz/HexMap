#include "Water.hlsl"
#include "HexCellData.hlsl"

void GetVertexDataWater_float(
    bool editMode,
    float3 Indices,
    float3 Weights,
    out float Visibility
) {
    float4 cell0 = GetCellData(editMode, Indices.x);
    float4 cell1 = GetCellData(editMode, Indices.y);
    float4 cell2 = GetCellData(editMode, Indices.z);

    Visibility = cell0.x * Weights.x + cell1.x * Weights.y + cell2.x * Weights.z;
    Visibility = lerp(0.25, 1, Visibility);
}

void FgetFragmentDataWater_float(
    UnityTexture2D NoiseTexture,
    float3 WorldPosition,
    float4 Color, 
    float Time,
    float Visibility,
    out float3 BaseColor,
    out float Alpha) {
    float waves = Waves(WorldPosition.xz, Time, NoiseTexture);

    float4 c = saturate(Color + waves);
    BaseColor = c.rgb * Visibility;
    Alpha = c.a;
}
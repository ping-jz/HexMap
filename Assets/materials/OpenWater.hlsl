#include "Water.hlsl"
#include "HexCellData.hlsl"

void GetVertexDataWater_float(
    bool editMode,
    float3 Indices,
    float3 Weights,
    out float2 Visibility
) {
    float4 cell0 = GetCellData(editMode, Indices.x);
    float4 cell1 = GetCellData(editMode, Indices.y);
    float4 cell2 = GetCellData(editMode, Indices.z);

    Visibility.x = cell0.x * Weights.x + cell1.x * Weights.y + cell2.x * Weights.z;
    Visibility.x = lerp(0.25, 1, Visibility.x);
    Visibility.y = cell0.y * Weights.x + cell1.y * Weights.y + cell2.y * Weights.z;;
}

void FgetFragmentDataWater_float(
    UnityTexture2D NoiseTexture,
    float3 WorldPosition,
    float4 Color, 
    float Time,
    float2 Visibility,
    out float3 BaseColor,
    out float Alpha,
    out float Exploration
    ) {
    float waves = Waves(WorldPosition.xz, Time, NoiseTexture);

    float4 c = saturate(Color + waves);
    BaseColor = c.rgb * Visibility.x;
    Alpha = c.a;
    Exploration = Visibility.y;
}
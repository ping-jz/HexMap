#include "Water.hlsl"
#include "HexCellData.hlsl"


void GetVertexDataWaterEstuary_float(
    bool editMode,
    float3 Indices,
    float3 Weights,
    out float2 Visibility
) {
    float4 cell0 = GetCellData(editMode, Indices.x);
    float4 cell1 = GetCellData(editMode, Indices.y);

    Visibility.x = cell0.x * Weights.x + cell1.x * Weights.y;
    Visibility.x = lerp(0.25, 1, Visibility.x);
    Visibility.y = cell0.y * Weights.x + cell1.y * Weights.y;
}


void FgetFragmentDataEstuary_float(
    UnityTexture2D NoiseTexture,
    float3 WorldPosition,
    float4 Color, 
    float2 ShoreUV,
    float2 RiverUV,
    float Time,
    float2 Visibility,
    out float3 BaseColor,
    out float Alpha,
    out float Exploration
    ) {

    float shore = ShoreUV.y;
    float foam = Foam(shore, WorldPosition.xz, Time, NoiseTexture);
    float waves = Waves(WorldPosition.xz, Time, NoiseTexture);
    waves *= 1 - shore;

    float river = River(RiverUV, Time, NoiseTexture);

    float shoreWater = max(foam, waves);
    float water = lerp(shoreWater, river, ShoreUV.x);
    float4 c = saturate(Color + water);
    BaseColor = c.rgb * Visibility.x;
    Alpha = c.a;
    Exploration = Visibility.y;
}
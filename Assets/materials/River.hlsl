#include "Water.hlsl"

void GetVertexDataRiver_float(
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


void FgetFragmentDataRiver_float(
    UnityTexture2D NoiseTexture,
    float2 RiverUV, 
    float4 Color, 
    float Time,
    float2 Visibility,
    out float3 BaseColor,
    out float Alpha,
    out float Exploration
    ) {
    float river = River(RiverUV, Time, NoiseTexture);
    float4 c = saturate(Color + river);
    BaseColor = c.rgb * Visibility.x;
    Alpha = c.a;
    Exploration = Visibility.y;
}
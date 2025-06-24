
#include "HexCellData.hlsl"

void FgetFragmentDataRoad_float(
    UnityTexture2D NoiseTexture,
    float3 WorldPosition,
    float2 BlendUV, 
    float4 Color, 
    float2 Visibility,
    out float3 BaseColor,
    out float Alpha,
    out float Exploration
    ) {
    float4 noise = NoiseTexture.Sample(
            NoiseTexture.samplerstate, WorldPosition.xz * (3 * TILING_SCALE));
    BaseColor = Color.rgb * ((noise.y * 0.75 + 0.25)) * Visibility.x;
    Alpha = BlendUV.x;
    Alpha *= noise.x + 0.5;
    Alpha = smoothstep(0.4, 0.7, Alpha);
    Exploration = Visibility.y;
} 

void FgetRoadViewOffset_float(
    bool editMode,
    float3 OriginPosition, 
    float YViewOffset, 
    float3 Indices,
    float3 Weights,
    out float3 Position,
    out float2 Visibility) {
    Position = OriginPosition;
    Position.y += YViewOffset;

    float4 cell0 = GetCellData(editMode, Indices.x);
    float4 cell1 = GetCellData(editMode, Indices.y);

    Visibility.x = cell0.x * Weights.x + cell1.x * Weights.y;
    Visibility.x = lerp(0.25, 1, Visibility.x);
    Visibility.y = cell0.y * Weights.x + cell1.y * Weights.y;
}

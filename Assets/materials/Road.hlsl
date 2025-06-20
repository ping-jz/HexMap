
#include "HexCellData.hlsl"

void FgetFragmentDataRoad_float(
    UnityTexture2D NoiseTexture,
    float3 WorldPosition,
    float2 BlendUV, 
    float4 Color, 
    float Visibility,
    out float3 BaseColor,
    out float Alpha) {
    float4 noise = NoiseTexture.Sample(
            NoiseTexture.samplerstate, WorldPosition.xz * (3 * TILING_SCALE));
    BaseColor = Color.rgb * ((noise.y * 0.75 + 0.25)) * Visibility;
    Alpha = BlendUV.x;
    Alpha *= noise.x + 0.5;
    Alpha = smoothstep(0.4, 0.7, Alpha);
} 

void FgetRoadViewOffset_float(
    float3 OriginPosition, 
    float YViewOffset, 
    float3 Indices,
    float3 Weights,
    out float3 Position,
    out float Visibility) {
    Position = OriginPosition;
    Position.y += YViewOffset;

    float4 cell0 = GetCellData(Indices.x);
    float4 cell1 = GetCellData(Indices.y);

    Visibility = cell0.x * Weights.x + cell1.x * Weights.y;
    Visibility = lerp(0.25, 1, Visibility);
}

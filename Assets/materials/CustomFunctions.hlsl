#include "Water.hlsl"

void FgetFragmentDataRiver_float(
    UnityTexture2D NoiseTexture,
    float2 RiverUV, 
    float4 Color, 
    float Time,  
    out float3 BaseColor,
    out float Alpha) {
    float river = River(RiverUV, Time, NoiseTexture);
    float4 c = saturate(Color + river);
    BaseColor = c.rgb;
    Alpha = c.a;
}

void FgetFragmentDataRoad_float(
    float2 BlendUV, 
    float4 Color, 
    out float3 BaseColor,
    out float Alpha) {
    float4 c = float4(BlendUV.x, BlendUV.y, 1.0, 1.0);
    BaseColor = c.rgb;
    Alpha = BlendUV.x;
} 
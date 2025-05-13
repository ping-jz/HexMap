#include "Water.hlsl"

void FgetFragmentDataRiver_float(
    float2 RiverUV, 
    float4 Color, 
    float Time,  
    out float3 BaseColor) {
    float river = River(RiverUV, Time);
    float4 c = saturate(Color + river);
    BaseColor = c.rgb;
} 
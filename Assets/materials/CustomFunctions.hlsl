#include "Water.hlsl"
#include "HexMertics.hlsl"

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
    UnityTexture2D NoiseTexture,
    float3 WorldPosition,
    float2 BlendUV, 
    float4 Color, 
    out float3 BaseColor,
    out float Alpha) {
    float4 noise = NoiseTexture.Sample(
            NoiseTexture.samplerstate, WorldPosition.xz * (3 * TILING_SCALE));
    BaseColor = Color.rgb * (noise.y * 0.75 + 0.25);
    Alpha = BlendUV.x;
    Alpha *= noise.x + 0.5;
    Alpha = smoothstep(0.4, 0.7, Alpha);
} 

void FgetRoadViewOffset_float(
    float3 OriginPosition, 
    float YViewOffset, 
    out float3 Position) {
    Position = OriginPosition;
    Position.y += YViewOffset;
}

void FgetFragmentDataWater_float(
    UnityTexture2D NoiseTexture,
    float3 WorldPosition,
    float4 Color, 
    float Time,  
    out float3 BaseColor,
    out float Alpha) {
    float waves = Waves(WorldPosition.xz, Time, NoiseTexture);

    float4 c = saturate(Color + waves);
    BaseColor = c.rgb;
    Alpha = c.a;
}

void FgetFragmentDataWaterShore_float(
    UnityTexture2D NoiseTexture,
    float3 WorldPosition,
    float4 Color, 
    float2 ShoreUV,
    float Time,  
    out float3 BaseColor,
    out float Alpha) {

    float shore = ShoreUV.y;
    float foam = Foam(shore, WorldPosition.xz, Time, NoiseTexture);
    float waves = Waves(WorldPosition.xz, Time, NoiseTexture);
    waves *= 1 - shore;
    
    float4 c = saturate(Color + max(foam, waves));
    BaseColor = c.rgb;
    Alpha = c.a;
}

void FgetFragmentDataEstuary_float(
    UnityTexture2D NoiseTexture,
    float3 WorldPosition,
    float4 Color, 
    float2 ShoreUV,
    float2 RiverUV,
    float Time,  
    out float3 BaseColor,
    out float Alpha) {

    float shore = ShoreUV.y;
    float foam = Foam(shore, WorldPosition.xz, Time, NoiseTexture);
    float waves = Waves(WorldPosition.xz, Time, NoiseTexture);
    waves *= 1 - shore;

    float river = River(RiverUV, Time, NoiseTexture);

    float shoreWater = max(foam, waves);
    float water = lerp(shoreWater, river, ShoreUV.x);
    float4 c = saturate(Color + water);
    BaseColor = c.rgb;
    Alpha = c.a;
}
float4 GetTerrianColor(UnityTexture2DArray textures, float3 worldPosition, float terrian, float color) {
    float3 uvw = float3(worldPosition.xz * 0.02, terrian);
    float4 c = textures.Sample(textures.samplerstate, uvw);
    return c * color;
}

void FgetFragmentDataTerrian_float(
    UnityTexture2DArray Textures,
    float3 WorldPosition,
    float4 color,
    float4 terrian,
    out float3 BaseColor) {
    float4 c = GetTerrianColor(Textures, WorldPosition, terrian.x, color.x) + 
               GetTerrianColor(Textures, WorldPosition, terrian.y, color.y) + 
               GetTerrianColor(Textures, WorldPosition, terrian.z, color.z);
    BaseColor = c.rgb;
}
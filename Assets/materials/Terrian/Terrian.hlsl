#include "../HexCellData.hlsl"

void GetVertexCellData_float(
    float3 Indices,
    float3 Weights,
    out float4 Terrain,
    out float4 Visibility
) {
    float4 cell0 = GetCellData(Indices.x);
    float4 cell1 = GetCellData(Indices.y);
    float4 cell2 = GetCellData(Indices.z);

    Terrain.x = cell0.w;
    Terrain.y = cell1.w;
    Terrain.z = cell2.w;
    Terrain.w = 0.0;

    Visibility.x = cell0.x;
    Visibility.y = cell1.x;
    Visibility.z = cell2.x;
    Visibility.w = 0.0;
    Visibility = lerp(0.25, 1, Visibility);
}

//根据给定的素材，使用世界坐标，素材下标，然后color应该是权重的意思
//来进行素材采样
float4 GetTerrianColor(UnityTexture2DArray textures, float3 worldPosition, float terrian, float weight, float visibility) {
    float3 uvw = float3(worldPosition.xz * 0.02, terrian);
    float4 c = textures.Sample(textures.samplerstate, uvw);
    return c * (weight * visibility);
}

void FgetFragmentDataTerrian_float(
    UnityTexture2DArray Textures,
    UnityTexture2D Grid,
    float3 WorldPosition,
    float4 weights,
    float4 terrian,
    float4 visibility,
    bool showGrid,
    out float3 BaseColor) {
    float4 c = GetTerrianColor(Textures, WorldPosition, terrian.x, weights.x, visibility.x) + 
               GetTerrianColor(Textures, WorldPosition, terrian.y, weights.y, visibility.y) + 
               GetTerrianColor(Textures, WorldPosition, terrian.z, weights.z, visibility.z);

    BaseColor = c.rgb;

    if(showGrid) {
        float2 gridUV = WorldPosition.xz;
        gridUV.x *= 1 / (4 * OUTER_RADIUS * OUTER_TO_INNER);
        gridUV.y *= 1 / (2 * 15.0);
        float4 grid = Grid.Sample(Grid.samplerstate, gridUV);
        BaseColor *= grid;
    }
}
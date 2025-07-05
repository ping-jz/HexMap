#include "../HexCellData.hlsl"

void GetVertexCellData_float(
    bool editMode,
    bool showMapData,
    float3 Indices,
    float3 Weights,
    out float4 Terrain,
    out float4 Visibility,
    out float MapData
) {
    float4 cell0 = GetCellData(editMode, Indices.x);
    float4 cell1 = GetCellData(editMode, Indices.y);
    float4 cell2 = GetCellData(editMode, Indices.z);

    Terrain.x = cell0.w;
    Terrain.y = cell1.w;
    Terrain.z = cell2.w;
    Terrain.w = 0.0;

    Visibility.x = cell0.x;
    Visibility.y = cell1.x;
    Visibility.z = cell2.x;
    Visibility.xyz = lerp(0.25, 1, Visibility.xyz);
    Visibility.w = 
        cell0.y * Weights.x + cell1.y * Weights.y + cell2.y * Weights.z;
    //for debug only
    MapData = -1.0;
    if (showMapData) {
        MapData = cell0.z * Weights + cell1.z * Weights.y + cell2.z * Weights.z;
    }
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
    //for debug only
    float mapData,
    float3 WorldPosition,
    float4 weights,
    float4 terrian,
    float4 visibility,
    bool showGrid,
    out float3 BaseColor,
    out float Exploration
    ) {
    float4 c = GetTerrianColor(Textures, WorldPosition, terrian.x, weights.x, visibility.x) + 
               GetTerrianColor(Textures, WorldPosition, terrian.y, weights.y, visibility.y) + 
               GetTerrianColor(Textures, WorldPosition, terrian.z, weights.z, visibility.z);


    
    float4 grid = 1;
    if(showGrid) {
        float2 gridUV = WorldPosition.xz;
        gridUV.x *= 1 / (4 * OUTER_RADIUS * OUTER_TO_INNER);
        gridUV.y *= 1 / (2 * 15.0);
        grid = Grid.Sample(Grid.samplerstate, gridUV);
    }
    if(0 <= mapData) {
        //for debug only
        BaseColor = mapData * grid;
    } else {
        BaseColor = c.rgb * grid;
    }

    Exploration = visibility.w;
}
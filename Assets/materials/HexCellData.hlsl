#include "HexMertics.hlsl"

TEXTURE2D(_HexCellData);
SAMPLER(sampler_HexCellData);
float4 _HexCellData_TexelSize;

float4 FilterCellData(bool editMode, float4 data) {
    if (editMode) {
        data.xy = 1;
    } 
        
    return data;
}

float4 GetCellData(bool editMode, float uvComponent) {
    float2 uv;
    //二维转一维，看着不太像？？
    //实在没搞懂
    uv.x = (uvComponent + 0.5) * _HexCellData_TexelSize.x;
    float row = floor(uv.x);
    uv.x -= row;
    uv.y = (row + 0.5) * _HexCellData_TexelSize.y;
    //地形素材的坐标就在第四个位置那里，但为什么可以锁定在255，看下具体素材的参数
    //应该是素材的数量就只有5个，255足够了
    float4 data = SAMPLE_TEXTURE2D_LOD(_HexCellData, sampler_HexCellData, uv, 0);
    data.w *= 255;
    return FilterCellData(editMode, data);
}

float4 GetCellData(bool editMode,  float2 cellDataCoordinates) {
    float2 uv2 = cellDataCoordinates + 0.5;
    uv2.x *= _HexCellData_TexelSize.x;
    uv2.y *= _HexCellData_TexelSize.y;
    return FilterCellData(editMode, SAMPLE_TEXTURE2D_LOD(_HexCellData, sampler_HexCellData, uv2, 0));
}

#define HEX_ANGLED_EDGE_VECTOR float2(1, sqrt(3))

// 2025-06-20 先抄答案吧
// Features and Visibility计算特色中提到计算格子坐标的方法失效了
// Calculate hex-based modulo to find position vector.
float2 HexModulo(float2 p)
{
	return p - HEX_ANGLED_EDGE_VECTOR * floor(p / HEX_ANGLED_EDGE_VECTOR);
}

// 2025-06-20 先抄答案吧
// Features and Visibility计算特色中提到计算格子坐标的方法失效了
// Get hex grid data analytically derived from world-space XZ position.
float2 GetHexGridData(float2 worldPositionXZ)
{
	float2 p = WoldToHexSpace(worldPositionXZ);
	
	// Vectors from nearest two cell centers to position.
	float2 gridOffset = HEX_ANGLED_EDGE_VECTOR * 0.5;
	float2 a = HexModulo(p) - gridOffset;
	float2 b = HexModulo(p - gridOffset) - gridOffset;
	bool aIsNearest = dot(a, a) < dot(b, b);

	float2 vectorFromCenterToPosition = aIsNearest ? a : b;


	float2 cellCenter = p - vectorFromCenterToPosition;
    float2 cellOffsetCoordinates;
	cellOffsetCoordinates.x = cellCenter.x - (aIsNearest ? 0.5 : 0.0);
	cellOffsetCoordinates.y = cellCenter.y / OUTER_TO_INNER;
	return cellOffsetCoordinates;
}

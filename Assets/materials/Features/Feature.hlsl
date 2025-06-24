#include "../HexCellData.hlsl"


// 2025-06-20 先抄答案吧
// Features and Visibility计算特色中提到计算格子坐标的方法失效了

//这段代码你要看这里，不要直接读
//初步理解就是。
//有一个六边形素材，包含2*2的六边形区域。然后的它的显示模式为重复，所以我可以通过对它
//进行采样来确定所在单元格。具体思路是这样
//细节等你熟悉之后在纠结吧
//https://catlikecoding.com/unity/tutorials/hex-map/part-20/#4
void GetVertexDataFeature_float(
    bool editMode,
    float3 position,
    out float2 Visibility
) {
    float2 cellOffsetCoordinates = GetHexGridData(position.xz);
	float4 cellData = GetCellData(editMode, cellOffsetCoordinates);

	Visibility.x = cellData.x;
	Visibility.x = lerp(0.25, 1, Visibility.x);
	Visibility.y = 1;
}

// 2025-06-20 先抄答案吧
// Features and Visibility计算特色中提到计算格子坐标的方法失效了
void FgetFragmentDataFeature_float(
    float4 Color,
    float2 Visibility,
    out float3 BaseColor) {
    BaseColor = Color * Visibility.x;
}
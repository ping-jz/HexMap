#include "HexMertics.hlsl"

TEXTURE2D(_HexCellData);
SAMPLER(sampler_HexCellData);
float4 _HexCellData_TexelSize;

float4 GetCellData(float uvComponent) {
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
    return data;
}

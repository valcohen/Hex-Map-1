﻿sampler2D   _HexCellData;
float4      _HexCellData_TexelSize;

/*
    force visibility = 1 in edit mode
*/
float4 FilterCellData (float4 data) {
    #if defined(HEX_MAP_EDIT_MODE)
        data.xy = 1;    // xy = visibility, exploration
    #endif
    return data;
}

/*
    cell indices are stored in v.texcoord2

    convert x & y indices to UV coordinates to sample cell data texture:

    U : divide cell index by texture wdth, by multiplying by TexelSize.x 
    (x, y hold multiplicative inverses of width & height)
    > returns Z.U, where Z is the row index and U is the U coord of the cell
    extract the row by flooring x, then subtract that from x to get U

    V: same but use textture height (TexelSize.y)

    To sample the center of the pixel, rather than the edge, add 0.5 before
    dividing by the texture sizes.
*/
float4 GetCellData (appdata_full v, int index) {
    float2 uv;

    uv.x = (v.texcoord2[index] + 0.5) * _HexCellData_TexelSize.x;    
    float row = floor(uv.x);
    uv.x -= row;
    uv.y = (row + 0.5) * _HexCellData_TexelSize.y;

    // sample _HexCellData
    float4 data = tex2Dlod(_HexCellData, float4(uv, 0, 0));

    // 4th data component is terrain type index, stored as a byte.
    // The GPU converts it to a float 0..1 ; convert back by multiplying by 255
    data.w *= 255;
    return FilterCellData(data);
}

/*
    convert XZ grid coordinates to cell data UV coordinates:
    shift to the pixel centers,
    then divide by texture sizes, 
    then sample the texture.
*/
float GetCellData (float2 cellDataCoordinates) {
    float2 uv = cellDataCoordinates + 0.5;

    uv.x *= _HexCellData_TexelSize.x;
    uv.y *= _HexCellData_TexelSize.y;

    return FilterCellData(tex2Dlod(_HexCellData, float4(uv, 0, 0)));
}
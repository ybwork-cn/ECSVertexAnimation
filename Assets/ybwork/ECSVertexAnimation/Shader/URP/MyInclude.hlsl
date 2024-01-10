// Created by 月北(ybwork-cn) https://github.com/ybwork-cn/

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float GetLoopTime(float time, float duration)
{
    return frac(time / duration);
}

float GetClampTime(float time, float duration)
{
    return clamp(time / duration, 0, 1);
}

float4 GetPosition(uint vid)
{
    float t = _CurrentTime;
    if(_Loop)
        t = GetLoopTime(t, _AnimLen);
    else
        t = GetClampTime(t, _AnimLen);
    float animMap_x = (vid + 0.5) * _AnimMap_TexelSize.x;
    float animMap_y = t;
    return tex2Dlod(_AnimMap, float4(animMap_x, animMap_y, 0, 0));
}

float4 GetNormal(uint vid)
{
    float t = UNITY_ACCESS_INSTANCED_PROP(Props, _CurrentTime);
    if(_Loop)
        t = GetLoopTime(t, _AnimLen);
    else
        t = GetClampTime(t, _AnimLen);
    float animMapNormal_x = (vid + 0.5) * _AnimMapNormal_TexelSize.x;
    float animMapNormal_y = t;
    return tex2Dlod(_AnimMapNormal, float4(animMapNormal_x, animMapNormal_y, 0, 0));
}

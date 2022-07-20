
CBUFFER_START(UnityInstanced)

Texture2DArray _visibleTexture;
SamplerState sampler_visibleTexture;
float4 _visibleTexture_TexelSize;//1 / width, 1 / height, width, height


CBUFFER_END

int lod;


inline int2 GetUV(int index, int width)
{
    return int2(index % width, index / width);
}

inline float2 GetUV(int index, float4 texSize)
{
    return (float2(index % (uint)texSize.z, index / (uint)texSize.z) + float2(0.5, 0.5)) * texSize.xy;
}


void setup()
{
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        
        float2 uv = GetUV(unity_InstanceID, _visibleTexture_TexelSize);
        float4 position = SAMPLE_TEXTURE2D_ARRAY_LOD(_visibleTexture, sampler_visibleTexture, uv, lod, 0);
        int extension = position.a;
        float wScale = (0x3ff & extension) / 100.0f;
        float hScale = (0x3ff & (extension >> 10)) / 100.0f;

        float rotation = ((extension >> 20) & 0x7) * 45.0 / 180.0f * 3.1415926;
        float3 scale = float3(wScale, hScale, wScale);
        
        float crot, srot;
        sincos(rotation, srot, crot);
        
        UNITY_MATRIX_M._11_21_31_41 = float4(crot * scale.x, 0, -srot * scale.x, 0);
        UNITY_MATRIX_M._12_22_32_42 = float4(0, scale.y, 0, 0);
        UNITY_MATRIX_M._13_23_33_43 = float4(srot * scale.z, 0, crot * scale.z, 0);
        UNITY_MATRIX_M._14_24_34_44 = float4(position.xyz, 1);
        

        UNITY_MATRIX_I_M = UNITY_MATRIX_M;
        UNITY_MATRIX_I_M._14_24_34 *= -1;
        UNITY_MATRIX_I_M._11_22_33 = 1.0f / UNITY_MATRIX_I_M._11_22_33;
    #endif
}
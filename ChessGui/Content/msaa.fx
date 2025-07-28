#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float texClamp(float2 t)
{
    if (t.x < 0 || t.y < 0 || t.x > 1 || t.y > 1)
    {
        return 0;

    }
    else
    {
        return 1;
    }
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 eighth = 0.125.xx;
    
    float2 pixelScale = float2(ddx(input.TextureCoordinates).x, ddy(input.TextureCoordinates).y);
    
    //https://stackoverflow.com/questions/74541193/what-algorithm-8xmsaa-16xmsaa-use-to-generate-the-position-of-8-points-16-poi
    float2 t1 = input.TextureCoordinates + eighth * pixelScale * float2(4, 0);
    float2 t2 = input.TextureCoordinates + eighth * pixelScale * float2(2, 1);
    float2 t3 = input.TextureCoordinates + eighth * pixelScale * float2(0, 2);
    float2 t4 = input.TextureCoordinates + eighth * pixelScale * float2(6, 3);
    float2 t5 = input.TextureCoordinates + eighth * pixelScale * float2(1, 4);
    float2 t6 = input.TextureCoordinates + eighth * pixelScale * float2(7, 5);
    float2 t7 = input.TextureCoordinates + eighth * pixelScale * float2(5, 6);
    float2 t8 = input.TextureCoordinates + eighth * pixelScale * float2(3, 7);
    
    float4 s1 = tex2D(SpriteTextureSampler, t1);
    float4 s2 = tex2D(SpriteTextureSampler, t2);
    float4 s3 = tex2D(SpriteTextureSampler, t3);
    float4 s4 = tex2D(SpriteTextureSampler, t4);
    float4 s5 = tex2D(SpriteTextureSampler, t5);
    float4 s6 = tex2D(SpriteTextureSampler, t6);
    float4 s7 = tex2D(SpriteTextureSampler, t7);
    float4 s8 = tex2D(SpriteTextureSampler, t8);
    
    float a1 = texClamp(t1);
    float a2 = texClamp(t2);
    float a3 = texClamp(t3);
    float a4 = texClamp(t4);
    float a5 = texClamp(t5);
    float a6 = texClamp(t6);
    float a7 = texClamp(t7);
    float a8 = texClamp(t8);
    
    float4 sum = s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8;
    float4 avg = (sum / 8);
    
    float sum_alpha = a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8;
    float avg_alpha = (sum_alpha / 8);
    
    float4 sampled_colour = avg * input.Color;
    //sampled_colour.a *= avg_alpha;
        
    return sampled_colour;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
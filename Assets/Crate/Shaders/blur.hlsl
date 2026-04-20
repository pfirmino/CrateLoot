void BlurFilter_float(UnityTexture2D input, float2 offset, float2 stride, float2 uv, float samples, out half4 output)
{
    half4 s = SAMPLE_TEXTURE2D(input, input.samplerstate, uv + offset) * 0;
    
    for(int i = 0; i < samples; i++ ){
        s += SAMPLE_TEXTURE2D(input, input.samplerstate, uv + offset + float2(stride * i/samples * 0.01)) * 0.5/samples;
        s += SAMPLE_TEXTURE2D(input, input.samplerstate, uv + offset - float2(stride * i/samples * 0.01)) * 0.5/samples;
        s += SAMPLE_TEXTURE2D(input, input.samplerstate, uv + offset + float2(stride.x * i/samples * 0.01, -stride.y * i/samples * 0.01)) * 0.5/samples;
        s += SAMPLE_TEXTURE2D(input, input.samplerstate, uv + offset - float2(stride.x * i/samples * 0.01, -stride.y * i/samples * 0.01)) * 0.5/samples;
    }
    
    output = s;
}
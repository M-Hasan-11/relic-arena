Shader "Relic/RealmSky"
{
 Properties { _Zenith("Zenith",Color)=(.08,.25,.5,1) _Horizon("Horizon",Color)=(.55,.75,.85,1) _Cloud("Cloud",Color)=(.7,.8,.9,1) _Stars("Stars",Range(0,1))=0 }
 SubShader
 {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata { float4 vertex:POSITION; };
   struct v2f { float4 position:SV_POSITION; float3 direction:TEXCOORD0; };
   float4 _Zenith,_Horizon,_Cloud;float _Stars;
   v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   float4 frag(v2f i):SV_Target
   {
    float3 d=normalize(i.direction);float h=saturate(d.y);
    float3 col=lerp(_Horizon.rgb,_Zenith.rgb,pow(h,.55));
    float2 uv=d.xz/(max(.12,d.y+.3))*1.8+_Time.y*.002;
    float n=noise(uv)*.55+noise(uv*2.1)*.28+noise(uv*4.3)*.17;
    float cloud=smoothstep(.48,.76,n)*smoothstep(-.05,.18,d.y)*(1-smoothstep(.65,1,d.y));
    col=lerp(col,_Cloud.rgb,cloud*.65);
    float sun=pow(saturate(dot(d,normalize(float3(-.5,.3,.7)))),450);
    col+=sun*float3(1,.75,.4)*(1-_Stars*.8);
    float star=step(.997,hash(floor(d.xz/(abs(d.y)+.3)*350)))*pow(saturate(d.y),.5);
    col+=star*_Stars*(1-cloud);
    return float4(col,1);
   }
   ENDHLSL
  }
 }
}

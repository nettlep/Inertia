texture Tex0;
texture Tex1;
float4 cameraPosition;
float4x4 matWorld;
float4x4 matView;
float4x4 matProjection;
float4x4 matNormals;
float reflectionFogDensity;
float reflectionPower;
float depthFogDensity;
float reflectionAlpha;
bool textured;
bool filtered;
bool planeReflected;
bool reflectionMapped;

float depthBlend(float z)
{
	return 1 / pow(2.71828, max(z, 0) * depthFogDensity);
}

float reflectionPlane(float y)
{
	return 1 / pow(2.71828, -y * reflectionFogDensity) * reflectionPower;
}

//////////////////////////////////////////////////
// Texture samplers
//////////////////////////////////////////////////

sampler FilteredSampler = sampler_state
{
	texture = (Tex0);
	mipfilter = LINEAR;
	minfilter = LINEAR;
	magfilter = LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

sampler PointSampler = sampler_state
{
	texture = (Tex0);
	mipfilter = POINT;
	minfilter = POINT;
	magfilter = POINT;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

sampler CubeSampler = sampler_state
{
	texture = (Tex1);
	mipfilter = LINEAR;
	minfilter = LINEAR;
	magfilter = LINEAR;
};

//////////////////////////////////////////////////
// Shaders
//////////////////////////////////////////////////

struct VSIn
{
	float4 obj : Position0;
	float3 normal : Normal;
	float4 diffuse : Color0;
	float2 uvText : TexCoord0;
};

struct VSOut
{
	float4 proj : Position0;
	float3 normal : Normal;
	float4 diffuse : Color0;
	float2 uvText : TexCoord0;
	float3 uvWorld : TexCoord1;
};

struct PSIn
{
	float3 normal : Normal;
	float4 diffuse : Color0;
	float2 uvText : TexCoord0;
	float4 uvWorld : TexCoord1;
};

VSOut VS(in VSIn input)
{
	VSOut output;
	float4 world = mul(input.obj, matWorld);
	
	// Reflect over Y?
	if (planeReflected)
	{
		float4x4 scale = 
		{
			1,  0, 0, 0,
			0, -1, 0, 0,
			0,  0, 1, 0,
			0,  0, 0, 1
		};
		world = mul(world, scale);
	}
	
	float4 view = mul(world, matView);

	output.proj = mul(view, matProjection);
    output.normal = normalize(mul(input.normal, (float3x3)matNormals));
    output.diffuse = input.diffuse;
	output.uvText = input.uvText;
	output.uvWorld = world;
	
	return output;
}

float4 PS(in PSIn input) : Color0
{
	// Our color
	float4 c;
	
	// Texture
	if (textured)
	{
		if (filtered)
		{
			c = tex2D(FilteredSampler, input.uvText);
		}
		else
		{
			c = tex2D(PointSampler, input.uvText);
		}
	}
	else
	{
		c = float4(1,1,1,1);
	}
	
	// Diffuse color
	c *= input.diffuse;
	
	// Apply the reflection map
	if (reflectionMapped)
	{
		float3 eyeToVertex = normalize(input.uvWorld - cameraPosition);
		float3 uvCube = reflect(eyeToVertex, input.normal);
		float4 cc = texCUBE(CubeSampler, uvCube) * reflectionAlpha;
		c.xyz += cc.xyz;
	}
	
	// Apply the depth blend
	c.xyz *= depthBlend(input.uvWorld.z);
	
	// Apply the reflection plane alpha
	if (planeReflected)
	{
		if (input.uvWorld.y >= 0.01)
		{
			c.w = 0;
		}
		else
		{
			c.w *= reflectionPlane(input.uvWorld.y);
		}
	}
//	else if (input.uvWorld.y < -0.01)
//	{
//		c.w = 0;
//	}
	
	return c;
}

technique Default
{
	pass p0
	{
		VertexShader = compile vs_3_0 VS();
		PixelShader = compile ps_3_0 PS();
	}
}

#version 430 core


in VSOUT
{
	vec3 n;
	vec3 v;
}fsIn;

out vec4 outputColor;

struct DirLight
{
	vec3 direction;
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
};

struct Material
{
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
	vec3 emissive;
	float transparency;
};

float specularPower = 0.5;

uniform DirLight dirLight[3];
uniform Material material;

void main(void)
{
	vec3 n = normalize(fsIn.n);
	vec3 v = normalize(fsIn.v);

	for(int i =0; i < 1; ++i)
	{
		vec3 l = normalize(-dirLight[i].direction);
		vec3 h = normalize(l + v);
		vec3 ambient = dirLight[i].ambient * material.ambient * 0.5;
		vec3 diffuse = max(dot(n,l), 0.0) * dirLight[i].diffuse * material.diffuse;
		vec3 specular = pow(max(dot(n,h), 0.0), specularPower) * dirLight[i].specular * material.specular;
		outputColor = vec4(ambient + diffuse + specular, material.transparency);	
		
	}
}


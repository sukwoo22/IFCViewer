#version 430 core

out vec4 outputColor;

in VSOUT
{
	vec3 n;
	vec3 v;
}fsIn;


in vec3 passColor;

uniform vec3 diffuseAlbedo = vec3(0.5, 0.2, 0.7);
uniform vec3 specularAlbedo = vec3(0.7);
uniform vec3 lightDir;
uniform float specularPower = 200.0;

void main(void)
{
	vec3 n = normalize(fsIn.n);
	vec3 l = normalize(lightDir);
	vec3 v = normalize(fsIn.v);
	vec3 h = normalize(l + v); 

	vec3 diffuse = max(dot(n,l), 0.0) * diffuseAlbedo;
	vec3 specular = pow(max(dot(n,h), 0.0), specularPower) * specularAlbedo;

	outputColor = vec4(diffuse + specular, 1.0);
	//outputColor = vec4(passColor, 1.0);
}
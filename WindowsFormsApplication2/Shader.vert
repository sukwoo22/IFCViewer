#version 430 core

in vec3 inputPosition;
in vec3 inputNormal;

uniform mat4 matView;
uniform mat4 matProj;

out VSOUT
{
	vec3 n;
	vec3 v;
}vsOut;

void main(void)
{
	
	vec4 p = matView * vec4(inputPosition, 1.0);
	vsOut.n = mat3(matView) * inputNormal; 
	vsOut.v = -p.xyz;
	
	gl_Position = matProj * p;
}
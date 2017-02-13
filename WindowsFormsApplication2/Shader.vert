#version 430 core

in vec3 inputPosition;
in vec3 inputNormal;

uniform mat4 matView;
uniform mat4 matProj;

out vec3 passColor;

void main(void)
{
	gl_Position = matProj * matView * vec4(inputPosition, 1.0);

	passColor = inputNormal;
}
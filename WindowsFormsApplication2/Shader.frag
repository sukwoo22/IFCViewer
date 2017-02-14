#version 430 core
out vec4 outputColor;

in vec3 passColor;

void main(void)
{
	outputColor = vec4(passColor, 1.0);
}
#version 430 core
int vec3 passColor;
out vec4 outputColor;

void main(void)
{
	outputColor = vec4(passColor, 1.0);
}
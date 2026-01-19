#version 450

layout(location = 0) in vec2 fragCoord;

layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4(fragCoord.x, fragCoord.y, 0, 1.0);
}

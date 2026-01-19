#version 450

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec2 vertexUv;

layout(location = 0) out vec2 fragCoord;

void main() {
    gl_Position = vec4(vertexPosition, 1.0);
    fragCoord = vertexUv;
}

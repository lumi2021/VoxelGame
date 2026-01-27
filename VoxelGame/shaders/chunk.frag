#version 450

layout(location = 0) in vec2 frag_coord;
layout(set = 0, binding = 0) uniform sampler2D texture0;

layout(location = 0) out vec4 outColor;

void main() {
    ivec2 tex_size = textureSize(texture0, 0);
    vec2 aspect = vec2(1.0 / tex_size.x * 32.0, 1.0 / tex_size.y * 32.0);
    outColor = texture(texture0, frag_coord * aspect);
}

#version 450

layout(location = 0) in vec3 vertex_position;
layout(location = 1) in vec2 vertex_uv;

layout(push_constant) uniform PushConstants {
    mat4 mat_projection;
    mat4 mat_view;
    mat4 mat_model;
} constants;

layout(location = 0) out vec2 frag_coord;

void main() {
    mat4 mvp = constants.mat_projection * constants.mat_view * constants.mat_model;
    gl_Position = mvp * vec4(vertex_position, 1.0);
    frag_coord = vertex_uv;
}

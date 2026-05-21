// assets/shaders/model_shader.glsl

#type vertex
#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTex;
layout(location = 2) in vec4 aCol;
layout(location = 3) in vec3 aNormal;

out vec2 fTex;
out vec4 fCol;
out vec3 fNormal;

uniform mat4 uMVP; // Managed by MatrixStack/Camera matrices automatically

void main() {
    gl_Position = uMVP * vec4(aPos, 1.0);
    fTex = aTex;
    fCol = aCol;
    fNormal = aNormal;
}

#type fragment
#version 330 core
in vec2 fTex;
in vec4 fCol;
in vec3 fNormal;

out vec4 FragColor;

uniform sampler2D uMainTex;
uniform vec4 uTint;

void main() {
    // Basic lighting setup to give the 3D OBJ depth
    vec3 normal = normalize(fNormal);
    vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3));
    float diffuse = max(dot(normal, lightDir), 0.2) + 0.3; // Ambient + Diffuse

    vec4 texColor = texture(uMainTex, fTex);
    FragColor = texColor * fCol * uTint * diffuse;
}

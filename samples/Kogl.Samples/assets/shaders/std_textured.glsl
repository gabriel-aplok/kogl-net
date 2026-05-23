#type vertex
#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTex;
layout(location = 2) in vec4 aCol;

out vec4 fCol;
out vec2 fTex;

uniform mat4 uMVP;

void main() {
    gl_Position = uMVP * vec4(aPos, 1.0);
    fCol = aCol;
    fTex = aTex;
}

#type fragment
#version 330 core
in vec4 fCol;
in vec2 fTex;

out vec4 FragColor;

uniform sampler2D uTex;

void main() {
    FragColor = texture(uTex, fTex) * fCol;
}

#type vertex
#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTex;
layout(location = 2) in vec4 aCol;

out vec4 fCol;

uniform mat4 uMVP;

void main() {
    gl_Position = uMVP * vec4(aPos, 1.0);
    fCol = aCol;
}

#type fragment
#version 330 core
in vec4 fCol;

out vec4 FragColor;

void main() {
    FragColor = fCol;
}

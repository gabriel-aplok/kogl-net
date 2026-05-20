#type vertex
#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTex;
layout(location = 2) in vec4 aCol;

out vec2 fTex;
out vec4 fCol;

uniform mat4 uMVP;

void main() {
    gl_Position = uMVP * vec4(aPos, 1.0);
    fTex = aTex;
    fCol = aCol;
}

#type fragment
#version 330 core
in vec2 fTex;
in vec4 fCol;
out vec4 FragColor;

uniform float uTime;

void main() {
    // Procedural animation that doesn't rely on texture details
    float wave = sin(fTex.x * 10.0 + uTime * 3.0) * 0.5 + 0.5;
    vec3 colorA = vec3(0.1, 0.5, 0.8); // Blue
    vec3 colorB = vec3(0.8, 0.2, 0.1); // Red

    vec3 finalRGB = mix(colorA, colorB, wave);
    FragColor = vec4(finalRGB, 1.0) * fCol;
}

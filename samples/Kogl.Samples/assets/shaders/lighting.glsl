#type vertex
#version 330 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTex;
layout(location = 2) in vec4 aCol;
layout(location = 3) in vec3 aNormal;

out vec3 fragPosition;
out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragNormal;

uniform mat4 uMVP;

void main() {
    fragPosition = aPos;
    fragTexCoord = aTex;
    fragColor = aCol;
    fragNormal = normalize(aNormal);

    gl_Position = uMVP * vec4(aPos, 1.0);
}

#type fragment
#version 330 core

in vec3 fragPosition;
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;

uniform sampler2D uTex;
uniform vec4 colDiffuse;

out vec4 finalColor;

#define     MAX_LIGHTS              4
#define     LIGHT_DIRECTIONAL       0
#define     LIGHT_POINT             1

struct Light {
    int enabled;
    int type;
    vec3 position;
    vec3 target;
    vec4 color;
};

uniform Light lights[MAX_LIGHTS];
uniform vec4 ambient;
uniform vec3 viewPos;

void main() {
    // texel color fetching from texture sampler
    vec4 texelColor = texture(uTex, fragTexCoord);
    vec3 lightDot = vec3(0.0);
    vec3 normal = normalize(fragNormal);
    vec3 viewD = normalize(viewPos - fragPosition);
    vec3 specular = vec3(0.0);

    vec4 tint = colDiffuse * fragColor;

    for(int i = 0; i < MAX_LIGHTS; i++) {
        if(lights[i].enabled == 1) {
            vec3 lightVec = vec3(0.0);

            if(lights[i].type == LIGHT_DIRECTIONAL) {
                lightVec = -normalize(lights[i].target - lights[i].position);
            } else if(lights[i].type == LIGHT_POINT) {
                lightVec = normalize(lights[i].position - fragPosition);
            }

            float NdotL = max(dot(normal, lightVec), 0.0);
            lightDot += lights[i].color.rgb * NdotL;

            float specCo = 0.0;
            if(NdotL > 0.0) {
                specCo = pow(max(0.0, dot(viewD, reflect(-lightVec, normal))), 16.0); // 16 refers to shine
            }
            specular += specCo;
        }
    }

    finalColor = (texelColor * ((tint + vec4(specular, 1.0)) * vec4(lightDot, 1.0)));
    finalColor += texelColor * (ambient / 10.0) * tint;

    // gamma correction
    finalColor = pow(finalColor, vec4(1.0 / 2.2));
}

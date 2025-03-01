#version 330 core
#define NUM_LIGHT_UFOS 3

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;
layout (location = 3) in vec3 aTangent;
layout (location = 4) in vec3 aBitangent;

out VS_OUT {
    vec3 FragPos;
    vec2 TexCoords;
    vec3 TangentDirLightDir;
    vec3 TangentSpotLightDir;
    vec3 TangentSpotLightPosition;
    vec3 TangentPointLightPositions[NUM_LIGHT_UFOS];
    vec3 TangentViewPos;
    vec3 TangentFragPos;
    mat3 TBN;
} vs_out;
struct DirLight {
    vec3 direction;

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};
struct PointLight {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;

    float constant;
    float linear;
    float quadratic;
};
struct SpotLight {
    vec3 position;
    vec3 direction;
    float cutOff;
    float outerCutOff;

    float constant;
    float linear;
    float quadratic;

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};
uniform DirLight dirLight;
uniform SpotLight spotLight;
uniform PointLight pointLights[NUM_LIGHT_UFOS];
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 viewPos;

void main()
{
    vs_out.FragPos = vec3(model * vec4(aPos, 1.0));
    vs_out.TexCoords = aTexCoords;

    mat3 normalMatrix = transpose(inverse(mat3(model)));
    vec3 T = normalize(normalMatrix * aTangent);
    vec3 N = normalize(normalMatrix * aNormal);
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T);

    mat3 TBN = mat3(T, B, N);

    vs_out.TangentDirLightDir = TBN * dirLight.direction;
    vs_out.TangentSpotLightDir = TBN * spotLight.direction;
    vs_out.TangentSpotLightPosition = TBN * spotLight.position;
    for (int i = 0; i < NUM_LIGHT_UFOS; i++)
        vs_out.TangentPointLightPositions[i] = TBN * pointLights[i].position;


    vs_out.TangentViewPos = TBN * viewPos;
    vs_out.TangentFragPos = TBN * vs_out.FragPos;
    vs_out.TBN = TBN;
    gl_Position = projection * view * model * vec4(aPos, 1.0);

}
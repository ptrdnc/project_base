#version 330 core
out vec4 FragColor;

#define NUM_LIGHT_UFOS 3

struct Material {
    sampler2D texture_diffuse1;
    sampler2D texture_specular1;
    sampler2D texture_normal1;
    sampler2D texture_disp1;
    float shininess;
};

in VS_OUT {
    vec3 FragPos;
    vec2 TexCoords;
    vec3 TangentDirLightDir;
    vec3 TangentSpotLightDir;
    vec3 TangentSpotLightPosition;
    vec3 TangentPointLightPositions[NUM_LIGHT_UFOS];
    vec3 TangentViewPos;
    vec3 TangentFragPos;
    mat3 TBN;
} fs_in;

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
uniform vec3 viewPos;
uniform Material material;
uniform DirLight dirLight;
uniform PointLight pointLights[NUM_LIGHT_UFOS];
uniform SpotLight spotLight;
uniform float heightScale;
uniform float tex;

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, int index, vec2 texCoords);
vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir, vec2 texCoords);
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec2 texCoords);
vec2 ParallaxMapping(vec2 texCoords, vec3 viewDir);
void main()
{

//     vec3 normal = texture(material.texture_normal1, fs_in.TexCoords).rgb;
//     normal = normalize(normal * 2.0 - 1.0);
//     normal = normalize(fs_in.TBN * normal);
//     vec3 viewDir = normalize(viewPos - fs_in.FragPos);
//
//     vec3 result = CalcDirLight(dirLight, normal, viewDir);
//     for(int i = 0; i < NUM_LIGHT_UFOS; i++)
//         result += CalcPointLight(pointLights[i], normal, fs_in.FragPos, viewDir);
//     result += CalcSpotLight(spotLight, normal, fs_in.FragPos, viewDir);
//
//
//     FragColor = vec4(result, 1.0);

    vec3 viewDir = normalize(fs_in.TangentViewPos - fs_in.TangentFragPos);
    vec2 texCoords = fs_in.TexCoords;

    texCoords = ParallaxMapping(fs_in.TexCoords, viewDir);
    if(texCoords.x/tex > 1.0 || texCoords.y/tex > 1.0 || texCoords.x/tex < 0.0 || texCoords.y/tex < 0.0)
        discard;


    vec3 normal = texture(material.texture_normal1, fs_in.TexCoords).rgb;
    normal = normalize(normal * 2.0 - 1.0);

    vec3 result = CalcDirLight(dirLight, normal, viewDir, texCoords);
    for(int i = 0; i < NUM_LIGHT_UFOS; i++)
        result += CalcPointLight(pointLights[i], normal, fs_in.TangentFragPos, viewDir, i, texCoords);
    result += CalcSpotLight(spotLight, normal, fs_in.TangentFragPos, viewDir, texCoords);

    FragColor = vec4(result, 1.0);
}

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, int index, vec2 texCoords)
{
    vec3 lightDir = normalize(fs_in.TangentPointLightPositions[index] - fragPos);
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    // attenuation
    float distance = length(light.position - fs_in.FragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));
    // combine results
    vec3 ambient = light.ambient * vec3(texture(material.texture_diffuse1, texCoords).rgb);
    vec3 diffuse = light.diffuse * diff * vec3(texture(material.texture_diffuse1, texCoords).rgb);
    vec3 specular = light.specular * spec * vec3(texture(material.texture_specular1, texCoords).rgb);
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
    return (ambient + diffuse + specular);
}


vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir, vec2 texCoords) {
    //vec3 lightDir = normalize(-light.direction);
    vec3 lightDir = fs_in.TangentDirLightDir;
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 reflectDir = reflect(lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

    vec3 ambient = dirLight.ambient * vec3(texture(material.texture_diffuse1, texCoords).rgb);
    vec3 diffuse = dirLight.diffuse * diff * vec3(texture(material.texture_diffuse1, texCoords).rgb);
    vec3 specular = dirLight.specular * spec * vec3(texture(material.texture_specular1, texCoords).rgb);

    return (ambient + diffuse + specular);

}

vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec2 texCoords)
{
    //vec3 lightDir = normalize(light.position - fragPos);
    vec3 lightDir = normalize(fs_in.TangentSpotLightPosition - fragPos);
    float diff = max(dot(normal, lightDir), 0.0);

    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    // attenuation
    //float distance = length(light.position - fragPos);
    float distance = length(light.position - fs_in.FragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));
    // spotlight intensity
    float theta = dot(lightDir, normalize(-fs_in.TangentSpotLightDir));
    float epsilon = light.cutOff - light.outerCutOff;
    float intensity = clamp((theta - light.outerCutOff) / epsilon, 0.0, 1.0);
    // combine results
    vec3 ambient = light.ambient * vec3(texture(material.texture_diffuse1, texCoords));
    vec3 diffuse = light.diffuse * diff * vec3(texture(material.texture_diffuse1, texCoords));
    vec3 specular = light.specular * spec * vec3(texture(material.texture_specular1, texCoords));
    ambient *= attenuation * intensity;
    diffuse *= attenuation * intensity;
    specular *= attenuation * intensity;
    return (ambient + diffuse + specular);
}
vec2 ParallaxMapping(vec2 texCoords, vec3 viewDir)
{
    float height =  texture(material.texture_disp1, texCoords).r;
    return texCoords - viewDir.xy * (height * heightScale);
}
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class VintageFilter : MonoBehaviour
{
    public Shader vintageShader;

    [Header("Filter Controls")]
    [Range(0f, 1f)]
    public float sepiaAmount = 0.35f;

    [Range(0f, 1f)]
    public float desaturation = 0.2f;

    [Header("Vignette Settings")]
    [Range(0f, 2f)]
    public float vignetteIntensity = 0.8f;

    [Range(0.1f, 2f)]
    public float vignetteSmoothness = 0.7f;

    [Header("Film Grain Settings")]
    [Range(0f, 0.2f)]
    public float grainIntensity = 0.04f;

    private Material material;

    private void OnEnable()
    {
        if (vintageShader == null)
        {
            vintageShader = Shader.Find("Hidden/VintageEffect");
        }

        if (vintageShader != null && material == null)
        {
            material = new Material(vintageShader);
            material.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    private void OnDisable()
    {
        if (material != null)
        {
            DestroyImmediate(material);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (vintageShader == null)
        {
            vintageShader = Shader.Find("Hidden/VintageEffect");
        }

        if (material == null && vintageShader != null)
        {
            material = new Material(vintageShader);
            material.hideFlags = HideFlags.HideAndDontSave;
        }

        if (material != null)
        {
            material.SetFloat("_SepiaAmount", sepiaAmount);
            material.SetFloat("_Desaturation", desaturation);
            material.SetFloat("_VignetteIntensity", vignetteIntensity);
            material.SetFloat("_VignetteSmoothness", vignetteSmoothness);
            material.SetFloat("_GrainIntensity", grainIntensity);

            Graphics.Blit(source, destination, material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}

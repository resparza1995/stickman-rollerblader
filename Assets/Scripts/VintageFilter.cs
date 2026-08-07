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

    private static readonly int SepiaAmountID = Shader.PropertyToID("_SepiaAmount");
    private static readonly int DesaturationID = Shader.PropertyToID("_Desaturation");
    private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int VignetteSmoothnessID = Shader.PropertyToID("_VignetteSmoothness");
    private static readonly int GrainIntensityID = Shader.PropertyToID("_GrainIntensity");

    private void OnEnable()
    {
        InitializeMaterial();
    }

    private void OnDisable()
    {
        if (material != null)
        {
            DestroyImmediate(material);
            material = null;
        }
    }

    private void InitializeMaterial()
    {
        if (vintageShader == null)
        {
            vintageShader = Shader.Find("Hidden/VintageEffect");
        }

        if (vintageShader != null && material == null)
        {
            material = new Material(vintageShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material == null)
        {
            InitializeMaterial();
        }

        if (material != null)
        {
            material.SetFloat(SepiaAmountID, sepiaAmount);
            material.SetFloat(DesaturationID, desaturation);
            material.SetFloat(VignetteIntensityID, vignetteIntensity);
            material.SetFloat(VignetteSmoothnessID, vignetteSmoothness);
            material.SetFloat(GrainIntensityID, grainIntensity);

            Graphics.Blit(source, destination, material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}

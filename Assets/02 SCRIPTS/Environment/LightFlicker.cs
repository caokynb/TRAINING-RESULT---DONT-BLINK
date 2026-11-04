using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class LightFlicker : MonoBehaviour
{
    public enum FlickerPreset
    {
        Custom,
        Subtle,
        Moderate,
        Heavy,
        Broken
    }

    [Header("Preset")]
    [SerializeField] private FlickerPreset preset = FlickerPreset.Moderate;
    [Range(0f, 1f)]
    [SerializeField] private float intensityFactor = 0.5f;

    [Header("References")]
    [SerializeField] private Light targetLight;
    [SerializeField] private Renderer emissiveRenderer;
    [SerializeField] private string emissionProperty = "_EmissionColor";

    [Header("Settings")]
    [SerializeField] private float baseIntensity = 1.5f;
    [Range(0f, 1f)]
    [SerializeField] private float minIntensityRatio = 0.05f;
    [Range(0.01f, 1f)]
    [SerializeField] private float flickerSpeed = 0.08f;
    [Range(0.1f, 10f)]
    [SerializeField] private float minInterval = 1.5f;
    [Range(0.2f, 15f)]
    [SerializeField] private float maxInterval = 5f;

    private Material materialInstance;
    private Color baseEmissionColor;
    private bool hasEmission;
    private Coroutine flickerCoroutine;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponentInChildren<Light>();

        if (targetLight != null && baseIntensity <= 0.01f)
            baseIntensity = targetLight.intensity;

        if (emissiveRenderer != null)
        {
            materialInstance = emissiveRenderer.material;
            if (materialInstance.HasProperty(emissionProperty))
            {
                baseEmissionColor = materialInstance.GetColor(emissionProperty);
                hasEmission = true;
            }
        }

        ApplyPreset(preset);
    }

    private void OnEnable()
    {
        if (flickerCoroutine != null)
            StopCoroutine(flickerCoroutine);

        flickerCoroutine = StartCoroutine(FlickerRoutine());
    }

    private void OnDisable()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        ResetToDefault();
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
            Destroy(materialInstance);
    }

    private void OnValidate()
    {
        if (preset != FlickerPreset.Custom)
            ApplyPreset(preset);
    }

    public void SetPreset(FlickerPreset newPreset)
    {
        preset = newPreset;
        ApplyPreset(newPreset);
    }

    public void SetIntensityFactor(float factor01)
    {
        intensityFactor = Mathf.Clamp01(factor01);
        if (factor01 < 0.2f) preset = FlickerPreset.Subtle;
        else if (factor01 < 0.55f) preset = FlickerPreset.Moderate;
        else if (factor01 < 0.85f) preset = FlickerPreset.Heavy;
        else preset = FlickerPreset.Broken;
        ApplyPreset(preset);
    }

    private void ApplyPreset(FlickerPreset p)
    {
        switch (p)
        {
            case FlickerPreset.Subtle:
                intensityFactor = 0.2f;
                minIntensityRatio = 0.4f;
                flickerSpeed = 0.1f;
                minInterval = 4f;
                maxInterval = 9f;
                break;

            case FlickerPreset.Moderate:
                intensityFactor = 0.5f;
                minIntensityRatio = 0.1f;
                flickerSpeed = 0.07f;
                minInterval = 1.5f;
                maxInterval = 4.5f;
                break;

            case FlickerPreset.Heavy:
                intensityFactor = 0.8f;
                minIntensityRatio = 0.0f;
                flickerSpeed = 0.04f;
                minInterval = 0.4f;
                maxInterval = 1.8f;
                break;

            case FlickerPreset.Broken:
                intensityFactor = 1.0f;
                minIntensityRatio = 0.0f;
                flickerSpeed = 0.03f;
                minInterval = 0.1f;
                maxInterval = 1.2f;
                break;

            case FlickerPreset.Custom:
                break;
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            float stableDuration = Random.Range(minInterval, maxInterval);
            SetLightRatio(1f);
            yield return new WaitForSeconds(stableDuration);

            if (intensityFactor <= 0.01f)
                continue;

            int burstCount = preset switch
            {
                FlickerPreset.Subtle => Random.Range(1, 3),
                FlickerPreset.Moderate => Random.Range(3, 7),
                FlickerPreset.Heavy => Random.Range(6, 14),
                FlickerPreset.Broken => Random.Range(8, 20),
                _ => Mathf.RoundToInt(Mathf.Lerp(2, 12, intensityFactor))
            };

            for (int i = 0; i < burstCount; i++)
            {
                float dimmedRatio = Random.value < 0.6f ? minIntensityRatio : Random.Range(minIntensityRatio, 0.6f);
                SetLightRatio(dimmedRatio);
                yield return new WaitForSeconds(Random.Range(flickerSpeed * 0.5f, flickerSpeed * 1.5f));

                SetLightRatio(Random.Range(0.7f, 1.1f));
                yield return new WaitForSeconds(Random.Range(flickerSpeed * 0.3f, flickerSpeed * 1.2f));
            }

            if (preset == FlickerPreset.Broken && Random.value < 0.4f)
            {
                SetLightRatio(0f);
                yield return new WaitForSeconds(Random.Range(0.5f, 2f));
            }

            SetLightRatio(1f);
        }
    }

    private void SetLightRatio(float ratio)
    {
        if (targetLight != null)
        {
            targetLight.intensity = baseIntensity * ratio;
            targetLight.enabled = ratio > 0.01f;
        }

        if (hasEmission && materialInstance != null)
        {
            Color currentEmission = baseEmissionColor * ratio;
            materialInstance.SetColor(emissionProperty, currentEmission);
            if (ratio > 0.05f)
                materialInstance.EnableKeyword("_EMISSION");
            else
                materialInstance.DisableKeyword("_EMISSION");
        }
    }

    private void ResetToDefault()
    {
        SetLightRatio(1f);
    }
}

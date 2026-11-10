using UnityEngine;

[CreateAssetMenu(fileName = "MonsterSettings", menuName = "Backrooms/Monster Settings", order = 101)]
public sealed class MonsterSettings : ScriptableObject
{
    [Header("Grace Period")]
    [Tooltip("Thời gian (giây) quái đứng yên đầu game trước khi bắt đầu đuổi.")]
    [SerializeField, Range(0f, 30f)] private float initialGracePeriod = 10f;

    [Header("Catch Rules")]
    [Tooltip("Khoảng cách tối thiểu tới người chơi để tính là bị bắt (m).")]
    [SerializeField, Range(0.5f, 5f)] private float catchDistance = 1.8f;

    [Header("Base Step Counts")]
    [Tooltip("Số bước nhảy khi người chơi ở gần (dưới nearCellThreshold ô).")]
    [SerializeField, Range(1, 5)] private int nearBlinkSteps = 1;

    [Tooltip("Số bước nhảy tối thiểu ở khoảng cách bình thường.")]
    [SerializeField, Range(1, 5)] private int normalMinBlinkSteps = 1;

    [Tooltip("Số bước nhảy tối đa ở khoảng cách bình thường.")]
    [SerializeField, Range(1, 10)] private int normalMaxBlinkSteps = 2;

    [Tooltip("Số bước nhảy khi người chơi ở xa (trên farCellThreshold ô).")]
    [SerializeField, Range(1, 10)] private int farBlinkSteps = 3;

    [Header("Base Step Intervals")]
    [Tooltip("Khoảng thời gian giữa các bước khi giữ nhắm mắt ở gần (giây).")]
    [SerializeField, Range(0.1f, 1f)] private float nearHoldStepInterval = 0.40f;

    [Tooltip("Khoảng thời gian giữa các bước khi giữ nhắm mắt ở cự ly bình thường (giây).")]
    [SerializeField, Range(0.1f, 1f)] private float normalHoldStepInterval = 0.35f;

    [Tooltip("Khoảng thời gian giữa các bước khi giữ nhắm mắt ở xa (giây).")]
    [SerializeField, Range(0.1f, 1f)] private float farHoldStepInterval = 0.30f;

    [Tooltip("Khoảng thời gian tối thiểu giữa các bước khi giữ nhắm mắt (giây).")]
    [SerializeField, Range(0.05f, 0.5f)] private float minHoldStepInterval = 0.12f;

    [Tooltip("Gia tốc giảm khoảng thời gian bước khi giữ nhắm mắt càng lâu.")]
    [SerializeField, Range(0.01f, 0.5f)] private float holdAcceleration = 0.10f;

    [Header("Distance Thresholds")]
    [Tooltip("Ngưỡng số ô BFS tính là ở gần.")]
    [SerializeField, Range(1, 10)] private int nearCellThreshold = 4;

    [Tooltip("Ngưỡng số ô BFS tính là ở xa.")]
    [SerializeField, Range(5, 30)] private int farCellThreshold = 10;

    [Header("Phase 1: Pre-Key Roaming (Tự do khám phá)")]
    [Tooltip("Độ khó trần tối đa khi chưa nhặt chìa khóa (0 - 1). Giữ thấp để người chơi roaming thoải mái.")]
    [SerializeField, Range(0f, 0.5f)] private float preKeyMaxDifficulty = 0.20f;

    [Tooltip("Thời gian chơi (giây) để độ khó roaming đạt mức trần preKeyMaxDifficulty.")]
    [SerializeField, Range(30f, 300f)] private float preKeyTimeToMaxDifficulty = 120f;

    [Tooltip("Số bước tối đa quái được phép đi trong 1 LẦN NHẮM MẮT (kể cả mỏi mắt - Forced Blink) khi chưa có chìa. Ngăn quái dịch chuyển nửa bản đồ.")]
    [SerializeField, Range(1, 6)] private int preKeyMaxStepsPerBlink = 3;

    [Tooltip("Số bước nhảy tối đa khi chớp mắt ở cự ly xa khi chưa có chìa.")]
    [SerializeField, Range(1, 4)] private int preKeyFarBlinkSteps = 2;

    [Tooltip("Hệ số giãn khoảng thời gian giữa các bước khi giữ nhắm mắt lúc chưa có chìa (lớn hơn 1 = bước thong thả hơn).")]
    [SerializeField, Range(1f, 3f)] private float preKeyHoldIntervalScale = 1.6f;

    [Header("Phase 2: Post-Key Escape (Exponential Hardcore)")]
    [Tooltip("Thời gian chạy trốn (giây) kể từ khi nhặt chìa khóa để đạt độ khó cực đại.")]
    [SerializeField, Range(20f, 180f)] private float postKeyTimeToMaxDifficulty = 60f;

    [Tooltip("Mức độ khó cơ bản ngay khi vừa nhặt chìa khóa (0 - 1).")]
    [SerializeField, Range(0.1f, 0.8f)] private float postKeyBaseDifficulty = 0.35f;

    [Tooltip("Số mũ tăng trưởng độ khó theo cấp số nhân sau khi nhặt chìa (>= 2 là hàm mũ cong vút lên).")]
    [SerializeField, Range(1.5f, 4f)] private float postKeyExponentialPower = 2.5f;

    [Tooltip("Đường cong tăng độ khó sau khi có chìa (dạng hàm mũ Exponential).")]
    [SerializeField] private AnimationCurve postKeyDifficultyCurve;

    [Tooltip("Số bước cộng thêm tối đa cho mỗi lần chớp mắt khi đạt đỉnh độ khó sau khi có chìa.")]
    [SerializeField, Range(1, 6)] private int postKeyMaxBonusSteps = 3;

    [Tooltip("Số bước quái nhảy thêm khi người chơi bị ép nhắm mắt (Forced Blink) sau khi đã có chìa.")]
    [SerializeField, Range(1, 4)] private int postKeyForcedBlinkBonusSteps = 2;

    [Tooltip("Hệ số nhân khoảng thời gian bước khi ở đỉnh độ khó sau khi có chìa (nhỏ hơn 1 là bước dồn dập).")]
    [SerializeField, Range(0.2f, 0.8f)] private float postKeyHoldIntervalScaleAtMax = 0.45f;

    [Tooltip("Giới hạn số bước tối đa trong một lần nhắm mắt sau khi có chìa khóa.")]
    [SerializeField, Range(4, 20)] private int postKeyMaxStepsPerBlink = 10;

    // --- Backwards Compatibility Fields ---
#pragma warning disable CS0414
    [SerializeField, HideInInspector] private bool enableDifficultyScaling = true;
    [SerializeField, HideInInspector] private float timeToMaxDifficulty = 75f;
    [SerializeField, HideInInspector] private AnimationCurve difficultyCurve;
    [SerializeField, HideInInspector] private int maxBonusSteps = 2;
    [SerializeField, HideInInspector] private float holdIntervalScaleAtMax = 0.60f;
    [SerializeField, HideInInspector] private float keyAggressionBonus = 0.35f;
    [SerializeField, HideInInspector] private int forcedBlinkBonusSteps = 1;
    [SerializeField, HideInInspector] private float keyHoldIntervalMultiplier = 0.8f;
#pragma warning restore CS0414

    // Public Getters
    public float InitialGracePeriod => initialGracePeriod;
    public float CatchDistance => catchDistance;
    public int NearBlinkSteps => nearBlinkSteps;
    public int NormalMinBlinkSteps => normalMinBlinkSteps;
    public int NormalMaxBlinkSteps => normalMaxBlinkSteps;
    public int FarBlinkSteps => farBlinkSteps;
    public float NearHoldStepInterval => nearHoldStepInterval;
    public float NormalHoldStepInterval => normalHoldStepInterval;
    public float FarHoldStepInterval => farHoldStepInterval;
    public float MinHoldStepInterval => minHoldStepInterval;
    public float HoldAcceleration => holdAcceleration;
    public int NearCellThreshold => nearCellThreshold;
    public int FarCellThreshold => farCellThreshold;

    public float PreKeyMaxDifficulty => preKeyMaxDifficulty;
    public float PreKeyTimeToMaxDifficulty => preKeyTimeToMaxDifficulty;
    public int PreKeyMaxStepsPerBlink => preKeyMaxStepsPerBlink;
    public int PreKeyFarBlinkSteps => preKeyFarBlinkSteps;
    public float PreKeyHoldIntervalScale => preKeyHoldIntervalScale;

    public float PostKeyTimeToMaxDifficulty => postKeyTimeToMaxDifficulty;
    public float PostKeyBaseDifficulty => postKeyBaseDifficulty;
    public float PostKeyExponentialPower => postKeyExponentialPower;
    public AnimationCurve PostKeyDifficultyCurve => postKeyDifficultyCurve;
    public int PostKeyMaxBonusSteps => postKeyMaxBonusSteps;
    public int PostKeyForcedBlinkBonusSteps => postKeyForcedBlinkBonusSteps;
    public float PostKeyHoldIntervalScaleAtMax => postKeyHoldIntervalScaleAtMax;
    public int PostKeyMaxStepsPerBlink => postKeyMaxStepsPerBlink;

    // Legacy Getters
    public int ForcedBlinkBonusSteps => postKeyForcedBlinkBonusSteps;
    public int MaxBonusSteps => postKeyMaxBonusSteps;

    private void OnValidate()
    {
        if (postKeyDifficultyCurve == null || postKeyDifficultyCurve.length == 0)
        {
            postKeyDifficultyCurve = CreateDefaultExponentialCurve();
        }
        if (normalMaxBlinkSteps < normalMinBlinkSteps)
        {
            normalMaxBlinkSteps = normalMinBlinkSteps;
        }
        if (farCellThreshold < nearCellThreshold)
        {
            farCellThreshold = nearCellThreshold;
        }
    }

    public static AnimationCurve CreateDefaultExponentialCurve()
    {
        Keyframe k0 = new Keyframe(0f, 0f, 0f, 0.2f);
        Keyframe k1 = new Keyframe(0.6f, 0.25f, 0.8f, 1.2f);
        Keyframe k2 = new Keyframe(1f, 1f, 3f, 0f);
        return new AnimationCurve(k0, k1, k2);
    }

    /// <summary>
    /// Đánh giá độ khó hiện tại (0 - 1).
    /// Khi chưa có chìa: Roaming thoải mái, độ khó tăng nhẹ và kịch trần ở preKeyMaxDifficulty.
    /// Khi đã có chìa: Độ khó tăng vọt theo cấp số nhân (Exponential) theo thời gian nhặt chìa.
    /// </summary>
    public float EvaluateDifficulty(float timeSinceGracePeriod, bool hasKey, float timeSinceKeyPickup)
    {
        if (!hasKey)
        {
            if (preKeyTimeToMaxDifficulty <= 0.01f)
                return 0f;

            float t = Mathf.Clamp01(timeSinceGracePeriod / preKeyTimeToMaxDifficulty);
            return Mathf.Clamp(t * preKeyMaxDifficulty, 0f, preKeyMaxDifficulty);
        }
        else
        {
            if (postKeyTimeToMaxDifficulty <= 0.01f)
                return 1f;

            float u = Mathf.Clamp01(timeSinceKeyPickup / postKeyTimeToMaxDifficulty);
            float expFactor;
            if (postKeyDifficultyCurve != null && postKeyDifficultyCurve.length > 0)
            {
                expFactor = Mathf.Clamp01(postKeyDifficultyCurve.Evaluate(u));
            }
            else
            {
                expFactor = Mathf.Clamp01(Mathf.Pow(u, Mathf.Max(1f, postKeyExponentialPower)));
            }

            return Mathf.Clamp01(Mathf.Lerp(postKeyBaseDifficulty, 1f, expFactor));
        }
    }

    public float EvaluateDifficulty(float timeSinceGracePeriod, bool hasKey = false)
    {
        return EvaluateDifficulty(timeSinceGracePeriod, hasKey, hasKey ? timeSinceGracePeriod : 0f);
    }

    public int GetBonusSteps(float timeSinceGracePeriod, bool hasKey, float timeSinceKeyPickup, bool isForcedBlink = false)
    {
        if (!hasKey)
        {
            return 0;
        }

        float diff = EvaluateDifficulty(timeSinceGracePeriod, true, timeSinceKeyPickup);
        int bonus = Mathf.RoundToInt(diff * postKeyMaxBonusSteps);
        if (bonus < 1)
            bonus = 1;

        if (isForcedBlink)
        {
            bonus += postKeyForcedBlinkBonusSteps;
        }

        return bonus;
    }

    public int GetBonusSteps(float timeSinceGracePeriod, bool hasKey = false)
    {
        return GetBonusSteps(timeSinceGracePeriod, hasKey, 0f, false);
    }

    public float GetHoldIntervalMultiplier(float timeSinceGracePeriod, bool hasKey, float timeSinceKeyPickup)
    {
        if (!hasKey)
        {
            return preKeyHoldIntervalScale;
        }

        float diff = EvaluateDifficulty(timeSinceGracePeriod, true, timeSinceKeyPickup);
        float mult = Mathf.Lerp(1f, postKeyHoldIntervalScaleAtMax, diff);
        return Mathf.Max(0.15f, mult);
    }

    public float GetHoldIntervalMultiplier(float timeSinceGracePeriod, bool hasKey = false)
    {
        return GetHoldIntervalMultiplier(timeSinceGracePeriod, hasKey, 0f);
    }

    public int GetMaxStepsPerBlink(bool hasKey)
    {
        return hasKey ? postKeyMaxStepsPerBlink : preKeyMaxStepsPerBlink;
    }
}

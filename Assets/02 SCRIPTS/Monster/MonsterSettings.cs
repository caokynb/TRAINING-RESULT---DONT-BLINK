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

    [Header("Step Counts")]
    [Tooltip("Số bước nhảy khi người chơi ở gần (dưới nearCellThreshold ô).")]
    [SerializeField, Range(1, 5)] private int nearBlinkSteps = 1;

    [Tooltip("Số bước nhảy tối thiểu ở khoảng cách bình thường.")]
    [SerializeField, Range(1, 5)] private int normalMinBlinkSteps = 1;

    [Tooltip("Số bước nhảy tối đa ở khoảng cách bình thường.")]
    [SerializeField, Range(1, 10)] private int normalMaxBlinkSteps = 2;

    [Tooltip("Số bước nhảy khi người chơi ở xa (trên farCellThreshold ô).")]
    [SerializeField, Range(1, 10)] private int farBlinkSteps = 3;

    [Header("Step Intervals")]
    [Tooltip("Khoảng thời gian giữa các bước khi giữ nhắm mắt ở gần (giây).")]
    [SerializeField, Range(0.1f, 1f)] private float nearHoldStepInterval = 0.45f;

    [Tooltip("Khoảng thời gian giữa các bước khi giữ nhắm mắt ở cự ly bình thường (giây).")]
    [SerializeField, Range(0.1f, 1f)] private float normalHoldStepInterval = 0.35f;

    [Tooltip("Khoảng thời gian giữa các bước khi giữ nhắm mắt ở xa (giây).")]
    [SerializeField, Range(0.05f, 0.8f)] private float farHoldStepInterval = 0.2f;

    [Tooltip("Khoảng thời gian tối thiểu giữa các bước khi giữ nhắm mắt (giây).")]
    [SerializeField, Range(0.05f, 0.5f)] private float minHoldStepInterval = 0.1f;

    [Tooltip("Gia tốc giảm khoảng thời gian bước khi giữ nhắm mắt càng lâu.")]
    [SerializeField, Range(0.05f, 1f)] private float holdAcceleration = 0.18f;

    [Header("Distance Thresholds")]
    [Tooltip("Ngưỡng số ô BFS tính là ở gần.")]
    [SerializeField, Range(1, 10)] private int nearCellThreshold = 4;

    [Tooltip("Ngưỡng số ô BFS tính là ở xa.")]
    [SerializeField, Range(5, 30)] private int farCellThreshold = 10;

    [Header("Difficulty Scaling")]
    [Tooltip("Bật tăng độ khó theo thời gian chơi.")]
    [SerializeField] private bool enableDifficultyScaling = true;

    [Tooltip("Thời gian chơi (giây) kể từ khi hết grace period để đạt độ khó tối đa.")]
    [SerializeField, Range(30f, 600f)] private float timeToMaxDifficulty = 180f;

    [Tooltip("Đường cong tăng độ khó (X: tỷ lệ thời gian 0-1, Y: tỷ lệ độ khó 0-1).")]
    [SerializeField] private AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("Số bước cộng thêm tối đa cho mỗi lần chớp mắt khi đạt độ khó tối đa.")]
    [SerializeField, Range(0, 5)] private int maxBonusSteps = 2;

    [Tooltip("Hệ số nhân khoảng thời gian bước khi giữ nhắm mắt ở độ khó tối đa (nhỏ hơn 1 là bước nhanh hơn).")]
    [SerializeField, Range(0.4f, 1f)] private float holdIntervalScaleAtMax = 0.7f;

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
    public bool EnableDifficultyScaling => enableDifficultyScaling;
    public float TimeToMaxDifficulty => timeToMaxDifficulty;
    public AnimationCurve DifficultyCurve => difficultyCurve;
    public int MaxBonusSteps => maxBonusSteps;
    public float HoldIntervalScaleAtMax => holdIntervalScaleAtMax;

    private void OnValidate()
    {
        if (difficultyCurve == null || difficultyCurve.length == 0)
        {
            difficultyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
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

    public float EvaluateDifficulty(float timeSinceGracePeriod)
    {
        if (!enableDifficultyScaling || timeToMaxDifficulty <= 0.01f || timeSinceGracePeriod <= 0f)
            return 0f;

        float t = Mathf.Clamp01(timeSinceGracePeriod / timeToMaxDifficulty);
        float eval = difficultyCurve != null ? difficultyCurve.Evaluate(t) : t;
        return Mathf.Clamp01(eval);
    }

    public int GetBonusSteps(float timeSinceGracePeriod)
    {
        float diff = EvaluateDifficulty(timeSinceGracePeriod);
        return Mathf.RoundToInt(diff * maxBonusSteps);
    }

    public float GetHoldIntervalMultiplier(float timeSinceGracePeriod)
    {
        float diff = EvaluateDifficulty(timeSinceGracePeriod);
        return Mathf.Lerp(1f, holdIntervalScaleAtMax, diff);
    }
}

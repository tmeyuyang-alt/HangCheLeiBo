using UnityEngine;

public class ZhuaShouCtrl : MonoBehaviour
{
    [Header("PLC")]
    public string Key;

    [Header("Animator")]
    public Animator animator;
    public string stateName;
    public int layer = 0;

    public float offset = 0.01f;

    [Range(0f, 1f)]
    public float progress;

    private int _stateHash;

    public float curr;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        UpdateStateHash();
    }

    private void OnEnable()
    {
        if (animator != null)
        {
            animator.speed = 0f;
        }

        ApplyProgress(progress);
    }

    private void OnValidate()
    {
        UpdateStateHash();
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(Key))
        {
            return;
        }
         curr = PLCConfigManager.Instance.GetFloatValue(Key)+offset;
        SetProgress(PLCConfigManager.Instance.GetFloatValue(Key)+offset);
    }

    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        ApplyProgress(progress);
    }

    private void ApplyProgress(float value)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        if (_stateHash == 0)
        {
            UpdateStateHash();
        }

        float normalizedTime = Mathf.Clamp01(value);
        if (normalizedTime >= 1f)
        {
            normalizedTime = 0.999f;
        }

        animator.Play(_stateHash, layer, normalizedTime);
        animator.Update(0f);
    }

    private void UpdateStateHash()
    {
        _stateHash = string.IsNullOrEmpty(stateName) ? 0 : Animator.StringToHash(stateName);
    }
}

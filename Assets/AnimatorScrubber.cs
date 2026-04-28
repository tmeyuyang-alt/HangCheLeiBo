using UnityEngine;

public class AnimatorScrubber : MonoBehaviour
{
    [Header("必填")]
    public Animator animator;
    public string stateName;  // 要控制的动画状态名（Animator Controller里的 State）
    [Range(0f, 1f)] public float t = 0f;  // 0~1 进度
    [Tooltip("动画所在的层（默认0）")]
    public int layer = 0;

    // 可选：通过 UI Slider 调用
    public void SetProgress(float value)
    {
        t = Mathf.Clamp01(value);
        if (t>=1)
        {
            t = 0.99f;
        }
        Apply();
    }

    void OnEnable()
    {
        if (animator != null)
            animator.speed = 0f; // 停住动画，完全由t控制
        Apply();
    }

    void Update()
    {
        Apply();
    }
    

    void Apply()
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;

        // 将该层切到目标状态，并设置 normalizedTime = t（0~1，对应整段动画）
        int hash = Animator.StringToHash(stateName);
        animator.Play(hash, layer, Mathf.Clamp01(t));

        // 立刻评估到该姿态（不推进时间）
        animator.Update(0f);
    }
}

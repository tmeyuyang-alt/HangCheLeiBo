using UnityEngine;
public class AutoHeight: MonoBehaviour
{

    public RectTransform m_Content;
    public RectTransform m_Target;

    // Update is called once per frame
    void Update()
    {
        m_Content.sizeDelta = new Vector2(m_Content.sizeDelta.x, m_Target.sizeDelta.y + 0.01f);
    }
}

using UnityEngine;
public class AutoWidth : MonoBehaviour
{

    public RectTransform m_Content;
    public RectTransform m_Target;

    // Update is called once per frame
    void Update()
    {
        m_Content.sizeDelta = new Vector2(m_Target.sizeDelta.x + 0.01f, m_Content.sizeDelta.y);
    }
}

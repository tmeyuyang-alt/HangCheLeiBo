
using System;
using UMP;
using UnityEngine;
using UnityEngine.UI;


public class MonitorPanel : MonoBehaviour
{
    public RectTransform rectTransform;

    private int PointIndex = -1;

    private Vector3 LastMousePos = Vector3.zero;

    public bool Drag = false;
    public static bool DragWindow = false;

    private float EdgeAndPointSize = 20;

    public static MonitorPanel ActivePanel = null;

    public Vector2 SizePanel;
    public Vector2 PositionPanel;

  
    public Vector2 scale= Vector2.one;

   // public UniversalMediaPlayer ump;
    void Start()
    { 
        rectTransform = GetComponent<RectTransform>();

        SizePanel = rectTransform.sizeDelta;
        PositionPanel = rectTransform.position;
       // gameObject.SetActive(false);

     
        scale = GameObject.Find("MainCanvas").transform.localScale;

         //ump = transform.GetComponentInChildren<UniversalMediaPlayer>();

        //ump.transform.parent = transform.parent.parent;

     //   ump.EventManager.PlayerEncounteredErrorListener += EncounteredErrorListener;
    }

    private void EncounteredErrorListener()
    {
        //Debug.LogError("EncounteredErrorListener");
    }

    public void RestPanel()
    {
        rectTransform.sizeDelta =SizePanel;
        rectTransform.position = PositionPanel;
        gameObject.SetActive(true);
    }

    public void Play()
    {
        
        //ump.Play();
    }

    public void Hidden()
    {
        gameObject.SetActive(false);
    }

    public Vector3 Mul(Vector3 a,Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            LastMousePos = Input.mousePosition;

            Vector2 box = new Vector2(rectTransform.rect.width, rectTransform.rect.height)* scale;

            float sd = sdBox(Input.mousePosition - rectTransform.position, box * 0.5f);

            //TODO �жϷ���
            var temp = box * 0.5f;
            var xdpos = Input.mousePosition - rectTransform.position;

            if (Mathf.Abs(sd) < EdgeAndPointSize)
            {
                //�ж��ǲ����ĸ���
                Vector2[] points = new Vector2[4];

                points[0] = new Vector2(-temp.x, temp.y);//����
                points[1] = new Vector2(temp.x, temp.y);//����
                points[2] = new Vector2(-temp.x, -temp.y);//����
                points[3] = new Vector2(temp.x, -temp.y);//����

                bool IsCorner = false;
                for (int i = 0; i < points.Length; i++)
                {
                    Vector2 pos = points[i] + (Vector2)rectTransform.position;

                    if ((pos - (Vector2)Input.mousePosition).magnitude < EdgeAndPointSize)
                    {
                        IsCorner = true;
                        PointIndex = i;

                        break;
                    }
                }

                if (IsCorner)
                {
                    Drag = true;

                 

                    return;
                }
            }



            if (Mathf.Abs(xdpos.x) < temp.x - EdgeAndPointSize && Mathf.Abs(xdpos.y) < temp.y - EdgeAndPointSize )
            {
                DragWindow = true;
                //Debug.Log("ѡ�����"+this.gameObject.name);
                this.transform.SetAsLastSibling();
                ActivePanel = this;
            }
        }
        //�������϶�
        if (Input.GetMouseButton(0))
        {
            if(DragWindow && ActivePanel == this)
            {
                Vector3 detalSize = Input.mousePosition - LastMousePos;

                rectTransform.position += detalSize;

                LastMousePos = Input.mousePosition;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            Drag = false;
            DragWindow = false;
            PointIndex = -1;
            //if (ActivePanel == this)
            
        }
        if (DragWindow) return;


        if (Input.GetMouseButton(0))
        {
            Vector3 detalSize = Input.mousePosition - LastMousePos;

            Vector3 scalePos = scale;
            detalSize = detalSize / scale;
            //detalSize = detalSize / scale;
            //���Ͻ�
            if (PointIndex == 0)
            {
                detalSize.x *= -1;
               // rectTransform.sizeDelta += (Vector2)detalSize;
                detalSize.y *= -1;
                //rectTransform.position -= detalSize*0.5f;
                rectTransform.position -= Mul(detalSize, scalePos) *0.5f;

            }
            else if (PointIndex == 1) //���Ͻ�
            {
               // rectTransform.sizeDelta += (Vector2)detalSize;
                detalSize.y *= -1;
                detalSize.x *= -1;
                //rectTransform.position -= detalSize * 0.5f;
                rectTransform.position -= Mul(detalSize, scalePos) * 0.5f;

            }
            else if (PointIndex == 2) //���½�
            {

                detalSize.y *= -1;
                detalSize.x *= -1;
              //  rectTransform.sizeDelta += (Vector2)detalSize;
                //rectTransform.position -= detalSize * 0.5f;
                rectTransform.position -= Mul(detalSize, scalePos) * 0.5f;

            }
            else if (PointIndex == 3) //���½�
            {
                detalSize.y *= -1;
               // rectTransform.sizeDelta += (Vector2)detalSize;
                detalSize.x *= -1;
                //rectTransform.position -= detalSize * 0.5f;
                rectTransform.position -= Mul(detalSize, scalePos) * 0.5f;

            }

            Vector2 max = Vector2.Max(new Vector2(128, 128), rectTransform.sizeDelta);
            //rectTransform.sizeDelta = max;
            LastMousePos = Input.mousePosition;
        }

    }

    public float sdBox(Vector2 p, in Vector2 b)
    {
        Vector2 d = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b;
        return Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0.0f);
    }

    public bool InCorner()
    {

        Vector2 box = new Vector2(rectTransform.rect.width, rectTransform.rect.height)* scale;

        float sd = sdBox(Input.mousePosition - rectTransform.position, box * 0.5f);

        if (Mathf.Abs(sd) < 10)
        {

            //TODO �жϷ���
            var temp = box * 0.5f;
            var xdpos = Input.mousePosition - rectTransform.position;


            //�ж��ǲ����ĸ���
            Vector2[] points = new Vector2[4];
            points[0] = new Vector2(-temp.x, temp.y);//����
            points[1] = new Vector2(temp.x, temp.y);//����
            points[2] = new Vector2(-temp.x, -temp.y);//����
            points[3] = new Vector2(temp.x, -temp.y);//����

            bool IsCorner = false;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 pos = points[i] + (Vector2)rectTransform.position;

                if ((pos - (Vector2)Input.mousePosition).sqrMagnitude < EdgeAndPointSize * EdgeAndPointSize)
                {
                    IsCorner = true;
                    PointIndex = i;
                }
            }

            return IsCorner;
        }
        return false;
    }
}






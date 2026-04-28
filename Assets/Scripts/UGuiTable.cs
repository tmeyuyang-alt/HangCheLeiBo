using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UGuiTable : MonoBehaviour
{

    [System.Serializable]
    public class FrameType
    { 
        public string typeName;
        public GameObject preset;
    }
    [System.Serializable]
    public class TableHeader
    {
        /// <summary>
        /// ��ͷ����
        /// </summary>
        public string name;
        public string type;
        public string tag;
        public TableHeader(string nm, string ty,string tag="")
        {
            this.name = nm;
            this.type = ty;
            this.tag = tag;
        }
    }

    public Color headerCol = Color.black;
    public TableHeader[] headers;
    public FrameType[] types;

    private int SelectedLineIndex = -1;

    private int focusX=-1, focusY=-1;

    public Vector2Int GetGridFocus
    {
        get {
            return new Vector2Int(focusX, focusY);
        }
        set {
            focusX = value.x;
            focusY = value.y;
        }
    }
    public RectTransform SelectFrame;

    public System.Action<int, int,string> OnTextChanged;
    /// <summary>
    /// Ԥ��
    /// </summary>
    private Dictionary<string,GameObject> presets;

    public int Col = 5;
    public int Row = 8;
    public float LineWidth = 2;//��������
    //public GameObject Line = null;
    //public GameObject Frame = null;
    

    public bool hasHeader = true;
    

    [Header("pivot ʹ��(0.5,1)")]
    public RectTransform content;

    private List<Transform> frames = new List<Transform>();
    private List<Transform> lines = new List<Transform>();

    public bool OnAwakeGen = true;

    private bool Initaled = false;

    public System.Action<int> OnMoveItem;

    public Transform LineSwitchBtns;
    void Awake()
    {

        if (SelectFrame != null)
        {
            //Transform btns = SelectFrame.Find("Btns");
            if (LineSwitchBtns!=null)
            {
                var up = LineSwitchBtns.GetChild(0).GetComponent<Button>();
                var down = LineSwitchBtns.GetChild(1).GetComponent<Button>();

                up.onClick.AddListener(MoveItemUp);

                down.onClick.AddListener(MoveItemDown);
            }
        }
        presets = new Dictionary<string, GameObject>();
        //��ʼ��Ԥ���ֵ�
        for (int i = 0; i < types.Length; i++)
        {
            presets.Add(types[i].typeName, types[i].preset);
            types[i].preset.SetActive(false);
        }

        if (!OnAwakeGen) return;
        
        Inital();
    }

    public void MoveItemUp()
    {
        Debug.Log("UP");
        if (this.OnMoveItem != null)
            this.OnMoveItem(-1);
    }
    public void MoveItemDown()
    {
        Debug.Log("DOWN");
        if(this.OnMoveItem!=null)
        this.OnMoveItem(1);
    }
    /// <summary>
    /// ���ñ�ͷ
    /// </summary>
    /// <param name="table_Headers"></param>
    public void SetHeader(TableHeader[] table_Headers)
    {
        this.headers = table_Headers;
    }
    /// <summary>
    /// 销毁所有已生成的格子并重置状态，调用后可安全地再次调用 Inital()。
    /// </summary>
    public void ResetTable()
    {
        foreach (Transform frame in frames)
        {
            if (frame != null)
                Destroy(frame.gameObject);
        }
        frames.Clear();
        Initaled = false;
    }

    [ContextMenu("Init")]
    public void Inital()
    {
        content = GetComponent<RectTransform>();


        float contentWidth = content.rect.width;
        float contentHeight = content.rect.height-40;

        float GridSizeX = (contentWidth - (Col + 1) * LineWidth) / Col;
        float GridSizeY = (contentHeight - (Row + 1) * LineWidth) / Row;

        for (int y = 0; y < Row; y++)
        {
            for (int x = 0; x < Col; x++)
            {
                GameObject frame = null;

                int indexX = x;
                int indexY = y;

                //�Ƿ���ͷ
                if (y == 0 && hasHeader)
                {
                    frame = GameObject.Instantiate(presets["Label"]);
                    frame.GetComponentInChildren<Text>().text = headers[x].name;
                    frame.GetComponentInChildren<Text>().color = headerCol;
                }
                else if (presets.ContainsKey(headers[x].type))
                {

                    frame = GameObject.Instantiate(presets[headers[x].type]);

                   var input = frame.GetComponentInChildren<InputField>();
                    if (input != null)
                    {
                        input.onValueChanged.AddListener((e) =>
                        {

                            OnTextChanged?.Invoke(indexX, indexY, input.text);
                        });

                        var et = input.GetComponentInChildren<EventTrigger>();
                        if (et != null)
                        {
                            et.triggers = new List<EventTrigger.Entry>();
                            EventTrigger.Entry entryClick = new EventTrigger.Entry();
                            EventTrigger.Entry entryEnter = new EventTrigger.Entry();


                            entryClick.eventID = EventTriggerType.PointerClick;
                            entryEnter.eventID = EventTriggerType.PointerEnter;

                            entryClick.callback = new EventTrigger.TriggerEvent();
                            entryEnter.callback = new EventTrigger.TriggerEvent();

                            UnityAction<BaseEventData> pointerClick = new UnityAction<BaseEventData>(delegate { OnPointerClick(indexX, indexY, input.text); });
                            UnityAction<BaseEventData> pointerEnter = new UnityAction<BaseEventData>(delegate { OnPointerEnter(indexX, indexY, input.text); });




                            entryClick.callback.AddListener(pointerClick);
                            entryEnter.callback.AddListener(pointerEnter);


                            et.triggers.Add(entryClick);
                            et.triggers.Add(entryEnter);
                        }

                    }
                }

                frame.transform.SetParent(content, false);
                frame.SetActive(true);
                frames.Add(frame.transform);
            }
        }

        Initaled = true;
        UpdateGrid();
    }

    void OnPointerClick(int x,int y,string text)
    {
        //Debug.Log("Click"+x+" "+y+" " +text);

        SelectedLineIndex = y;
    }

    void OnPointerEnter(int x, int y, string text)
    {
        //Debug.Log("Enter");

        focusX = x;
        focusY = y;

        //Debug.Log(focusX +","+ focusY);
    }

    void Update()
    {
        if (Initaled)
            UpdateGrid();
    }

    public void Clear(int startLine)
    {
        int start = startLine * Col;

        for (int i = start; i < frames.Count; i++)
        {
            Transform item = frames[i];

            for (int j = 0; j < item.childCount; j++)
            {
                item.GetChild(j).gameObject.SetActive(false);
            }

        }
    }

    public void Show(int startLine, int endLine)
    {
        int start = startLine * Col;

        int end = endLine == -1 ? frames.Count : endLine * Col;

        for (int i = start; i < end; i++)
        {
            Transform item = frames[i];

            for (int j = 0; j < item.childCount; j++)
            {
                item.GetChild(j).gameObject.SetActive(true);
            }

        }
    }

    /// <summary>实际数据行数（不含表头行）</summary>
    public int MaxDataRows => (Col > 0 && frames.Count > 0) ? frames.Count / Col - 1 : 0;

    public Transform GetItem(int line, int index)
    {
        if (index >= headers.Length) return null;
        int frameIdx = line * Col + index;
        if (frameIdx < 0 || frameIdx >= frames.Count) return null;
        return frames[frameIdx].GetChild(0);
    }

    public Transform GetItemRaw(int line, int index)
    {
        return frames[line * Col + index];
    }

    public void RemoveLine()
    {
        content = GetComponent<RectTransform>();

        float contentHeight = content.rect.height;

        float GridSizeY = (contentHeight - (Row + 1) * LineWidth) / Row;

        content.sizeDelta = new Vector2(content.sizeDelta.x, content.sizeDelta.y - GridSizeY);
        Row -= 1;
        UpdateGrid();
    }
    public void AddRowOne()
    {
        content = GetComponent<RectTransform>();


        //float contentWidth = content.rect.width;
        float contentHeight = content.rect.height;

        //float GridSizeX = (contentWidth - (Row + 1) * LineWidth) / Row;
        float GridSizeY = (contentHeight - (Row + 1) * LineWidth) / Row;

        var size = content.sizeDelta;

        size.y += GridSizeY + LineWidth;

        content.sizeDelta = size;

        Row += 1;

  

        UpdateGrid();
    }

    private void Registered(GameObject frame,int indexX, int indexY)
    {
        var input = frame.GetComponentInChildren<InputField>();
        if (input != null)
        {
            input.onValueChanged.AddListener((e) =>
            {

                OnTextChanged?.Invoke(indexX, indexY, input.text);
            });

            var et = input.GetComponentInChildren<EventTrigger>();
            if (et != null)
            {
                et.triggers = new List<EventTrigger.Entry>();
                EventTrigger.Entry entryClick = new EventTrigger.Entry();
                EventTrigger.Entry entryEnter = new EventTrigger.Entry();


                entryClick.eventID = EventTriggerType.PointerClick;
                entryEnter.eventID = EventTriggerType.PointerEnter;

                entryClick.callback = new EventTrigger.TriggerEvent();
                entryEnter.callback = new EventTrigger.TriggerEvent();

                UnityAction<BaseEventData> pointerClick = new UnityAction<BaseEventData>(delegate { OnPointerClick(indexX, indexY, input.text); });
                UnityAction<BaseEventData> pointerEnter = new UnityAction<BaseEventData>(delegate { OnPointerEnter(indexX, indexY, input.text); });




                entryClick.callback.AddListener(pointerClick);
                entryEnter.callback.AddListener(pointerEnter);


                et.triggers.Add(entryClick);
                et.triggers.Add(entryEnter);
            }

        }
    }
    [ContextMenu("Menu")]
    private void UpdateGrid()
    {

        //�ж��Ƿ���Ҫ����
        if (Col * Row > frames.Count)
        {
            int count = Col * Row - frames.Count;

            for (int i = 0; i < count; i++)
            {

                GameObject frame=null;

                if (i > headers.Length)
                {
                    frame = GameObject.Instantiate(presets[headers[0].type]);
                }
                else
                {
                    if (presets.ContainsKey(headers[i].type))
                    {
                        frame = GameObject.Instantiate(presets[headers[i].type]);

                        //Registered(frame, Col-1,Row-1);
                        Registered(frame, i%Col,Row-1);
                    }
                }

                frame.GetComponentInChildren<Text>().text = "";
                frame.transform.SetParent(content, false);
                frame.SetActive(true);
                frames.Add(frame.transform);
            }
        }
        else if (Col * Row < frames.Count)
        {
            int count = frames.Count - Col * Row;

            if (frames[frames.Count - 1].gameObject.activeSelf)
            {

                for (int i = Col * Row; i < frames.Count; i++)
                {
                    frames[i].gameObject.SetActive(false);
                }
            }
        }
 

        float contentWidth = content.rect.width;
        float contentHeight = content.rect.height;

        float GridSizeX = (contentWidth - (Col + 1) * LineWidth) / Col;
        float GridSizeY = (contentHeight - (Row + 1) * LineWidth) / Row;

        int index = 0;

        for (int y = 0; y < Row; y++)
        {
            if (y == 0)
            {
                GridSizeY = (contentHeight - (Row + 1) * LineWidth) / Row + 40;
             }
            else
            {
                GridSizeY = (contentHeight - (Row + 1) * LineWidth) / Row;
            }
            for (int x = 0; x < Col; x++)
            {
              
                Transform frame = frames[index++];

                if (!frame.gameObject.activeSelf) { frame.gameObject.SetActive(true); }
       

                Vector2 offset = new Vector2((-contentWidth + GridSizeX) * 0.5f + LineWidth,
                                            (-GridSizeY) * 0.5f - LineWidth);

                if (y >0)
                {
                    frame.localPosition = new Vector3(x * GridSizeX + x * LineWidth + offset.x, -(y * GridSizeY + y * LineWidth+40) + offset.y);
                }
                else
                {
                    frame.localPosition = new Vector3(x * GridSizeX + x * LineWidth + offset.x, -(y * GridSizeY + y * LineWidth) + offset.y);
                }
               
              

                var sizeDelata = frame.GetComponent<RectTransform>().sizeDelta;

                sizeDelata.x = GridSizeX;
                sizeDelata.y = GridSizeY;

                frame.GetComponent<RectTransform>().sizeDelta = sizeDelata;

            }
        }

        if (SelectFrame != null && SelectedLineIndex != -1)
        {
            var sizeDeata = SelectFrame.sizeDelta;

            sizeDeata.x = contentWidth;
            sizeDeata.y = GridSizeY;

            SelectFrame.sizeDelta = sizeDeata;


            Vector2 offset = new Vector2(0, (-GridSizeY) * 0.5f - LineWidth);

            float scale = GridSizeY + LineWidth;

            SelectFrame.localPosition = new Vector2(0, -scale * SelectedLineIndex - (GridSizeY + LineWidth) * 0.5f);//-(SelectedLineIndex* GridSizeY+ SelectedLineIndex*LineWidth)+offset.y);

            SelectFrame.SetAsLastSibling();
        }
        else if (SelectFrame != null)
        {
            SelectFrame.localPosition = new Vector3(0, 20000);
        }
    }

    public void SetActiveLine(int index)
    {
        SelectedLineIndex = index;
    }
    public int GetActiveLine()
    {
        return SelectedLineIndex;
    }

    //public GameObject Instance()
    //{
    //    //�Ƿ���ͷ
    //    if (y == 0)
    //    {
    //        frame = GameObject.Instantiate(presets["Label"]);
    //        frame.GetComponentInChildren<Text>().text = headers[x].name;
    //    }
    //    else if (presets.ContainsKey(headers[x].type))
    //    {

    //        frame = GameObject.Instantiate(presets[headers[x].type]);
    //    }
    //    else
    //    {
    //        frame = GameObject.Instantiate(Frame);
    //    }
    //}
}

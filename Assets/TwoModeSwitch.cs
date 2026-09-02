using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TwoModeSwitch : MonoBehaviour
{
    public enum Mode
    {
        Mode1,
        Mode2
    }

    public Button mode1Button;
    public Button mode2Button;

    public Image mode1ButtonImage;
    public Image mode2ButtonImage;
    public Sprite normalSprite;
    public Sprite selectedSprite;

   // public GameObject mode1SelectedObject;
   // public GameObject mode2SelectedObject;

    public GameObject[] mode1ShowObjects;
    public GameObject[] mode1HideObjects;
    public GameObject[] mode2ShowObjects;
    public GameObject[] mode2HideObjects;

    public bool disableSelectedButton = true;
    public bool invokeEventOnStart = true;
    public Mode defaultMode = Mode.Mode1;

    public UnityEvent onMode1;
    public UnityEvent onMode2;

    private Mode currentMode;

    private void Awake()
    {
        if (mode1ButtonImage == null && mode1Button != null)
        {
            mode1ButtonImage = mode1Button.GetComponent<Image>();
        }

        if (mode2ButtonImage == null && mode2Button != null)
        {
            mode2ButtonImage = mode2Button.GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        if (mode1Button != null)
        {
            mode1Button.onClick.AddListener(SetMode1);
        }

        if (mode2Button != null)
        {
            mode2Button.onClick.AddListener(SetMode2);
        }
    }

    private void Start()
    {
        SetMode(defaultMode, invokeEventOnStart);
    }

    private void OnDisable()
    {
        if (mode1Button != null)
        {
            mode1Button.onClick.RemoveListener(SetMode1);
        }

        if (mode2Button != null)
        {
            mode2Button.onClick.RemoveListener(SetMode2);
        }
    }

    public void SetMode1()
    {
        SetMode(Mode.Mode1, true);
    }

    public void SetMode2()
    {
        SetMode(Mode.Mode2, true);
    }

    public void SetMode(Mode mode)
    {
        SetMode(mode, true);
    }

    private void SetMode(Mode mode, bool invokeEvent)
    {
        currentMode = mode;
        bool isMode1 = currentMode == Mode.Mode1;

        SetButtonSelected(mode1Button, mode1ButtonImage, isMode1);
        SetButtonSelected(mode2Button, mode2ButtonImage, !isMode1);

        // SetActive(mode1SelectedObject, isMode1);
        // SetActive(mode2SelectedObject, !isMode1);

        SetActive(mode1ShowObjects, isMode1);
        SetActive(mode1HideObjects, !isMode1);
        SetActive(mode2ShowObjects, !isMode1);
        SetActive(mode2HideObjects, isMode1);

        if (!invokeEvent)
        {
            return;
        }

        if (isMode1)
        {
            onMode1?.Invoke();
        }
        else
        {
            onMode2?.Invoke();
        }
    }

    private void SetButtonSelected(Button button, Image image, bool selected)
    {
        if (image != null)
        {
            image.sprite = selected ? selectedSprite : normalSprite;
        }

        if (button != null && disableSelectedButton)
        {
            button.interactable = !selected;
        }
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void SetActive(GameObject[] targets, bool active)
    {
        if (targets == null)
        {
            return;
        }

        foreach (GameObject target in targets)
        {
            SetActive(target, active);
        }
    }
}

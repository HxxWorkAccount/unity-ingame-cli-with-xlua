namespace UnityLearning {

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using UnityLearning.Utils;
using System.Linq;
using UnityEngine.Rendering;

public class GameCLILogItem
    : MonoBehaviour,
      IPointerClickHandler
{
    [SerializeField]
    private Image m_bgImage;
    [SerializeField]
    private Image m_logTypeImage;
    [SerializeField]
    private TMP_Text m_logText;
    [SerializeField]
    private TMP_Text m_stackText;

    private int    m_index = -1;
    private Sprite m_logSprite;
    private Sprite m_warningSprite;
    private Sprite m_errorSprite;

    void Awake() {
        // Load sprites
        m_logSprite     = Resources.Load<Sprite>("Sprites/Log");
        m_warningSprite = Resources.Load<Sprite>("Sprites/Alert");
        m_errorSprite   = Resources.Load<Sprite>("Sprites/Close");
    }

    void OnEnable() {
        if (GameCLI.RawInstance != null)
            GameCLI.RawInstance.RegisterSelectingChanged(OnSelectingChanged);
    }

    void OnDisable() {
        m_index = -1;
        if (GameCLI.RawInstance != null)
            GameCLI.RawInstance.UnregisterSelectingChanged(OnSelectingChanged);
    }

    private void OnSelectingChanged(int index) {
        UpdateBgColor();
    }

    public void SetData(LogItemData data, int index) {
        if (m_index == index)
            return;

        m_index          = index;
        m_logText.text   = $"[{data.timeString}] {data.logString}";
        m_stackText.text = data.stackTrace;
        UpdateBgColor();

        switch (data.logType) {
        case LogType.Error:
        case LogType.Assert:
        case LogType.Exception:
            m_logTypeImage.color  = new Color(1, 0.45f, 0.45f);
            m_logTypeImage.sprite = m_errorSprite;
            break;
        case LogType.Warning:
            m_logTypeImage.color  = Color.yellow;
            m_logTypeImage.sprite = m_warningSprite;
            break;
        case LogType.Log:
            m_logTypeImage.color  = Color.white;
            m_logTypeImage.sprite = m_logSprite;
            break;
        }
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        GameCLI.RawInstance.SelectingIndex = m_index;
    }

    private void UpdateBgColor() {
        if (m_index == GameCLI.RawInstance.SelectingIndex)
            m_bgImage.color = new Color(0.227f, 0.447f, 0.69f, 0.572f);
        else
            m_bgImage.color =  new Color(1, 1, 1, m_index % 2 == 0 ? 0.15f : 0.2f);
    }
}

}

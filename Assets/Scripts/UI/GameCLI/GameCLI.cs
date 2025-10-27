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
using UnityEngine.Events;
using XLua;

public class LogItemData
{
    public string  logString;
    public string  stackTrace;
    public string  timeString;
    public LogType logType;

    public LogItemData(string logString, string stackTrace, LogType logType) {
        this.logString  = logString;
        this.stackTrace = stackTrace;
        this.logType    = logType;
        this.timeString = DateTime.Now.ToString("HH:mm:ss");
    }
}

[DefaultExecutionOrder(10000)]  // 靠后执行
public class GameCLI : Singleton<GameCLI>
{
    const int MAX_LOG_COUNT          = 100000 * 100;  // 最大日志字符数，假设每行 100 字，支持 100000 行
    const int LEAST_LOG_SHRINK_COUNT = 1000 * 100;

    /*====-------------- Members --------------====*/

    [SerializeField]
    private GameObject m_rootCanvas;
    [SerializeField]
    private InputActionAsset m_inputActions;

    [Header("UI Elements")]
    [SerializeField]
    private Image m_bgImage;
    [SerializeField]
    private Button m_closeButton;
    [SerializeField]
    private Button m_clearButton;
    [SerializeField]
    private Button m_lockButton;
    [SerializeField]
    private ScrollRect m_logScrollRect;
    [SerializeField]
    private RectTransform m_logScrollList;
    [SerializeField]
    private RectTransform m_logViewport;
    [SerializeField]
    private TMP_InputField m_inputField;

    private InputAction m_toggleConsoleAction;
    private InputAction m_copyLogAction;
    private InputAction m_previousHistoryAction;
    private InputAction m_nextHistoryAction;

    private bool m_registered = false;

    // Log
    /* TODO == 之后对 Log 做更多支持：多线程支持、缓存上限 */
    private readonly UnityEvent<int> m_onSelectingChanged = new();
    private bool                     m_dirtyLog           = true;
    private int                      m_selectingIndex     = -1;
    private GameCLILogItem           m_logItemTemplate;
    private readonly List<GameCLILogItem> m_logItemList             = new();
    private readonly List<LogItemData> m_logEntities                = new();
    private readonly ConcurrentQueue<LogItemData> m_tempLogEntities = new();
    private bool                                  m_lockScroll      = false;
    private Sprite                                m_lockSprite;
    private Sprite                                m_unlockSprite;

    // Input
    private readonly List<string> m_inputHistory     = new();
    private string                m_tempHistory      = string.Empty;
    private int                   m_currHistoryIndex = -1;
    private LuaTable              m_env;

    /*====-------------- Messages --------------====*/

    protected override void Awake() {
        base.Awake();

        /* Binding and load resources */
        m_toggleConsoleAction   = m_inputActions.FindActionMap("GameCLI").FindAction("ToggleConsole");
        m_copyLogAction         = m_inputActions.FindActionMap("GameCLI").FindAction("CopyLog");
        m_previousHistoryAction = m_inputActions.FindActionMap("GameCLI").FindAction("PreviousHistory");
        m_nextHistoryAction     = m_inputActions.FindActionMap("GameCLI").FindAction("NextHistory");

        m_lockSprite   = Resources.Load<Sprite>("Sprites/Lock");
        m_unlockSprite = Resources.Load<Sprite>("Sprites/Unlock");

        /* Events */
        RegisterEvents(true);

        /* Init */
        m_logItemTemplate = GetComponentInChildren<GameCLILogItem>(true);
        m_logItemTemplate.gameObject.SetActive(false);

        LockScroll = false;
        if (m_rootCanvas.activeSelf)
            OpenCLI(false);
    }

    void OnEnable() {
        m_toggleConsoleAction.Enable();
        m_toggleConsoleAction.performed += OnToggleConsole;
        m_copyLogAction.Enable();
        m_copyLogAction.performed += OnCopyLog;
        m_previousHistoryAction.Enable();
        m_previousHistoryAction.performed += OnPreviousHistory;
        m_nextHistoryAction.Enable();
        m_nextHistoryAction.performed += OnNextHistory;
        m_closeButton.onClick.AddListener(OnClickCloseButton);
    }

    void OnDisable() {
        m_toggleConsoleAction.performed -= OnToggleConsole;
        m_toggleConsoleAction.Disable();
        m_copyLogAction.performed -= OnCopyLog;
        m_copyLogAction.Disable();
        m_previousHistoryAction.performed -= OnPreviousHistory;
        m_previousHistoryAction.Disable();
        m_nextHistoryAction.performed -= OnNextHistory;
        m_nextHistoryAction.Disable();
        m_closeButton.onClick.RemoveListener(OnClickCloseButton);
    }

    void Start() {
        // StartCoroutine(TestLog());
    }

    IEnumerator TestLog() {
        while (true) {
            Debug.Log(
                "很长很长的 Log 哟~~~一二三一二三, Hello world, may I have a cookie?!#@*$()%))$##!! 哈哈哈 666 12223333 iasdjfoiasodijfoaisjdoifjoasijdofijasodifjoasidjfoasijdoijf，很长很长的 Log 哟~~~一二三一二三"
            );
            yield return new WaitForSeconds(1.0f);
            // Debug.LogError("Error Log");
            // yield return new WaitForSeconds(1.0f);
            // Debug.LogWarning("Warning Log");
            // yield return new WaitForSeconds(1.0f);
        }
    }

    void Update() {
        bool newLog = m_tempLogEntities.Count > 0;
        while (m_tempLogEntities.TryDequeue(out LogItemData logEntity)) {
            m_logEntities.Add(logEntity);
        }
        if (newLog)
            UpdateHeight();

        /* Render */
        if (m_rootCanvas.activeInHierarchy) {
            if (m_dirtyLog)
                UpdateLog();
        }
    }

    void OnDestroy() {
        RegisterEvents(false);
    }

    /*====-------------- Events --------------====*/

    private void RegisterEvents(bool register) {
        if (register && !m_registered) {
            Application.logMessageReceived += HandleLog;
            m_clearButton.onClick.AddListener(OnClickClearButton);
            m_lockButton.onClick.AddListener(OnClickLockButton);
            m_logScrollRect.onValueChanged.AddListener(OnLogScroll);
            m_inputField.onSubmit.AddListener(OnInputSubmit);
        } else if (!register && m_registered) {
            Application.logMessageReceived -= HandleLog;
            m_clearButton.onClick.RemoveListener(OnClickClearButton);
            m_lockButton.onClick.RemoveListener(OnClickLockButton);
            m_logScrollRect.onValueChanged.RemoveListener(OnLogScroll);
            m_inputField.onSubmit.RemoveListener(OnInputSubmit);
        }
    }

    private void OnToggleConsole(InputAction.CallbackContext context) {
        OpenCLI(!m_rootCanvas.activeSelf);
    }

    private void OnClickCloseButton() {
        OpenCLI(false);
    }

    private void OnClickClearButton() {
        m_logEntities.Clear();
        m_selectingIndex = -1;
        UpdateHeight();
        m_dirtyLog = true;
    }

    private void OnClickLockButton() {
        LockScroll = !LockScroll;
    }

    public void ForwardScrollEvent(BaseEventData data) {
        PointerEventData ped = (PointerEventData)data;
        m_logScrollRect.OnScroll(ped);
    }

    /*====-------------- UI --------------====*/

    public void OpenCLI(bool open) {
        m_rootCanvas.SetActive(open);
        if (!open) {
            m_dirtyLog       = true;
            m_selectingIndex = -1;
        }
    }

    /*====-------------- Scroll Log --------------====*/

    public bool LockScroll {
        get => m_lockScroll;
        set {
            m_lockScroll              = value;
            m_lockButton.image.sprite = m_lockScroll ? m_lockSprite : m_unlockSprite;
        }
    }

    public void RegisterSelectingChanged(UnityAction<int> callback) {
        m_onSelectingChanged.AddListener(callback);
    }
    public void UnregisterSelectingChanged(UnityAction<int> callback) {
        m_onSelectingChanged.RemoveListener(callback);
    }
    public int SelectingIndex {
        get => m_selectingIndex;
        set {
            if (m_selectingIndex == value)
                return;
            m_selectingIndex = value;
            m_onSelectingChanged.Invoke(m_selectingIndex);
        }
    }

    private void OnLogScroll(Vector2 pos) {
        m_dirtyLog = true;
    }

    private void OnCopyLog(InputAction.CallbackContext context) {
        if (m_selectingIndex >= 0 && m_selectingIndex < m_logEntities.Count) {
            LogItemData data            = m_logEntities[m_selectingIndex];
            GUIUtility.systemCopyBuffer = String.Concat(data.logString, "\n", data.stackTrace);
        }
    }

    private void UpdateHeight() {
        float y                   = m_logItemTemplate.GetComponent<RectTransform>().rect.height * m_logEntities.Count;
        m_logScrollList.sizeDelta = new Vector2(m_logScrollList.sizeDelta.x, y);
    }

    // TODO == 优化（循环列表，中间控件不更新）
    private void UpdateLog() {
        RectTransform templateRect = m_logItemTemplate.GetComponent<RectTransform>();

        if (!LockScroll)
            m_logScrollRect.verticalNormalizedPosition = 0;

        // 从第几个 item 开始，显示多少个 item
        float yPos       = m_logScrollList.anchoredPosition.y;
        int   firstIndex = Mathf.Max(0, (int)(yPos / templateRect.rect.height - 2));
        int   count      = (int)(m_logViewport.rect.height / templateRect.rect.height);
        count += 5;  // 多显示几行

        // 如果不够就创建并插入
        if (m_logItemList.Count < count) {
            int need = count - m_logItemList.Count;
            for (int i = 0; i < need; i++) {
                GameCLILogItem newItem = Instantiate(m_logItemTemplate, m_logScrollList);
                m_logItemList.Add(newItem);
            }
        }

        // 遍历 LogItem 列表，将头 n 个启用、SetData、设置位置。关闭后面的 LogItem
        for (int i = 0; i < m_logItemList.Count; i++) {
            GameCLILogItem item = m_logItemList[i];
            if (i < count && firstIndex + i < m_logEntities.Count) {
                item.gameObject.SetActive(true);
                item.SetData(m_logEntities[firstIndex + i], firstIndex + i);
                RectTransform itemRectTransform    = item.GetComponent<RectTransform>();
                Vector2       pos                  = itemRectTransform.anchoredPosition;
                pos.y                              = -itemRectTransform.rect.height * (firstIndex + i);
                itemRectTransform.anchoredPosition = pos;
            } else {
                item.gameObject.SetActive(false);
            }
        }

        m_dirtyLog = false;
    }

    private void HandleLog(string logString, string stackTrace, LogType type) {
        LogItemData logEntity = new LogItemData(logString, stackTrace, type);
        m_tempLogEntities.Enqueue(logEntity);
        m_dirtyLog = true;
    }

    /*====-------------- Input --------------====*/

    private void OnInputSubmit(string input) {
        if (string.IsNullOrWhiteSpace(input))
            return;

        // 更新历史
        if (m_inputHistory.Count > 0 && m_currHistoryIndex >= 0)
            m_inputHistory.RemoveRange(0, Mathf.Min(m_inputHistory.Count, m_currHistoryIndex + 1));
        m_currHistoryIndex = -1;
        m_tempHistory      = string.Empty;
        m_inputHistory.Insert(0, input);
        m_inputField.text = string.Empty;
        m_inputField.ActivateInputField();

        HandleInput(input);
    }

    private void OnPreviousHistory(InputAction.CallbackContext context) {
        if (m_inputHistory.Count == 0 || !m_inputField.isFocused)
            return;
        SwitchHistory(true);
    }

    private void OnNextHistory(InputAction.CallbackContext context) {
        if (m_inputHistory.Count == 0 || !m_inputField.isFocused)
            return;
        SwitchHistory(false);
    }

    private void SwitchHistory(bool previous) {
        int targetHistoryIndex = previous ? m_currHistoryIndex + 1 : m_currHistoryIndex - 1;
        if (targetHistoryIndex < -1 || targetHistoryIndex >= m_inputHistory.Count)
            return;
        if (targetHistoryIndex == -1) {  // 恢复临时输入
            m_inputField.text = m_tempHistory;
        } else {
            if (m_currHistoryIndex == -1)
                m_tempHistory = m_inputField.text;
            m_inputField.text = m_inputHistory[targetHistoryIndex];
        }
        m_currHistoryIndex         = targetHistoryIndex;
        m_inputField.caretPosition = m_inputField.text.Length;
        m_inputField.ActivateInputField();
    }

    private void TryInitEnv() {
        if (m_env != null || LuaManager.RawInstance == null)
            return;
        m_env               = LuaManager.Instance.LuaEnv.NewTable();
        using LuaTable meta = LuaManager.Instance.LuaEnv.NewTable();
        meta.Set("__index", LuaManager.Instance.LuaEnv.Global);
        m_env.SetMetaTable(meta);
        m_env.Set("global", LuaManager.Instance.LuaEnv.Global);
    }

    private void HandleInput(string input) {
        Debug.Log($"> {input}");
        if (input.StartsWith("$")) {
            Debug.LogWarning("Command mode not implemented yet.");
            return;
        } else if (LuaManager.RawInstance == null) {
            Debug.LogWarning("LuaManager not initialized.");
            return;
        } else {
            // TODO 在一个 GameCLI 的 table 环境中执行代码（可访问 _G，但不能直接写入）
            LuaFunction func = null;
            try {
                func = LuaManager.Instance.LuaEnv.LoadString("return " + input, "GameCLI");
            } catch (Exception) { }
            if (func == null)
                func = LuaManager.Instance.LuaEnv.LoadString(input, "GameCLI");

            TryInitEnv();
            if (m_env == null) {
                Debug.LogError("Failed to create CLI lua environment.");
                return;
            }

            func.SetEnv(m_env);
            object[] results = func.Call();
            if (results != null && results.Length != 0) {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < results.Length; i++) {
                    object result = results[i];
                    if (result == null) {
                        sb.Append("nil");
                    } else if (result is string s) {
                        sb.Append($"\"{s}\"");
                    } else if (result is double || result is float) {
                        sb.Append(string.Format("{0:G}", result));
                    } else {
                        sb.Append(result.ToString());
                    }
                    if (i < results.Length - 1)
                        sb.Append("    ");
                }
                Debug.Log($"LUA > {sb}");
            }
        }
    }
}

}

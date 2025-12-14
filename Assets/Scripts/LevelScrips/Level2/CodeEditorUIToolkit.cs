using UnityEngine;
using UnityEngine.UIElements;

public class CodeEditorUIToolkit : MonoBehaviour
{
    private UIDocument uiDocument;
    public StyleSheet customStyleSheet;
    public RectTransform codeEditorPanelRect;  // Перетащи сюда CodeEditorPanel
    
    private TextField codeInput;
    private ScrollView lineNumbersScroll;
    private Label lineNumbers;
    private Button runButton;
    private Button resetButton;
    private Label consoleOutput;
    private ScrollView codeScrollView;
    
    private string[] keywords = { "moveRight", "moveLeft", "jump", "wait", "repeat", "if" };
    private bool isUpdatingScroll = false;
    private int previousCaretPosition = 0;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            uiDocument = gameObject.AddComponent<UIDocument>();
        }

        BuildUI();
    }

    void BuildUI()
    {
        var root = uiDocument.rootVisualElement;
        root.Clear();

        if (customStyleSheet != null)
        {
            root.styleSheets.Add(customStyleSheet);
        }

        // === Фиксированная позиция справа (как CodeEditorPanel) ===
        var mainContainer = new VisualElement();
        mainContainer.name = "MainContainer";
        mainContainer.style.position = Position.Absolute;
        
        // Для разрешения 1920x1080:
        // CodeEditorPanel: Right=-360, Width=720
        // Значит: left = 1920 - 720 - 360 = 840
        mainContainer.style.right = 0;   // ПРОЩЕ: просто справа с отступом 10px
        mainContainer.style.top = 0;
        mainContainer.style.width = 720;
        mainContainer.style.bottom = 0;  // растягиваем до низа экрана
        mainContainer.style.height = Screen.height;
        
        mainContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.19f);
        mainContainer.style.paddingTop = 10;
        mainContainer.style.paddingBottom = 10;
        mainContainer.style.paddingLeft = 10;
        mainContainer.style.paddingRight = 10;
        mainContainer.style.flexDirection = FlexDirection.Column;
        root.Add(mainContainer);

        // === Панель редактора ===
        var editorPanel = new VisualElement();
        editorPanel.name = "EditorPanel";
        editorPanel.style.flexGrow = 1;  // растёт ВНУТРИ mainContainer
        editorPanel.style.flexShrink = 1;  // может сжиматься
        editorPanel.style.minHeight = 400;  // минимум 400px
        editorPanel.style.flexDirection = FlexDirection.Row;
        editorPanel.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
        editorPanel.style.borderBottomWidth = 1;
        editorPanel.style.borderTopWidth = 1;
        editorPanel.style.borderLeftWidth = 1;
        editorPanel.style.borderRightWidth = 1;
        editorPanel.style.borderBottomColor = new Color(0.24f, 0.24f, 0.26f);
        editorPanel.style.borderTopColor = new Color(0.24f, 0.24f, 0.26f);
        editorPanel.style.borderLeftColor = new Color(0.24f, 0.24f, 0.26f);
        editorPanel.style.borderRightColor = new Color(0.24f, 0.24f, 0.26f);
        editorPanel.style.marginBottom = 10;
        mainContainer.Add(editorPanel);

        // === ScrollView для номеров строк ===
        lineNumbersScroll = new ScrollView(ScrollViewMode.Vertical);
        lineNumbersScroll.name = "LineNumbersScroll";
        lineNumbersScroll.style.width = 50;
        lineNumbersScroll.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
        lineNumbersScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        lineNumbersScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        editorPanel.Add(lineNumbersScroll);

        // === Номера строк ===
        lineNumbers = new Label("1");
        lineNumbers.name = "LineNumbers";
        lineNumbers.style.color = new Color(0.52f, 0.52f, 0.52f);
        lineNumbers.style.fontSize = 14;
        lineNumbers.style.paddingTop = 10;
        lineNumbers.style.paddingBottom = 10;
        lineNumbers.style.paddingLeft = 5;
        lineNumbers.style.paddingRight = 5;
        lineNumbers.style.unityTextAlign = TextAnchor.UpperLeft;
        lineNumbers.style.whiteSpace = WhiteSpace.Pre;
        lineNumbersScroll.Add(lineNumbers);

        // === Разделитель ===
        var separator = new VisualElement();
        separator.style.width = 1;
        separator.style.backgroundColor = new Color(0.24f, 0.24f, 0.26f);
        editorPanel.Add(separator);

        // === ScrollView для кода ===
        codeScrollView = new ScrollView(ScrollViewMode.Vertical);
        codeScrollView.name = "CodeScrollView";
        codeScrollView.style.flexGrow = 1;
        editorPanel.Add(codeScrollView);

        // === TextField (многострочный редактор) ===
        codeInput = new TextField();
        codeInput.name = "CodeInput";
        codeInput.multiline = true;
        codeInput.value = "// Напиши код здесь\nmoveRight(5)\njump(8)";
        codeInput.style.fontSize = 14;
        codeInput.style.color = new Color(0.95f, 0.95f, 0.95f);
        codeInput.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f);
        codeInput.style.minHeight = 870;
        
        codeInput.RegisterValueChangedCallback(OnCodeChanged);
        
        // Отслеживаем фокус для каретки
        codeInput.RegisterCallback<FocusInEvent>(evt => UpdateCaretVisibility());
        codeInput.RegisterCallback<KeyDownEvent>(evt => 
        {
            codeInput.schedule.Execute(() => ScrollToCaretPosition()).ExecuteLater(10);
        });
        
        codeScrollView.Add(codeInput);

        // Синхронизация скролла
        codeScrollView.verticalScroller.valueChanged += SyncScroll;

        // Стилизация внутреннего элемента
        codeInput.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            var inputElement = codeInput.Q("unity-text-input");
            if (inputElement != null)
            {
                inputElement.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f);
                inputElement.style.color = new Color(0.95f, 0.95f, 0.95f);
                inputElement.style.paddingTop = 10;
                inputElement.style.paddingBottom = 10;
                inputElement.style.paddingLeft = 10;
                inputElement.style.paddingRight = 10;
                inputElement.style.borderBottomWidth = 0;
                inputElement.style.borderTopWidth = 0;
                inputElement.style.borderLeftWidth = 0;
                inputElement.style.borderRightWidth = 0;
                inputElement.style.whiteSpace = WhiteSpace.Pre;
            }
        });

        // === Панель кнопок ===
        var buttonPanel = new VisualElement();
        buttonPanel.name = "ButtonPanel";
        buttonPanel.style.flexDirection = FlexDirection.Row;
        buttonPanel.style.justifyContent = Justify.SpaceAround;
        buttonPanel.style.height = 40;
        buttonPanel.style.flexShrink = 0;
        buttonPanel.style.marginBottom = 10;
        mainContainer.Add(buttonPanel);

        // === Кнопка Run ===
        runButton = new Button(OnRunCode);
        runButton.name = "RunButton";
        runButton.text = "▶ Run Code";
        runButton.style.width = new Length(45, LengthUnit.Percent);
        runButton.style.backgroundColor = new Color(0f, 0.48f, 0.8f);
        runButton.style.color = Color.white;
        runButton.style.fontSize = 16;
        runButton.style.borderTopLeftRadius = 4;
        runButton.style.borderTopRightRadius = 4;
        runButton.style.borderBottomLeftRadius = 4;
        runButton.style.borderBottomRightRadius = 4;
        buttonPanel.Add(runButton);

        // === Кнопка Reset ===
        resetButton = new Button(OnReset);
        resetButton.name = "ResetButton";
        resetButton.text = "⟲ Reset";
        resetButton.style.width = new Length(45, LengthUnit.Percent);
        resetButton.style.backgroundColor = new Color(0.77f, 0.77f, 0.77f);
        resetButton.style.color = new Color(0.12f, 0.12f, 0.12f);
        resetButton.style.fontSize = 16;
        resetButton.style.borderTopLeftRadius = 4;
        resetButton.style.borderTopRightRadius = 4;
        resetButton.style.borderBottomLeftRadius = 4;
        resetButton.style.borderBottomRightRadius = 4;
        buttonPanel.Add(resetButton);

        // === Консоль ===
        var consolePanel = new VisualElement();
        consolePanel.name = "ConsolePanel";
        consolePanel.style.height = 120;
        consolePanel.style.flexShrink = 0;
        consolePanel.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
        consolePanel.style.borderBottomWidth = 1;
        consolePanel.style.borderTopWidth = 1;
        consolePanel.style.borderLeftWidth = 1;
        consolePanel.style.borderRightWidth = 1;
        consolePanel.style.borderBottomColor = new Color(0.24f, 0.24f, 0.26f);
        consolePanel.style.borderTopColor = new Color(0.24f, 0.24f, 0.26f);
        consolePanel.style.borderLeftColor = new Color(0.24f, 0.24f, 0.26f);
        consolePanel.style.borderRightColor = new Color(0.24f, 0.24f, 0.26f);
        mainContainer.Add(consolePanel);

        var consoleLabel = new Label("Console");
        consoleLabel.style.fontSize = 12;
        consoleLabel.style.color = new Color(0.67f, 0.67f, 0.67f);
        consoleLabel.style.paddingTop = 5;
        consoleLabel.style.paddingLeft = 10;
        consolePanel.Add(consoleLabel);

        var consoleScrollView = new ScrollView(ScrollViewMode.Vertical);
        consoleScrollView.style.flexGrow = 1;
        consolePanel.Add(consoleScrollView);

        consoleOutput = new Label("Console ready.");
        consoleOutput.name = "ConsoleOutput";
        consoleOutput.style.fontSize = 12;
        consoleOutput.style.color = new Color(0.8f, 0.8f, 0.8f);
        consoleOutput.style.paddingTop = 5;
        consoleOutput.style.paddingLeft = 10;
        consoleOutput.style.whiteSpace = WhiteSpace.Pre;
        consoleScrollView.Add(consoleOutput);

        UpdateLineNumbers(codeInput.value);
    }

    void UpdateCaretVisibility()
    {
        if (codeInput != null)
        {
            var inputElement = codeInput.Q("unity-text-input");
            if (inputElement != null)
            {
                inputElement.style.unityBackgroundImageTintColor = Color.white;
            }
        }
    }

    void ScrollToCaretPosition()
    {
        if (codeInput == null || codeScrollView == null) return;

        int caretIndex = codeInput.cursorIndex;
        if (caretIndex == previousCaretPosition) return;
        
        previousCaretPosition = caretIndex;

        string text = codeInput.value.Substring(0, Mathf.Min(caretIndex, codeInput.value.Length));
        int lineNumber = text.Split('\n').Length - 1;

        float lineHeight = 20f;
        float caretY = lineNumber * lineHeight;
        float viewportHeight = codeScrollView.contentViewport.resolvedStyle.height;
        float currentScroll = codeScrollView.scrollOffset.y;

        if (caretY > currentScroll + viewportHeight - 40)
        {
            codeScrollView.scrollOffset = new Vector2(0, caretY - viewportHeight + 60);
        }
        else if (caretY < currentScroll)
        {
            codeScrollView.scrollOffset = new Vector2(0, caretY);
        }
    }

    void SyncScroll(float scrollValue)
    {
        if (lineNumbersScroll != null && !isUpdatingScroll)
        {
            isUpdatingScroll = true;
            lineNumbersScroll.scrollOffset = new Vector2(0, scrollValue);
            isUpdatingScroll = false;
        }
    }

    void OnCodeChanged(ChangeEvent<string> evt)
    {
        UpdateLineNumbers(evt.newValue);
        codeInput.schedule.Execute(() => ScrollToCaretPosition()).ExecuteLater(10);
    }

    void UpdateLineNumbers(string code)
    {
        if (lineNumbers == null || string.IsNullOrEmpty(code))
        {
            if (lineNumbers != null) lineNumbers.text = "1";
            return;
        }

        int lineCount = code.Split('\n').Length;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        for (int i = 1; i <= lineCount; i++)
        {
            sb.Append(i);
            if (i < lineCount) sb.Append('\n');
        }

        lineNumbers.text = sb.ToString();
    }

    void OnRunCode()
    {
        string code = codeInput?.value ?? "";
        
        if (string.IsNullOrWhiteSpace(code))
        {
            AddConsoleLog("❌ Код пустой!", true);
            return;
        }
        
        AddConsoleLog("▶️ Запуск кода...");
        
        var executor = FindObjectOfType<JavaCodeExecutor>();
        if (executor != null)
        {
            executor.ExecuteCode();
        }
        else
        {
            AddConsoleLog("❌ JavaCodeExecutor не найден!", true);
        }
    }

    void OnReset()
    {
        if (codeInput != null)
        {
            codeInput.value = "// Напиши код здесь\n";
        }
        
        AddConsoleLog("⟲ Сброс");
        
        var levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.OnResetLevel();
        }
    }

    void Start()
    {
        if (codeInput != null)
        {
            codeInput.schedule.Execute(() =>
            {
                codeInput.Focus();
            }).ExecuteLater(100);
        }
    }

    public void AddConsoleLog(string message, bool isError = false)
    {
        if (consoleOutput != null)
        {
            string coloredMessage = isError 
                ? $"<color=#FF5555>{message}</color>" 
                : message;
            
            consoleOutput.text += "\n" + coloredMessage;
        }
    }

    public void ClearConsole()
    {
        if (consoleOutput != null)
        {
            consoleOutput.text = "";
        }
    }

    public void SetCode(string code)
    {
        if (codeInput != null)
        {
            codeInput.value = code;
            UpdateLineNumbers(code);
        }
    }

    public string GetCode()
    {
        return codeInput?.value ?? "";
    }

    // public string GetCode()
    // {
    //     return codeInput?.value ?? "";
    // }

    // public void SetCode(string code)
    // {
    //     if (codeInput != null)
    //     {
    //         codeInput.value = code;
    //     }
    // }
}

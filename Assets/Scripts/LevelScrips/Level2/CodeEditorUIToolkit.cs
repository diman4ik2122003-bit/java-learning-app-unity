using UnityEngine;
using UnityEngine.UIElements;

public class CodeEditorUIToolkit : MonoBehaviour
{
    private UIDocument uiDocument;
    public StyleSheet customStyleSheet;
    
    private TextField codeInput;
    private ScrollView lineNumbersScroll;
    private Label lineNumbers;
    private Button runButton;
    private Button resetButton;
    private Label consoleOutput;
    private ScrollView codeScrollView;
    private VisualElement mainContainer;
    private VisualElement consolePanel;
    private VisualElement buttonPanel;
    
    private bool isUpdatingScroll = false;
    private int previousCaretPosition = 0;
    
    // ⭐ Масштабирование
    private float baseScale = 1f;
    private float currentScale = 1f;
    private const float MIN_SCALE = 0.7f;
    private const float MAX_SCALE = 2f;
    private const float SCALE_STEP = 0.1f;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            uiDocument = gameObject.AddComponent<UIDocument>();
        }

        // ⭐ Определяем базовый масштаб по разрешению
        CalculateBaseScale();
        currentScale = baseScale;
        
        BuildUI();
    }

    // ⭐ Вычисляем базовый масштаб по разрешению
    void CalculateBaseScale()
    {
        float screenWidth = Screen.width;
        
        if (screenWidth >= 3840) // 4K
        {
            baseScale = 1.6f;
        }
        else if (screenWidth >= 2560) // QHD
        {
            baseScale = 1.3f;
        }
        else if (screenWidth >= 1920) // Full HD
        {
            baseScale = 1f;
        }
        else if (screenWidth >= 1366) // HD
        {
            baseScale = 0.9f;
        }
        else // Маленькие экраны
        {
            baseScale = 0.8f;
        }
        
        Debug.Log($"Screen: {screenWidth}px → Base Scale: {baseScale}x");
    }

    void BuildUI()
    {
        var root = uiDocument.rootVisualElement;
        root.Clear();

        if (customStyleSheet != null)
        {
            root.styleSheets.Add(customStyleSheet);
        }

        // === Главный контейнер ===
        mainContainer = new VisualElement();
        mainContainer.name = "MainContainer";
        mainContainer.style.position = Position.Absolute;
        mainContainer.style.right = 0;
        mainContainer.style.top = 0;
        mainContainer.style.bottom = 0;
        
        mainContainer.style.width = new Length(37.5f, LengthUnit.Percent);
        mainContainer.style.minWidth = 400;
        
        mainContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.19f);
        mainContainer.style.paddingTop = 10;
        mainContainer.style.paddingBottom = 10;
        mainContainer.style.paddingLeft = 10;
        mainContainer.style.paddingRight = 10;
        mainContainer.style.flexDirection = FlexDirection.Column;
        
        root.Add(mainContainer);

        // === Панель масштабирования (вверху) ===
        var scalePanel = new VisualElement();
        scalePanel.name = "ScalePanel";
        scalePanel.style.flexDirection = FlexDirection.Row;
        scalePanel.style.justifyContent = Justify.FlexEnd;
        scalePanel.style.height = 30;
        scalePanel.style.marginBottom = 5;
        mainContainer.Add(scalePanel);

        // Кнопка уменьшения масштаба
        var zoomOutButton = new Button(() => ChangeScale(-SCALE_STEP));
        zoomOutButton.text = "−";
        zoomOutButton.style.width = 30;
        zoomOutButton.style.height = 25;
        zoomOutButton.style.fontSize = 18;
        zoomOutButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        zoomOutButton.style.color = Color.white;
        zoomOutButton.style.borderTopLeftRadius = 4;
        zoomOutButton.style.borderBottomLeftRadius = 4;
        scalePanel.Add(zoomOutButton);

        // Метка масштаба
        var scaleLabel = new Label($"{Mathf.RoundToInt(currentScale * 100)}%");
        scaleLabel.name = "ScaleLabel";
        scaleLabel.style.width = 50;
        scaleLabel.style.height = 25;
        scaleLabel.style.fontSize = 12;
        scaleLabel.style.color = Color.white;
        scaleLabel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        scaleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        scalePanel.Add(scaleLabel);

        // Кнопка увеличения масштаба
        var zoomInButton = new Button(() => ChangeScale(SCALE_STEP));
        zoomInButton.text = "+";
        zoomInButton.style.width = 30;
        zoomInButton.style.height = 25;
        zoomInButton.style.fontSize = 18;
        zoomInButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        zoomInButton.style.color = Color.white;
        zoomInButton.style.borderTopRightRadius = 4;
        zoomInButton.style.borderBottomRightRadius = 4;
        scalePanel.Add(zoomInButton);

        // Кнопка сброса масштаба
        var resetScaleButton = new Button(() => ResetScale());
        resetScaleButton.text = "100%";
        resetScaleButton.style.width = 45;
        resetScaleButton.style.height = 25;
        resetScaleButton.style.fontSize = 11;
        resetScaleButton.style.marginLeft = 5;
        resetScaleButton.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        resetScaleButton.style.color = new Color(0.8f, 0.8f, 0.8f);
        resetScaleButton.style.borderTopLeftRadius = 4;
        resetScaleButton.style.borderTopRightRadius = 4;
        resetScaleButton.style.borderBottomLeftRadius = 4;
        resetScaleButton.style.borderBottomRightRadius = 4;
        scalePanel.Add(resetScaleButton);

        // === Панель редактора ===
        var editorPanel = new VisualElement();
        editorPanel.name = "EditorPanel";
        editorPanel.style.flexGrow = 1;
        editorPanel.style.flexShrink = 1;
        editorPanel.style.minHeight = 300;
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

        // === TextField ===
        codeInput = new TextField();
        codeInput.name = "CodeInput";
        codeInput.multiline = true;
        codeInput.value = "// Твой код здесь\nint distanceRight = 5;\nint distanceUp = 5;\nPlayer.moveRight(distanceRight);\nPlayer.moveUp(distanceUp);";
        codeInput.style.color = new Color(0.95f, 0.95f, 0.95f);
        codeInput.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f);
        codeInput.style.minHeight = 870;
        
        codeInput.RegisterValueChangedCallback(OnCodeChanged);
        codeInput.RegisterCallback<FocusInEvent>(evt => 
        {
            var inputElement = codeInput.Q("unity-text-input");
            if (inputElement != null)
            {
                inputElement.style.unityBackgroundImageTintColor = Color.white;
            }
        });
        codeInput.RegisterCallback<KeyDownEvent>(evt => 
        {
            codeInput.schedule.Execute(() => ScrollToCaretPosition()).ExecuteLater(10);
        });
        
        codeScrollView.Add(codeInput);

        codeScrollView.verticalScroller.valueChanged += SyncScroll;

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
        buttonPanel = new VisualElement();
        buttonPanel.name = "ButtonPanel";
        buttonPanel.style.flexDirection = FlexDirection.Row;
        buttonPanel.style.justifyContent = Justify.SpaceAround;
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
        resetButton.style.borderTopLeftRadius = 4;
        resetButton.style.borderTopRightRadius = 4;
        resetButton.style.borderBottomLeftRadius = 4;
        resetButton.style.borderBottomRightRadius = 4;
        buttonPanel.Add(resetButton);

        // === Консоль (⭐ увеличена высота) ===
        consolePanel = new VisualElement();
        consolePanel.name = "ConsolePanel";
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
        consoleLabel.name = "ConsoleLabel";
        consoleLabel.style.color = new Color(0.67f, 0.67f, 0.67f);
        consoleLabel.style.paddingTop = 5;
        consoleLabel.style.paddingLeft = 10;
        consolePanel.Add(consoleLabel);

        var consoleScrollView = new ScrollView(ScrollViewMode.Vertical);
        consoleScrollView.style.flexGrow = 1;
        consolePanel.Add(consoleScrollView);

        consoleOutput = new Label("Console ready.");
        consoleOutput.name = "ConsoleOutput";
        consoleOutput.style.color = new Color(0.8f, 0.8f, 0.8f);
        consoleOutput.style.paddingTop = 5;
        consoleOutput.style.paddingLeft = 10;
        consoleOutput.style.whiteSpace = WhiteSpace.Pre;
        consoleScrollView.Add(consoleOutput);

        // ⭐ Применяем начальный масштаб
        ApplyScale();
        
        UpdateLineNumbers(codeInput.value);
    }

    void Update()
    {
        // ⭐ Горячие клавиши масштабирования
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                ChangeScale(SCALE_STEP);
            }
            else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                ChangeScale(-SCALE_STEP);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
            {
                ResetScale();
            }
        }
    }

    // ⭐ Изменение масштаба
    void ChangeScale(float delta)
    {
        currentScale = Mathf.Clamp(currentScale + delta, MIN_SCALE, MAX_SCALE);
        ApplyScale();
        
        Debug.Log($"Scale changed: {Mathf.RoundToInt(currentScale * 100)}%");
    }

    // ⭐ Сброс масштаба к базовому
    void ResetScale()
    {
        currentScale = baseScale;
        ApplyScale();
        
        Debug.Log($"Scale reset to base: {Mathf.RoundToInt(currentScale * 100)}%");
    }

    // ⭐ Применение масштаба ко всем элементам
    void ApplyScale()
    {
        var root = uiDocument.rootVisualElement;
        
        // Обновляем метку масштаба
        var scaleLabel = root.Q<Label>("ScaleLabel");
        if (scaleLabel != null)
        {
            scaleLabel.text = $"{Mathf.RoundToInt(currentScale * 100)}%";
        }
        
        // Шрифты редактора кода
        if (codeInput != null)
        {
            codeInput.style.fontSize = Mathf.RoundToInt(14 * currentScale);
        }
        
        if (lineNumbers != null)
        {
            lineNumbers.style.fontSize = Mathf.RoundToInt(14 * currentScale);
        }
        
        // Шрифты кнопок
        if (runButton != null)
        {
            runButton.style.fontSize = Mathf.RoundToInt(16 * currentScale);
        }
        
        if (resetButton != null)
        {
            resetButton.style.fontSize = Mathf.RoundToInt(16 * currentScale);
        }
        
        // Высота кнопок
        if (buttonPanel != null)
        {
            buttonPanel.style.height = Mathf.RoundToInt(40 * currentScale);
        }
        
        // Консоль (⭐ увеличена высота + адаптивная)
        if (consolePanel != null)
        {
            consolePanel.style.height = Mathf.RoundToInt(180 * currentScale); // Было 120, стало 180
        }
        
        var consoleLabel = root.Q<Label>("ConsoleLabel");
        if (consoleLabel != null)
        {
            consoleLabel.style.fontSize = Mathf.RoundToInt(12 * currentScale);
        }
        
        if (consoleOutput != null)
        {
            consoleOutput.style.fontSize = Mathf.RoundToInt(12 * currentScale);
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

        float lineHeight = 20f * currentScale; // ⭐ Учитываем масштаб
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
            codeInput.value = "// Твой код здесь\n";
        }
        
        AddConsoleLog("⟲ Сброс");
        
        var levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.OnResetLevel();
        }
    }

    public void AddConsoleLog(string message, bool isError = false)
    {
        if (consoleOutput != null)
        {
            string color = isError ? "#ff6b6b" : "#ffffff";
            consoleOutput.text += $"<color={color}>{message}</color>\n";
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
}

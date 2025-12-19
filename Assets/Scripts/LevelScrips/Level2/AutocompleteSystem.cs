using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class AutocompleteSystem
{
    private VisualElement suggestionBox;
    private ListView suggestionList;
    private TextField targetField;
    private List<string> allSuggestions;
    private List<string> filteredSuggestions;
    
    private static readonly string[] SUGGESTIONS = {
        "moveRight()", "moveLeft()", "moveUp()", "moveDown()",
        "jump()", "wait()", "repeat()", "collect()", "interact()",
        "Player.moveRight()", "Player.moveLeft()", "Player.jump()",
        "int", "float", "String", "boolean",
        "if", "else", "for", "while", "return"
    };

    public AutocompleteSystem(TextField field, VisualElement container)
    {
        targetField = field;
        allSuggestions = SUGGESTIONS.ToList();
        filteredSuggestions = new List<string>();
        
        CreateSuggestionBox(container);
        field.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    void CreateSuggestionBox(VisualElement container)
    {
        suggestionBox = new VisualElement();
        suggestionBox.name = "AutocompleteBox";
        suggestionBox.style.position = Position.Absolute;
        suggestionBox.style.width = 250;
        suggestionBox.style.maxHeight = 200;
        suggestionBox.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        suggestionBox.style.borderTopWidth = 1;
        suggestionBox.style.borderBottomWidth = 1;
        suggestionBox.style.borderLeftWidth = 1;
        suggestionBox.style.borderRightWidth = 1;
        suggestionBox.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
        suggestionBox.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
        suggestionBox.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
        suggestionBox.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        suggestionBox.style.display = DisplayStyle.None;
        
        suggestionList = new ListView();
        suggestionList.style.flexGrow = 1;
        suggestionList.itemsSource = filteredSuggestions;
        suggestionList.makeItem = () => new Label();
        suggestionList.bindItem = (element, i) => 
        {
            var label = element as Label;
            label.text = filteredSuggestions[i];
            label.style.color = Color.white;
            label.style.paddingLeft = 5;
            label.style.paddingTop = 3;
            label.style.paddingBottom = 3;
        };
        suggestionList.selectionChanged += OnSuggestionSelected;
        
        suggestionBox.Add(suggestionList);
        container.Add(suggestionBox);
    }

    void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.ctrlKey && evt.keyCode == KeyCode.Space)
        {
            ShowSuggestions();
            evt.StopPropagation();
            evt.PreventDefault();
        }
        else if (evt.keyCode == KeyCode.Escape)
        {
            HideSuggestions();
        }
    }

    void ShowSuggestions()
    {
        string currentWord = GetCurrentWord();
        
        filteredSuggestions.Clear();
        if (string.IsNullOrEmpty(currentWord))
        {
            filteredSuggestions.AddRange(allSuggestions);
        }
        else
        {
            filteredSuggestions.AddRange(
                allSuggestions.Where(s => s.StartsWith(currentWord, System.StringComparison.OrdinalIgnoreCase))
            );
        }
        
        if (filteredSuggestions.Count == 0)
        {
            HideSuggestions();
            return;
        }
        
        suggestionList.itemsSource = filteredSuggestions;
        suggestionList.Rebuild();
        suggestionBox.style.display = DisplayStyle.Flex;
        suggestionBox.style.top = 100;
        suggestionBox.style.left = 10;
    }

    void HideSuggestions()
    {
        suggestionBox.style.display = DisplayStyle.None;
    }

    void OnSuggestionSelected(IEnumerable<object> selected)
    {
        var item = selected.FirstOrDefault() as string;
        if (item != null)
        {
            InsertSuggestion(item);
            HideSuggestions();
        }
    }

    string GetCurrentWord()
    {
        int cursorPos = targetField.cursorIndex;
        string text = targetField.value;
        
        if (cursorPos == 0) return "";
        
        int start = cursorPos - 1;
        while (start >= 0 && char.IsLetterOrDigit(text[start]))
        {
            start--;
        }
        
        return text.Substring(start + 1, cursorPos - start - 1);
    }

    void InsertSuggestion(string suggestion)
    {
        int cursorPos = targetField.cursorIndex;
        string text = targetField.value;
        
        int start = cursorPos - 1;
        while (start >= 0 && char.IsLetterOrDigit(text[start]))
        {
            start--;
        }
        start++;
        
        string before = text.Substring(0, start);
        string after = text.Substring(cursorPos);
        targetField.value = before + suggestion + after;
        targetField.SelectRange(start + suggestion.Length, start + suggestion.Length);
    }
}

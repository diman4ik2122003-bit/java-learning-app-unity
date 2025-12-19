using System.Text.RegularExpressions;

public sealed class CodeSyntaxHighlighter
{
    // Цвета подсветки (VS Code Dark+ theme)
    private const string COLOR_KEYWORD = "#569CD6";      // Синий
    private const string COLOR_TYPE = "#4EC9B0";         // Бирюзовый
    private const string COLOR_STRING = "#CE9178";       // Оранжевый
    private const string COLOR_NUMBER = "#B5CEA8";       // Зелёный
    private const string COLOR_COMMENT = "#6A9955";      // Зелёный
    private const string COLOR_FUNCTION = "#DCDCAA";     // Жёлтый
    
    // Java/C# ключевые слова
    private static readonly string[] KEYWORDS = {
        "if", "else", "for", "while", "do", "switch", "case", "break", "continue",
        "return", "void", "public", "private", "protected", "static", "final",
        "class", "interface", "extends", "implements", "new", "this", "super",
        "true", "false", "null", "try", "catch", "finally", "throw", "throws"
    };
    
    // Типы данных
    private static readonly string[] TYPES = {
        "int", "float", "double", "boolean", "char", "byte", "short", "long",
        "String", "var", "const", "let"
    };
    
    // Кастомные методы игры
    private static readonly string[] GAME_FUNCTIONS = {
        "moveRight", "moveLeft", "moveUp", "moveDown", "jump", "wait", 
        "repeat", "collect", "interact", "Player"
    };

    public static string ApplyHighlighting(string code)
    {
        if (string.IsNullOrEmpty(code))
            return code;
        
        // Экранируем HTML символы
        code = code.Replace("<", "&lt;").Replace(">", "&gt;");
        
        // 1. Комментарии
        code = Regex.Replace(code, @"(//.*?)(\n|$)", 
            m => $"<color={COLOR_COMMENT}>{m.Groups[1].Value}</color>{m.Groups[2].Value}");
        code = Regex.Replace(code, @"(/\*.*?\*/)", 
            m => $"<color={COLOR_COMMENT}>{m.Value}</color>", RegexOptions.Singleline);
        
        // 2. Строки
        code = Regex.Replace(code, @""".*?""", 
            m => $"<color={COLOR_STRING}>{m.Value}</color>");
        code = Regex.Replace(code, @"'.*?'", 
            m => $"<color={COLOR_STRING}>{m.Value}</color>");
        
        // 3. Числа
        code = Regex.Replace(code, @"\b(\d+\.?\d*)\b", 
            m => $"<color={COLOR_NUMBER}>{m.Value}</color>");
        
        // 4. Кастомные функции игры
        foreach (var func in GAME_FUNCTIONS)
        {
            code = Regex.Replace(code, $@"\b({func})\b", 
                m => $"<color={COLOR_FUNCTION}><b>{m.Value}</b></color>");
        }
        
        // 5. Типы данных
        foreach (var type in TYPES)
        {
            code = Regex.Replace(code, $@"\b({type})\b", 
                m => $"<color={COLOR_TYPE}>{m.Value}</color>");
        }
        
        // 6. Ключевые слова
        foreach (var keyword in KEYWORDS)
        {
            code = Regex.Replace(code, $@"\b({keyword})\b", 
                m => $"<color={COLOR_KEYWORD}><b>{m.Value}</b></color>");
        }
        
        // 7. Функции (имяФункции())
        code = Regex.Replace(code, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", 
            m => $"<color={COLOR_FUNCTION}>{m.Groups[1].Value}</color>(");
        
        return code;
    }
}

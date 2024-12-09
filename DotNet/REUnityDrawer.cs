using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

public static class REUnityDrawer
{
    public class DebugLineConfigurator
    {
        readonly LineRenderer lineRenderer;
        readonly DebugTextConfigurator textConfig;

        public DebugLineConfigurator(LineRenderer lineRenderer, DebugTextConfigurator textConfig)
        {
            this.lineRenderer = lineRenderer;
            this.textConfig = textConfig;
        }

        public DebugLineConfigurator WithColor(Color color)
        {
            lineRenderer.material = REUnityDrawer.GetOrCreateMaterial(color);
            return this;
        }

        public DebugLineConfigurator WithWidth(float width)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            return this;
        }

        public DebugTextConfigurator Text => textConfig;
    }

    public class DebugTextConfigurator
    {
        readonly TextMesh textMesh;

        public DebugTextConfigurator(TextMesh textMesh)
        {
            this.textMesh = textMesh;
        }

        public DebugTextConfigurator WithTextColor(Color color)
        {
            textMesh.color = color;
            return this;
        }

        public DebugTextConfigurator WithOffset(Vector3 offset)
        {
            textMesh.transform.localPosition += offset;
            return this;
        }

        public DebugTextConfigurator WithFontSize(int fontSize)
        {
            textMesh.fontSize = fontSize;
            return this;
        }
    }

    public class DebugText2D
    {
        public string Text { get; set; }
        public Color Color { get; set; }
        public int FontSize { get; set; }
        public Vector2 Position { get; set; }

        public DebugText2D()
        {
            this.FontSize = 12;
            this.Color = Color.white;
        }

        public DebugText2D WithFontSize(int size)
        {
            this.FontSize = size;
            return this;
        }

        public DebugText2D WithTextColor(Color color)
        {
            this.Color = color;
            return this;
        }
    }

    private class TextRenderer2D : MonoBehaviour
    {
        public static GameObject Instance => instance;

        static GameObject instance;
        static Dictionary<string, DebugText2D> debugTexts = new Dictionary<string, DebugText2D>();

        static TextRenderer2D()
        {
            instance = new GameObject("REUnityDrawer.TextRenderer2D");
            instance.AddComponent<TextRenderer2D>();
            UnityObject.DontDestroyOnLoad(instance);
        }

        public static DebugText2D GetOrCreateGUIText(string label)
        {
            if (!debugTexts.TryGetValue(label, out DebugText2D debugText))
            {
                debugText = new DebugText2D();
                debugTexts[label] = debugText;
            }
            return debugText;
        }

        public static void SetActive(bool isActive)
        {
            if (instance != null)
            {
                instance.SetActive(isActive);
            }
        }

        private void OnGUI()
        {
            foreach (var guiText in debugTexts.Values)
            {
                GUI.color = guiText.Color;
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = guiText.FontSize,
                    normal = new GUIStyleState { textColor = guiText.Color }
                };

                var position = guiText.Position;
                var textSize = style.CalcSize(new GUIContent(guiText.Text));
                var xPos = position.x < 0 ? Screen.width + position.x - textSize.x : position.x;
                var yPos = position.y < 0 ? Screen.height + position.y - textSize.y : position.y;
                GUI.Label(new Rect(xPos, yPos, textSize.x, textSize.y), guiText.Text, style);
            }
        }

        public static void Clear()
        {
            debugTexts.Clear();
        }
    }

    public static bool IsActive => isActive;

    static bool isActive;
    static Shader unlitColorShader;
    static Transform defaultOrigin;
    static readonly List<LineRenderer> attachedLineRenderers = new List<LineRenderer>();
    static readonly Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();
    static readonly Dictionary<string, TextMesh> textRenderers = new Dictionary<string, TextMesh>();
    static readonly Dictionary<string, LineRenderer> lineRenderers = new Dictionary<string, LineRenderer>();

    public static Material GetOrCreateMaterial(Color color)
    {
        if (materialCache.TryGetValue(color, out var material))
        {
            return material;
        }

        var shader = GetUnlitColorShader();
        if (shader == null)
        {
            return null;
        }

        material = new Material(shader)
        {
            color = color
        };
        materialCache[color] = material;
        return material;
    }

    public static void SetDefaultOrigin(Transform origin)
    {
        REUnityDrawer.defaultOrigin = origin;
    }

    public static DebugText2D DrawText(float x, float y, string text, string label)
    {
        return DrawText(new Vector2(x, y), text, label);
    }

    public static DebugText2D DrawText(Vector2 point, string text, string label)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(label))
        {
            return null;
        }

        var guiText = TextRenderer2D.GetOrCreateGUIText(label);
        guiText.Position = point;
        guiText.Text = text;
        return guiText;
    }

    public static DebugTextConfigurator DrawText(Vector3 point, string text, string label)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(label))
        {
            return null;
        }

        var textMesh = GetOrCreateTextRenderer(label, null);
        UpdateTextMesh(textMesh, text, point);

        return new DebugTextConfigurator(textMesh);
    }

    private static TextMesh GetOrCreateTextRenderer(string label, GameObject parent)
    {
        var key = GetLabelKey(label, parent);
        if (textRenderers.TryGetValue(key, out var textMesh))
        {
            return textMesh;
        }

        var textObject = new GameObject($"DebugText - {label}");
        textMesh = textObject.AddComponent<TextMesh>();
        InitializeTextMesh(textMesh);

        if (parent != null)
        {
            textObject.transform.SetParent(parent.transform);
        }

        textRenderers[key] = textMesh;
        return textMesh;
    }    

    public static DebugTextConfigurator DrawObjectText(GameObject gameObject, string text = null, string label = null)
    {
        if (gameObject == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(label))
        {
            label = gameObject.GetInstanceID().ToString();
        }

        if (string.IsNullOrEmpty(text))
        {
            text = gameObject.name;
        }

        var textMesh = GetOrCreateTextRenderer(label, gameObject);
        UpdateTextMesh(textMesh, text, Vector3.zero);

        return new DebugTextConfigurator(textMesh);
    }

    public static DebugLineConfigurator DrawLine(Vector3 point, string text, string label, Vector3? origin = null)
    {
        if (string.IsNullOrEmpty(label))
        {
            return null;
        }

        var lineRenderer = GetOrCreateLineRenderer(label, null);
        UpdateLineRenderer(lineRenderer, origin, point);

        var textConfig = DrawText(point, text, label);
        return new DebugLineConfigurator(lineRenderer, textConfig);
    }

    public static DebugLineConfigurator DrawObjectLine(GameObject gameObject, string text = null, string label = null, Vector3? origin = null)
    {
        if (gameObject == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(label))
        {
            return SetupObjectLine(gameObject, text, origin);
        }

        var lineRenderer = GetOrCreateLineRenderer(label, gameObject);
        UpdateLineRenderer(lineRenderer, origin, gameObject.transform.position);

        var textConfig = DrawObjectText(gameObject, text, label);
        return new DebugLineConfigurator(lineRenderer, textConfig);
    }

    private static DebugLineConfigurator SetupObjectLine(GameObject gameObject, string text, Vector3? origin)
    {
        var lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            attachedLineRenderers.Add(lineRenderer);
            InitializeLineRenderer(lineRenderer);
        }
        UpdateLineRenderer(lineRenderer, origin, gameObject.transform.position);

        var textConfig = DrawObjectText(gameObject, text, null);
        return new DebugLineConfigurator(lineRenderer, textConfig);
    }

    private static LineRenderer GetOrCreateLineRenderer(string label, GameObject parent)
    {
        var key = GetLabelKey(label, parent);
        if (lineRenderers.TryGetValue(key, out var lineRenderer))
        {
            return lineRenderer;
        }

        var lineObject = new GameObject($"DebugLine - {label}");
        lineRenderer = lineObject.AddComponent<LineRenderer>();
        InitializeLineRenderer(lineRenderer);

        if (parent != null)
        {
            lineObject.transform.SetParent(parent.transform);
        }

        lineRenderers[key] = lineRenderer;
        return lineRenderer;
    }

    private static void InitializeTextMesh(TextMesh textMesh)
    {
        textMesh.fontSize = 12;
        textMesh.characterSize = 0.25F;
        textMesh.anchor = TextAnchor.MiddleCenter;
    }

    private static void UpdateTextMesh(TextMesh textMesh, string text, Vector3 position)
    {
        var textTransform = textMesh.transform;
        textMesh.text = text;
        textTransform.localPosition = position;
        AdjustTextToCamera(textTransform);
    }

    private static void AdjustTextToCamera(Transform textTransform)
    {
        var camera = Camera.main;
        if (camera != null)
        {
            var direction = textTransform.position - camera.transform.position;
            direction.y = 0; //Zero out the Y component (keep it upright)
            textTransform.rotation = Quaternion.LookRotation(direction);

            const float distanceScaleFactor = 0.1F;
            var distance = direction.magnitude;
            textTransform.localScale = Vector3.one * (distance * distanceScaleFactor);
        }
    }

    private static void InitializeLineRenderer(LineRenderer lineRenderer)
    {
        lineRenderer.endWidth = 0.05F;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05F;
        lineRenderer.material = REUnityDrawer.GetOrCreateMaterial(Color.red);
    }

    private static Shader GetUnlitColorShader()
    {
        if (unlitColorShader == null)
        {
            unlitColorShader = Shader.Find("Unlit/Color");
            if (unlitColorShader == null)
            {
                RELogger.Log("[Warning] Shader 'Unlit/Color' was not found!");
                unlitColorShader = Shader.Find("Standard"); //Fallback to "Standard" shared
            }
        }
        return unlitColorShader;
    }

    private static void UpdateLineRenderer(LineRenderer lineRenderer, Vector3? origin, Vector3 targetPoint)
    {
        var startPoint = origin ?? GetDefaultOrigin();
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, targetPoint);
    }

    private static string GetLabelKey(string label, GameObject parent)
    {
        return parent == null ? label : $"{parent.GetInstanceID()}_{label}";
    }

    private static Vector3 GetDefaultOrigin()
    {
        if (defaultOrigin != null)
        {
            return defaultOrigin.position;
        }

        var camera = Camera.main;
        if (camera != null)
        {
            return camera.transform.position;
        }
        return Vector3.zero;
    }

    public static void ToggleActive()
    {
        SetActive(!isActive);
    }

    public static void SetActive(bool isActive)
    {
        if (REUnityDrawer.isActive == isActive)
        {
            return;
        }

        foreach (var lineRenderer in attachedLineRenderers)
        {
            lineRenderer.enabled = isActive;
        }
        foreach (var lineRenderer in lineRenderers.Values)
        {
            lineRenderer.enabled = isActive;
        }
        foreach (var textRenderer in textRenderers.Values)
        {
            textRenderer.gameObject.SetActive(isActive);
        }
        TextRenderer2D.SetActive(isActive);
        REUnityDrawer.isActive = isActive;
    }

    public static void Clear()
    {
        ClearText2D();
        ClearAttachedRenderers();
        ClearRenderers(lineRenderers, "DebugLine");
        ClearRenderers(textRenderers, "DebugText");
    }

    public static void ClearText2D()
    {
        TextRenderer2D.Clear();
    }

    private static void ClearAttachedRenderers()
    {
        foreach (var renderer in attachedLineRenderers)
        {
            UnityObject.Destroy(renderer);
        }
        attachedLineRenderers.Clear();
    }

    private static void ClearRenderers<T>(Dictionary<string, T> renderers, string prefix) where T : Component
    {
        foreach (var renderer in renderers.Values)
        {
            var gameObject = renderer.gameObject;
            if (gameObject.name.StartsWith(prefix))
            {
                UnityObject.Destroy(gameObject);
                continue;
            }
            UnityObject.Destroy(renderer);
        }
        renderers.Clear();
    }

    public static void Unload()
    {
        Clear();
        ClearMaterials();
    }

    private static void ClearMaterials()
    {
        foreach (var material in materialCache.Values)
        {
            UnityObject.Destroy(material);
        }
        materialCache.Clear();
    }
}

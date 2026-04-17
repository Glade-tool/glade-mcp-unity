using System;
using UnityEditor;
using UnityEngine;

namespace GladeAgenticAI.Bridge
{
    /// <summary>
    /// Editor window for monitoring GladeKit MCP bridge status.
    /// Window > GladeKit MCP
    ///
    /// The MCP server is a stdio process launched by the AI client (Cursor, Claude Code, etc.),
    /// not by Unity. This window monitors the bridge that the MCP server connects to.
    /// </summary>
    public class GladeKitMCPWindow : EditorWindow
    {
        private GUIStyle _headerStyle;
        private GUIStyle _helpBoxStyle;
        private bool _stylesInitialized;
        private bool _showSetup;
        private Vector2 _scrollPos;

        [MenuItem("Window/GladeKit MCP")]
        public static void ShowWindow()
        {
            var window = GetWindow<GladeKitMCPWindow>("GladeKit MCP");
            window.minSize = new Vector2(360, 300);
        }

        private void OnEnable()
        {
            EditorApplication.update += RepaintPeriodic;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintPeriodic;
        }

        private float _lastRepaintTime;
        private void RepaintPeriodic()
        {
            if (EditorApplication.timeSinceStartup - _lastRepaintTime > 2.0)
            {
                _lastRepaintTime = (float)EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("GladeKit MCP", _headerStyle);
            EditorGUILayout.Space(4);

            DrawBridgeStatus();
            EditorGUILayout.Space(8);
            DrawClientStatus();
            EditorGUILayout.Space(8);
            DrawToolStats();
            EditorGUILayout.Space(12);
            DrawSetupHelp();

            EditorGUILayout.EndScrollView();
        }

        private void DrawBridgeStatus()
        {
            EditorGUILayout.LabelField("Unity Bridge", EditorStyles.boldLabel);

            bool running = UnityBridgeServer.IsRunning;

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawStatusDot(running);
                EditorGUILayout.LabelField(
                    running ? "Running on localhost:8765" : "Not running",
                    GUILayout.Width(220));

                if (running)
                {
                    if (GUILayout.Button("Stop", GUILayout.Width(50)))
                        UnityBridgeServer.StopServer();
                }
                else
                {
                    if (GUILayout.Button("Start", GUILayout.Width(50)))
                        UnityBridgeServer.StartServer();
                }
            }
        }

        private void DrawClientStatus()
        {
            EditorGUILayout.LabelField("AI Client", EditorStyles.boldLabel);

            bool connected = UnityBridgeServer.IsConnected();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawStatusDot(connected);
                if (connected)
                {
                    EditorGUILayout.LabelField("Connected");
                }
                else if (UnityBridgeServer.LastRequestTime != DateTime.MinValue)
                {
                    double ago = (DateTime.Now - UnityBridgeServer.LastRequestTime).TotalSeconds;
                    if (ago < 60)
                        EditorGUILayout.LabelField($"Last seen {ago:F0}s ago");
                    else if (ago < 3600)
                        EditorGUILayout.LabelField($"Last seen {ago / 60:F0}m ago");
                    else
                        EditorGUILayout.LabelField("Disconnected");
                }
                else
                {
                    EditorGUILayout.LabelField("No client connected yet");
                }
            }
        }

        private void DrawToolStats()
        {
            EditorGUILayout.LabelField("Session Stats", EditorStyles.boldLabel);

            int callCount = UnityBridgeServer.ToolCallCount;
            string lastTool = UnityBridgeServer.LastToolCalled ?? "\u2014";

            EditorGUILayout.LabelField($"Tool calls:  {callCount}");
            EditorGUILayout.LabelField($"Last tool:   {lastTool}");
        }

        private void DrawSetupHelp()
        {
            _showSetup = EditorGUILayout.Foldout(_showSetup, "Setup Instructions", true);
            if (!_showSetup) return;

            EditorGUILayout.HelpBox(
                "The MCP server is launched by your AI client, not Unity.\n\n" +
                "Add this to your client's MCP config:\n\n" +
                "{\n" +
                "  \"mcpServers\": {\n" +
                "    \"gladekit-unity\": {\n" +
                "      \"command\": \"uvx\",\n" +
                "      \"args\": [\"gladekit-mcp\"]\n" +
                "    }\n" +
                "  }\n" +
                "}\n\n" +
                "Requires uv: https://docs.astral.sh/uv\n\n" +
                "Config locations:\n" +
                "  Unity AI Gateway: Edit > Project Settings > AI > MCP Servers\n" +
                "  Cursor: Settings > MCP > Add server\n" +
                "  Claude Desktop: claude_desktop_config.json\n" +
                "  Windsurf: ~/.codeium/windsurf/mcp_config.json\n" +
                "  VS Code: .vscode/mcp.json\n" +
                "  Claude Code: .mcp.json (auto-detected)",
                MessageType.Info);

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy MCP Config"))
                {
                    string config =
                        "{\n" +
                        "  \"mcpServers\": {\n" +
                        "    \"gladekit-unity\": {\n" +
                        "      \"command\": \"uvx\",\n" +
                        "      \"args\": [\"gladekit-mcp\"]\n" +
                        "    }\n" +
                        "  }\n" +
                        "}";
                    EditorGUIUtility.systemCopyBuffer = config;
                    Debug.Log("[GladeKit MCP] Config copied to clipboard.");
                }

                if (GUILayout.Button("Copy Unity AI Gateway Config"))
                {
                    string config =
                        "{\n" +
                        "  \"enabled\": true,\n" +
                        "  \"path\": \"\",\n" +
                        "  \"mcpServers\": {\n" +
                        "    \"gladekit-unity\": {\n" +
                        "      \"type\": \"stdio\",\n" +
                        "      \"command\": \"uvx\",\n" +
                        "      \"args\": [\"gladekit-mcp\"]\n" +
                        "    }\n" +
                        "  }\n" +
                        "}";
                    EditorGUIUtility.systemCopyBuffer = config;
                    Debug.Log("[GladeKit MCP] Unity AI Gateway config copied to clipboard. Paste into Edit > Project Settings > AI > MCP Servers config file.");
                }
            }
        }

        private void DrawStatusDot(bool active)
        {
            var rect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
            rect.y += 2;
            EditorGUI.DrawRect(new Rect(rect.x + 2, rect.y + 2, 8, 8),
                active ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.5f, 0.5f, 0.5f));
        }
    }
}

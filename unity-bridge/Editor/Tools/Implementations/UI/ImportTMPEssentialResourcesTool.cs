using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    /// <summary>
    /// Tool to import TextMeshPro Essential Resources.
    /// Opens the Unity import dialog - user must click "Import" button to complete the import.
    /// This is a one-time setup. Once imported, TMP will be available for all UI actions.
    /// </summary>
    public class ImportTMPEssentialResourcesTool : ITool
    {
        public string Name => "import_tmp_essential_resources";

        public string Execute(Dictionary<string, object> args)
        {
            // CRITICAL: Check if Essential Resources folder exists in Assets/TextMesh Pro/Resources
            // This is where Unity places Essential Resources when you import them
            string resourcesPath = System.IO.Path.Combine(Application.dataPath, "TextMesh Pro", "Resources");
            bool essentialResourcesImported = System.IO.Directory.Exists(resourcesPath);
            
            UnityEngine.Debug.LogError($"[GladeAI] ===== TMP IMPORT CHECK - START =====");
            UnityEngine.Debug.LogError($"[GladeAI] Application.dataPath: {Application.dataPath}");
            UnityEngine.Debug.LogError($"[GladeAI] Checking Resources Path: {resourcesPath}");
            UnityEngine.Debug.LogError($"[GladeAI] Folder Exists: {essentialResourcesImported}");
            
            // FORCE CHECK: Verify the folder actually exists by checking multiple ways
            if (essentialResourcesImported)
            {
                // Double-check: Look for Fonts & Materials subfolder
                string fontsPath = System.IO.Path.Combine(resourcesPath, "Fonts & Materials");
                bool fontsExist = System.IO.Directory.Exists(fontsPath);
                
                // Triple-check: Look for actual files in Resources
                bool hasFiles = false;
                try
                {
                    var files = System.IO.Directory.GetFiles(resourcesPath, "*", System.IO.SearchOption.TopDirectoryOnly);
                    hasFiles = files.Length > 0;
                }
                catch { }
                
                UnityEngine.Debug.LogError($"[GladeAI] Fonts folder exists: {fontsExist}");
                UnityEngine.Debug.LogError($"[GladeAI] Resources has files: {hasFiles}");
                
                // ONLY return "already imported" if ALL checks pass
                if (fontsExist && hasFiles)
                {
                    UnityEngine.Debug.LogError("[GladeAI] TMP Essential Resources CONFIRMED - already imported");
                    UnityEngine.Debug.LogError($"[GladeAI] Verified: Resources folder exists at {resourcesPath}");
                    UnityEngine.Debug.LogError($"[GladeAI] Verified: Fonts folder exists at {fontsPath}");
                    UnityEngine.Debug.LogError($"[GladeAI] Verified: Resources folder contains files");
                    var extras = new Dictionary<string, object>
                    {
                        { "imported", true },
                        { "status", "ready" },
                        { "message", "✅ VERIFIED: TextMeshPro Essential Resources are already imported in Assets/TextMesh Pro/Resources. TMP is ready to use." }
                    };
                    return ToolUtils.CreateSuccessResponse("✅ VERIFIED: TextMeshPro Essential Resources are already imported in Assets/TextMesh Pro/Resources. TMP is ready to use.", extras);
                }
                else
                {
                    UnityEngine.Debug.LogError($"[GladeAI] Resources folder exists but incomplete (fonts={fontsExist}, files={hasFiles}) - MUST IMPORT");
                    essentialResourcesImported = false; // Force import
                }
            }
            
            // If we get here, Essential Resources are NOT imported - open import dialog
            UnityEngine.Debug.LogError($"[GladeAI] ===== TMP NOT FOUND - OPENING IMPORT DIALOG =====");
            UnityEngine.Debug.LogError($"[GladeAI] Resources folder DOES NOT EXIST at: {resourcesPath}");
            UnityEngine.Debug.LogError($"[GladeAI] Opening import dialog for user to click Import button");
            
            // Open the import dialog - user must click "Import" button
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            
            var importExtras = new Dictionary<string, object>
            {
                { "imported", false },
                { "status", "requires_user_action" },
                { "message", "The TextMeshPro Essential Resources import dialog has been opened in Unity Editor. Please click the 'Import' button in the dialog to complete the import. Once imported, you can retry your UI action request." }
            };
            
            return ToolUtils.CreateSuccessResponse(
                "📦 TextMeshPro Essential Resources import dialog opened. " +
                "Please go to Unity Editor and click the 'Import' button in the dialog that appeared. " +
                "After clicking Import, wait for Unity to finish importing, then retry your UI action request. " +
                "This is a one-time setup - once TMP is imported, you won't need to do this again.",
                importExtras);
        }
    }
}

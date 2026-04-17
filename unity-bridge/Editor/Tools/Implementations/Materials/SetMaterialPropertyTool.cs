using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Materials
{
    public class SetMaterialPropertyTool : ITool
    {
        public string Name => "set_material_property";

        public string Execute(Dictionary<string, object> args)
        {
            string materialPath = args.ContainsKey("materialPath") ? args["materialPath"].ToString() : "";
            string propertyName = args.ContainsKey("propertyName") ? args["propertyName"].ToString() : "";
            string valueStr = args.ContainsKey("value") ? args["value"].ToString() : "";
            
            if (string.IsNullOrEmpty(materialPath))
            {
                return ToolUtils.CreateErrorResponse("materialPath is required");
            }
            
            if (string.IsNullOrEmpty(propertyName))
            {
                return ToolUtils.CreateErrorResponse("propertyName is required");
            }
            
            if (string.IsNullOrEmpty(valueStr))
            {
                return ToolUtils.CreateErrorResponse("value is required");
            }
            
            // Ensure path starts with Assets/
            if (!materialPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                materialPath = "Assets/" + materialPath;
            }
            
            // Load material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            {
                return ToolUtils.CreateErrorResponse($"Material not found at '{materialPath}'");
            }
            
            try
            {
                Undo.RecordObject(mat, $"Set Material Property: {materialPath}.{propertyName}");
                
                // Try to set as shader property first (most common case)
                if (mat.HasProperty(propertyName))
                {
                    // Try different property types based on value format
                    // Try Color first (format: "r,g,b,a" or "r,g,b")
                    if (valueStr.Contains(","))
                    {
                        string[] parts = valueStr.Split(',');
                        // If it has 3-4 comma-separated values, try as color
                        if (parts.Length >= 3 && parts.Length <= 4)
                        {
                            try
                            {
                                Color color = ToolUtils.ParseColor(valueStr);
                                mat.SetColor(propertyName, color);
                            }
                            catch
                            {
                                // Not a color, try as vector
                                try
                                {
                                    Vector4 vec = ToolUtils.ParseVector4(valueStr);
                                    mat.SetVector(propertyName, vec);
                                }
                                catch
                                {
                                    return ToolUtils.CreateErrorResponse($"Failed to parse value '{valueStr}' as color or vector");
                                }
                            }
                        }
                        else if (parts.Length == 2)
                        {
                            // Vector2
                            try
                            {
                                var vec3 = ToolUtils.ParseVector3(valueStr + ",0");
                                mat.SetVector(propertyName, new Vector2(vec3.x, vec3.y));
                            }
                            catch
                            {
                                return ToolUtils.CreateErrorResponse($"Failed to parse value '{valueStr}' as Vector2");
                            }
                        }
                    }
                    // Try Texture (valueStr is a path ending with image extension)
                    else if (valueStr.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                             valueStr.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                             valueStr.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                             valueStr.EndsWith(".tga", StringComparison.OrdinalIgnoreCase) ||
                             valueStr.EndsWith(".exr", StringComparison.OrdinalIgnoreCase))
                    {
                        string texturePath = valueStr;
                        if (!texturePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                        {
                            texturePath = "Assets/" + texturePath;
                        }
                        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                        if (texture == null)
                        {
                            return ToolUtils.CreateErrorResponse($"Texture not found at '{texturePath}'");
                        }
                        mat.SetTexture(propertyName, texture);
                    }
                    // Try Float (single numeric value)
                    else
                    {
                        if (float.TryParse(valueStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatValue))
                        {
                            mat.SetFloat(propertyName, floatValue);
                        }
                        else
                        {
                            return ToolUtils.CreateErrorResponse($"Failed to parse value '{valueStr}' as float, color, vector, or texture path");
                        }
                    }
                }
                else
                {
                    // Try setting as Material property via reflection
                    var prop = typeof(Material).GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (prop != null && prop.CanWrite)
                    {
                        object convertedValue = ToolUtils.ConvertValueToPropertyType(valueStr, prop.PropertyType);
                        prop.SetValue(mat, convertedValue);
                    }
                    else
                    {
                        return ToolUtils.CreateErrorResponse($"Property '{propertyName}' not found on material or shader");
                    }
                }
                
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();
                
                return ToolUtils.CreateSuccessResponse($"Set property '{propertyName}' on material '{materialPath}' to '{valueStr}'");
            }
            catch (Exception e)
            {
                return ToolUtils.CreateErrorResponse($"Failed to set material property: {e.Message}");
            }
        }
    }
}

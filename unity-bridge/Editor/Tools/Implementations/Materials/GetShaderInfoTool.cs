using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Materials
{
    /// <summary>
    /// Gets detailed information about a shader including its properties, render queue, and supported features.
    /// </summary>
    public class GetShaderInfoTool : ITool
    {
        public string Name => "get_shader_info";

        public string Execute(Dictionary<string, object> args)
        {
            string shaderName = args.ContainsKey("shaderName") ? args["shaderName"].ToString() : "";
            
            if (string.IsNullOrEmpty(shaderName))
            {
                return ToolUtils.CreateErrorResponse("shaderName is required");
            }
            
            // Find the shader
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                return ToolUtils.CreateErrorResponse($"Shader '{shaderName}' not found. Use list_available_shaders to see available shaders.");
            }
            
            // Get shader properties
            var properties = new List<Dictionary<string, object>>();
            int propertyCount = shader.GetPropertyCount();
            
            for (int i = 0; i < propertyCount; i++)
            {
                string propName = shader.GetPropertyName(i);
                ShaderPropertyType propType = shader.GetPropertyType(i);
                
                properties.Add(new Dictionary<string, object>
                {
                    ["name"] = propName,
                    ["type"] = propType.ToString()
                });
            }
            
            // Get render queue
            int renderQueue = shader.renderQueue;
            string renderQueueName = "";
            if (renderQueue < 2500) renderQueueName = "Geometry";
            else if (renderQueue < 3000) renderQueueName = "Transparent";
            else if (renderQueue < 3500) renderQueueName = "Overlay";
            else renderQueueName = "Custom";
            
            var result = new Dictionary<string, object>
            {
                ["shaderName"] = shaderName,
                ["renderQueue"] = renderQueue,
                ["renderQueueName"] = renderQueueName,
                ["propertyCount"] = propertyCount,
                ["properties"] = properties,
                ["isSupported"] = shader.isSupported
            };
            
            string message = $"Retrieved information for shader '{shaderName}': {propertyCount} properties, render queue {renderQueue} ({renderQueueName})";
            
            return ToolUtils.CreateSuccessResponse(message, result);
        }
    }
}

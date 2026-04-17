using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class RemoveAnimatorParameterTool : ITool
    {
        public string Name => "remove_animator_parameter";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string parameterName = args.ContainsKey("parameterName") ? args["parameterName"].ToString() : "";

            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            if (string.IsNullOrEmpty(parameterName))
                return ToolUtils.CreateErrorResponse("parameterName is required");

            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return ToolUtils.CreateErrorResponse($"Animator Controller not found at '{controllerPath}'");

            // Find parameter by name and remove by index
            int paramIndex = -1;
            for (int i = 0; i < controller.parameters.Length; i++)
            {
                if (controller.parameters[i].name == parameterName)
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
                return ToolUtils.CreateErrorResponse($"Parameter '{parameterName}' not found");

            // Remove parameter by index using reflection (RemoveParameter method signature varies by Unity version)
            var removeMethod = controller.GetType().GetMethod("RemoveParameter", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (removeMethod != null)
            {
                // Try with parameter object first
                var param = controller.parameters[paramIndex];
                var paramTypes = removeMethod.GetParameters();
                if (paramTypes.Length == 1 && paramTypes[0].ParameterType.Name == "AnimatorControllerParameter")
                {
                    removeMethod.Invoke(controller, new object[] { param });
                }
                else
                {
                    // Fallback: remove by index if available
                    var removeByIndexMethod = controller.GetType().GetMethod("RemoveParameter", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                        null, new[] { typeof(int) }, null);
                    if (removeByIndexMethod != null)
                    {
                        removeByIndexMethod.Invoke(controller, new object[] { paramIndex });
                    }
                    else
                    {
                        // Last resort: use the parameter object
                        removeMethod.Invoke(controller, new object[] { param });
                    }
                }
            }
            else
            {
                return ToolUtils.CreateErrorResponse("Unable to remove parameter: RemoveParameter method not found");
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return ToolUtils.CreateSuccessResponse($"Removed parameter '{parameterName}'");
        }
    }
}

using System;
using UnityEngine;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    /// <summary>
    /// Utility methods for IK (Inverse Kinematics) operations.
    /// Provides parsing and field name resolution for IK goals and hints.
    /// </summary>
    public static class IKUtils
    {
        /// <summary>
        /// Converts a string IK goal name to AvatarIKGoal enum.
        /// </summary>
        public static AvatarIKGoal ParseIKGoal(string goalName)
        {
            if (string.IsNullOrEmpty(goalName))
                throw new ArgumentException("IK goal name cannot be null or empty");

            switch (goalName.ToLower())
            {
                case "lefthand":
                    return AvatarIKGoal.LeftHand;
                case "righthand":
                    return AvatarIKGoal.RightHand;
                case "leftfoot":
                    return AvatarIKGoal.LeftFoot;
                case "rightfoot":
                    return AvatarIKGoal.RightFoot;
                case "leftelbow":
                    // Elbow uses hand goal but with hint
                    return AvatarIKGoal.LeftHand;
                case "rightelbow":
                    // Elbow uses hand goal but with hint
                    return AvatarIKGoal.RightHand;
                case "leftknee":
                    // Knee uses foot goal but with hint
                    return AvatarIKGoal.LeftFoot;
                case "rightknee":
                    // Knee uses foot goal but with hint
                    return AvatarIKGoal.RightFoot;
                default:
                    throw new ArgumentException($"Invalid IK goal: {goalName}. Valid values: LeftHand, RightHand, LeftFoot, RightFoot, LeftElbow, RightElbow, LeftKnee, RightKnee");
            }
        }

        /// <summary>
        /// Converts a string IK hint name to AvatarIKHint enum.
        /// </summary>
        public static AvatarIKHint ParseIKHint(string hintName)
        {
            if (string.IsNullOrEmpty(hintName))
                throw new ArgumentException("IK hint name cannot be null or empty");

            switch (hintName.ToLower())
            {
                case "leftelbow":
                    return AvatarIKHint.LeftElbow;
                case "rightelbow":
                    return AvatarIKHint.RightElbow;
                case "leftknee":
                    return AvatarIKHint.LeftKnee;
                case "rightknee":
                    return AvatarIKHint.RightKnee;
                default:
                    throw new ArgumentException($"Invalid IK hint: {hintName}. Valid values: LeftElbow, RightElbow, LeftKnee, RightKnee");
            }
        }

        /// <summary>
        /// Gets the field name for an IK target Transform based on the goal.
        /// </summary>
        public static string GetIKGoalFieldName(AvatarIKGoal goal)
        {
            switch (goal)
            {
                case AvatarIKGoal.LeftHand:
                    return "leftHandTarget";
                case AvatarIKGoal.RightHand:
                    return "rightHandTarget";
                case AvatarIKGoal.LeftFoot:
                    return "leftFootTarget";
                case AvatarIKGoal.RightFoot:
                    return "rightFootTarget";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the field name for an IK weight based on the goal.
        /// </summary>
        public static string GetIKWeightFieldName(AvatarIKGoal goal)
        {
            switch (goal)
            {
                case AvatarIKGoal.LeftHand:
                    return "leftHandWeight";
                case AvatarIKGoal.RightHand:
                    return "rightHandWeight";
                case AvatarIKGoal.LeftFoot:
                    return "leftFootWeight";
                case AvatarIKGoal.RightFoot:
                    return "rightFootWeight";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the field name for an IK hint target Transform.
        /// </summary>
        public static string GetIKHintTargetFieldName(string hintName)
        {
            switch (hintName.ToLower())
            {
                case "leftelbow":
                    return "leftElbowTarget";
                case "rightelbow":
                    return "rightElbowTarget";
                case "leftknee":
                    return "leftKneeTarget";
                case "rightknee":
                    return "rightKneeTarget";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the field name for an IK hint weight.
        /// </summary>
        public static string GetIKHintWeightFieldName(string hintName)
        {
            switch (hintName.ToLower())
            {
                case "leftelbow":
                    return "leftElbowWeight";
                case "rightelbow":
                    return "rightElbowWeight";
                case "leftknee":
                    return "leftKneeWeight";
                case "rightknee":
                    return "rightKneeWeight";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Checks if a goal name represents a hint goal (elbow or knee).
        /// </summary>
        public static bool IsHintGoal(string goalName)
        {
            if (string.IsNullOrEmpty(goalName))
                return false;

            string lower = goalName.ToLower();
            return lower.Contains("elbow") || lower.Contains("knee");
        }
    }
}

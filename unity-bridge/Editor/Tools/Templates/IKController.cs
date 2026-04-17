using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IKController : MonoBehaviour
{
    [Header("IK Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public Transform leftElbowTarget;
    public Transform rightElbowTarget;
    public Transform leftKneeTarget;
    public Transform rightKneeTarget;
    
    [Header("IK Weights")]
    [Range(0f, 1f)] public float leftHandWeight = 1f;
    [Range(0f, 1f)] public float rightHandWeight = 1f;
    [Range(0f, 1f)] public float leftFootWeight = 1f;
    [Range(0f, 1f)] public float rightFootWeight = 1f;
    [Range(0f, 1f)] public float leftElbowWeight = 0f;
    [Range(0f, 1f)] public float rightElbowWeight = 0f;
    [Range(0f, 1f)] public float leftKneeWeight = 0f;
    [Range(0f, 1f)] public float rightKneeWeight = 0f;
    
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
    }
    
    void OnAnimatorIK(int layerIndex)
    {
        SetIKGoal(AvatarIKGoal.LeftHand, leftHandTarget, leftHandWeight);
        SetIKGoal(AvatarIKGoal.RightHand, rightHandTarget, rightHandWeight);
        SetIKGoal(AvatarIKGoal.LeftFoot, leftFootTarget, leftFootWeight);
        SetIKGoal(AvatarIKGoal.RightFoot, rightFootTarget, rightFootWeight);
        
        if (leftElbowTarget != null && leftElbowWeight > 0f)
        {
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, leftElbowWeight);
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftElbowTarget.position);
        }
        
        if (rightElbowTarget != null && rightElbowWeight > 0f)
        {
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, rightElbowWeight);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, rightElbowTarget.position);
        }
        
        if (leftKneeTarget != null && leftKneeWeight > 0f)
        {
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, leftKneeWeight);
            animator.SetIKHintPosition(AvatarIKHint.LeftKnee, leftKneeTarget.position);
        }
        
        if (rightKneeTarget != null && rightKneeWeight > 0f)
        {
            animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, rightKneeWeight);
            animator.SetIKHintPosition(AvatarIKHint.RightKnee, rightKneeTarget.position);
        }
    }
    
    private void SetIKGoal(AvatarIKGoal goal, Transform target, float weight)
    {
        if (target != null && weight > 0f)
        {
            animator.SetIKPositionWeight(goal, weight);
            animator.SetIKRotationWeight(goal, weight);
            animator.SetIKPosition(goal, target.position);
            animator.SetIKRotation(goal, target.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(goal, 0f);
            animator.SetIKRotationWeight(goal, 0f);
        }
    }
    
    // Public methods for runtime control
    public void SetIKWeight(AvatarIKGoal goal, float weight)
    {
        switch (goal)
        {
            case AvatarIKGoal.LeftHand: leftHandWeight = weight; break;
            case AvatarIKGoal.RightHand: rightHandWeight = weight; break;
            case AvatarIKGoal.LeftFoot: leftFootWeight = weight; break;
            case AvatarIKGoal.RightFoot: rightFootWeight = weight; break;
        }
    }
    
    public void SetIKTarget(AvatarIKGoal goal, Transform target)
    {
        switch (goal)
        {
            case AvatarIKGoal.LeftHand: leftHandTarget = target; break;
            case AvatarIKGoal.RightHand: rightHandTarget = target; break;
            case AvatarIKGoal.LeftFoot: leftFootTarget = target; break;
            case AvatarIKGoal.RightFoot: rightFootTarget = target; break;
        }
    }
}

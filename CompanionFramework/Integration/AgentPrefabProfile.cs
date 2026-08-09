using UnityEngine;

public class AgentPrefabProfile : MonoBehaviour
{
    public string AgentId = string.Empty;
    public string IconId = string.Empty;
    public string TombstonePrefabName = string.Empty;
    public float MaxCarryWeight = 300f;
    public int InventoryWidth = 8;
    public int InventoryHeight = 6;
    public bool CanJump = true;
    public bool CanFly;
    public bool RequiresInteractionToRegister;
    public bool CanPullCart;
    public bool Mountable;
    public bool MountAllowsCombat = true;
    public bool MountCanFly;
    public Vector3 SaddleLocalOffset = Vector3.zero;
    public float MountedGroundSpeed = 5f;
    public float MountedRunSpeed = 8f;
    public float MountedFlySpeed = 8f;
    public float MountedFlyRunSpeed = 13f;
    public float CartSearchRadius = 6f;
    public float CartPullDistance = 2.35f;
    public string[] HiddenRendererNameContains = new string[0];
    public BodypartSystem.bodyPart[] HiddenBodyParts = new BodypartSystem.bodyPart[0];
    public bool AmputateBodyParts;
    public string[] BodyMeshNameContains = new string[0];
    public string[] ExcludedMeshNameContains = new string[0];
    public bool EnableJumpMotionAssist;
    public float JumpAssistForwardVelocity = 8f;
    public float JumpAssistUpVelocity = 2.5f;
    public float JumpAssistDuration = 0.45f;
    public float JumpAssistMaxTargetDistance = 18f;
    public string[] JumpAssistClipNameContains = new[]
    {
        "jump",
        "leap",
        "pounce"
    };
}

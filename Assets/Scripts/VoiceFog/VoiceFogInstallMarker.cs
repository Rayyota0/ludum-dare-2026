using UnityEngine;

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Internal tag so <see cref="VoiceFogRuntimeBootstrap"/> does not stack duplicate behaviours on the same camera.
    /// </summary>
    public sealed class VoiceFogInstallMarker : MonoBehaviour
    {
    }
}

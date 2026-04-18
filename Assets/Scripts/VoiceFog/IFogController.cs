namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Drives global fog strength: 0 = clear, 1 = scene baseline (from <see cref="DenseFogBootstrap"/> snapshot).
    /// </summary>
    public interface IFogController
    {
        void SetFogWeight(float weight01);
    }
}

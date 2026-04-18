namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Drives global fog strength: 1 = scene baseline (from <see cref="DenseFogBootstrap"/> / <see cref="UniversalVolumeFogDriver"/>).
    /// Weight 0 is «minimum fog» as configured on the driver (may still be a dense wall beyond a short near-camera band — not necessarily global clear).
    /// </summary>
    public interface IFogController
    {
        void SetFogWeight(float weight01);
    }
}

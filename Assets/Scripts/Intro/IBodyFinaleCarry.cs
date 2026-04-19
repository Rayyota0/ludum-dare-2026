using UnityEngine;

namespace LudumDare.Intro
{
    /// <summary>
    /// Body bag carried on the player for scenes that run <see cref="FinaleSequence"/>.
    /// </summary>
    public interface IBodyFinaleCarry
    {
        bool IsCarriedForFinale { get; }
        Transform BodyTransform { get; }
        void DetachForFinale();
    }
}

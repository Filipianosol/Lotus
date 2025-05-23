using Lotus.Roles.Interactions.Interfaces;

namespace Lotus.Roles.Interactions;

public class IndirectInteraction : DirectInteraction, IIndirectInteraction
{
    public IndirectInteraction(Intent intent, CustomRole? customRole = null) : base(intent, customRole)
    {
    }

    public override Interaction Modify(Intent intent) => new IndirectInteraction(intent, Emitter());
}
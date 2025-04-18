using System.Collections.Generic;
using System.Linq;
using Core.Helpers;

public class Ignite : ActivatedAbility
{
    public override bool NeedsTarget() => false;

    public override bool IsCardValid(ID id, Card card)
    {
        if (id.IsPlayerField() && !id.IsOwnedBy(BattleVars.Shared.AbilityIDOrigin.owner)) return true;
        return id.IsCreatureField() && card.IsBurrowedOrImmaterial();
    }

    public override void Activate(ID targetId, Card targetCard)
    {
        if (!IsCardValid(targetId, targetCard)) return;
        EventBus<ClearCardDisplayEvent>.Raise(new ClearCardDisplayEvent(BattleVars.Shared.AbilityIDOrigin));

        if (targetCard is null)
        {
            EventBus<ModifyPlayerHealthEvent>.Raise(new ModifyPlayerHealthEvent(20, true, true, targetId.owner));
        }
        else
        {
            targetCard.SetDefDamage(1);
            EventBus<UpdateCreatureCardEvent>.Raise(new UpdateCreatureCardEvent(targetId, targetCard, true));
        }
    }
}

using UnityEngine;
using System.Collections;

/**
Class responsible for ability use cheating detection of remote players.
Class checks such things as ability cast range, cooldown, mp cost if they satisfies requirements.
The threshold minimizes error count of positive negative cheat detection.
Some rules uses abilities inner logic validations - same are used on local player when using ability.
*/
public class AbilityCheatDetection
{
	public delegate void CheatHandler();
	public event CheatHandler CheatDetected;
	
	const float COOLDOWN_THRESHOLD = 1f;
	const float COST_TRESHOLD = 2f;
	const float RANGE_TRESHOLD = -2f;

	public bool DoCheck(Ability ability, Vector3 point, GameObject target)
	{
		bool validTargeting = TargetCheck(ability, point, target);
		bool validCooldown = CooldownCheck(ability);
		bool validCost = CostCheck(ability);
		bool validAbilityUse = validTargeting && validCooldown && validCost;
		if(!validAbilityUse)
		{
			if(CheatDetected != null) CheatDetected();
		}
		return validAbilityUse;
	}

	private bool TargetCheck(Ability ability, Vector3 point, GameObject target)
	{
		if(!ability.NeedTargeting()) return true;

		float rangeDif;
		bool validRange = ability.ValidateTargetPosition(point, out rangeDif);
		validRange = validRange || (rangeDif > RANGE_TRESHOLD);

		bool validTarget = true;
		if(ability.targetingType == TargetingType.Unit)
		{
			validTarget = ability.ValidateTargetTeam(target);
		}

		return validRange && validTarget;
	}

	private bool CooldownCheck(Ability ability)
	{
		bool validCooldown = !ability.IsOnCooldown();
		float cooldown = ability.GetParameters().cooldown;
		validCooldown = validCooldown || (cooldown - ability.cooldownTimeLeft + COOLDOWN_THRESHOLD >= cooldown);
		return validCooldown;
	}

	private bool CostCheck(Ability ability)
	{
		bool validCost = ability.CostCheck();
		float mpCost = ability.GetParameters().costMp;
		validCost = validCost || (ability.statController.mana.value + COST_TRESHOLD >= mpCost);
		return validCost;
	}

}

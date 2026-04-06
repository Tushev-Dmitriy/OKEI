using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4SquadCompositionModule : MonoBehaviour
{
    public bool IsAllowedFinalComposition(Level4FlowController flow)
    {
        GetCommittedFinalCompositionCounts(flow, out int attackers, out int healers, out int defenders, out int bases, out int total);

        if (bases > 0 || total != 5)
            return false;

        return (attackers == 2 && healers == 2 && defenders == 1) ||
               (attackers == 2 && healers == 1 && defenders == 2) ||
               (attackers == 3 && healers == 1 && defenders == 1);
    }

    public void GetFinalCompositionCounts(Level4FlowController flow, out int attackers, out int healers, out int defenders, out int bases, out int total)
    {
        attackers = 0;
        healers = 0;
        defenders = 0;
        bases = 0;
        total = 0;

        if (flow == null)
            return;

        if (!flow.FinalRunStarted && flow.PlannedFinalSquad.Count > 0)
        {
            for (int i = 0; i < flow.PlannedFinalSquad.Count; i++)
            {
                total++;
                switch (flow.PlannedFinalSquad[i])
                {
                    case RobotType.Base:
                        bases++;
                        break;
                    case RobotType.Attacker:
                        attackers++;
                        break;
                    case RobotType.Healer:
                        healers++;
                        break;
                    case RobotType.Defender:
                        defenders++;
                        break;
                }
            }

            return;
        }

        for (int i = 0; i < flow.FinalSquad.Count; i++)
        {
            Robot robot = flow.FinalSquad[i];
            if (robot == null)
                continue;

            total++;
            switch (robot.RobotType)
            {
                case RobotType.Base:
                    bases++;
                    break;
                case RobotType.Attacker:
                    attackers++;
                    break;
                case RobotType.Healer:
                    healers++;
                    break;
                case RobotType.Defender:
                    defenders++;
                    break;
            }
        }
    }

    public void GetCommittedFinalCompositionCounts(Level4FlowController flow, out int attackers, out int healers, out int defenders, out int bases, out int total)
    {
        attackers = 0;
        healers = 0;
        defenders = 0;
        bases = 0;
        total = 0;

        if (flow == null)
            return;

        if (flow.FinalCommittedTotal > 0)
        {
            attackers = flow.FinalCommittedAttackers;
            healers = flow.FinalCommittedHealers;
            defenders = flow.FinalCommittedDefenders;
            bases = flow.FinalCommittedBases;
            total = flow.FinalCommittedTotal;
            return;
        }

        GetFinalCompositionCounts(flow, out attackers, out healers, out defenders, out bases, out total);
    }
}

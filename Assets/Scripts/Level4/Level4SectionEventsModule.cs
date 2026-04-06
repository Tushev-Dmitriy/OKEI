using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4SectionEventsModule : MonoBehaviour
{
    internal void HandleEnemyDied(Level4FlowController flow, EnemyUnit enemy)
    {
        if (flow == null || !flow.AttemptActive || enemy == null)
            return;

        if (flow.ActiveEnemies.Remove(enemy))
            flow.RefreshStatus();
    }

    internal void HandlePlayerRobotDied(Level4FlowController flow, Robot robot)
    {
        if (flow == null || !flow.AttemptActive || !flow.HasCurrentSection || robot != flow.PlayerRobotRef)
            return;

        if (flow.CurrentSectionDef.Id == Level4FlowController.SectionId.Base && robot.RobotType == RobotType.Base)
        {
            flow.AdvanceAfterRequiredRobotTestForModule("Ѕазовый робот прошел первый этап: он умеет двигатьс€ и получать урон, но в бою слаб. “еперь открыт атакующий робот.", false);
            return;
        }

        if (flow.CurrentSectionDef.Id == Level4FlowController.SectionId.Attacker && robot.RobotType == RobotType.Attacker)
        {
            flow.AdvanceAfterRequiredRobotTestForModule("јтакующий робот протестирован. ќн наследует базовые возможности и добавл€ет атаку. “еперь открыт хилер.", false);
            return;
        }

        if (flow.CurrentSectionDef.Id == Level4FlowController.SectionId.Healer && robot.RobotType == RobotType.Healer)
        {
            flow.AdvanceAfterRequiredRobotTestForModule("’илер протестирован. ќн сохран€ет базовое поведение и добавл€ет лечение. “еперь открыт защитник.", false);
            return;
        }

        if (flow.CurrentSectionDef.Id == Level4FlowController.SectionId.Defender && robot.RobotType == RobotType.Defender)
        {
            flow.AdvanceAfterRequiredRobotTestForModule("¬се классы роботов были вызваны и проверены по одному разу. –ежим армии открыт: собери отр€д из 5 роботов и пройди до конца.", false);
            return;
        }

        flow.FailCurrentSectionForModule(flow.CurrentSectionDef.FailureText);
    }

    internal void HandleEscortRobotDied(Level4FlowController flow, Robot robot)
    {
        if (flow == null || !flow.AttemptActive || !flow.HasCurrentSection || robot != flow.EscortRobotRef)
            return;

        flow.FailCurrentSectionForModule(flow.CurrentSectionDef.FailureText);
    }

    internal void HandleFinalRobotDied(Level4FlowController flow, Robot robot)
    {
        if (flow == null || !flow.AttemptActive || robot == null || !flow.CurrentSectionIsFinal)
            return;

        flow.UnsubscribeFinalRobotDeathForModule(robot);

        if (flow.FinalRunStarted && !flow.HasLivingFinalRobotForModule())
        {
            flow.FailCurrentSectionForModule(flow.CurrentSectionDef.FailureText);
            return;
        }

        flow.RefreshStatus();
    }

    internal void ActivateWave(Level4FlowController flow, int waveIndex)
    {
        if (flow == null)
            return;

        flow.ActivateCorridorEnemiesForRunForModule();
    }

    internal void ActivateSectionEnemies(Level4FlowController flow, SectionDefinition section)
    {
        if (flow == null)
            return;

        flow.ActivateCorridorEnemiesForRunForModule();
    }
}


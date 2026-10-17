using NPP.Sim;

namespace NPP.Sim
{
    /// <summary>Текущее (подтверждённое) значение органа управления из снапшота.</summary>
    public static class CommandState
    {
        public static float Read(PlantSnapshot s, ControlId id)
        {
            switch (id)
            {
                case ControlId.RodTarget: return s.rodTarget;
                case ControlId.Pumps: return s.pumpsMask;
                case ControlId.Valve: return s.valve;
                case ControlId.Feed: return s.feed;
                case ControlId.FeedAuto: return s.feedAuto ? 1f : 0f;
                case ControlId.Breaker: return s.breaker ? 1f : 0f;
                case ControlId.Vent: return s.vent ? 1f : 0f;
                case ControlId.Scram: return s.scrammed ? 1f : 0f;
                default: return 0f;
            }
        }
    }
}

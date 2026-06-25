using Unity.Profiling;

namespace QuestCommandRTS
{
    public static class RtsProfilerMarkers
    {
        public static readonly ProfilerMarker GameUpdate = new ProfilerMarker("QuestCommandRTS.GameUpdate");
        public static readonly ProfilerMarker FogUpdate = new ProfilerMarker("QuestCommandRTS.FogUpdate");
        public static readonly ProfilerMarker UnitOrders = new ProfilerMarker("QuestCommandRTS.UnitOrders");
        public static readonly ProfilerMarker Production = new ProfilerMarker("QuestCommandRTS.Production");
        public static readonly ProfilerMarker EnemyDirector = new ProfilerMarker("QuestCommandRTS.EnemyDirector");
        public static readonly ProfilerMarker SaveCapture = new ProfilerMarker("QuestCommandRTS.SaveCapture");
        public static readonly ProfilerMarker SaveRestore = new ProfilerMarker("QuestCommandRTS.SaveRestore");
    }
}

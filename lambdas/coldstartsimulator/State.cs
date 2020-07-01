namespace ColdStartSimulator
{
    /// <summary>
    /// The state passed between the step function executions.
    /// </summary>
    public class State
    {
        public string FunctionName { get; set; }
        public long StartTimeInEpoch { get; set; }
        public long EndTimeInEpoch { get; set; }
        public int Index { get; set; }
        public int Step { get; set; }
        public int Count { get; set; }
        public bool Continue { get; set; }
    }
}
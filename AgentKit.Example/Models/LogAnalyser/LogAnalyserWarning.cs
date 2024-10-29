namespace AgentKit.Example.Models.LogAnalyser;

public class LogAnalyserWarning
{
    public int CriticalLevel { get; set; }
    public string WarningReason { get; set; }
    
    public string Log { get; set; }
}
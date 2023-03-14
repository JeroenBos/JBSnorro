namespace JBSnorro.Testing.IntertestDependency;

internal interface ITestRun
{
    bool CompletedUnsuccessfully { get; }
    bool CompletedSuccessfully { get; }
    bool Pending { get; }
    Task<Task> TestTask { get; }
}

using System.Diagnostics;

namespace SemanticFlow.DemoConsoleApp.Services;

public class SessionService
{
    private readonly string _id;

    public SessionService()
    {
        _id = Process.GetCurrentProcess().Id.ToString();
    }

    public string GetId()
    {
        return _id;
    }
}
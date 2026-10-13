using AventStack.ExtentReports;

namespace Atf.Reporting.Extent.Adapter;

internal interface IExtentNodeView
{
    string Name { get; }
    IReadOnlyList<IExtentNodeView> Children { get; }
    IReadOnlyList<string> LogDetails { get; }
}

/// <summary>
/// Renderer boundary for one report node. Scenario state depends on this
/// contract, never on the Extent vendor object itself.
/// </summary>
internal interface IExtentNodeAdapter : IExtentNodeView
{

    IExtentNodeAdapter CreateNode(string title, string description);
    void SetStatus(Status status);
    void Info(string message);
    void Pass(string message);
    void Fail(string message);
    void Skip(string message);
    void Warning(string message);
    void Log(Status status, string message);
}

/// <summary>
/// The only class in the scenario reporting path that knows about ExtentTest.
/// </summary>
internal sealed class ExtentNodeAdapter : IExtentNodeAdapter
{
    private readonly ExtentTest _node;

    public ExtentNodeAdapter(ExtentTest node)
    {
        _node = node ?? throw new ArgumentNullException(nameof(node));
    }

    public string Name => _node.Model.Name;

    public IReadOnlyList<IExtentNodeView> Children =>
        _node.Model.Children.Select(child => (IExtentNodeView)new ExtentNodeModelView(child)).ToArray();

    public IReadOnlyList<string> LogDetails =>
        _node.Model.Logs.Select(log => log.Details).ToArray();

    public IExtentNodeAdapter CreateNode(string title, string description) =>
        new ExtentNodeAdapter(_node.CreateNode(title, description));

    public void SetStatus(Status status) => _node.Model.Status = status;

    public void Info(string message) => _node.Info(message);
    public void Pass(string message) => _node.Pass(message);
    public void Fail(string message) => _node.Fail(message);
    public void Skip(string message) => _node.Skip(message);
    public void Warning(string message) => _node.Warning(message);
    public void Log(Status status, string message) => _node.Log(status, message);
}

internal sealed class ExtentNodeModelView : IExtentNodeView
{
    private readonly AventStack.ExtentReports.Model.Test _model;

    public ExtentNodeModelView(AventStack.ExtentReports.Model.Test model) =>
        _model = model ?? throw new ArgumentNullException(nameof(model));

    public string Name => _model.Name;

    public IReadOnlyList<IExtentNodeView> Children =>
        _model.Children.Select(child => (IExtentNodeView)new ExtentNodeModelView(child)).ToArray();

    public IReadOnlyList<string> LogDetails =>
        _model.Logs.Select(log => log.Details).ToArray();
}

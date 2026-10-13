namespace Atf.Core.Http.Transport;

public interface IApiAuthenticator
{
    void Apply(HttpRequestMessage message, string profileName);
    string DescribeMode(string profileName);
    string DescribeTokenState(string profileName);
}

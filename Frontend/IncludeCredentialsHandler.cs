using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Frontend;

/// <summary>Dołącza HttpOnly cookie sesji również przy lokalnym API na innym porcie.</summary>
public sealed class IncludeCredentialsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return base.SendAsync(request, cancellationToken);
    }
}

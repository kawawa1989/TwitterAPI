using System.Collections.Generic;
using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Net;


namespace OAuth
{
    public class OAuthRequestHttpClient : IOAuthRequest
    {
        private static HttpClient httpClient = new HttpClient();
        private string m_url = "";
        private OAuthRequestMethod m_method = OAuthRequestMethod.GET;

        public string Url => m_url;
        public OAuthRequestMethod Method => m_method;

        public OAuthRequestHttpClient(string url, OAuthRequestMethod method)
        {
            m_url = url;
            m_method = method;
        }

        public void Request(Dictionary<string, string> parameters, Action<OAuthRequestResult> onSuccess, Action<OAuthRequestResult> onError)
        {
            var uri = new Uri(Url);
            switch (m_method)
            {
                case OAuthRequestMethod.GET:
                    m_url = $"{uri.Scheme}://{uri.Authority}{uri.LocalPath}";
                    var queries = parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}");
                    var query = string.Join("&", queries);
                    if (queries.Count() > 0)
                    {
                        m_url = $"{m_url}?{query}";
                    }
                    break;
                case OAuthRequestMethod.POST:
                    m_url = $"{uri.Scheme}://{uri.Authority}{uri.LocalPath}";
                    break;
            }

            var context = SynchronizationContext.Current;
            Task.Run(() =>
            {
                return Run(m_url, m_method, parameters, onSuccess, onError, context);
            });
        }

        private static async Task Run(string url, OAuthRequestMethod method, Dictionary<string, string> parameters, Action<OAuthRequestResult> onSuccess, Action<OAuthRequestResult> onError, SynchronizationContext context)
        {
            var result = new OAuthRequestResult();
            switch (method)
            {
                case OAuthRequestMethod.GET:
                    {
                        var response = await httpClient.GetAsync(url);
                        var text = await response.Content.ReadAsStringAsync();
                        result.Text = text;
                        result.IsSuccess = response.IsSuccessStatusCode;
                        result.StatusCode = (long)response.StatusCode;
                    }
                    break;
                case OAuthRequestMethod.POST:
                    {
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                        var content = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                        request.Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded");
                        var response = await httpClient.SendAsync(request);
                        var text = await response.Content.ReadAsStringAsync();
                        result.Text = text;
                        result.IsSuccess = response.IsSuccessStatusCode;
                        result.StatusCode = (long)response.StatusCode;
                    }
                    break;
            }

            context.Post(_ =>
            {
                if (!result.IsSuccess)
                {
                    onError(result);
                    return;
                }
                onSuccess(result);
            }, null);
        }
    }
}


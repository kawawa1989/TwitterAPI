using System.Collections.Generic;
using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using UnityEngine;


namespace OAuth
{
    public class OAuthRequestHttpClient : OAuthRequestObject
    {
        private static HttpClient httpClient = new HttpClient();
        private bool m_isConnecting = false;

        public OAuthRequestHttpClient(string url, OAuthRequestMethod method) : base(url, method)
        {
        }

        public override void Request(Dictionary<string, string> parameters, Action<OAuthRequestResult> onSuccess, Action<OAuthRequestResult> onError)
        {
            if (m_isConnecting)
            {
                Debug.LogError("未完了のリクエストがあります！");
                return;
            }
            m_isConnecting = true;
            Action<OAuthRequestResult> onSuccessCallback = (result) =>
            {
                m_isConnecting = false;
                onSuccess(result);
            };
            Action<OAuthRequestResult> onErrorCallback = (result) =>
            {
                m_isConnecting = false;
                onError(result);
            };

            var context = SynchronizationContext.Current;
            Task.Run(() =>
            {
                return Run(ParseURL(parameters), Method, parameters, onSuccessCallback, onErrorCallback, context);
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


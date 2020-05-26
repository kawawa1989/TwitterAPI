using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine.Networking;

using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Web;

namespace OAuth
{
    public enum OAuthRequestMethod
    {
        GET,
        POST,
    }

    public class HttpResult
    {
        public string Text;
    }

    public class OAuthToken
    {
        public readonly string Token;
        public readonly string Secret;
        public OAuthToken(string token, string secret)
        {
            Token = token;
            Secret = secret;
        }
    }

    public class OAuthClient
    {
        public readonly string ConsumerKey;
        public readonly string ConsumerSecret;
        public OAuthClient(string consumerKey, string consumerSecret)
        {
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
        }
    }

    public abstract class OAuthSignature
    {
        public abstract string Name { get; }
        public abstract string GetSignature(OAuthRequestMethod method, string url, OAuthClient client, OAuthToken token, Dictionary<string, string> parameters);
    }

    public class OAuthSignature_HMACSHA1 : OAuthSignature
    {
        public override string Name => "HMAC-SHA1";

        public override string GetSignature(OAuthRequestMethod method, string url, OAuthClient client, OAuthToken token, Dictionary<string, string> parameters)
        {
            var sortedParams = parameters.Select(p => p.Key).OrderBy(p => p).Select(k => $"{k}={Uri.EscapeDataString(parameters[k])}");
            var parameterStrings = string.Join("&", sortedParams);
            var escapedMethod = Uri.EscapeDataString(method.ToString());
            var escapedUrl = Uri.EscapeDataString(url);
            var escapedNormalizedParameters = Uri.EscapeDataString(parameterStrings);
            var rawData = $"{escapedMethod}&{escapedUrl}&{escapedNormalizedParameters}";
            var key = $"{client.ConsumerSecret}&";
            if (token != null)
            {
                key += token.Secret;
            }
            return Convert.ToBase64String(new HMACSHA1(Encoding.UTF8.GetBytes(key)).ComputeHash(Encoding.UTF8.GetBytes(rawData)));
        }
    }

    public class OAuthRequest
    {
        private const string Version = "1.0";
        private string m_url;
        private OAuthRequestMethod m_method;
        private Dictionary<string, string> m_parameters;

        public OAuthRequest(string url, OAuthRequestMethod method, OAuthClient client, OAuthToken token, OAuthSignature signature, Dictionary<string, string> body = null)
        {
            m_method = method;
            var uri = new Uri(url);
            var parameters = new Dictionary<string, string>();
            parameters.Add("oauth_consumer_key", client.ConsumerKey);
            if (token != null)
            {
                parameters.Add("oauth_token", token.Token);
            }

            var urlWithoutQuery = $"{uri.Scheme}://{uri.Authority}{uri.LocalPath}";
            var timestamp = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var nonce = new System.Random().Next(100000000);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            foreach (var key in queryParams.AllKeys)
            {
                parameters.Add(key, queryParams[key]);
            }
            if (body != null)
            {
                foreach (var kvp in body)
                {
                    parameters.Add(kvp.Key, kvp.Value);
                }
            }

            parameters.Add("oauth_timestamp", ((int)timestamp.TotalSeconds).ToString());
            parameters.Add("oauth_nonce", nonce.ToString());
            parameters.Add("oauth_version", Version);
            parameters.Add("oauth_signature_method", signature.Name);

            var oauthSignature = signature.GetSignature(method, urlWithoutQuery, client, token, parameters);
            parameters.Add("oauth_signature", oauthSignature);

            switch (method)
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
            m_parameters = parameters;
        }

        public void SendWebRequest(Action<HttpResult> onSuccess, Action<HttpResult> onError)
        {
            var context = SynchronizationContext.Current;

            Task.Run(() =>
            {
                return Run(m_url, m_method, m_parameters, onSuccess, onError, context);
            });
        }

        private static HttpClient httpClient = new HttpClient();
        private static async Task Run(string url, OAuthRequestMethod method, Dictionary<string,string> parameters, Action<HttpResult> onSuccess, Action<HttpResult> onError, SynchronizationContext context)
        {
            switch (method)
            {
                case OAuthRequestMethod.GET:
                    {
                        var response = await httpClient.GetAsync(url);
                        var text = await response.Content.ReadAsStringAsync();
                        var result = new HttpResult();
                        result.Text = text;
                        context.Post(_ =>
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                onError(result);
                                return;
                            }
                            onSuccess(result);
                        }, null);
                    }
                    break;
                case OAuthRequestMethod.POST:
                    {
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                        var content = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                        request.Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded");
                        var response = await httpClient.SendAsync(request);
                        var text = await response.Content.ReadAsStringAsync();
                        var result = new HttpResult();
                        result.Text = text;

                        var requestMessage = await response.RequestMessage.Content.ReadAsStringAsync();
                        context.Post(_ =>
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                onError(result);
                                return;
                            }
                            onSuccess(result);
                        }, null);
                    }
                    break;
            }
        }
    }
}
using System;
using System.Linq;
using System.Web;
using System.Net;
using System.Collections.Generic;
using UnityEngine;

namespace OAuth
{
    public enum OAuthRequestMethod
    {
        GET,
        POST,
    }

    public class OAuthRequestResult
    {
        public string Text;
        public bool IsSuccess;
        public long StatusCode;
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

    public abstract class OAuthRequestObject
    {
        public string Url { get; private set; }
        public OAuthRequestMethod Method { get; private set; }
        public abstract void Request(Dictionary<string, string> parameters, Action<OAuthRequestResult> onSuccess, Action<OAuthRequestResult> onError);

        public OAuthRequestObject(string url, OAuthRequestMethod method)
        {
            Url = url;
            Method = method;
        }

        public string ParseURL(Dictionary<string, string> parameters)
        {
            var uri = new Uri(Url);
            var parsedUrl = "";
            switch (Method)
            {
                case OAuthRequestMethod.GET:
                    parsedUrl = $"{uri.Scheme}://{uri.Authority}{uri.LocalPath}";
                    var queries = parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}");
                    var query = string.Join("&", queries);
                    if (queries.Count() > 0)
                    {
                        parsedUrl = $"{parsedUrl}?{query}";
                    }
                    break;
                case OAuthRequestMethod.POST:
                    parsedUrl = $"{uri.Scheme}://{uri.Authority}{uri.LocalPath}";
                    break;
            }

            return parsedUrl;
        }
    }

    public class OAuthRequest
    {
        private const string Version = "1.0";
        private Dictionary<string, string> m_parameters = new Dictionary<string, string>();
        private OAuthRequestObject m_request = null;

        public OAuthRequest(OAuthRequestObject request, OAuthClient client, OAuthToken token, OAuthSignature signature, Dictionary<string, string> body = null)
        {
            var uri = new Uri(request.Url);
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

            var oauthSignature = signature.GetSignature(request.Method, urlWithoutQuery, client, token, parameters);
            parameters.Add("oauth_signature", oauthSignature);
            m_parameters = parameters;
            m_request = request;
        }

        public void SendWebRequest(Action<OAuthRequestResult> onSuccess, Action<OAuthRequestResult> onError)
        {
            m_request.Request(m_parameters, onSuccess, onError);
        }
    }
}
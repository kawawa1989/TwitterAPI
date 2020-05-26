﻿using System;
using System.Linq;
using System.Web;
using System.Net;
using System.Collections.Generic;


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
        public bool IsSuccess;
        public HttpStatusCode StatusCode;
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

    public interface IOAuthRequest
    {
        string Url { get; }
        OAuthRequestMethod Method { get; }
        void Request(Dictionary<string, string> parameters, Action<HttpResult> onSuccess, Action<HttpResult> onError);
    }

    public class OAuthRequest
    {
        private const string Version = "1.0";
        private Dictionary<string, string> m_parameters = new Dictionary<string, string>();
        private IOAuthRequest m_request = null;

        public OAuthRequest(IOAuthRequest request, OAuthClient client, OAuthToken token, OAuthSignature signature, Dictionary<string, string> body = null)
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
            var nonce = new Random().Next(100000000);
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

        public void SendWebRequest(Action<HttpResult> onSuccess, Action<HttpResult> onError)
        {
            m_request.Request(m_parameters, onSuccess, onError);
        }
    }
}
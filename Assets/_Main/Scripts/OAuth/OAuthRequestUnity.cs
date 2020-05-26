using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine;

namespace OAuth
{
    public class OAuthRequestUnity : IOAuthRequest
    {
        private UnityWebRequest m_request = null;
        public string Url { get; private set; }
        public OAuthRequestMethod Method { get; private set; }

        public OAuthRequestUnity(string url, OAuthRequestMethod method)
        {
            Url = url;
            Method = method;
        }

        public void Request(Dictionary<string, string> parameters, Action<OAuthRequestResult> onSuccess, Action<OAuthRequestResult> onError)
        {
            if (m_request != null && !m_request.isDone)
            {
                Debug.LogError("未完了のリクエストがあります！");
                return;
            }

            var uri = new Uri(Url);
            var url = "";
           
            switch (Method)
            {
                case OAuthRequestMethod.GET:
                    url = $"{uri.Scheme}://{uri.Authority}{uri.LocalPath}";
                    var queries = parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}");
                    var query = string.Join("&", queries);
                    if (queries.Count() > 0)
                    {
                        url = $"{url}?{query}";
                    }
                    m_request = UnityWebRequest.Get(url);
                    break;
                case OAuthRequestMethod.POST:
                    var form = new WWWForm();
                    url = $"{uri.Scheme}://{uri.Authority}{uri.LocalPath}";
                    foreach(var param in parameters)
                    {
                        form.AddField(param.Key, param.Value);
                    }
                    m_request = UnityWebRequest.Post(url, form);
                    break;
            }

            var operation = m_request.SendWebRequest();
            operation.completed += (op) =>
            {
                var result = new OAuthRequestResult();
                result.Text = m_request.downloadHandler.text;
                result.IsSuccess = !m_request.isHttpError && !m_request.isNetworkError;
                result.StatusCode = m_request.responseCode;

                if (!result.IsSuccess)
                {
                    onError(result);
                    return;
                }
                onSuccess(result);
            };
        }
    }
}
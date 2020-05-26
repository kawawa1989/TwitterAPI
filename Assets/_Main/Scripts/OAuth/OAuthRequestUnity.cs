using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine;

namespace OAuth
{
    public class OAuthRequestUnity : OAuthRequestObject
    {
        private UnityWebRequest m_request = null;

        public OAuthRequestUnity(string url, OAuthRequestMethod method) : base(url, method)
        {
        }

        public override void Request(Dictionary<string, string> parameters, Action<OAuthRequestResult> onSuccess, Action<OAuthRequestResult> onError)
        {
            if (m_request != null && !m_request.isDone)
            {
                Debug.LogError("未完了のリクエストがあります！");
                return;
            }

            var parsedUrl = ParseURL(parameters);
            switch (Method)
            {
                case OAuthRequestMethod.GET:
                    m_request = UnityWebRequest.Get(parsedUrl);
                    break;
                case OAuthRequestMethod.POST:
                    var form = new WWWForm();
                    foreach (var param in parameters)
                    {
                        form.AddField(param.Key, param.Value);
                    }
                    m_request = UnityWebRequest.Post(parsedUrl, form);
                    break;
            }

            var operation = m_request.SendWebRequest();
            operation.completed += (op) =>
            {
                var result = new OAuthRequestResult();
                result.Text = m_request.downloadHandler.text;
                result.IsSuccess = !m_request.isHttpError && !m_request.isNetworkError;
                result.StatusCode = m_request.responseCode;
                m_request = null;

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
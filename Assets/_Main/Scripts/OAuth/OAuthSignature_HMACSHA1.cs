using System.Collections.Generic;
using System;
using System.Text;
using System.Linq;
using System.Security.Cryptography;

namespace OAuth
{
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
}

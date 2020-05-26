using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OAuth;

public class TwitterTestController : MonoBehaviour
{
    [SerializeField]
    private InputField m_input;
    [SerializeField]
    private InputField m_tweetContent;
    [SerializeField]
    private Camera m_mediaCamera;

    private OAuthClient m_oauthClient = new OAuthClient("HFJomVRXS1to55cg6SCSgpc3d", "CFzneG6qbkPmuaGfR75CL7BhzYsYZ1ieS1VIiKYfeU10FxYcq0");
    private OAuthToken m_token = null;

    public void Login()
    {
        var url = "https://api.twitter.com/oauth/request_token?oauth_callback=oob";
        var req = new OAuthRequest(new OAuthRequestHttpClient(url, OAuthRequestMethod.GET), m_oauthClient, null, new OAuthSignature_HMACSHA1());
        req.SendWebRequest((result) =>
        {
            Debug.Log("OnSuccess!!");
            var parameters = System.Web.HttpUtility.ParseQueryString(result.Text);
            var oauthToken = parameters["oauth_token"];
            var oauthTokenSecret = parameters["oauth_token_secret"];
            m_token = new OAuthToken(oauthToken, oauthTokenSecret);

            var authenticateUrl = $"https://api.twitter.com/oauth/authenticate?oauth_token={oauthToken}";
            Application.OpenURL(authenticateUrl);
        },
        (result) =>
        {
            Debug.LogError(result.Text);
        });
    }

    public void OnEnterPIN()
    {
        var url = "https://api.twitter.com/oauth/access_token";
        var pin = m_input.text;
        var body = new Dictionary<string, string>();
        body.Add("oauth_verifier", pin);
        var req = new OAuthRequest(new OAuthRequestHttpClient(url, OAuthRequestMethod.POST), m_oauthClient, m_token, new OAuthSignature_HMACSHA1(), body);
        req.SendWebRequest((result) =>
        {
            Debug.Log("AccessToken Success!!");
            Debug.Log("Result:" + result.Text);
            Debug.Log(result.Text);
            var parameters = System.Web.HttpUtility.ParseQueryString(result.Text);
            var oauthToken = parameters["oauth_token"];
            var oauthTokenSecret = parameters["oauth_token_secret"];
            m_token = new OAuthToken(oauthToken, oauthTokenSecret);

        }, (result) =>
        {
            Debug.LogError(result.Text);
        });
    }

    public void SubmitTweet()
    {
        var url = "https://api.twitter.com/1.1/statuses/update.json";
        var tweet = m_tweetContent.text;
        var body = new Dictionary<string, string>();
        m_token = new OAuthToken("1154717183953346562-SujLIxSWkAwMVBZswz7rzPPRt5iV5E", "MwvUMicjOxwi9EQnjuvtmksQs8Gab3HRyVVqVALSxeKQX");
        body.Add("status", tweet);
        var req = new OAuthRequest(new OAuthRequestHttpClient(url, OAuthRequestMethod.POST), m_oauthClient, m_token, new OAuthSignature_HMACSHA1(), body);
        req.SendWebRequest((result) =>
        {
            Debug.LogError(result.Text);
        }, (result) =>
        {
            Debug.LogError(result.Text);
        });
    }

    // {"media_id":1265180818491633665,"media_id_string":"1265180818491633665","size":9481,"expires_after_secs":86400,"image":{"image_type":"image\/jpeg","w":256,"h":256}}
    public void UploadMedia()
    {
        var url = "https://upload.twitter.com/1.1/media/upload.json";
        var body = new Dictionary<string, string>();
        m_token = new OAuthToken("1154717183953346562-SujLIxSWkAwMVBZswz7rzPPRt5iV5E", "MwvUMicjOxwi9EQnjuvtmksQs8Gab3HRyVVqVALSxeKQX");

        var renderTexture = m_mediaCamera.targetTexture;
        var tex = new Texture2D(renderTexture.width, renderTexture.height);

        RenderTexture.active = renderTexture;
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex.Apply();
        var jpg = tex.EncodeToJPG();
        var jpgData = System.Convert.ToBase64String(jpg);

        body.Add("media_data", jpgData);
        var req = new OAuthRequest(new OAuthRequestHttpClient(url, OAuthRequestMethod.POST), m_oauthClient, m_token, new OAuthSignature_HMACSHA1(), body);
        req.SendWebRequest((result) =>
        {
            Debug.LogError(result.Text);
        }, (result) =>
        {
            Debug.LogError(result.Text);
        });
    }
}

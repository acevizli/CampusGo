using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.SceneManagement;
using Assets.Scripts;
using System.IO;

public class AuthManager : MonoBehaviour
{
    public TMP_InputField LoginUsername;
    public TMP_InputField LoginPass;
    public Button LoginButton;
    public Button SignupButton;

    public UserData User;
    public static AuthManager Instance;

    private void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(LoginDirectly());
    }
    IEnumerator LoginDirectly()
    {
        yield return new WaitForEndOfFrame();
        if (File.Exists(Application.persistentDataPath + "/uganda.save"))
        {
            Toast.Instance.TextShow("Logging in");
            yield return new WaitForSeconds(1f);
            var bytes = File.ReadAllBytes(Application.persistentDataPath + "/uganda.save");
            var save = JsonUtility.FromJson<Save>(Encoding.UTF8.GetString(bytes));
            var request = UnityWebRequest.Get("http://159.20.87.203:8000/api/directlogin");
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + save.User.token);
            User = save.User;

            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                SceneManager.LoadScene("ArScene");
            }
            else
            {
                var json = Encoding.UTF8.GetString(request.downloadHandler.data);
                Toast.Instance.TextShow("Could not login: " + json);
            }
        }
        yield return null;
    }
    // Start is called before the first frame update
    IEnumerator Post(string url, string bodyJsonString)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if(request.result == UnityWebRequest.Result.Success)
        {
            var json = Encoding.UTF8.GetString(request.downloadHandler.data);
            User = JsonUtility.FromJson<UserData>(json);
            Toast.Instance.TextShow("Succesfull");
            SceneManager.LoadScene("ArScene");
        }
        else
        {
            var json = Encoding.UTF8.GetString(request.downloadHandler.data);
            Toast.Instance.TextShow("Could not login: "+json);
        }
    }
    public bool Validate()
    {
        if (string.IsNullOrEmpty(LoginUsername.text) || string.IsNullOrEmpty(LoginPass.text)) return false;
        return true;
    }
    public void Login()
    {
        if (!Validate())
        {
            Toast.Instance.TextShow("Please fill fields");
            return;
        }
        UserData user = new UserData
        {
            username = LoginUsername.text,
            password = LoginPass.text,
        };
        string json = JsonUtility.ToJson(user);
        StartCoroutine(Post("http://159.20.87.203:8000/api/login", json));
    }

    public void Signup()
    {
        if (!Validate())
        {
            Toast.Instance.TextShow("Please fill fields");
            return;
        }
        UserData user = new UserData
        {
            username = LoginUsername.text,
            password = LoginPass.text,
        };
        string json = JsonUtility.ToJson(user, true);
        StartCoroutine(Post("http://159.20.87.203:8000/api/register", json));
    }
}

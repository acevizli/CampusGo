using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;



[RequireComponent(typeof(ARAnchorManager))]
[RequireComponent(typeof(ARRaycastManager))]
[RequireComponent(typeof(ARPlaneManager))]
public class AnchorCreator : MonoBehaviour
{
    [SerializeField]
    GameObject m_AnchorPrefab;
    [SerializeField]
    Camera ArCamera;
    string persistentPath;
    Sprite sprite;

    public GameObject AnchorPrefab
    {
        get => m_AnchorPrefab;
        set => m_AnchorPrefab = value;
    }

    public void RemoveAllAnchors()
    {
        foreach (var anchor in m_AnchorPoints)
        {
            Destroy(anchor);
        }
        m_AnchorPoints.Clear();
        foreach (var image in Images)
        {
            Destroy(image);
        }
        Images.Clear();
    }


    public void ShowMediaPicker()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            var texture = NativeGallery.LoadImageAtPath(path);
            if (texture == null)
            {
                Toast.Instance.TextShow("image is null");
                return;
            }
            
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            var spriteRenderer = m_AnchorPrefab.transform.Find("myAnchor").GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            var save = new Save();
            save.User = AuthManager.Instance.User;
            save.anchorPath = path;
            string json = JsonUtility.ToJson(save, true);
            File.WriteAllBytes(persistentPath, Encoding.UTF8.GetBytes(json));
            StartCoroutine(UploadImage());
        });

    }

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
        m_AnchorManager = GetComponent<ARAnchorManager>();
        m_PlaneManager = GetComponent<ARPlaneManager>();
        m_AnchorPoints = new List<ARAnchor>();
        Images = new List<GameObject>();
        persistentPath = Application.persistentDataPath + "/uganda.save";
    }
    private void Start()
    {
        if(!File.Exists(persistentPath))
        {
            ShowMediaPicker();
        }
        else
        {
            var bytes = File.ReadAllBytes(persistentPath);
            var save = JsonUtility.FromJson<Save>(Encoding.UTF8.GetString(bytes));
            var texture = NativeGallery.LoadImageAtPath(save.anchorPath);
            if (texture is null)
            {
                ShowMediaPicker();
            }
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            m_AnchorPrefab.transform.Find("myAnchor").GetComponent<SpriteRenderer>().sprite = sprite;
            StartCoroutine(GetDatabaseAnchors());
        }
    }
    void Update()
    {
        if (Input.touchCount == 0)
            return;

        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began || touch.type != TouchType.Direct)
            return;
        if (m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            var hitPose = s_Hits[0].pose;
            var hitTrackableId = s_Hits[0].trackableId;
            var hitPlane = m_PlaneManager.GetPlane(hitTrackableId);

            var anchor = m_AnchorManager.AttachAnchor(hitPlane, hitPose);
            var Image = Instantiate(m_AnchorPrefab, anchor.transform);

            if (anchor == null)
            {
                Toast.Instance.TextShow("error on creating anchor");
            }
            else
            {
                m_AnchorPoints.Add(anchor);
                Images.Add(Image);

                StartCoroutine(SendToDatabase());
            }
        }
    }

    public IEnumerator GetDatabaseAnchors()
    {
        
        var location = LocationService.Instance;
        int tryCount = 10;
        while (!location.retrieved && tryCount != 0)
        {
            tryCount--;
            yield return new WaitForSeconds(1);
        }
        if (tryCount != 0)
        {
            LocationData data = new LocationData
            {
                latitude = location.latitude,
                longitude = location.longitude,
            };
            string json = JsonUtility.ToJson(data, true);
            var request = new UnityWebRequest("http://159.20.87.203:8000/api/locations/", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + AuthManager.Instance.User.token);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                var json2 = Encoding.UTF8.GetString(request.downloadHandler.data);
                LocationDataHolder Locations = new LocationDataHolder();
                Locations = JsonUtility.FromJson<LocationDataHolder>(json2);
                var list = Locations.List;
                for (int i = 0; i < list.Count; i++)
                {
                    yield return new WaitUntil(() => CanPlaceAnchor());
                    Toast.Instance.TextShow("raycasted" + list[i].imagename);
                    var hitPose = s_Hits[0].pose;
                    var hitTrackableId = s_Hits[0].trackableId;
                    var hitPlane = m_PlaneManager.GetPlane(hitTrackableId);
                    var anchor = m_AnchorManager.AttachAnchor(hitPlane, hitPose);
                    yield return StartCoroutine(DownloadImage(list[i].imagename,anchor));
                }
                m_AnchorPrefab.transform.Find("myAnchor").GetComponent<SpriteRenderer>().sprite = sprite;
            }
            else
            {
                Toast.Instance.TextShow("Connection Failure");
            }
        }
        yield return null;
    }
    bool CanPlaceAnchor()
    {
        Vector2 screenPos = ArCamera.ViewportToScreenPoint(new Vector2(0.5f + UnityEngine.Random.Range(-0.5f, 0.5f), 0.5f + UnityEngine.Random.Range(-0.5f, 0.5f)));
        return m_RaycastManager.Raycast(screenPos, s_Hits, TrackableType.Planes);
    }
     IEnumerator SendToDatabase()
    {
        var location = LocationService.Instance;
        int tryCount = 20;
        while (!location.retrieved && tryCount != 0)
        {
            tryCount--;
            yield return new WaitForEndOfFrame();
        }
        if(tryCount != 0)
        {
            var bytes = File.ReadAllBytes(persistentPath);
            var save = JsonUtility.FromJson<Save>(Encoding.UTF8.GetString(bytes));
            var list = save.anchorPath.Split('/');
            var name = list[list.Length - 1];
            LocationData data = new LocationData
            {
                latitude = location.latitude,
                longitude = location.longitude,
                accuracy = location.accuracy,
                imagename = name
            };
            string json = JsonUtility.ToJson(data, true);
            var request = new UnityWebRequest("http://159.20.87.203:8000/api/location/", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer "+AuthManager.Instance.User.token);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Toast.Instance.TextShow("Added");
            }
            else
            {
                Toast.Instance.TextShow("Failure");
            }
        }
        yield return null;
    }

    IEnumerator UploadImage()
    {
        var bytes = File.ReadAllBytes(persistentPath);
        var save = JsonUtility.FromJson<Save>(Encoding.UTF8.GetString(bytes));
        string path = save.anchorPath;
        string[] list = path.Split('/');
        var imagename = save.User.username + list[list.Length - 1];
        UnityWebRequest request = UnityWebRequest.Get("http://159.20.87.203:8000/api/image/"+imagename+"/");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + AuthManager.Instance.User.token);
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {

            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            var texture = NativeGallery.LoadImageAtPath(save.anchorPath,markTextureNonReadable:false);
            formData.Add(new MultipartFormFileSection("image",texture.EncodeToPNG(),imagename,"image/png"));
            formData.Add(new MultipartFormDataSection("name", imagename));
            byte[] boundary = UnityWebRequest.GenerateBoundary();
            byte[] formSections = UnityWebRequest.SerializeFormSections(formData, boundary);
            // my termination string consisting of CRLF--{boundary}--
            byte[] terminate = Encoding.UTF8.GetBytes(String.Concat("\r\n--", Encoding.UTF8.GetString(boundary), "--"));
            // Make my complete body from the two byte arrays
            byte[] body = new byte[formSections.Length + terminate.Length];
            Buffer.BlockCopy(formSections, 0, body, 0, formSections.Length);
            Buffer.BlockCopy(terminate, 0, body, formSections.Length, terminate.Length);
            // Set the content type - NO QUOTES around the boundary
            string contentType = String.Concat("multipart/form-data; boundary=", Encoding.UTF8.GetString(boundary));
            // Make my request object and add the raw body. Set anything else you need here
            UnityWebRequest wr = new UnityWebRequest("http://159.20.87.203:8000/api/upload/", "POST");
            UploadHandler uploader = new UploadHandlerRaw(body);
            uploader.contentType = contentType;
            wr.uploadHandler = uploader;
            wr.SetRequestHeader("Authorization", "Bearer " + AuthManager.Instance.User.token);
            yield return wr.SendWebRequest();

            if (wr.result != UnityWebRequest.Result.Success)
            {
                Toast.Instance.TextShow("Image cannot uploaded");
            }
            else
            {
                Toast.Instance.TextShow("Image uploaded!");
            }

        }
    }

    IEnumerator DownloadImage(string MediaUrl,ARAnchor anchor)
    {
        Toast.Instance.TextShow("Image Download");
        UnityWebRequest request = UnityWebRequestTexture.GetTexture("http://159.20.87.203:8000/images/" + MediaUrl);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            var texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            m_AnchorPrefab.transform.Find("myAnchor").GetComponent<SpriteRenderer>().sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        var Image = Instantiate(m_AnchorPrefab, anchor.transform);

        if (anchor == null)
        {
            Toast.Instance.TextShow("error on creating anchor");
        }
        else
        {
            m_AnchorPoints.Add(anchor);
            Images.Add(Image);
        }
    }
    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    List<ARAnchor> m_AnchorPoints;
    List<GameObject> Images;

    ARRaycastManager m_RaycastManager;

    ARAnchorManager m_AnchorManager;

    ARPlaneManager m_PlaneManager;

    [System.Serializable]
    public class LocationData
    {
        public float latitude;
        public float longitude;
        public float accuracy;
        public string imagename;
        public int id;
        public int user;
    }
    [System.Serializable]
    public class LocationDataHolder
    {
        public List<LocationData> List;
    }
}

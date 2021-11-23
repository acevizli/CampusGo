using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
public class LocationService : MonoBehaviour
{
    public float latitude;
    public float longitude;
    public float accuracy;

    public bool retrieved = false;
    public static LocationService Instance;
    public void Start()
    {
#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
#endif
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(getLocations());
    }
    public IEnumerator getLocations()
    {
        // Check if the user has location service enabled.
        if (!Input.location.isEnabledByUser)
            yield break;

        // Starts the location service.
        Input.location.Start();

        // Waits until the location service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // If the service didn't initialize in 20 seconds this cancels location service use.
        if (maxWait < 1)
        {
            yield break;
        }

        // If the connection failed this cancels location service use.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            yield break;
        }
        else
        {
            // If the connection succeeded, this retrieves the device's current location and displays it in the Console window.
            latitude = Input.location.lastData.latitude;
            longitude = Input.location.lastData.longitude;
            accuracy = Input.location.lastData.horizontalAccuracy;
            retrieved = true;
        }

        // Stops the location service if there is no need to query location updates continuously.
        Input.location.Stop();
    }
}
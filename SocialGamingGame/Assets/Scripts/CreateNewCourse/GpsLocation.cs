using System;
using System.Collections;
#if !UNITY_SERVER
using CesiumForUnity;
#endif
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Android;

namespace CreateNewCourse
{
    public class GpsLocation : MonoBehaviour
    {
        #if !UNITY_SERVER
        
        public static bool Finished = false;
        public static double3 Location;
        [SerializeField]private Cesium3DTileset tileset;
        [SerializeField] private CesiumGeoreference georeference;

        private void Start()
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);
            }

        }

        /// <summary>
        /// Gets real life location and saves it in the location variable. Also sets Finished Flag.
        /// </summary>
        /// <param name="desiredAccuracyInMeters"></param>
        /// <param name="desiredDistanceInMeters"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IEnumerator GetRealLifeLocation(float desiredAccuracyInMeters, float desiredDistanceInMeters)
        {
            #if UNITY_EDITOR
            Location = new double3(georeference.latitude, georeference.longitude, georeference.height);
            Finished = true;
            yield break;
            #endif
            Finished = false;
            if (!Input.location.isEnabledByUser)
            {
                
                Location = new double3(georeference.latitude, georeference.longitude, georeference.height);
                Finished = true;
                Debug.LogError("GPS is not enabled");
            }
            
            Input.location.Start(desiredAccuracyInMeters, desiredDistanceInMeters);
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }
            
            if (maxWait < 1)
            {
                throw new System.Exception("Timed out");
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                throw new System.Exception("Unable to determine device location");
            }
            
            double3 latLongHeight = new double3(Input.location.lastData.latitude, Input.location.lastData.longitude, Input.location.lastData.altitude);
            var result = tileset.SampleHeightMostDetailed(latLongHeight.x, latLongHeight.y);
            Location = latLongHeight;
            georeference.latitude = latLongHeight.x;
            georeference.longitude = latLongHeight.y;
            georeference.height = latLongHeight.z;
            Input.location.Stop();
            Finished = true;
        }

        #endif
    }
    
    
}

using Unity.Netcode;
using UnityEngine;

namespace Networking
{
    public class PlayerSetupNetworking : NetworkBehaviour
    {
        [SerializeField] private Object[] objectsToDestroy;
        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                foreach (Object obj in objectsToDestroy )
                {
                    if(obj is GameObject go)
                    {
                        go.SetActive(false);
                    }else if (obj is Behaviour co)
                    {
                        co.enabled = false;
                    }else if (obj is Rigidbody rb)
                    {
                        rb.isKinematic = true;
                    }
                }
            } else //is owner
            {
                Debug.Log("Spawned");
            }
            Destroy(this);
        }
    }
}

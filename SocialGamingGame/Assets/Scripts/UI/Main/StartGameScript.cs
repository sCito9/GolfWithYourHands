using System.Collections.Generic;
using DataBackend;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace UI.Main
{
    public class StartGameScript : MonoBehaviour
    {
        [SerializeField] private Transform location;
        [SerializeField] private GameObject coursePrefab;

        private void Start()
        {
            UpdateCourses();
        }

        public void OnEnable()
        {
            SessionManager.Instance.OnRefresh += UpdateCourses;
        }
        
        public void OnDisable()
        {
            SessionManager.Instance.OnRefresh -= UpdateCourses;
        }
        
        private async void ListLobbies()
        {
            try
            {
                
                
                QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
                {
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    }
                };
                QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

                foreach (Lobby lobby in queryResponse.Results)
                {
                    var obj = Instantiate(coursePrefab, location);
                    obj.GetComponent<CourseExisting>().SetGame(lobby, lobby.Name);
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
        
        public void UpdateCourses()
        {
            // Clear course list
            foreach (Transform child in location)
                Destroy(child.gameObject);
            
            ListLobbies();
        }
    }
}

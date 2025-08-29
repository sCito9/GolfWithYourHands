
using DataBackend;
using EventArgs;
using R3;
using Sensoren;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

#if !UNITY_SERVER
using CesiumForUnity;
#endif

namespace UI.Club
{
    public class ClubBattleSceneManager : MonoBehaviour
    {
        #if !UNITY_SERVER
        [Header("References")]
        [SerializeField] private GolfHoleSensor holeSensor;
        [SerializeField] private GolfAbschlagController abschlagController;
        
        [Space] 
        [SerializeField] private CesiumGeoreference cesium;
        

        [Space] 
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject finishObject;

        [Space]
        [SerializeField] private TMP_Text currentStrokeCountText;
        [SerializeField] private TMP_Text previousStrokeCountText;
        
        
        
        /*
         * hidden variables
         */
        private int _strokes;

        private void Awake()
        {
            player.transform.position = SessionManager.Instance.currentCourse.startPosition;
            finishObject.transform.position = SessionManager.Instance.currentCourse.endPosition;
            _strokes = 0;
        }

        private void Start()
        {
            // Get previous best stroke count
            int previousStrokeCount;
            if (SessionManager.Instance.ClubBattle.club1Id == SessionManager.Instance.Club.id)
            {
                previousStrokeCount = SessionManager.Instance.ClubBattle.club1Scores.Find(entry => entry.userId == SessionManager.Instance.User.id).score[SessionManager.Instance.currentClubBattleCourseIndex];
            }
            else
            {
                previousStrokeCount = SessionManager.Instance.ClubBattle.club2Scores.Find(entry => entry.userId == SessionManager.Instance.User.id).score[SessionManager.Instance.currentClubBattleCourseIndex];
            }
            previousStrokeCountText.text = "Score to beat:\n" + previousStrokeCount.ToString();

            double3 location = SessionManager.Instance.currentCourse.mapOrigin;
            cesium.SetOriginLongitudeLatitudeHeight(location.y, location.x, location.z);
            holeSensor.SensorTriggered.Subscribe(Finished).AddTo(this);
            
            abschlagController.AbschlagTriggeredStream
                .Where(args => args != null)
                .Subscribe(OnAbschlagTriggered)
                .AddTo(this);
            
            Observable.TimerFrame(1).Take(1)
                .Subscribe(_ => abschlagController.ForceFreeCam(false, true)).AddTo(this);
        }

        private void OnAbschlagTriggered(AbschlagControlEventArgs args)
        {
            _strokes++;
            currentStrokeCountText.text = "Strokes:\n" + _strokes.ToString();
        }

        private void Finished(System.EventArgs args)
        {
            Debug.Log("Finished");
            //Let club know the amount of strokes
            StartCoroutine(DataLoader.UpdateClubBattleScore(
                SessionManager.Instance.ClubBattle.id, SessionManager.Instance.User.id, SessionManager.Instance.currentClubBattleCourseIndex, _strokes,
                clubBattle => {
                    if (clubBattle == null)
                    {
                        Debug.Log("Failed to update club battle score.");
                        return;
                    }
                    SceneManager.LoadScene("ClubBattleMenu"); 
                }));
        }

        public void ExitScene()
        {
            SceneManager.LoadScene("ClubBattleMenu");
        }
        #endif
    }
}
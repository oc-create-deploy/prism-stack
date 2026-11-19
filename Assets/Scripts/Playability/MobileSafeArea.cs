using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MobileSafeArea : MonoBehaviour
{
    private Rect previous;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() { SceneManager.sceneLoaded += (_,__)=>Wrap(); Wrap(); }
    private static void Wrap()
    {
        foreach(Canvas canvas in Object.FindObjectsOfType<Canvas>())
        {
            if(canvas.GetComponentInChildren<PlayableGames.SafeAreaPanel>()!=null) continue;
            if(!canvas.isRootCanvas || canvas.renderMode==RenderMode.WorldSpace || canvas.GetComponentInChildren<MobileSafeArea>()!=null) continue;
            var safe=new GameObject("Mobile safe area",typeof(RectTransform),typeof(MobileSafeArea));
            safe.transform.SetParent(canvas.transform,false);
            var children=new System.Collections.Generic.List<Transform>();
            foreach(Transform child in canvas.transform) if(child!=safe.transform) children.Add(child);
            foreach(Transform child in children) child.SetParent(safe.transform,false);
            safe.GetComponent<MobileSafeArea>().Apply();
        }
    }
    private void LateUpdate() { if(previous!=Screen.safeArea) Apply(); }
    private void Apply()
    {
        previous=Screen.safeArea;var r=(RectTransform)transform;
        r.anchorMin=new Vector2(previous.xMin/Screen.width,previous.yMin/Screen.height);
        r.anchorMax=new Vector2(previous.xMax/Screen.width,previous.yMax/Screen.height);
        r.offsetMin=r.offsetMax=Vector2.zero;
    }
}

using UnityEngine;
namespace PlayableGames
{
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        private Rect previous;
        private void LateUpdate()
        {
            Rect safe=Screen.safeArea; if(safe==previous) return; previous=safe;
            RectTransform r=(RectTransform)transform;
            r.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);
            r.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
            r.offsetMin=r.offsetMax=Vector2.zero;
        }
    }
}

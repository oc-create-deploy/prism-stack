using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PlayableGames
{
    public static class NativeUi
    {
        public static Font Font
        {
            get {
                var bundled = Resources.Load<Font>("PrismSans");
                if(bundled != null) return bundled;
#if UNITY_2022_2_OR_NEWER
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            }
        }
        public static RectTransform Create(string name, bool opaque=true)
        {
            var go=new GameObject(name,typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(430,932);scaler.matchWidthOrHeight=.5f;
            if(opaque) { var image=go.AddComponent<Image>();image.color=new Color(.025f,.055f,.105f);image.raycastTarget=false; }
            if(EventSystem.current==null) new GameObject("UI Events",typeof(EventSystem),typeof(StandaloneInputModule));
            var safe=new GameObject("Safe content",typeof(RectTransform),typeof(SafeAreaPanel));safe.transform.SetParent(go.transform,false);
            var r=(RectTransform)safe.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            return r;
        }
        public static RectTransform Place(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;
            r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        public static Text Label(Transform parent,string value,float x,float y,float w,float h,int size=22)
        {
            var r=Place(parent,value,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.text=value;t.font=Font;t.fontSize=size;
            t.resizeTextForBestFit=true;t.resizeTextMinSize=12;t.resizeTextMaxSize=size;t.color=Color.white;
            t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;
        }
        public static Button Button(Transform parent,string value,float x,float y,float w,float h,Action action)
        {
            var r=Place(parent,value,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.10f,.35f,.52f);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>action());
            var t=Label(r,value,.03f,.08f,.94f,.84f,24);t.fontStyle=FontStyle.Bold;return b;
        }
    }
}

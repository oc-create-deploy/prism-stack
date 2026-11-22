using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using PlayableGames;

public static class PrismPresentation
{
    private static TMP_FontAsset runtimeFont;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() { SceneManager.sceneLoaded += (_,__)=>Repair(); Repair(); }
    private static void Repair()
    {
        if(runtimeFont==null) runtimeFont=Resources.Load<TMP_FontAsset>("PrismFont");
        if(runtimeFont==null) throw new System.InvalidOperationException("Bundled PrismFont missing from build");
        foreach(TMP_Text t in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if(!t.gameObject.scene.IsValid()) continue;
            t.font=runtimeFont;t.fontSharedMaterial=runtimeFont.material;t.ForceMeshUpdate();
        }
        if(SceneManager.GetActiveScene().name!="Menu")
        {
            var grid=Object.FindObjectOfType<BlockLineClearer>();
            var camera=Camera.main;
            if(grid!=null && camera!=null)
            {
                float halfWidth=(grid.cols-1)*grid.step*.5f+grid.step;
                if(camera.orthographic) camera.orthographicSize=Mathf.Max(camera.orthographicSize,halfWidth/camera.aspect);
                var position=camera.transform.position;
                position.x=grid.startX+(grid.cols-1)*grid.step*.5f;
                if(!camera.orthographic) position.z=-Mathf.Max(Mathf.Abs(position.z),halfWidth/(Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)*camera.aspect));
                camera.transform.position=position;
            }
            var controller=Object.FindObjectOfType<GameController>();
            if(controller!=null && controller.infoText!=null)
            {
                var rect=controller.infoText.rectTransform;
                rect.anchorMin=new Vector2(.15f,.90f);rect.anchorMax=new Vector2(.85f,.95f);
                rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
            }
            if(Object.FindObjectOfType<PrismJewelWorld>()==null) new GameObject("Prism jewel world").AddComponent<PrismJewelWorld>();
            return;
        }
        foreach(Canvas canvas in Object.FindObjectsOfType<Canvas>()) canvas.gameObject.SetActive(false);
        var root=NativeUi.Create("Prism Stack menu");
        for(int i=0;i<3;i++) {
            var gem=NativeUi.Place(root,"Logo-inspired jewel",.32f+i*.12f,.86f+(i==1?.025f:0),.11f,.05f);
            var art=gem.gameObject.AddComponent<UnityEngine.UI.Image>();art.sprite=PrismJewelWorld.Jewel;art.color=PrismJewelWorld.Palette[i];art.preserveAspect=true;art.raycastTarget=false;
        }
        NativeUi.Label(root,"PRISM STACK",.08f,.76f,.84f,.09f,38);
        NativeUi.Label(root,"A journey through light and color.",.08f,.68f,.84f,.055f,20);
        NativeUi.Label(root,"LEVEL "+PrismJewelWorld.Level+"  ·  "+PrismJewelWorld.Realm+"\nBEST  "+PlayerPrefs.GetInt("RecordPoints",0),.1f,.57f,.8f,.06f,22);
        NativeUi.Button(root,"PLAY",.16f,.41f,.68f,.095f,()=>{
            PlayerPrefs.SetInt("Points",0);PlayerPrefs.SetInt("AlreadyRevive",0);PlayerPrefs.Save();SceneManager.LoadScene("Game");
        });
        var help=NativeUi.Label(root,"Drag pieces onto the grid. Complete rows or columns to clear space. The round ends when no piece fits.",.12f,.15f,.76f,.16f,21);
        help.gameObject.SetActive(false);
        NativeUi.Button(root,"HOW TO PLAY",.16f,.30f,.68f,.075f,()=>help.gameObject.SetActive(!help.gameObject.activeSelf));
    }
}

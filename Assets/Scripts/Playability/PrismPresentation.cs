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
        if(SceneManager.GetActiveScene().name!="Menu") return;
        foreach(Canvas canvas in Object.FindObjectsOfType<Canvas>()) canvas.gameObject.SetActive(false);
        var root=NativeUi.Create("Prism Stack menu");
        NativeUi.Label(root,"PRISM STACK",.08f,.76f,.84f,.09f,38);
        NativeUi.Label(root,"Find your next perfect fit.",.08f,.68f,.84f,.055f,20);
        NativeUi.Label(root,"BEST SCORE  "+PlayerPrefs.GetInt("RecordPoints",0),.1f,.57f,.8f,.06f,22);
        NativeUi.Button(root,"PLAY",.16f,.41f,.68f,.095f,()=>{
            PlayerPrefs.SetInt("Points",0);PlayerPrefs.SetInt("AlreadyRevive",0);PlayerPrefs.Save();SceneManager.LoadScene("Game");
        });
        var help=NativeUi.Label(root,"Drag pieces onto the grid. Complete rows or columns to clear space. The round ends when no piece fits.",.12f,.15f,.76f,.16f,21);
        help.gameObject.SetActive(false);
        NativeUi.Button(root,"HOW TO PLAY",.16f,.30f,.68f,.075f,()=>help.gameObject.SetActive(!help.gameObject.activeSelf));
    }
}

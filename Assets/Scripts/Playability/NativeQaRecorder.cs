using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Read-only simulator diagnostics. Never plays, wins or changes the game's model.
public sealed class NativeQaRecorder : MonoBehaviour
{
    [Serializable] public class Value {public string key,value;}
    [Serializable] public class Item
    {
        public string kind,name,text;
        public int id,group;public float x,y;public bool interactable;
        public List<string> colors=new List<string>();public List<Vector2> shape=new List<Vector2>();
    }
    [Serializable] public class Snapshot
    {
        public string scene;public int frame,width,height;
        public float safeX,safeY,safeW,safeH;
        public List<Value> values=new List<Value>();public List<Item> items=new List<Item>();public List<string> errors=new List<string>();
    }
    private static NativeQaRecorder instance;
    private readonly List<string> errors=new List<string>();
    private float next;
    private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if(instance!=null || !Environment.GetCommandLineArgs().Any(x=>x.Contains("playabilityQA") || x.Contains("playability-qa"))) return;
        instance=new GameObject("Read-only QA recorder").AddComponent<NativeQaRecorder>();DontDestroyOnLoad(instance.gameObject);
        Application.logMessageReceived += instance.Log;
    }
    private void Log(string message,string stack,LogType type)
    {
        if((type==LogType.Exception || type==LogType.Error || type==LogType.Assert) && errors.Count<30) errors.Add(type+": "+message+"\n"+stack);
    }
    private void OnDestroy() {Application.logMessageReceived-=Log;}
    private static object Read(object target,string key)
    {
        if(target==null) return null;Type type=target.GetType();FieldInfo f=type.GetField(key,Flags);
        if(f!=null) return f.GetValue(target);PropertyInfo p=type.GetProperty(key,Flags);return p!=null && p.GetIndexParameters().Length==0 ? p.GetValue(target,null) : null;
    }
    private static string Describe(object value)
    {
        if(value==null) return "null";if(value is UnityEngine.Object obj) return obj!=null ? "present" : "null";
        Type type=value.GetType();return type.IsPrimitive || type.IsEnum || value is string || value is decimal ? value.ToString() : JsonUtility.ToJson(value);
    }
    private static Vector2 ScreenPosition(Component component)
    {
        var rect=component.transform as RectTransform;
        if(rect!=null) return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        return Camera.main!=null ? (Vector2)Camera.main.WorldToScreenPoint(component.transform.position) : Vector2.zero;
    }
    private Item Entry(Component c,string kind)
    {
        Vector2 point=ScreenPosition(c);
        return new Item{kind=kind,name=c.name,x=point.x/Screen.width,y=1-point.y/Screen.height,interactable=true};
    }
    private void LateUpdate()
    {
        if(Time.unscaledTime<next) return;next=Time.unscaledTime+.35f;
        try
        {
            Rect safe=Screen.safeArea;
            var state=new Snapshot{scene=SceneManager.GetActiveScene().name,frame=Time.frameCount,width=Screen.width,height=Screen.height,safeX=safe.x,safeY=safe.y,safeW=safe.width,safeH=safe.height,errors=new List<string>(errors)};
            string[] fields={"Score","Coins","IsPlaying","Remaining","TrayCount","Won","Lost","coins","workers","factories","prestige","best","lane","distance","state","points","recordPoints","playerCount","currentLevel","carryingItem"};
            string[] models={"MetroDashGame","EmpireIdleGame","MatchscapeGame","GameManager","GameController","GameFlowManager","LevelController","PlayerManager"};
            foreach(MonoBehaviour c in FindObjectsOfType<MonoBehaviour>())
            {
                if(c==null || !c.isActiveAndEnabled) continue;string type=c.GetType().Name;
                if(models.Contains(type)) foreach(string key in fields)
                {
                    try {object value=Read(c,key);if(value!=null) state.values.Add(new Value{key=type+"."+key,value=Describe(value)});}catch(Exception){}
                }
                bool hidden=c.GetComponentsInParent<CanvasGroup>().Any(g=>g.alpha<.1f);
                if(c is Button b && !hidden)
                {
                    var item=Entry(c,"button");item.interactable=b.IsInteractable();
                    item.text=string.Join(" ",b.GetComponentsInChildren<Component>().Where(x=>x!=null && x.GetType().GetProperty("text")!=null).Select(x=>x.GetType().GetProperty("text").GetValue(x,null) as string));state.items.Add(item);
                }
                if((c is Text || type.Contains("TextMeshPro")) && !hidden)
                {var item=Entry(c,"text");item.text=Read(c,"text") as string;state.items.Add(item);}
                if(type=="TubeController")
                {
                    var item=Entry(c,"tube");var collider=c.GetComponentInChildren<Collider2D>();
                    if(collider!=null && Camera.main!=null) {Vector3 pt=Camera.main.WorldToScreenPoint(collider.bounds.center);item.x=pt.x/Screen.width;item.y=1-pt.y/Screen.height;}
                    var colors=(IEnumerable)c.GetType().GetMethod("GetCurrentColors").Invoke(c,null);foreach(object color in colors) item.colors.Add(ColorUtility.ToHtmlStringRGBA((Color)color));state.items.Add(item);
                }
                if(type=="MatchscapePiece")
                {var item=Entry(c,"cube");item.id=(int)Read(c,"id");item.group=(int)Read(c,"group");state.items.Add(item);}
                if(type=="Slot")
                {var item=Entry(c,"slot");item.id=(int)Read(c,"id");item.text=Read(c,"state").ToString();object current=Read(c,"currentItem");item.group=current==null || (current is UnityEngine.Object o && o==null) ? -1 : Convert.ToInt32(Read(current,"id"));state.items.Add(item);}
                if(type=="LetterButton")
                {var item=Entry(c,"letter");item.text=Read(c,"ButtonChar").ToString();state.items.Add(item);}
                if(type=="DraggablePiece1" && !(bool)Read(c,"isPlaced"))
                {
                    var item=Entry(c,"piece");object builder=Read(c,"builder");if(builder!=null && Read(builder,"blockOffsets") is IEnumerable offsets)
                        foreach(object value in offsets) {var v=(Vector2Int)value;item.shape.Add(new Vector2(v.x,v.y));}
                    state.items.Add(item);
                }
                if(type=="BlockLineClearer" && Camera.main!=null)
                {
                    int rows=(int)Read(c,"rows"),cols=(int)Read(c,"cols");float sx=(float)Read(c,"startX"),sy=(float)Read(c,"startY"),step=(float)Read(c,"step");
                    for(int r=0;r<rows;r++) for(int col=0;col<cols;col++)
                    {
                        Vector3 world=new Vector3(sx+col*step,sy-r*step,0),point=Camera.main.WorldToScreenPoint(world);
                        bool occupied=Physics2D.OverlapPointAll(world).Any(x=>x.CompareTag("Block"));
                        state.items.Add(new Item{kind="cell",name=r+","+col,id=r*cols+col,group=occupied?1:0,x=point.x/Screen.width,y=1-point.y/Screen.height,interactable=true});
                    }
                }
            }
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type yg=assembly.GetType("YG.YG2");if(yg==null) continue;
                FieldInfo f=yg.GetField("saves",BindingFlags.Static|BindingFlags.Public);if(f!=null && f.GetValue(null)!=null) state.values.Add(new Value{key="YG2.saves",value=JsonUtility.ToJson(f.GetValue(null))});break;
            }
            string path=Path.Combine(Application.persistentDataPath,"qa-state.json");File.WriteAllText(path,JsonUtility.ToJson(state,true));
        }
        catch(Exception) { /* A missing probe fails host-side QA; never change gameplay to make a probe pass. */ }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PlayableGames;

// Original procedural jewel art and synthesized chimes; no external assets.
public class PrismJewelWorld : MonoBehaviour
{
    public static PrismJewelWorld Instance;
    public static readonly Color[] Palette = { new Color(.05f,.85f,1), new Color(.62f,.20f,1), new Color(1,.24f,.45f), new Color(1,.73f,.08f) };
    static Sprite jewel;
    AudioSource voice;
    AudioClip place, reject, clear, complete;
    Text goal, message;
    Image progress;
    int level, lines;
    float messageUntil;
    public static int Level => PlayerPrefs.GetInt("PrismLevel",1);
    public static int Target => 2 + Mathf.Min(Level-1,8);
    public static string Realm => new[]{"CYAN DAWN","VIOLET VAULT","CORAL GARDEN","GOLDEN SUMMIT"}[(Level-1)%4];

    public static Sprite Jewel
    {
        get {
            if(jewel!=null) return jewel;
            const int n=64; var tex=new Texture2D(n,n,TextureFormat.RGBA32,false);
            var pixels=new Color[n*n];
            for(int y=0;y<n;y++) for(int x=0;x<n;x++) {
                float u=(x+.5f)/n,v=(y+.5f)/n;
                float edge=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v));
                // Cut corners and bevels make each cell a readable faceted gem.
                bool outside=(u+v<.10f || u+1-v<.10f || 1-u+v<.10f || 2-u-v<.10f);
                float shade=edge<.075f ? (v>u?1f:.62f) : (v>u ? .93f : (u+v>1 ? .73f : .83f));
                if(Mathf.Abs(v-(.78f-.22f*u))<.018f && u>.15f && u<.85f) shade=1f;
                pixels[y*n+x]=outside?Color.clear:new Color(shade,shade,shade,1);
            }
            tex.SetPixels(pixels);tex.Apply();tex.filterMode=FilterMode.Bilinear;
            jewel=Sprite.Create(tex,new Rect(0,0,n,n),new Vector2(.5f,.5f),64);
            return jewel;
        }
    }
    public static void Style(GameObject block, int color)
    {
        var sr=block.GetComponent<SpriteRenderer>(); if(sr==null)return;
        // Preserve original world footprint, including the prefab's scale.
        var size=sr.sprite!=null ? sr.sprite.bounds.size : Vector3.one;
        sr.sprite=Jewel; sr.color=Palette[Mathf.Abs(color)%Palette.Length];
        block.transform.localScale=Vector3.Scale(block.transform.localScale,new Vector3(size.x,size.y,1));
    }
    void Awake()
    {
        Instance=this; level=Level; lines=PlayerPrefs.GetInt("PrismLevelLines",0);
        voice=gameObject.AddComponent<AudioSource>(); voice.playOnAwake=false; voice.spatialBlend=0;
        place=Tone("Gem place",440,.10f);reject=Tone("Soft return",180,.12f);
        clear=Tone("Crystal clear",880,.32f);complete=Tone("Level flourish",1320,.55f);
        var camera=Camera.main;if(camera!=null){camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.025f,.12f);}
        var hud=NativeUi.Create("Jewel journey",false);
        goal=NativeUi.Label(hud,"",.17f,.82f,.66f,.045f,18);
        message=NativeUi.Label(hud,"Drag a gem. Fill a row or column.",.08f,.08f,.84f,.05f,17);
        var rail=NativeUi.Place(hud,"Level progress",.2f,.805f,.6f,.006f).gameObject.AddComponent<Image>();rail.color=new Color(.12f,.15f,.30f);rail.raycastTarget=false;
        progress=NativeUi.Place(rail.transform,"Earned clears",0,0,1,1).gameObject.AddComponent<Image>();progress.color=Palette[(level-1)%4];progress.raycastTarget=false;
        var sound=NativeUi.Button(hud,Muted?"SOUND OFF":"SOUND ON",.70f,.015f,.27f,.045f,()=>{});
        sound.onClick.AddListener(()=>{PlayerPrefs.SetInt("PrismMuted",Muted?0:1);PlayerPrefs.Save();sound.GetComponentInChildren<Text>().text=Muted?"SOUND OFF":"SOUND ON";});
        Refresh();
        var grid=FindObjectOfType<BlockLineClearer>();
        if(grid!=null) for(int r=0;r<grid.rows;r++)for(int c=0;c<grid.cols;c++) {
            var tile=new GameObject("Inset jewel socket"); tile.transform.SetParent(transform);
            tile.transform.position=new Vector3(grid.startX+c*grid.step,grid.startY-r*grid.step,1);
            tile.transform.localScale=Vector3.one*grid.step*.94f;
            var sr=tile.AddComponent<SpriteRenderer>();sr.sprite=Jewel;sr.color=new Color(.075f,.095f,.23f);sr.sortingOrder=-20;
        }
    }
    bool Muted => PlayerPrefs.GetInt("PrismMuted",0)==1;
    void Play(AudioClip clip){if(!Muted)voice.PlayOneShot(clip,.24f);}
    static AudioClip Tone(string name,float hz,float duration)
    {
        int count=(int)(22050*duration);var data=new float[count];
        for(int i=0;i<count;i++){float t=i/22050f;float env=Mathf.Min(t/.008f,1)*Mathf.Pow(1-t/duration,2);data[i]=env*(Mathf.Sin(2*Mathf.PI*hz*t)+.25f*Mathf.Sin(2*Mathf.PI*hz*1.5f*t))*.45f;}
        var clip=AudioClip.Create(name,count,1,22050,false);clip.SetData(data,0);return clip;
    }
    void Refresh(){progress.color=Palette[(level-1)%4];goal.text="LEVEL "+level+"  ·  "+Realm+"  "+lines+" / "+Target+" LINES";progress.rectTransform.anchorMax=new Vector2(Mathf.Clamp01((float)lines/Target),1);}
    public void Placed(){Play(place);}
    public void Returned(){Play(reject);message.text="That space is occupied. Try another fit.";messageUntil=Time.unscaledTime+2;}
    public void Cleared(int count, System.Collections.Generic.HashSet<GameObject> blocks)
    {
        foreach(var b in blocks)if(b!=null){var sr=b.GetComponent<SpriteRenderer>();if(sr!=null)StartCoroutine(Spark(b.transform.position,sr.color));}
        lines+=count; bool advanced=false;
        while(lines>=Target){lines-=Target;level++;PlayerPrefs.SetInt("PrismLevel",level);advanced=true;}
        PlayerPrefs.SetInt("PrismLevelLines",lines);PlayerPrefs.Save();
        Play(advanced?complete:clear);message.text=advanced?"LEVEL COMPLETE  ·  "+Realm:count>1?count+" LINE COMBO!":"CRYSTAL CLEAR!";
        messageUntil=Time.unscaledTime+2.5f;Refresh();
    }
    IEnumerator Spark(Vector3 pos,Color color)
    {
        var go=new GameObject("Clear shimmer");go.transform.position=pos;
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=Jewel;sr.color=color;sr.sortingOrder=110;
        for(float t=0;t<.3f;t+=Time.deltaTime){float k=t/.3f;go.transform.localScale=Vector3.one*(1+k*.5f);sr.color=new Color(color.r,color.g,color.b,1-k);yield return null;}
        Destroy(go);
    }
    void Update(){if(messageUntil>0 && Time.unscaledTime>messageUntil){message.text="Fill rows or columns · match colors for more points";messageUntil=0;}}
    void OnDestroy(){if(Instance==this)Instance=null;foreach(var clip in new[]{place,reject,clear,complete})if(clip!=null)Destroy(clip);}
}

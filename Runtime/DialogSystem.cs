using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Audio;

public delegate void SoundHook(char s);//char containing last letter printed for 'speaking'
public delegate void ReadyEvent();

public class DialogSystem : MonoBehaviour
{
    //if in pc, get path info from this file and then try to open in streaming asssets.
    //need UnityEditor
    //Debug.Log(AssetDatabase.GetAssetPath(material));
    public TextAsset dialogFile;
    public float delayBetweenPages = 2f;
    public float delayBetweenLetters = 0.02f;
    public float delayBetweenFadeout = 2f;
    public bool leaveDialogVisibleAfterComplete = false;
    public bool showLastLineOnly = false;
    private static DialogFile dialog;

    public GameObject onscreenUI;
    public GameObject portraitContainer;
    public Image portraitImage;

    private Text txtmesh;
    public static DialogSystem ins;
    //is there a dialog currently running?
    public static bool activeDialog = false;
    private Dictionary<string, Sprite> portraitList;
    private static Callback completeCallback;
    public static SoundHook onCharacterType;
    public static ReadyEvent onLoaded;

    public AudioClip dialogSound;
    public AudioMixerGroup mixer;
    private AudioSource sfx;

    private void Awake()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.loop = false;
        sfx.playOnAwake = false;
        sfx.outputAudioMixerGroup = mixer;

        if (DialogSystem.ins != null)
        {
            Destroy(DialogSystem.ins);
        }
        else
        {
            DialogSystem.ins = this;
            DontDestroyOnLoad(gameObject);
        }

    }
    void Start()
    {
        //need to support streaming assets on desktop mode.
        if (!dialogFile) { Debug.LogError("Dialog System-Need a dialog file!"); }
        XElement dialogDoc = XDocument.Parse(dialogFile.text).Element("DialogFile");
        dialog = new DialogFile(dialogDoc, "");
        //NOTE: Textmeshpro does not handle bitmap fonts well. Thats why we use a UnityUI Text component
        txtmesh = onscreenUI.GetComponentInChildren<Text>();

        Sprite[] sprites = Resources.LoadAll<Sprite>(dialog.portraitFolder + "/");
        if(sprites.Length < 1) { Debug.LogError("Dialog System- cant load images: " + dialog.portraitFolder + "/. Does the folder have images imported as sprites?");  }
        portraitList = sprites.ToDictionary(x => x.name, x => x);
        if(onLoaded != null) { onLoaded.Invoke(); }
    }

    public void ResetSystem()
    {
        txtmesh.text = "";
        StopAllCoroutines();
        activeDialog = false;
    }

    public static bool EventExists(string id)
    {
        bool found = dialog.events.TryGetValue(id, out DialogEvent ev);
        return found;
    }

    public static void DialogEvent(string id, bool overrideExisting = false, Callback callback=null)//if override is true, cancels any current dialogs.
    {
        bool found = dialog.events.TryGetValue(id, out DialogEvent ev);
        ins.sfx.PlayOneShot(ins.dialogSound);
        if (overrideExisting) { ins.StopCoroutine("TypeDialog"); activeDialog = false; }
        if (!found) { Debug.LogError("Dialog System- Event not found:" + id); }
        else if(!activeDialog)
        {
            if(callback!= null) { completeCallback = callback; }
            ins.onscreenUI.SetActive(true);
            ins.StartCoroutine("TypeDialog", ev);
        }
    }

    private void setPortrait(string name)
    {
        if(name == null) { return; }
        bool found = portraitList.TryGetValue(name, out Sprite img);
        if (!found) { Debug.LogError("Dialog System- cant find image: " + dialog.portraitFolder + "/. Does the folder have images imported as sprites?"); return; }
        portraitImage.sprite = img;
        portraitContainer.SetActive(true);
    }

    IEnumerator TypeDialog(DialogEvent ev)
    {
        DialogSystem.activeDialog = true;
        
        setPortrait(ev.portrait);
        string fullText = "";
        for (int i = 0; i< ev.dialogPages.Length;i++)
        {
            txtmesh.text = "";
            string text = ev.dialogPages[i];
            string[] newPortrCheck = text.Split('>');
            if(newPortrCheck.Length > 1) 
            {
                text = newPortrCheck[1];
                setPortrait(newPortrCheck[0].Trim('<'));
            }
            //TODO: add option for WaitForSecondsRealtime if dialog/cutscene needs a full game pause.
            foreach (char c in text)
            {
                txtmesh.text += c;
                if(onCharacterType != null) { onCharacterType.Invoke(c); }
                yield return new WaitForSeconds(delayBetweenLetters);
            }
            if (showLastLineOnly) { fullText = txtmesh.text; }
            else { fullText += txtmesh.text + '\n'; }
            
            yield return new WaitForSeconds(delayBetweenPages);
        }


        yield return new WaitForSeconds(delayBetweenFadeout);
        txtmesh.text = "";
        DialogSystem.activeDialog = false ;
        if (!leaveDialogVisibleAfterComplete) {  portraitContainer.SetActive(false); onscreenUI.SetActive(false); }
        else { txtmesh.text = fullText; }
        if (completeCallback != null) { completeCallback.Invoke(""); }
        yield break;
    }

}

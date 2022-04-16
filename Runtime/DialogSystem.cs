using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DialogSystem : MonoBehaviour
{
    //if in pc, get path info from this file and then try to open in streaming asssets.
    //need UnityEditor
    //Debug.Log(AssetDatabase.GetAssetPath(material));
    public TextAsset dialogFile;
    public float delayBetweenPages = 2f;
    public float delayBetweenLetters = 0.02f;
    public float delayBetweenFadeout = 2f;
    private static DialogFile dialog;

    public GameObject onscreenUI;
    public GameObject portraitContainer;
    public Image portraitImage;

    private Text txtmesh;
    public static DialogSystem ins;
    //is there a dialog currently running?
    public static bool activeDialog = false;
    private Dictionary<string, Sprite> portraitList;

    private void Awake()
    {
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
        txtmesh = onscreenUI.GetComponent<Text>();

        Sprite[] sprites = Resources.LoadAll<Sprite>(dialog.portraitFolder + "/");
        if(sprites.Length < 1) { Debug.LogError("Dialog System- cant load images: " + dialog.portraitFolder + "/. Does the folder have images imported as sprites?");  }
        portraitList = sprites.ToDictionary(x => x.name, x => x);
    }

    public static void DialogEvent(string id, bool overrideExisting = false)//if override is true, cancels any current dialogs.
    {
        bool found = dialog.events.TryGetValue(id, out DialogEvent ev);
        if (overrideExisting) { ins.StopCoroutine("TypeDialog"); activeDialog = false; }
        if (!found) { Debug.LogError("Dialog System- Event not found"); }
        else if(!activeDialog)
        {
            ins.StartCoroutine("TypeDialog", ev);
        }
    }

    private void setPortrait(string name)
    {
        bool found = portraitList.TryGetValue(name, out Sprite img);
        if (!found) { Debug.LogError("Dialog System- cant find image: " + dialog.portraitFolder + "/. Does the folder have images imported as sprites?"); return; }
        portraitImage.sprite = img;
        portraitContainer.SetActive(true);
    }

    IEnumerator TypeDialog(DialogEvent ev)
    {
        DialogSystem.activeDialog = true;

        setPortrait(ev.portrait);

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
            
            foreach (char c in text)
            {
                txtmesh.text += c;
                yield return new WaitForSeconds(delayBetweenLetters);
            }
            yield return new WaitForSeconds(delayBetweenPages);
        }


        yield return new WaitForSeconds(delayBetweenFadeout);
        txtmesh.text = "";
        DialogSystem.activeDialog = false ;
        portraitContainer.SetActive(false);
        yield break;
    }

}

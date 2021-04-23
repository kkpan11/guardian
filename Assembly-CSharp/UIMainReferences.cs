using System.Collections;
using System.IO;
using UnityEngine;

public class UIMainReferences : MonoBehaviour
{
    public GameObject panelMain;
    public GameObject panelOption;
    public GameObject panelMultiROOM;
    public GameObject PanelMultiJoinPrivate;
    public GameObject PanelMultiWait;
    public GameObject PanelDisconnect;
    public GameObject panelMultiSet;
    public GameObject panelMultiStart;
    public GameObject panelCredits;
    public GameObject panelSingleSet;
    public GameObject PanelMultiPWD;
    public GameObject PanelSnapShot;
    private static bool IsFirstLaunch = true;
    public static string Version = "01042015";
    public static string FengVersion = "01042015";

    public static AssetBundle Ext;
    public static Texture2D AOT_2_LOGO;

    private void Start()
    {
        string rcBuild = "8/12/2015";

        NGUITools.SetActive(panelMain, state: true);
        GameObject.Find("VERSION").GetComponent<UILabel>().text = "[9999FF]RC " + rcBuild + "[-] | [0099FF]Guardian " + Guardian.Mod.Build;

        if (IsFirstLaunch)
        {
            IsFirstLaunch = false;

            Version = FengVersion;
            GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("InputManagerController"));
            gameObject.name = "InputManagerController";
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            FengGameManagerMKII.S = "verified343,hair,character_eye,glass,character_face,character_head,character_hand,character_body,character_arm,character_leg,character_chest,character_cape,character_brand,character_3dmg,r,character_blade_l,character_3dmg_gas_r,character_blade_r,3dmg_smoke,HORSE,hair,body_001,Cube,Plane_031,mikasa_asset,character_cap_,character_gun".Split(',');
            LoginFengKAI.LoginState = LoginState.LoggedOut;

            StartCoroutine(CoLoadAssets());
        }
    }

    private IEnumerator CoLoadAssets()
    {
        AssetBundleCreateRequest abcr = AssetBundle.CreateFromMemory(File.ReadAllBytes(Application.dataPath + "/RCAssets.unity3d"));
        yield return abcr;
        FengGameManagerMKII.RCAssets = abcr.assetBundle;

        using (WWW www = new WWW("file:///" + Application.dataPath + "/Resources/Textures/patreon.png"))
        {
            yield return www;
            AOT_2_LOGO = www.texture;
        }

        FengGameManagerMKII.IsAssetLoaded = true;
    }
}

using UnityEngine;

public class BTN_START_SINGLE_GAMEPLAY : MonoBehaviour
{
    private void OnClick()
    {
        string map = GameObject.Find("PopupListMap").GetComponent<UIPopupList>().selection;
        string character = GameObject.Find("PopupListCharacter").GetComponent<UIPopupList>().selection;
        IN_GAME_MAIN_CAMERA.Difficulty = (GameObject.Find("CheckboxHard").GetComponent<UICheckbox>().isChecked ? 1 : (GameObject.Find("CheckboxAbnormal").GetComponent<UICheckbox>().isChecked ? 2 : 0));
        IN_GAME_MAIN_CAMERA.Gametype = GameType.Singleplayer;
        IN_GAME_MAIN_CAMERA.SingleCharacter = character.ToUpper();
        Screen.lockCursor = IN_GAME_MAIN_CAMERA.CameraMode == CameraType.TPS;
        Screen.showCursor = false;
        if (map == "trainning_0")
        {
            IN_GAME_MAIN_CAMERA.Difficulty = -1;
        }
        FengGameManagerMKII.Level = LevelInfo.GetInfo(map);
        Application.LoadLevel(FengGameManagerMKII.Level.MapName);
    }
}

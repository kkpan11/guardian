﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class InRoomChat : Photon.MonoBehaviour
{
    public static InRoomChat Instance;
    public static Rect MessagesRect = new Rect(1f, 0f, 329f, 225f);
    public static Rect ChatBoxRect = new Rect(30f, 575f, 300f, 25f);
    public static List<Message> Messages = new List<Message>();
    private static readonly Regex Detagger = new Regex("<\\/?(color|size|b|i|material|quad)[^>]*>", RegexOptions.IgnoreCase);

    public string inputLine = string.Empty;
    public bool IsVisible = true;
    private bool AlignBottom = true;
    private Vector2 ScrollPosition = Guardian.Utilities.GameHelper.ScrollBottom;
    private string TextFieldName = "ChatInput";
    private GUIStyle labelStyle;

    // Guardian
    private static string g_draftMessage = string.Empty;

    void Awake()
    {
        Instance = this;
        UpdatePosition();
    }

    void OnDestroy()
    {
        g_draftMessage = inputLine;
    }

    public void UpdatePosition()
    {
        if (AlignBottom)
        {
            ScrollPosition = Guardian.Utilities.GameHelper.ScrollBottom;
            MessagesRect = new Rect(1f, Screen.height - 255f, 329f, 225f);
            ChatBoxRect = new Rect(30f, Screen.height - 25f, 300f, 25f);
        }
    }

    public void AddLine(string message)
    {
        AddMessage(string.Empty, message);
    }

    public void AddMessage(string sender, string text)
    {
        sender = Guardian.Utilities.GameHelper.DangerousTagsPattern.Replace(sender, string.Empty);
        text = Guardian.Utilities.GameHelper.DangerousTagsPattern.Replace(text, string.Empty);

        if (sender.Length != 0 || text.Length != 0)
        {
            Messages.Add(new Message(sender, text));

            if (Messages.Count > Guardian.GuardianClient.Properties.MaxChatLines.Value)
            {
                Messages.RemoveAt(0);
            }

            ScrollPosition = Guardian.Utilities.GameHelper.ScrollBottom;
        }
    }

    private void DrawMessageHistory()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
            };
        }

        if (Guardian.GuardianClient.Properties.DrawChatBackground.Value)
        {
            GUILayout.BeginArea(MessagesRect, Guardian.UI.GuiSkins.Box);
        }
        else
        {
            GUILayout.BeginArea(MessagesRect);
        }
        GUILayout.FlexibleSpace();
        ScrollPosition = GUILayout.BeginScrollView(ScrollPosition);

        foreach (Message message in Messages)
        {
            try
            {
                string messageText = message.ToString();

                if (Guardian.GuardianClient.Properties.ChatTimestamps.Value)
                {
                    messageText = $"[{message.Timestamp}] " + messageText;
                }

                GUILayout.Label(messageText, labelStyle);
                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)
                    && Event.current.type != EventType.Repaint
                    && GUI.GetNameOfFocusedControl().Equals(TextFieldName))
                {
                    if (Input.GetMouseButtonDown(0)) // Left-click
                    {
                        string text = Detagger.Replace(message.Content, string.Empty);

                        Guardian.GuardianClient.Commands.Find("translate").Execute(this, new string[] {
                            "auto", Guardian.Utilities.Translator.SystemLanguage, text
                        });
                    }
                    else if (Input.GetMouseButtonDown(1)) // Right-click
                    {
                        TextEditor te = new TextEditor
                        {
                            content = new GUIContent(message.Content)
                        };
                        te.SelectAll();
                        te.Copy();
                    }
                }
            }
            catch { }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void HandleInput()
    {
        if (Event.current == null) return;

        KeyCode rcChatKey = FengGameManagerMKII.InputRC.humanKeys[InputCodeRC.Chat];
        if (Event.current.type == EventType.KeyUp)
        {
            if (rcChatKey == KeyCode.None) return;
            if (Event.current.keyCode != rcChatKey) return;
            if (GUI.GetNameOfFocusedControl().Equals(TextFieldName)) return;

            TakeControl();
        }

        if (Event.current.type != EventType.KeyDown) return;

        if (Event.current.character == '/'
            && !GUI.GetNameOfFocusedControl().Equals(TextFieldName))
        {
            GUI.FocusControl(TextFieldName);
            inputLine = "/";
        }
        else if (Event.current.character == '\t' && rcChatKey != KeyCode.Tab
            && !IN_GAME_MAIN_CAMERA.IsPausing)
        {
            Event.current.Use();
        }

        if (Event.current.keyCode != KeyCode.KeypadEnter && Event.current.keyCode != KeyCode.Return) return;

        if (GUI.GetNameOfFocusedControl().Equals(TextFieldName))
        {
            if (!string.IsNullOrEmpty(inputLine) && inputLine != "\t")
            {
                if (FengGameManagerMKII.RCEvents.ContainsKey("OnChatInput"))
                {
                    string key = (string)FengGameManagerMKII.RCVariableNames["OnChatInput"];
                    if (FengGameManagerMKII.StringVariables.ContainsKey(key))
                    {
                        FengGameManagerMKII.StringVariables[key] = inputLine;
                    }
                    else
                    {
                        FengGameManagerMKII.StringVariables.Add(key, inputLine);
                    }
                    RCEvent rcEvent = (RCEvent)FengGameManagerMKII.RCEvents["OnChatInput"];
                    rcEvent.CheckEvent();
                }

                if (!inputLine.StartsWith("/"))
                {
                    string name = GExtensions.AsString(PhotonNetwork.player.customProperties[PhotonPlayerProperty.Name]);
                    if (name.StripNGUI().Length > 0) name = name.NGUIToUnity();

                    FengGameManagerMKII.Instance.photonView.RPC("Chat", PhotonTargets.All, FormatMessage(inputLine, name));
                }
                else
                {
                    Guardian.GuardianClient.Commands.HandleCommand(this);
                }
            }

            GUI.FocusControl(string.Empty);
            inputLine = string.Empty;
        }
        else
        {
            TakeControl();
        }
    }

    private void TakeControl()
    {
        GUI.FocusControl(TextFieldName);
        inputLine = g_draftMessage.Length == 0 ? "\t" : g_draftMessage;
        g_draftMessage = string.Empty;
    }

    private void DrawMessageTextField()
    {
        GUILayout.BeginArea(ChatBoxRect);
        GUILayout.BeginHorizontal();
        GUI.SetNextControlName(TextFieldName);
        inputLine = GUILayout.TextField(inputLine, GUILayout.MaxWidth(300));
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    public void OnGUI()
    {
        if (!IsVisible || !PhotonNetwork.connected) return;

        DrawMessageHistory();
        HandleInput();
        DrawMessageTextField();

        // if (g_draftMessage.Length > 0) TakeControl();
    }

    public static object[] FormatMessage(string message, string name)
    {
        // Auto-translate
        if (Guardian.GuardianClient.Properties.TranslateOutgoing.Value)
        {
            string[] result = Guardian.Utilities.Translator.Translate(message, Guardian.GuardianClient.Properties.IncomingLanguage.Value, Guardian.GuardianClient.Properties.OutgoingLanguage.Value);

            if (result.Length > 1 && !result[0].Equals(Guardian.GuardianClient.Properties.OutgoingLanguage.Value))
            {
                message = result[1];
            }
        }

        // Emotes
        message = Guardian.Utilities.EmoteHelper.FormatText(message);

        // 4chan green-text
        if (message.StripUnityColors().StartsWith(">"))
        {
            message = message.StripUnityColors().AsColor("B5BD68"); // #789922 is the true color, but contrasts terribly :(
        }
        else
        {
            // Color
            string chatColor = Guardian.GuardianClient.Properties.TextColor.Value;
            if (chatColor.Length > 0)
            {
                message = message.AsColor(chatColor);
            }
        }

        // Mentions
        string tempMessage = string.Empty;
        for (int i = 0; i < message.Length; i++)
        {
            char c = message[i];
            string playerId = string.Empty;

            if (c == '@')
            {
                for (int off = 1; off < message.Length - i; off++)
                {
                    char num = message[i + off];
                    if (num < '0' || num > '9') break;

                    playerId += num;
                }
            }

            if (playerId.Length > 0)
            {
                PhotonPlayer player = PhotonPlayer.Find(int.Parse(playerId));
                tempMessage += player != null ? $"@{player.Username.NGUIToUnity()}" : $"@{playerId}";
                i += playerId.Length;
            }
            else
            {
                tempMessage += c;
            }
        }

        message = tempMessage;

        // Bold chat
        if (Guardian.GuardianClient.Properties.BoldText.Value)
        {
            message = message.AsBold();
        }
        // Italic chat
        if (Guardian.GuardianClient.Properties.ItalicText.Value)
        {
            message = message.AsItalic();
        }

        // Custom name
        string customName = Guardian.GuardianClient.Properties.ChatName.Value;
        if (customName.Length != 0)
        {
            name = customName.NGUIToUnity();
        }
        // Bold name
        if (Guardian.GuardianClient.Properties.BoldName.Value)
        {
            name = name.AsBold();
        }
        // Italic name
        if (Guardian.GuardianClient.Properties.ItalicName.Value)
        {
            name = name.AsItalic();
        }

        return new object[] { $"{Guardian.GuardianClient.Properties.TextPrefix.Value}{message}{Guardian.GuardianClient.Properties.TextSuffix.Value}", name };
    }

    public class Message
    {
        public string Sender;
        public string Content;
        public long Time;
        public string Timestamp;

        public Message(string sender, string content)
        {
            this.Sender = sender;
            this.Content = content;
            this.Time = Guardian.Utilities.GameHelper.CurrentTimeMillis();

            DateTime date = Guardian.Utilities.GameHelper.Epoch.AddMilliseconds(this.Time).ToLocalTime();
            this.Timestamp = date.ToString("HH:mm:ss");
        }

        public override string ToString()
        {
            if (Sender.Length == 0)
            {
                return Content;
            }

            return Sender + ": " + Content;
        }
    }
}

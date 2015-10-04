﻿using System;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Subsurface.Networking;
using FarseerPhysics;
using FarseerPhysics.Factories;
using FarseerPhysics.Dynamics;
using System.IO;
using System.Collections.Generic;

namespace Subsurface
{
    class NetLobbyScreen : Screen
    {
        private GUIFrame menu;
        private GUIFrame infoFrame;
        private GUIListBox playerList;

        private GUIListBox subList, modeList, chatBox;

        private GUIListBox jobList;

        private GUITextBox textBox, seedBox;

        private GUIScrollBar durationBar;

        private GUIFrame playerFrame;

        private GUIFrame jobInfoFrame;

        private float camAngle;

        public bool IsServer;
        public string ServerName, ServerMessage;

        private GUITextBox serverMessage;

        public GUIListBox SubList
        {
            get { return subList; }
        }

        public Submarine SelectedMap
        {
            get { return subList.SelectedData as Submarine; }
        }

        public GameModePreset SelectedMode
        {
            get { return modeList.SelectedData as GameModePreset; }
        }

        //for guitextblock delegate
        public string GetServerName()
        {
            return ServerName;
        }
        public string GetServerMessage()
        {
            return ServerMessage;
        }

        public TimeSpan GameDuration
        {
            get 
            {
                int minutes = (int)(durationBar.BarScroll* 60.0f);
                return new TimeSpan(0, minutes, 0);
            }
        }

        public List<JobPrefab> JobPreferences
        {
            get
            {
                List<JobPrefab> jobPreferences = new List<JobPrefab>();
                foreach (GUIComponent child in jobList.children)
                {
                    JobPrefab jobPrefab = child.UserData as JobPrefab;
                    if (jobPrefab == null) continue;
                    jobPreferences.Add(jobPrefab);
                }
                return jobPreferences;
            }
        }

        private string levelSeed;

        public string LevelSeed
        {
            get
            {
                return levelSeed;
            }
            private set
            {
                levelSeed = value;
                seedBox.Text = levelSeed;
            }
        }

        public string DurationText()
        {
            return "Game duration: " + GameDuration + " min";
        }
                
        public NetLobbyScreen()
        {
            int width = Math.Min(GameMain.GraphicsWidth - 80, 1500);
            int height = Math.Min(GameMain.GraphicsHeight - 80, 800);

            Rectangle panelRect = new Rectangle(0,0,width,height);

            menu = new GUIFrame(panelRect, Color.Transparent, Alignment.Center);
            //menu.Padding = GUI.style.smallPadding;

            //server info panel ------------------------------------------------------------

            infoFrame = new GUIFrame(new Rectangle(0, 0, (int)(panelRect.Width * 0.7f), (int)(panelRect.Height * 0.6f)), GUI.Style, menu);
            //infoFrame.Padding = GUI.style.smallPadding;
            
            //chatbox ----------------------------------------------------------------------
            GUIFrame chatFrame = new GUIFrame(
                new Rectangle(0, (int)(panelRect.Height * 0.6f + 20),
                    (int)(panelRect.Width * 0.7f),
                    (int)(panelRect.Height * 0.4f - 20)),
                GUI.Style, menu);

            chatBox = new GUIListBox(new Rectangle(0,0,0,chatFrame.Rect.Height-80), Color.White, GUI.Style, chatFrame);            
            textBox = new GUITextBox(new Rectangle(0, 25, 0, 25), Alignment.Bottom, GUI.Style, chatFrame);
            textBox.Font = GUI.SmallFont;
            textBox.OnEnter = EnterChatMessage;

            //player info panel ------------------------------------------------------------

            playerFrame = new GUIFrame(
                new Rectangle((int)(panelRect.Width * 0.7f + 20), 0,
                    (int)(panelRect.Width * 0.3f - 20), (int)(panelRect.Height * 0.6f)),
                GUI.Style, menu);

            //player list ------------------------------------------------------------------

            GUIFrame playerListFrame = new GUIFrame(
                new Rectangle((int)(panelRect.Width * 0.7f + 20), (int)(panelRect.Height * 0.6f + 20),
                    (int)(panelRect.Width * 0.3f - 20), (int)(panelRect.Height * 0.4f - 20)),
                GUI.Style, menu);

            playerList = new GUIListBox(new Rectangle(0,0,0,0), null, GUI.Style, playerListFrame);

            //submarine list ------------------------------------------------------------------

            int columnWidth = infoFrame.Rect.Width / 5 - 30;
            int columnX = 0;

            new GUITextBlock(new Rectangle(columnX, 120, columnWidth, 30), "Selected submarine:", GUI.Style, infoFrame);
            subList = new GUIListBox(new Rectangle(columnX, 150, columnWidth, infoFrame.Rect.Height - 150 - 80), Color.White, GUI.Style, infoFrame);
            subList.OnSelected = SelectMap;

            if (Submarine.SavedSubmarines.Count > 0)
            {
                foreach (Submarine sub in Submarine.SavedSubmarines)
                {
                    GUITextBlock textBlock = new GUITextBlock(
                        new Rectangle(0, 0, 0, 25),
                        sub.Name, GUI.Style,
                        Alignment.Left, Alignment.Left,
                        subList);
                    textBlock.Padding = new Vector4(10.0f, 0.0f, 0.0f, 0.0f);
                    textBlock.UserData = sub;
                }
            }
            else
            {
                DebugConsole.ThrowError("No saved submarines found!");
                return;
            }

            columnX += columnWidth + 20;

            //gamemode ------------------------------------------------------------------

            new GUITextBlock(new Rectangle(columnX, 120, 0, 30), "Selected game mode: ", GUI.Style, infoFrame);
            modeList = new GUIListBox(new Rectangle(columnX, 150, columnWidth, infoFrame.Rect.Height - 150 - 80), GUI.Style, infoFrame);


            foreach (GameModePreset mode in GameModePreset.list)
            {
                if (mode.IsSinglePlayer) continue;

                GUITextBlock textBlock = new GUITextBlock(
                    new Rectangle(0, 0, 0, 25),
                    mode.Name, GUI.Style,
                    Alignment.Left, Alignment.Left,
                    modeList);
                textBlock.Padding = new Vector4(10.0f, 0.0f, 0.0f, 0.0f);
                textBlock.UserData = mode;
            }

            columnX += columnWidth;

            //gamemode description ------------------------------------------------------------------
            

            var modeDescription = new GUITextBlock(
                new Rectangle(columnX, 150, (int)(columnWidth * 1.5f), infoFrame.Rect.Height - 150 - 80), 
                "", Color.Black*0.3f, Color.White, Alignment.TopLeft, Alignment.TopLeft, GUI.Style, infoFrame, true);

            modeList.UserData = modeDescription;

            columnX += modeDescription.Rect.Width + 40;

            //duration ------------------------------------------------------------------
            
            GUITextBlock durationText = new GUITextBlock(new Rectangle(columnX, 120, columnWidth, 20),
                "Game duration: ", GUI.Style, Alignment.Left, Alignment.TopLeft, infoFrame);
            durationText.TextGetter = DurationText;

            durationBar = new GUIScrollBar(new Rectangle(columnX, 150, columnWidth, 20),
                GUI.Style, 0.1f, infoFrame);
            durationBar.BarSize = 0.1f;

            //seed ------------------------------------------------------------------
            
            new GUITextBlock(new Rectangle(columnX, 190, columnWidth, 20),
                "Level Seed: ", GUI.Style, Alignment.Left, Alignment.TopLeft, infoFrame);

            seedBox = new GUITextBox(new Rectangle(columnX, 220, columnWidth, 20),
                Alignment.TopLeft, GUI.Style, infoFrame);
            seedBox.OnTextChanged = SelectSeed;
            LevelSeed = ToolBox.RandomSeed(8);

            //server info ------------------------------------------------------------------
            
            var serverName = new GUITextBox(new Rectangle(0, 0, 200, 20), null, null, Alignment.TopLeft, Alignment.TopLeft, GUI.Style, infoFrame);
            serverName.TextGetter = GetServerName;
            serverName.Enabled = GameMain.Server != null;
            serverName.OnTextChanged = ChangeServerName;

            serverMessage = new GUITextBox(new Rectangle(0, 30, 360, 70), null, null, Alignment.TopLeft, Alignment.TopLeft, GUI.Style, infoFrame);
            serverMessage.Wrap = true;
            serverMessage.TextGetter = GetServerMessage;
            serverMessage.OnTextChanged = UpdateServerMessage;
        }

        public override void Deselect()
        {
            textBox.Deselect();
        }

        public override void Select()
        {
            GameMain.LightManager.LosEnabled = false;

            //infoFrame.ClearChildren();
            
            textBox.Select();

            Character.Controlled = null;
            GameMain.GameScreen.Cam.TargetPos = Vector2.Zero;
            
            subList.Enabled         = GameMain.Server != null;
            modeList.Enabled        = GameMain.Server != null;
            durationBar.Enabled     = GameMain.Server != null;                      
            seedBox.Enabled         = GameMain.Server != null;                       
            serverMessage.Enabled   = GameMain.Server != null;
            ServerName = (GameMain.Server==null) ? "Server" : GameMain.Server.Name;

            modeList.OnSelected += SelectMode;

            infoFrame.RemoveChild(infoFrame.children.Find(c => c.UserData as string == "startButton"));

            if (IsServer && GameMain.Server != null)
            {
                GUIButton startButton = new GUIButton(new Rectangle(0, 0, 200, 30), "Start", Alignment.BottomRight, GUI.Style, infoFrame);
                startButton.OnClicked = GameMain.Server.StartGameClicked;
                startButton.UserData = "startButton";
                
                //mapList.OnSelected = new GUIListBox.OnSelectedHandler(Game1.server.UpdateNetLobby);
                modeList.OnSelected += GameMain.Server.UpdateNetLobby;                
                durationBar.OnMoved = GameMain.Server.UpdateNetLobby;

                if (subList.CountChildren > 0 && subList.Selected == null) subList.Select(-1);
                if (GameModePreset.list.Count > 0 && modeList.Selected == null) modeList.Select(-1);

                if (playerFrame.children.Find(c => c.UserData as string == "playyourself") == null)
                {
                    var playYourself = new GUITickBox(new Rectangle(-30, -30, 20, 20), "Play yourself", Alignment.TopLeft, playerFrame);
                    playYourself.Selected = GameMain.Server.CharacterInfo != null;
                    playYourself.OnSelected = TogglePlayYourself;
                    playYourself.UserData = "playyourself";
                }
            }
            else
            {
                UpdatePlayerFrame(GameMain.Client.CharacterInfo);
            }

            base.Select();
        }

        private void UpdatePlayerFrame(CharacterInfo characterInfo)
        {
            if (playerFrame.children.Count <= 1)
            {
                playerFrame.ClearChildren();

                if (IsServer && GameMain.Server != null)
                {
                    var playYourself = new GUITickBox(new Rectangle(-30, -30, 20, 20), "Play yourself", Alignment.TopLeft, playerFrame);
                    playYourself.Selected = GameMain.Server.CharacterInfo != null;
                    playYourself.OnSelected = TogglePlayYourself;
                    playYourself.UserData = "playyourself";
                }

                new GUITextBlock(new Rectangle(60, 0, 200, 30), "Name: ", GUI.Style, playerFrame);

                GUITextBox playerName = new GUITextBox(new Rectangle(60, 30, 0, 20),
                    Alignment.TopLeft, GUI.Style, playerFrame);
                playerName.Text = characterInfo.Name;
                playerName.OnEnter += ChangeCharacterName;

                new GUITextBlock(new Rectangle(0, 70, 200, 30), "Gender: ", GUI.Style, playerFrame);

                GUIButton maleButton = new GUIButton(new Rectangle(0, 100, 70, 20), "Male",
                    Alignment.TopLeft, GUI.Style, playerFrame);
                maleButton.UserData = Gender.Male;
                maleButton.OnClicked += SwitchGender;

                GUIButton femaleButton = new GUIButton(new Rectangle(90, 100, 70, 20), "Female",
                    Alignment.TopLeft, GUI.Style, playerFrame);
                femaleButton.UserData = Gender.Female;
                femaleButton.OnClicked += SwitchGender;

                new GUITextBlock(new Rectangle(0, 150, 200, 30), "Job preferences:", GUI.Style, playerFrame);

                jobList = new GUIListBox(new Rectangle(0, 180, 250, 0), GUI.Style, playerFrame);
                jobList.Enabled = false;


                int i = 1;
                foreach (JobPrefab job in JobPrefab.List)
                {
                    GUITextBlock jobText = new GUITextBlock(new Rectangle(0, 0, 0, 20), i + ". " + job.Name+"    ", GUI.Style, Alignment.Left, Alignment.Right, jobList);
                    jobText.UserData = job;

                    GUIButton infoButton = new GUIButton(new Rectangle(0, 0, 15, 15), "?", GUI.Style, jobText);
                    infoButton.UserData = -1;
                    infoButton.OnClicked += ViewJobInfo;

                    GUIButton upButton = new GUIButton(new Rectangle(30, 0, 15, 15), "^", GUI.Style, jobText);
                    upButton.UserData = -1;
                    upButton.OnClicked += ChangeJobPreference;

                    GUIButton downButton = new GUIButton(new Rectangle(50, 0, 15, 15), "˅", GUI.Style, jobText);
                    downButton.UserData = 1;
                    downButton.OnClicked += ChangeJobPreference;
                }

                UpdateJobPreferences(jobList);

                //UpdatePreviewPlayer(Game1.Client.CharacterInfo);

                UpdatePreviewPlayer(characterInfo);
            }
        }

        private bool TogglePlayYourself(object obj)
        {
            GUITickBox tickBox = obj as GUITickBox;
            if (tickBox.Selected)
            {
                GameMain.Server.CharacterInfo = new CharacterInfo(Character.HumanConfigFile, GameMain.Server.Name);
                UpdatePlayerFrame(GameMain.Server.CharacterInfo);
            }
            else
            {
                playerFrame.ClearChildren();

                if (IsServer && GameMain.Server != null)
                {
                    GameMain.Server.CharacterInfo = null;
                    GameMain.Server.Character = null;

                    var playYourself = new GUITickBox(new Rectangle(0, -20, 20, 20), "Play yourself", Alignment.TopLeft, playerFrame);
                    playYourself.OnSelected = TogglePlayYourself;
                }
            }
            return false;
        }

        private bool SelectMap(GUIComponent component, object obj)
        {
            if (GameMain.Server != null) GameMain.Server.UpdateNetLobby(obj);

            Submarine sub = (Submarine)obj;

            //submarine already loaded
            if (Submarine.Loaded != null && sub.FilePath == Submarine.Loaded.FilePath) return true;

            sub.Load();

            return true;
        }

        public bool ChangeServerName(GUITextBox textBox, string text)
        {
            if (GameMain.Server == null) return false;
            ServerName = text;
            GameMain.Server.UpdateNetLobby(null, null);

            return true;
        }

        public bool UpdateServerMessage(GUITextBox textBox, string text)
        {
            if (GameMain.Server == null) return false;
            ServerMessage = text;
            GameMain.Server.UpdateNetLobby(null, null);

            return true;
        }

        public void AddPlayer(Client client)
        {
            GUITextBlock textBlock = new GUITextBlock(
                new Rectangle(0, 0, 0, 25),
                 client.name + ((client.assignedJob==null) ? "" : " (" + client.assignedJob.Name + ")"), 
                 GUI.Style, Alignment.Left, Alignment.Left,
                playerList);
            textBlock.Padding = new Vector4(10.0f, 0.0f, 0.0f, 0.0f);
            textBlock.UserData = client;          
        }

        public void RemovePlayer(int clientID)
        {
            GUIComponent child = playerList.children.Find(c =>
                {
                    Client client = c.UserData as Client;
                    return (client.ID == clientID);
                });

            if (child != null) playerList.RemoveChild(child);
        }

        public void RemovePlayer(Client client)
        {
            if (client == null) return;
            playerList.RemoveChild(playerList.GetChild(client));
        }

        public void ClearPlayers()
        {
            for (int i = 1; i<playerList.CountChildren; i++)
            {
                playerList.RemoveChild(playerList.children[i]);
            }
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);
            
            Vector2 pos = new Vector2(
                Submarine.Borders.X + Submarine.Borders.Width / 2,
                Submarine.Borders.Y - Submarine.Borders.Height / 2);

            camAngle += (float)deltaTime / 10.0f;
            Vector2 offset = (new Vector2(
                (float)Math.Cos(camAngle) * (Submarine.Borders.Width / 2.0f),
                (float)Math.Sin(camAngle) * (Submarine.Borders.Height / 2.0f)));
            
            pos += offset * 0.8f;
            
            GameMain.GameScreen.Cam.TargetPos = pos;
            GameMain.GameScreen.Cam.MoveCamera((float)deltaTime);

            menu.Update((float)deltaTime);

            if (jobInfoFrame != null) jobInfoFrame.Update((float)deltaTime);
                        
            //durationBar.BarScroll = Math.Max(durationBar.BarScroll, 1.0f / 60.0f);
        }

        public override void Draw(double deltaTime, GraphicsDevice graphics, SpriteBatch spriteBatch)
        {
            graphics.Clear(Color.CornflowerBlue);

            GameMain.GameScreen.DrawMap(graphics, spriteBatch);

            spriteBatch.Begin();

            menu.Draw(spriteBatch);

            if (jobInfoFrame != null) jobInfoFrame.Draw(spriteBatch);

            //if (previewPlayer!=null) previewPlayer.Draw(spriteBatch);

            GUI.Draw((float)deltaTime, spriteBatch, null);

            spriteBatch.End();
        }

        public void NewChatMessage(string message, Color color)
        {
            float prevSize = chatBox.BarSize;
            float oldScroll = chatBox.BarScroll;

            while (chatBox.CountChildren>20)
            {
                chatBox.RemoveChild(chatBox.children[1]);
            }

            GUITextBlock msg = new GUITextBlock(new Rectangle(0, 0, 0, 20),
                message, 
                ((chatBox.CountChildren % 2) == 0) ? Color.Transparent : Color.Black*0.1f, color, 
                Alignment.Left, GUI.Style, null, true);
            msg.Font = GUI.SmallFont;
            msg.CanBeFocused = false;

            msg.Padding = new Vector4(20, 0, 0, 0);
            chatBox.AddChild(msg);

            if ((prevSize == 1.0f && chatBox.BarScroll == 0.0f) || (prevSize < 1.0f && chatBox.BarScroll == 1.0f)) chatBox.BarScroll = 1.0f;
        }
        
        public bool EnterChatMessage(GUITextBox textBox, string message)
        {
            if (String.IsNullOrEmpty(message)) return false;

            GameMain.NetworkMember.SendChatMessage(GameMain.NetworkMember.Name + ": " + message);
            
            return true;
        }

        private void UpdatePreviewPlayer(CharacterInfo characterInfo)
        {
            GUIComponent existing = playerFrame.FindChild("playerhead");
            if (existing != null) playerFrame.RemoveChild(existing);

            GUIImage image = new GUIImage(new Rectangle(0, 0, 30, 30), characterInfo.HeadSprite, Alignment.TopLeft, playerFrame);
            image.UserData = "playerhead";
        }

        private bool SwitchGender(GUIButton button, object obj)
        {
            Gender gender = (Gender)obj;
            GameMain.NetworkMember.CharacterInfo.Gender = gender;
            if (GameMain.Client != null) GameMain.Client.SendCharacterData();
                
            UpdatePreviewPlayer(GameMain.NetworkMember.CharacterInfo);
            return true;
        }

        private bool SelectMode(GUIComponent component, object obj)
        {
            GameModePreset modePreset = obj as GameModePreset;
            if (modePreset == null) return false;

            GUITextBlock description = modeList.UserData as GUITextBlock;

            description.Text = modePreset.Description;

            //if (Game1.Server != null) Game1.Server.UpdateNetLobby(null);

            return true;
        }


        private bool SelectSeed(GUITextBox textBox, string seed)
        {
            if (!string.IsNullOrWhiteSpace(seed))
            {
                LevelSeed = seed;
            }

            //textBox.Text = LevelSeed;
            //textBox.Selected = false;

            if (GameMain.Server != null) GameMain.Server.UpdateNetLobby(null);

            return true;
        }

        private bool ChangeCharacterName(GUITextBox textBox, string newName)
        {
            if (string.IsNullOrEmpty(newName)) return false;

            if (GameMain.NetworkMember == null || GameMain.NetworkMember.CharacterInfo == null) return true;

            GameMain.NetworkMember.CharacterInfo.Name = newName;
            if (GameMain.Client != null)
            {
                GameMain.Client.Name = newName;
                GameMain.Client.SendCharacterData();
            }

            textBox.Text = newName;
            textBox.Selected = false;

            return true;
        }

        private bool ViewJobInfo(GUIButton button, object obj)
        {
            GUIComponent jobText = button.Parent;

            JobPrefab jobPrefab = jobText.UserData as JobPrefab;
            if (jobPrefab == null) return false;

            jobInfoFrame = jobPrefab.CreateInfoFrame();
            GUIButton closeButton = new GUIButton(new Rectangle(0,0,100,20), "Close", Alignment.BottomRight, GUI.Style, jobInfoFrame);
            closeButton.OnClicked = CloseJobInfo;
            return true;
        }

        private bool CloseJobInfo(GUIButton button, object obj)
        {
            jobInfoFrame = null;
            return true;
        }

        private bool ChangeJobPreference(GUIButton button, object obj)
        {
            GUIComponent jobText = button.Parent;
            GUIListBox jobList = jobText.Parent as GUIListBox;

            int index = jobList.children.IndexOf(jobText);
            int newIndex = index + (int)obj;
            if (newIndex < 0 || newIndex > jobList.children.Count - 1) return false;

            GUIComponent temp = jobList.children[newIndex];
            jobList.children[newIndex] = jobText;
            jobList.children[index] = temp;

            UpdateJobPreferences(jobList);

            return true;
        }

        private void UpdateJobPreferences(GUIListBox listBox)
        {
            listBox.Deselect();
            for (int i = 0; i < listBox.children.Count; i++)
            {
                float a = (float)(i - 1) / 3.0f;
                a = Math.Min(a, 3);
                Color color = new Color(1.0f - a, (1.0f - a) * 0.6f, 0.0f, 0.3f);

                listBox.children[i].Color = color;
                listBox.children[i].HoverColor = color;
                listBox.children[i].SelectedColor = color;

                (listBox.children[i] as GUITextBlock).Text = (i+1) + ". " + (listBox.children[i].UserData as JobPrefab).Name;
            }

            if (GameMain.Client!=null) GameMain.Client.SendCharacterData();
        }

        public bool TrySelectMap(string mapName, string md5Hash)
        {

            Submarine map = Submarine.SavedSubmarines.Find(m => m.Name == mapName);
            if (map == null)
            {
                DebugConsole.ThrowError("The map ''" + mapName + "'' has been selected by the server.");
                DebugConsole.ThrowError("Matching map not found in your map folder.");
                return false;
            }
            else
            {
                if (map.MD5Hash.Hash != md5Hash)
                {
                    DebugConsole.ThrowError("Your version of the map file ''" + map.Name + "'' doesn't match the server's version!");
                    DebugConsole.ThrowError("Your file: " + map.Name + "(MD5 hash : " + map.MD5Hash.Hash + ")");
                    DebugConsole.ThrowError("Server's file: " + mapName + "(MD5 hash : " + md5Hash + ")");
                    return false;
                }
                else
                {
                    subList.Select(map);
                    //map.Load();
                    return true;
                }
            }
        }
        
        public void WriteData(NetOutgoingMessage msg)
        {
            Submarine selectedMap = subList.SelectedData as Submarine;

            if (selectedMap==null)
            {
                msg.Write(" ");
                msg.Write(" ");
            }
            else
            {
                msg.Write(Path.GetFileName(selectedMap.Name));
                msg.Write(selectedMap.MD5Hash.Hash);
            }

            msg.Write(ServerName);
            msg.Write(ServerMessage);

            msg.Write(modeList.SelectedIndex-1);
            msg.Write(durationBar.BarScroll);
            msg.Write(LevelSeed);

            //msg.Write(playerList.CountChildren - 1);
            //for (int i = 1; i < playerList.CountChildren; i++)
            //{
            //    Client client = playerList.children[i].UserData as Client;
            //    msg.Write(client.ID);
            //    msg.Write(client.assignedJob==null ? "" : client.assignedJob.Name);
            //}

        }



        public void ReadData(NetIncomingMessage msg)
        {
            string mapName="", md5Hash="";
            
            int modeIndex = 0;
            float durationScroll = 0.0f;
            string levelSeed = "";

            try
            {
                mapName = msg.ReadString();
                md5Hash  = msg.ReadString();

                ServerName = msg.ReadString();
                ServerMessage = msg.ReadString();

                modeIndex = msg.ReadInt32();

                durationScroll = msg.ReadFloat();

                levelSeed = msg.ReadString();
            }

            catch
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(mapName)) TrySelectMap(mapName, md5Hash);

            modeList.Select(modeIndex);

            durationBar.BarScroll = durationScroll;

            LevelSeed = levelSeed;
        }

    }
}
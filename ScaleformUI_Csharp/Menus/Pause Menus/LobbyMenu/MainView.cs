﻿using CitizenFX.Core;
using ScaleformUI.Menu;
using ScaleformUI.PauseMenus;
using ScaleformUI.Scaleforms;
using static CitizenFX.Core.Native.API;

namespace ScaleformUI.LobbyMenu
{
    public delegate void LobbyMenuOpenEvent(MainView menu);
    public delegate void LobbyMenuCloseEvent(MainView menu);

    public class MainView : PauseMenuBase
    {
        // Button delay
        private int time;
        private bool _firstDrawTick = true;
        private int times;
        private int delay = 150;
        internal List<Column> listCol;
        internal PauseMenuScaleform _pause;
        internal bool _loaded;
        internal readonly static string _browseTextLocalized = Game.GetGXTEntry("HUD_INPUT1C");
        public event LobbyMenuOpenEvent OnLobbyMenuOpen;
        public event LobbyMenuCloseEvent OnLobbyMenuClose;
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string SideStringTop { get; set; }
        public string SideStringMiddle { get; set; }
        public string SideStringBottom { get; set; }
        public Tuple<string, string> HeaderPicture { internal get; set; }
        public Tuple<string, string> CrewPicture { internal get; set; }
        public SettingsListColumn SettingsColumn { get; private set; }
        public MissionsListColumn MissionsColumn { get; private set; }
        public PlayerListColumn PlayersColumn { get; private set; }
        public MissionDetailsPanel MissionPanel { get; private set; }
        public int FocusLevel
        {
            get => focusLevel;
        }

        public bool TemporarilyHidden { get; set; }
        public bool HideTabs { get; set; }
        public bool DisplayHeader = true;

        public MainView(string title) : this(title, "", "", "", "")
        {
        }
        public MainView(string title, string subtitle) : this(title, subtitle, "", "", "")
        {
        }

        public MainView(string title, string subtitle, string sideTop, string sideMid, string sideBot)
        {
            Title = title;
            SubTitle = subtitle;
            SideStringTop = sideTop;
            SideStringMiddle = sideMid;
            SideStringBottom = sideBot;
            Index = 0;
            TemporarilyHidden = false;
            InstructionalButtons = new()
            {
                new InstructionalButton(Control.PhoneSelect, UIMenu._selectTextLocalized),
                new InstructionalButton(Control.PhoneCancel, UIMenu._backTextLocalized),
            };
            _pause = Main.PauseMenu;
        }

        public override bool Visible
        {
            get { return _visible; }
            set
            {
                Game.IsPaused = value;
                if (value)
                {
                    ActivateFrontendMenu((uint)Game.GenerateHash("FE_MENU_VERSION_EMPTY_NO_BACKGROUND"), true, -1);
                    BuildPauseMenu();
                    SendPauseMenuOpen();
                    AnimpostfxPlay("PauseMenuIn", 800, true);
                    Main.InstructionalButtons.SetInstructionalButtons(InstructionalButtons);
                    SetPlayerControl(Game.Player.Handle, false, 0);
                    _firstDrawTick = true;
                    MenuHandler.currentBase = this;
                }
                else
                {
                    _pause.Dispose();
                    AnimpostfxStop("PauseMenuIn");
                    AnimpostfxPlay("PauseMenuOut", 800, false);
                    SendPauseMenuClose();
                    SetPlayerControl(Game.Player.Handle, true, 0);
                    MenuHandler.currentBase = null;
                    ActivateFrontendMenu((uint)Game.GenerateHash("FE_MENU_VERSION_EMPTY_NO_BACKGROUND"), false, -1);
                    Main.InstructionalButtons.ClearButtonList();
                }
                base.Visible = value;
                _visible = value;
                _pause.Visible = value;
            }
        }

        public int Index;
        private bool _visible;
        private int focusLevel;

        private async void updateFocus(int value, bool isMouse = false)
        {
            bool goingLeft = value < focusLevel;
            if (listCol[value].Type != "players")
            {
                if (PlayersColumn != null && PlayersColumn.Items.Count > 0 && !PlayersColumn.Items[PlayersColumn.CurrentSelection].KeepPanelVisible)
                    ClearPedInPauseMenu();
            }
            focusLevel = value;
            if (_pause is not null)
            {
                if (focusLevel < 0)
                    focusLevel = listCol.Count - 1;
                else if (focusLevel > listCol.Count - 1)
                    focusLevel = 0;
                if (listCol[focusLevel].Type == "panel")
                {
                    if (goingLeft)
                        updateFocus(focusLevel - 1, isMouse);
                    else
                        updateFocus(focusLevel + 1, isMouse);
                    return;
                }
                if (Visible)
                {
                    int idx = await _pause._lobby.CallFunctionReturnValueInt("SET_FOCUS", focusLevel);
                    Debug.WriteLine($"idx: {idx}");
                    if (!isMouse)
                    {
                        switch (listCol[focusLevel].Type)
                        {
                            case "players":
                                PlayersColumn.CurrentSelection = idx;
                                PlayersColumn.IndexChangedEvent();
                                break;
                            case "settings":
                                SettingsColumn.CurrentSelection = idx;
                                SettingsColumn.IndexChangedEvent();
                                break;
                            case "missions":
                                MissionsColumn.CurrentSelection = idx;
                                MissionsColumn.IndexChangedEvent();
                                break;
                        }
                    }
                }
            }
        }

        public void SetUpColumns(List<Column> columns)
        {
            if (columns.Count > 3)
                throw new Exception("You must have 3 columns!");
            if (columns.Count == 3 && columns[2] is PlayerListColumn)
                throw new Exception("For panel designs reasons, you can't have Players list in 3rd column!");

            listCol = columns;
            foreach (Column col in columns)
            {
                switch (col)
                {
                    case SettingsListColumn:
                        SettingsColumn = col as SettingsListColumn;
                        SettingsColumn.Parent = this;
                        SettingsColumn.Order = columns.IndexOf(col);
                        break;
                    case PlayerListColumn:
                        PlayersColumn = col as PlayerListColumn;
                        PlayersColumn.Parent = this;
                        PlayersColumn.Order = columns.IndexOf(col);
                        break;
                    case MissionsListColumn:
                        MissionsColumn = col as MissionsListColumn;
                        MissionsColumn.Parent = this;
                        MissionsColumn.Order = columns.IndexOf(col);
                        break;
                    case MissionDetailsPanel:
                        MissionPanel = col as MissionDetailsPanel;
                        MissionPanel.Parent = this;
                        MissionPanel.Order = columns.IndexOf(col);
                        break;
                }
            }
        }

        public void ShowHeader()
        {
            if (String.IsNullOrEmpty(SubTitle) || String.IsNullOrWhiteSpace(SubTitle))
                _pause.SetHeaderTitle(Title);
            else
            {
                _pause.ShiftCoronaDescription(true, false);
                _pause.SetHeaderTitle(Title, SubTitle);
            }
            if (HeaderPicture != null)
                _pause.SetHeaderCharImg(HeaderPicture.Item2, HeaderPicture.Item2, true);
            if (CrewPicture != null)
                _pause.SetHeaderSecondaryImg(CrewPicture.Item1, CrewPicture.Item2, true);
            _pause.SetHeaderDetails(SideStringTop, SideStringMiddle, SideStringBottom);
            _pause.AddLobbyMenuTab(listCol[0].Label, 2, 0, listCol[0].Color);
            _pause.AddLobbyMenuTab(listCol[1].Label, 2, 0, listCol[1].Color);
            _pause.AddLobbyMenuTab(listCol[2].Label, 2, 0, listCol[2].Color);
            _pause._header.CallFunction("SET_ALL_HIGHLIGHTS", true, (int)HudColor.HUD_COLOUR_PAUSE_BG);

            _loaded = true;
        }

        private bool canBuild = true;
        public async void BuildPauseMenu()
        {
            ShowHeader();
            switch (listCol.Count)
            {
                case 1:
                    _pause._lobby.CallFunction("CREATE_MENU", listCol[0].Type);
                    break;
                case 2:
                    _pause._lobby.CallFunction("CREATE_MENU", listCol[0].Type, listCol[1].Type);
                    break;
                case 3:
                    _pause._lobby.CallFunction("CREATE_MENU", listCol[0].Type, listCol[1].Type, listCol[2].Type);
                    break;
            }

            if (listCol.Any(x => x is PlayerListColumn))
                buildPlayers();

            if (listCol.Any(x => x is SettingsListColumn))
                buildSettings();

            if (listCol.Any(x => x is MissionsListColumn))
                buildMissions();

            if (listCol.Any(x => x is MissionDetailsPanel))
            {
                _pause._lobby.CallFunction("ADD_MISSION_PANEL_PICTURE", MissionPanel.TextureDict, MissionPanel.TextureName);
                _pause._lobby.CallFunction("SET_MISSION_PANEL_TITLE", MissionPanel.Title);
                if (MissionPanel.Items.Count > 0)
                {
                    foreach (UIFreemodeDetailsItem item in MissionPanel.Items)
                    {
                        _pause._lobby.CallFunction("ADD_MISSION_PANEL_ITEM", item.Type, item.TextLeft, item.TextRight, (int)item.Icon, (int)item.IconColor, item.Tick, item._labelFont.FontName, item._labelFont.FontID, item._rightLabelFont.FontName, item._rightLabelFont.FontID);
                    }
                }
            }

            _pause._lobby.CallFunction("LOAD_MENU");
            await BaseScript.Delay(500);
            if (listCol[0].Type == "players" || (listCol.Any(x => x.Type == "players") && PlayersColumn.Items.Count > 0 && PlayersColumn.Items[0].KeepPanelVisible))
            {
                if (PlayersColumn.Items[PlayersColumn.CurrentSelection].ClonePed != null)
                    PlayersColumn.Items[PlayersColumn.CurrentSelection].CreateClonedPed();
            }

        }

        private async void buildSettings()
        {
            if (SettingsColumn.Items.Count > 0)
            {
                int i = 0;
                while (i < SettingsColumn.Items.Count)
                {
                    await BaseScript.Delay(1);
                    if (!canBuild) break;
                    UIMenuItem item = SettingsColumn.Items[i];
                    int index = SettingsColumn.Items.IndexOf(item);
                    AddTextEntry($"menu_lobby_desc_{index}", item.Description);
                    BeginScaleformMovieMethod(_pause._lobby.Handle, "ADD_LEFT_ITEM");
                    PushScaleformMovieFunctionParameterInt(item._itemId);
                    PushScaleformMovieMethodParameterString(item._formatLeftLabel);
                    if (item.DescriptionHash != 0 && string.IsNullOrWhiteSpace(item.Description))
                    {
                        BeginTextCommandScaleformString("STRTNM1");
                        AddTextComponentSubstringTextLabelHashKey(item.DescriptionHash);
                        EndTextCommandScaleformString_2();
                    }
                    else
                    {
                        BeginTextCommandScaleformString($"menu_lobby_desc_{index}");
                        EndTextCommandScaleformString_2();
                    }
                    PushScaleformMovieFunctionParameterBool(item.Enabled);
                    PushScaleformMovieFunctionParameterBool(item.BlinkDescription);
                    switch (item)
                    {
                        case UIMenuListItem:
                            UIMenuListItem it = (UIMenuListItem)item;
                            AddTextEntry($"listitem_lobby_{index}_list", string.Join(",", it.Items));
                            BeginTextCommandScaleformString($"listitem_lobby_{index}_list");
                            EndTextCommandScaleformString();
                            PushScaleformMovieFunctionParameterInt(it.Index);
                            PushScaleformMovieFunctionParameterInt((int)it.MainColor);
                            PushScaleformMovieFunctionParameterInt((int)it.HighlightColor);
                            PushScaleformMovieFunctionParameterInt((int)it.TextColor);
                            PushScaleformMovieFunctionParameterInt((int)it.HighlightedTextColor);
                            EndScaleformMovieMethod();
                            break;
                        case UIMenuCheckboxItem:
                            UIMenuCheckboxItem check = (UIMenuCheckboxItem)item;
                            PushScaleformMovieFunctionParameterInt((int)check.Style);
                            PushScaleformMovieMethodParameterBool(check.Checked);
                            PushScaleformMovieFunctionParameterInt((int)check.MainColor);
                            PushScaleformMovieFunctionParameterInt((int)check.HighlightColor);
                            PushScaleformMovieFunctionParameterInt((int)check.TextColor);
                            PushScaleformMovieFunctionParameterInt((int)check.HighlightedTextColor);
                            EndScaleformMovieMethod();
                            break;
                        case UIMenuSliderItem:
                            UIMenuSliderItem prItem = (UIMenuSliderItem)item;
                            PushScaleformMovieFunctionParameterInt(prItem._max);
                            PushScaleformMovieFunctionParameterInt(prItem._multiplier);
                            PushScaleformMovieFunctionParameterInt(prItem.Value);
                            PushScaleformMovieFunctionParameterInt((int)prItem.MainColor);
                            PushScaleformMovieFunctionParameterInt((int)prItem.HighlightColor);
                            PushScaleformMovieFunctionParameterInt((int)prItem.TextColor);
                            PushScaleformMovieFunctionParameterInt((int)prItem.HighlightedTextColor);
                            PushScaleformMovieFunctionParameterInt((int)prItem.SliderColor);
                            PushScaleformMovieFunctionParameterBool(prItem._heritage);
                            EndScaleformMovieMethod();
                            break;
                        case UIMenuProgressItem:
                            UIMenuProgressItem slItem = (UIMenuProgressItem)item;
                            PushScaleformMovieFunctionParameterInt(slItem._max);
                            PushScaleformMovieFunctionParameterInt(slItem._multiplier);
                            PushScaleformMovieFunctionParameterInt(slItem.Value);
                            PushScaleformMovieFunctionParameterInt((int)slItem.MainColor);
                            PushScaleformMovieFunctionParameterInt((int)slItem.HighlightColor);
                            PushScaleformMovieFunctionParameterInt((int)slItem.TextColor);
                            PushScaleformMovieFunctionParameterInt((int)slItem.HighlightedTextColor);
                            PushScaleformMovieFunctionParameterInt((int)slItem.SliderColor);
                            EndScaleformMovieMethod();
                            break;
                        default:
                            PushScaleformMovieFunctionParameterInt((int)item.MainColor);
                            PushScaleformMovieFunctionParameterInt((int)item.HighlightColor);
                            PushScaleformMovieFunctionParameterInt((int)item.TextColor);
                            PushScaleformMovieFunctionParameterInt((int)item.HighlightedTextColor);
                            EndScaleformMovieMethod();
                            _pause._lobby.CallFunction("UPDATE_SETTINGS_ITEM_LABEL_RIGHT", index, item._formatRightLabel);
                            if (item.RightBadge != BadgeIcon.NONE)
                            {
                                _pause._lobby.CallFunction("SET_SETTINGS_ITEM_RIGHT_BADGE", index, (int)item.RightBadge);
                            }
                            break;
                    }
                    _pause._lobby.CallFunction("SET_SETTINGS_ITEM_LABEL_FONT", index, item.labelFont.FontName, item.labelFont.FontID);
                    _pause._lobby.CallFunction("SET_SETTINGS_ITEM_RIGHT_LABEL_FONT", index, item.rightLabelFont.FontName, item.rightLabelFont.FontID);
                    if (item.LeftBadge != BadgeIcon.NONE)
                        _pause._lobby.CallFunction("SET_SETTINGS_ITEM_LEFT_BADGE", index, (int)item.LeftBadge);
                    i++;
                }
                SettingsColumn.CurrentSelection = 0;
            }
        }

        private async void buildPlayers()
        {
            if (PlayersColumn.Items.Count > 0)
            {
                int i = 0;
                while (i < PlayersColumn.Items.Count)
                {
                    LobbyItem item = PlayersColumn.Items[i];
                    int index = PlayersColumn.Items.IndexOf(item);
                    switch (item)
                    {
                        case FriendItem:
                            FriendItem fi = (FriendItem)item;
                            _pause._lobby.CallFunction("ADD_PLAYER_ITEM", 1, 1, fi.Label, (int)fi.ItemColor, fi.ColoredTag, fi.iconL, fi.boolL, fi.iconR, fi.boolR, fi.Status, (int)fi.StatusColor, fi.Rank, fi.CrewTag, fi.KeepPanelVisible);
                            break;
                    }
                    item.Panel?.UpdatePanel(true);
                    i++;
                }
                PlayersColumn.CurrentSelection = 0;
            }
        }

        private async void buildMissions()
        {
            if (MissionsColumn.Items.Count > 0)
            {
                int i = 0;
                while (i < MissionsColumn.Items.Count)
                {
                    MissionItem item = MissionsColumn.Items[i];
                    _pause._lobby.CallFunction("ADD_MISSIONS_ITEM", 0, item.Label, (int)item.MainColor, (int)item.HighlightColor, (int)item.LeftIcon, (int)item.LeftIconColor, (int)item.RightIcon, (int)item.RightIconColor, item.RightIconChecked, item.Enabled);
                    i++;
                }
                MissionsColumn.CurrentSelection = 0;
            }

        }

        private bool controller = false;
        public override async void Draw()
        {
            if (!Visible || TemporarilyHidden) return;
            base.Draw();
            _pause.Draw(true);
            if (_firstDrawTick)
            {
                _pause._lobby.CallFunction("FADE_IN");
                _firstDrawTick = false;
            }
        }

        private int eventType = 0;
        private int itemId = 0;
        private int context = 0;
        private int unused = 0;
        private bool cursorPressed;
        public override async void ProcessMouse()
        {
            if (!IsInputDisabled(2))
            {
                return;
            }
            SetMouseCursorActiveThisFrame();
            SetInputExclusive(2, 239);
            SetInputExclusive(2, 240);
            SetInputExclusive(2, 237);
            SetInputExclusive(2, 238);

            bool success = GetScaleformMovieCursorSelection(_pause._lobby.Handle, ref eventType, ref context, ref itemId, ref unused);
            if (success)
            {
                switch (eventType)
                {
                    case 5:
                        if (FocusLevel != context) { }
                        updateFocus(context, true);
                        switch (listCol[context])
                        {
                            case SettingsListColumn:
                                {
                                    ClearPedInPauseMenu();
                                    SettingsListColumn col = listCol[context] as SettingsListColumn;
                                    if (PlayersColumn != null && PlayersColumn.Items.Count > 0)
                                        foreach (LobbyItem p in PlayersColumn.Items) p.Selected = false;
                                    if (MissionsColumn != null && MissionsColumn.Items.Count > 0)
                                        foreach (MissionItem p in MissionsColumn.Items) p.Selected = false;

                                    if (!col.Items[col.CurrentSelection].Enabled)
                                    {
                                        Game.PlaySound("ERROR", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                        return;
                                    }
                                    if (col.Items[itemId].Selected)
                                    {
                                        BeginScaleformMovieMethod(_pause._lobby.Handle, "SET_INPUT_EVENT");
                                        ScaleformMovieMethodAddParamInt(16);
                                        EndScaleformMovieMethod();
                                        UIMenuItem item = col.Items[itemId];
                                        switch (item)
                                        {
                                            case UIMenuCheckboxItem:
                                                UIMenuCheckboxItem cbIt = item as UIMenuCheckboxItem;
                                                cbIt.Checked = !cbIt.Checked;
                                                cbIt.CheckboxEventTrigger();
                                                break;
                                            default:
                                                item.ItemActivate(null);
                                                break;
                                        }
                                        return;
                                    }
                                    col.CurrentSelection = itemId;
                                }
                                break;
                            case PlayerListColumn:
                                {
                                    PlayerListColumn col = listCol[context] as PlayerListColumn;
                                    if (MissionsColumn != null && MissionsColumn.Items.Count > 0)
                                        foreach (MissionItem p in MissionsColumn.Items) p.Selected = false;
                                    if (SettingsColumn != null && SettingsColumn.Items.Count > 0)
                                        foreach (UIMenuItem p in SettingsColumn.Items) p.Selected = false;
                                    if (!col.Items[col.CurrentSelection].Enabled)
                                    {
                                        Game.PlaySound("ERROR", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                        return;
                                    }
                                    if (col.Items[itemId].Selected)
                                    {
                                        // code here
                                        return;
                                    }
                                    col.CurrentSelection = itemId;
                                    if (col.Items[itemId].ClonePed != null)
                                    {
                                        col.Items[itemId].CreateClonedPed();
                                    }
                                    else ClearPedInPauseMenu();
                                }
                                break;
                            case MissionsListColumn:
                                {
                                    ClearPedInPauseMenu();
                                    MissionsListColumn col = listCol[context] as MissionsListColumn;
                                    if (PlayersColumn != null && PlayersColumn.Items.Count > 0)
                                        foreach (LobbyItem p in PlayersColumn.Items) p.Selected = false;
                                    if (!col.Items[col.CurrentSelection].Enabled)
                                    {
                                        Game.PlaySound("ERROR", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                        return;
                                    }
                                    if (col.Items[itemId].Selected)
                                    {
                                        // code here
                                        return;
                                    }
                                    col.CurrentSelection = itemId;
                                }
                                break;
                        }
                        break;
                }
            }
        }

        public override async void ProcessControls()
        {
            if (!Visible || TemporarilyHidden) return;
            if (Game.IsControlPressed(2, Control.PhoneUp))
            {
                if (Main.GameTime - time > delay)
                {
                    ButtonDelay();
                    GoUp();
                }
            }
            else if (Game.IsControlPressed(2, Control.PhoneDown))
            {
                if (Main.GameTime - time > delay)
                {
                    ButtonDelay();
                    GoDown();
                }
            }

            else if (Game.IsControlPressed(2, Control.PhoneLeft))
            {
                if (Main.GameTime - time > delay)
                {
                    ButtonDelay();
                    GoLeft();
                }
            }
            else if (Game.IsControlPressed(2, Control.PhoneRight))
            {
                if (Main.GameTime - time > delay)
                {
                    ButtonDelay();
                    GoRight();
                }
            }

            else if (Game.IsControlJustPressed(2, Control.FrontendAccept))
            {
                Select();
            }

            else if (Game.IsControlJustReleased(2, Control.PhoneCancel))
            {
                GoBack();
            }

            if (Game.IsControlJustPressed(1, Control.CursorScrollUp))
            {
                _pause.SendScrollEvent(-1);
            }
            else if (Game.IsControlJustPressed(1, Control.CursorScrollDown))
            {
                _pause.SendScrollEvent(1);
            }

            // IsControlBeingPressed doesn't run every frame so I had to use this
            if (!Game.IsControlPressed(2, Control.PhoneUp) && !Game.IsControlPressed(2, Control.PhoneDown) && !Game.IsControlPressed(2, Control.PhoneLeft) && !Game.IsControlPressed(2, Control.PhoneRight))
            {
                times = 0;
                delay = 150;
            }
        }

        public async void Select()
        {
            BeginScaleformMovieMethod(_pause._lobby.Handle, "SET_INPUT_EVENT");
            ScaleformMovieMethodAddParamInt(16);
            int ret = EndScaleformMovieMethodReturnValue();
            while (!IsScaleformMovieMethodReturnValueReady(ret)) await BaseScript.Delay(0);
            string result = GetScaleformMovieFunctionReturnString(ret);

            int[] split = result.Split(',').Select(int.Parse).ToArray();
            if (FocusLevel == SettingsColumn.Order)
            {
                UIMenuItem item = SettingsColumn.Items[SettingsColumn.CurrentSelection];
                if (!item.Enabled)
                {
                    Game.PlaySound("ERROR", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                    return;
                }
                switch (item)
                {
                    case UIMenuCheckboxItem:
                        UIMenuCheckboxItem cbIt = item as UIMenuCheckboxItem;
                        cbIt.Checked = !cbIt.Checked;
                        cbIt.CheckboxEventTrigger();
                        break;
                    case UIMenuListItem:
                        {
                            UIMenuListItem it = item as UIMenuListItem;
                            it.ListSelectedTrigger(it.Index);
                            break;
                        }
                    default:
                        item.ItemActivate(null);
                        break;
                }
            }
            else if (FocusLevel == PlayersColumn.Order)
            {

            }
        }

        public async void GoBack()
        {
            if (CanPlayerCloseMenu)
                Visible = false;
        }

        public async void GoUp()
        {
            string result = await _pause._lobby.CallFunctionReturnValueString("SET_INPUT_EVENT", 8);

            int[] split = result.Split(',').Select(int.Parse).ToArray();
            Debug.WriteLine(string.Join(", ", split));
            //updateFocus(split[0]);
            if (listCol[FocusLevel] is SettingsListColumn)
            {
                SettingsColumn.CurrentSelection = split[1];
                SettingsColumn.IndexChangedEvent();
            }
            else if (listCol[FocusLevel] is MissionsListColumn)
            {
                MissionsColumn.CurrentSelection = split[1];
                MissionsColumn.IndexChangedEvent();
            }
            else if (listCol[FocusLevel] is PlayerListColumn)
            {
                PlayersColumn.CurrentSelection = split[1];
                PlayersColumn.IndexChangedEvent();
            }
        }

        public async void GoDown()
        {
            string result = await _pause._lobby.CallFunctionReturnValueString("SET_INPUT_EVENT", 9);

            int[] split = result.Split(',').Select(int.Parse).ToArray();
            //updateFocus(split[0]);
            if (listCol[FocusLevel] is SettingsListColumn)
            {
                SettingsColumn.CurrentSelection = split[1];
                SettingsColumn.IndexChangedEvent();
            }
            else if (listCol[FocusLevel] is MissionsListColumn)
            {
                MissionsColumn.CurrentSelection = split[1];
                MissionsColumn.IndexChangedEvent();
            }
            else if (listCol[FocusLevel] is PlayerListColumn)
            {
                PlayersColumn.CurrentSelection = split[1];
                PlayersColumn.IndexChangedEvent();
            }
        }

        public async void GoLeft()
        {
            string result = await _pause._lobby.CallFunctionReturnValueString("SET_INPUT_EVENT", 10);

            int[] split = result.Split(',').Select(int.Parse).ToArray();

            if (listCol.Any(x => x.Type == "settings"))
                SettingsColumn.Items[SettingsColumn.CurrentSelection].Selected = false;
            if (listCol.Any(x => x.Type == "missions"))
                MissionsColumn.Items[MissionsColumn.CurrentSelection].Selected = false;
            if (listCol.Any(x => x.Type == "players"))
            {
                PlayersColumn.Items[PlayersColumn.CurrentSelection].Selected = false;
                if (listCol[0].Type == "players" || PlayersColumn.Items[PlayersColumn.CurrentSelection].KeepPanelVisible)
                {
                    if (PlayersColumn.Items[PlayersColumn.CurrentSelection].ClonePed != null)
                        PlayersColumn.Items[PlayersColumn.CurrentSelection].CreateClonedPed();
                    else
                        ClearPedInPauseMenu();
                }
                else
                    ClearPedInPauseMenu();
            }
            else
                ClearPedInPauseMenu();

            switch (listCol[focusLevel].Type)
            {
                case "settings":
                    {
                        UIMenuItem item = SettingsColumn.Items[SettingsColumn.CurrentSelection];
                        if (!item.Enabled)
                        {
                            SettingsColumn.Items[SettingsColumn.CurrentSelection].Selected = false;
                            updateFocus(focusLevel - 1);
                            return;
                        }

                        if (item is UIMenuListItem it)
                        {
                            it.Index = split[2];
                            //ListChange(it, it.Index);
                            it.ListChangedTrigger(it.Index);
                        }
                        else if (item is UIMenuSliderItem slit)
                        {
                            slit.Value = split[2];
                            slit.SliderChanged(slit.Value);
                            //SliderChange(it, it.Value);
                        }
                        else if (item is UIMenuProgressItem prit)
                        {
                            prit.Value = split[2];
                            prit.ProgressChanged(prit.Value);
                            //ProgressChange(it, it.Value);
                        }
                        else
                        {
                            SettingsColumn.Items[SettingsColumn.CurrentSelection].Selected = false;
                            updateFocus(focusLevel - 1);
                        }
                    }
                    break;
                case "missions":
                    MissionsColumn.Items[MissionsColumn.CurrentSelection].Selected = false;
                    updateFocus(focusLevel - 1);
                    break;
                case "panel":
                    updateFocus(focusLevel - 1);
                    break;
                case "players":
                    PlayersColumn.Items[PlayersColumn.CurrentSelection].Selected = false;
                    updateFocus(focusLevel - 1);
                    break;
            }
        }

        public async void GoRight()
        {
            string result = await _pause._lobby.CallFunctionReturnValueString("SET_INPUT_EVENT", 11);

            int[] split = result.Split(',').Select(int.Parse).ToArray();

            if (listCol.Any(x => x.Type == "settings"))
                SettingsColumn.Items[SettingsColumn.CurrentSelection].Selected = false;
            if (listCol.Any(x => x.Type == "missions"))
                MissionsColumn.Items[MissionsColumn.CurrentSelection].Selected = false;
            if (listCol.Any(x => x.Type == "players"))
            {
                PlayersColumn.Items[PlayersColumn.CurrentSelection].Selected = false;
                if (listCol[0].Type == "players" || PlayersColumn.Items[PlayersColumn.CurrentSelection].KeepPanelVisible)
                {
                    if (PlayersColumn.Items[PlayersColumn.CurrentSelection].ClonePed != null)
                        PlayersColumn.Items[PlayersColumn.CurrentSelection].CreateClonedPed();
                    else
                        ClearPedInPauseMenu();
                }
                else
                    ClearPedInPauseMenu();
            }
            else
                ClearPedInPauseMenu();

            switch (listCol[focusLevel].Type)
            {
                case "settings":
                    {
                        UIMenuItem item = SettingsColumn.Items[SettingsColumn.CurrentSelection];
                        if (!item.Enabled)
                        {
                            SettingsColumn.Items[SettingsColumn.CurrentSelection].Selected = false;
                            updateFocus(focusLevel - 1);
                            return;
                        }

                        if (item is UIMenuListItem it)
                        {
                            it.Index = split[2];
                            //ListChange(it, it.Index);
                            it.ListChangedTrigger(it.Index);
                        }
                        else if (item is UIMenuSliderItem slit)
                        {
                            slit.Value = split[2];
                            slit.SliderChanged(slit.Value);
                            //SliderChange(it, it.Value);
                        }
                        else if (item is UIMenuProgressItem prit)
                        {
                            prit.Value = split[2];
                            prit.ProgressChanged(prit.Value);
                            //ProgressChange(it, it.Value);
                        }
                        else
                        {
                            SettingsColumn.Items[SettingsColumn.CurrentSelection].Selected = false;
                            updateFocus(focusLevel - 1);
                        }
                    }
                    break;
                case "missions":
                    MissionsColumn.Items[MissionsColumn.CurrentSelection].Selected = false;
                    updateFocus(focusLevel - 1);
                    break;
                case "panel":
                    updateFocus(focusLevel - 1);
                    break;
                case "players":
                    PlayersColumn.Items[PlayersColumn.CurrentSelection].Selected = false;
                    updateFocus(focusLevel - 1);
                    break;
            }
        }

        void ButtonDelay()
        {
            // Increment the "changed indexes" counter
            times++;

            // Each time "times" is a multiple of 5 we decrease the delay.
            // Min delay for the scaleform is 50.. less won't change due to the
            // awaiting time for the scaleform itself.
            if (times % 5 == 0)
            {
                delay -= 10;
                if (delay < 50) delay = 50;
            }
            // Reset the time to the current game timer.
            time = Main.GameTime;
        }

        internal void SendPauseMenuOpen()
        {
            OnLobbyMenuOpen?.Invoke(this);
        }

        internal void SendPauseMenuClose()
        {
            OnLobbyMenuClose?.Invoke(this);
        }



    }
}
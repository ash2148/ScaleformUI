﻿using CitizenFX.Core.Native;
using ScaleformUI.LobbyMenu;
using ScaleformUI.Scaleforms;

namespace ScaleformUI.PauseMenu
{
    public class PlayerListTab : BaseTab
    {
        private const int V = 2;
        private bool _focused;
        private int focus = 0;
        internal List<Column> listCol;

        public override bool Focused
        {
            get { return _focused; }
            set
            {
                _focused = value;
            }
        }
        public int Focus
        {
            get => focus;
        }
        public SettingsListColumn SettingsColumn { get; private set; }
        public PlayerListColumn PlayersColumn { get; private set; }
        public MissionsListColumn MissionsColumn { get; private set; }
        public MissionDetailsPanel MissionPanel { get; private set; }
        public PlayerListTab(string name) : base(name)
        {
            _type = V;
            SettingsColumn = new("", HudColor.NONE);
            PlayersColumn = new("", HudColor.NONE);
        }

        internal async void UpdateFocus(int value, bool isMouse = false)
        {
            bool goingLeft = value < focus;
            if (listCol[value].Type != "players")
            {
                if (PlayersColumn != null && PlayersColumn.Items.Count > 0 && !PlayersColumn.Items[PlayersColumn.CurrentSelection].KeepPanelVisible)
                    API.ClearPedInPauseMenu();
            }
            focus = value;
            if (focus < 0)
                focus = listCol.Count - 1;
            else if (focus > listCol.Count - 1)
                focus = 0;
            if (listCol[Focus].Type == "panel")
            {
                if (goingLeft)
                    UpdateFocus(focus - 1, isMouse);
                else
                    UpdateFocus(focus + 1, isMouse);
                return;
            }
            if (Parent != null && Parent.Visible)
            {
                int idx = await Parent._pause._pause.CallFunctionReturnValueInt("SET_PLAYERS_TAB_FOCUS", Parent.Tabs.IndexOf(this), focus);
                if (!isMouse)
                {
                    switch (listCol[Focus].Type)
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

        public void SetUpColumns(List<Column> columns)
        {
            if (columns.Count > 3)
                throw new Exception("You must have 3 columns!");
            if (columns.Count == 3 && columns[2] is PlayerListColumn)
                throw new Exception("For panel designs reasons, you can't have Players list in 3rd column!");

            listCol = columns;
            foreach (Column col in columns)
            {
                if (this.Parent != null)
                {
                    col.Parent = this.Parent;
                    col.ParentTab = Parent.Tabs.IndexOf(this);
                }

                switch (col)
                {
                    case SettingsListColumn:
                        SettingsColumn = col as SettingsListColumn;
                        SettingsColumn.Order = columns.IndexOf(col);
                        break;
                    case PlayerListColumn:
                        PlayersColumn = col as PlayerListColumn;
                        PlayersColumn.Order = columns.IndexOf(col);
                        break;
                    case MissionsListColumn:
                        MissionsColumn = col as MissionsListColumn;
                        MissionsColumn.Order = columns.IndexOf(col);
                        break;
                    case MissionDetailsPanel:
                        MissionPanel = col as MissionDetailsPanel;
                        MissionPanel.Order = columns.IndexOf(col);
                        break;
                }
            }
        }
    }
}
﻿using CitizenFX.Core;

namespace ScaleformUI.Scaleforms
{
    public class MediumMessageHandler
    {
        internal ScaleformWideScreen _sc;
        private int _start;
        private int _timer;
        private bool _hasAnimatedOut;

        public MediumMessageHandler() { }

        public async Task Load()
        {
            if (_sc != null) return;
            _sc = new ScaleformWideScreen("MIDSIZED_MESSAGE");

            int timeout = 1000;
            int start = Main.GameTime;
            while (!_sc.IsLoaded && Main.GameTime - start < timeout) await BaseScript.Delay(0);
        }

        public void Dispose()
        {
            _sc.Dispose();
            _sc = null;
        }

        public async void ShowColoredShard(string msg, string desc, HudColor bgColor, bool useDarkerShard = false, bool useCondensedShard = false, int time = 5000)
        {
            await Load();
            _start = Main.GameTime;
            _sc.CallFunction("SHOW_SHARD_MIDSIZED_MESSAGE", msg, desc, (int)bgColor, useDarkerShard, useCondensedShard);
            _timer = time;
            _hasAnimatedOut = false;
        }

        internal async void Update()
        {
            _sc.Render2D();
            if (_start != 0 && Main.GameTime - _start > _timer)
            {
                if (!_hasAnimatedOut)
                {
                    _sc.CallFunction("SHARD_ANIM_OUT", (int)HudColor.HUD_COLOUR_PURPLE, 750);
                    _hasAnimatedOut = true;
                    _timer += 750;
                }
                else
                {
                    Audio.PlaySoundFrontend("Shard_Disappear", "GTAO_FM_Events_Soundset");
                    _start = 0;
                    Dispose();
                    _sc = null;
                }
            }
        }
    }
}

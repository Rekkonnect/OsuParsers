﻿using OsuParsers.Enums.Beatmaps;
using System.Numerics;

namespace OsuParsers.Beatmaps.Objects.Mania
{
    public class ManiaHold : ManiaHit
    {
        public ManiaHold(Vector2 position, int startTime, int endTime, HitSoundType hitSound, Extras extras, bool isNewCombo, int comboOffset)
            : base(position, startTime, endTime, hitSound, extras, isNewCombo, comboOffset)
        {
        }
    }
}

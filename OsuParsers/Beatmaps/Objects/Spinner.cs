﻿using OsuParsers.Enums.Beatmaps;
using System.Numerics;

namespace OsuParsers.Beatmaps.Objects;

public class Spinner : HitObject, ISpinnerHitObject
{
    public Spinner(Vector2 position, int startTime, int endTime, HitSoundType hitSound, Extras extras, bool isNewCombo, int comboOffset)
        : base(position, startTime, endTime, hitSound, extras, isNewCombo, comboOffset)
    {
    }
}

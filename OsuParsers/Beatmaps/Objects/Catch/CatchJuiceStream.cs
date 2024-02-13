using OsuParsers.Enums.Beatmaps;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace OsuParsers.Beatmaps.Objects.Catch;

// TODO: Change inheritance to not rely on Slider
public class CatchJuiceStream : Slider, ISliderHitObject
{
    public CatchJuiceStream(Vector2 position, int startTime, int endTime, HitSoundType hitSound, CurveType type,
        List<Vector2> points, int repeats, double pixelLength, bool isNewCombo, int comboOffset, List<HitSoundType> edgeHitSounds = null,
        List<Tuple<SampleSet, SampleSet>> edgeAdditions = null, Extras extras = null)
        : base(position, startTime, endTime, hitSound, type, points, repeats, pixelLength, isNewCombo, comboOffset, edgeHitSounds, edgeAdditions, extras)
    {
    }
}

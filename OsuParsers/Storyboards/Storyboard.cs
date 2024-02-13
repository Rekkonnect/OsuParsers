using OsuParsers.Enums.Storyboards;
using OsuParsers.Storyboards.Interfaces;
using System.Collections.Generic;
using System.IO;
using OsuParsers.Writers;

namespace OsuParsers.Storyboards;

public class Storyboard
{
    public List<IStoryboardObject> BackgroundLayer = [];
    public List<IStoryboardObject> FailLayer = [];
    public List<IStoryboardObject> PassLayer = [];
    public List<IStoryboardObject> ForegroundLayer = [];
    public List<IStoryboardObject> OverlayLayer = [];
    public List<IStoryboardObject> SamplesLayer = [];

    public Dictionary<string, string> Variables = [];

    /// <summary>
    /// Returns specified storyboard layer.
    /// </summary>
    public List<IStoryboardObject> GetLayer(StoryboardLayer layer)
    {
        return layer switch
        {
            StoryboardLayer.Background => BackgroundLayer,
            StoryboardLayer.Fail => FailLayer,
            StoryboardLayer.Pass => PassLayer,
            StoryboardLayer.Foreground => ForegroundLayer,
            StoryboardLayer.Overlay => OverlayLayer,
            StoryboardLayer.Samples => SamplesLayer,
            _ => BackgroundLayer,
        };
    }

    /// <summary>
    /// Saves this <see cref="Storyboard"/> to the specified path.
    /// </summary>
    public void Save(string path)
    {
        File.WriteAllLines(path, StoryboardEncoder.Encode(this));
    }
}

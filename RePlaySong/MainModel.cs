using RePlaySong.Properties;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace RePlaySong
{
    public class MainModel
    {
        public Dictionary<string, string> Songs { get; set; }

        public MainModel()
        { 
            if(string.IsNullOrEmpty(Settings.Default.SongsDictionaryJson))
            {
                Settings.Default.SongsDictionaryJson = "";
            }
            else
            {
                Songs = JsonConvert.DeserializeObject<Dictionary<string, string>>(Settings.Default.SongsDictionaryJson);
                if(Songs!=null) Songs.Keys.OrderBy(i => i);
            }

        }
    }
}

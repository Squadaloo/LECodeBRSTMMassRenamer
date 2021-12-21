using System;
using System.Collections.Generic;
using System.Text;

namespace LECodeBSTRMMassRenamer
{
    public class TrackDefinition
    {
        public string MusicSlotID { get; set; }
        public string TrackSlotID { get; set; }
        public string FileName { get; set; }
        public string Info { get; set; }
        public string ID { get; set; }

        public TrackDefinition(string trackConfig)
        {
            string[] trackConfigSplit = trackConfig.Split(";");
            trackConfigSplit[0] = trackConfigSplit[0].Trim().Substring(1); // Cut out first letter (we've confirmed it's H or T)
            this.MusicSlotID = trackConfigSplit[0].Trim();
            //Technically we really only need the file name, but hey, in case the other settings become relevant in the future, might as well get them!
            if(trackConfigSplit.Length > 1)
            {
                this.MusicSlotID = trackConfigSplit[1].Trim();
            }
            if (trackConfigSplit.Length > 2)
            {
                this.TrackSlotID = trackConfigSplit[2].Trim();
            }
            if (trackConfigSplit.Length > 3)
            {
                this.FileName = trackConfigSplit[3].Replace("\"", "").Trim();
            }
            if (trackConfigSplit.Length > 4)
            {
                this.Info = trackConfigSplit[4].Replace("\"", "").Trim();
            }
            if (trackConfigSplit.Length > 5)
            {
                this.ID = trackConfigSplit[5].Replace("\"", "").Trim();
            }
        }
    }
}

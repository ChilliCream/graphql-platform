using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.ConferencePlanner.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HotChocolate.ConferencePlanner.Imports
{
    public class DataImporter
    {
        public async Task LoadDataAsync(ApplicationDbContext db)
        {
            using Stream? stream = GetType().Assembly.GetManifestResourceStream(
                "HotChocolate.ConferencePlanner.Imports.Data.json")!;
            using var reader = new JsonTextReader(new StreamReader(stream));

            var speakerNames = new Dictionary<string, Speaker>();
            var tracks = new Dictionary<string, Track>();

            JArray conference = await JArray.LoadAsync(reader);
            var speakers = new Dictionary<string, Speaker>();

            foreach (JObject conferenceDay in conference)
            {
                foreach (JObject roomData in conferenceDay["rooms"]!)
                {
                    var track = new Track
                    {
                        Name = roomData["name"]!.ToString()
                    };

                    foreach (JObject sessionData in roomData["sessions"]!)
                    {
                        var session = new Session
                        {
                            Title = sessionData["title"]!.ToString(),
                            Abstract = sessionData["description"]!.ToString(),
                            StartTime = sessionData["startsAt"]!.Value<DateTime>(),
                            EndTime = sessionData["endsAt"]!.Value<DateTime>(),
                        };

                        track.Sessions.Add(session);

                        foreach (JObject speakerData in sessionData["speakers"]!)
                        {
                            if (!speakers.TryGetValue(speakerData["id"]!.ToString(), out Speaker? speaker))
                            {
                                speaker = new Speaker
                                { 
                                    Name = speakerData["name"]!.ToString()
                                };
                                db.Speakers.Add(speaker);
                            }

                            session.SessionSpeakers.Add(new SessionSpeaker
                            {
                                Speaker = speaker,
                                Session = session
                            });
                        }
                    }

                    db.Tracks.Add(track);
                }
            }

            await db.SaveChangesAsync();
        }
    }
}

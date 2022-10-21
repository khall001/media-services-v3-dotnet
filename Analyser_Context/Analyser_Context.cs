using Newtonsoft.Json;


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Security.AccessControl;

namespace Analyser_Context
{


    using Analyser_Context;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;
    using System.Data.SqlClient;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters;
    using System.Security.AccessControl;


public class Anlyser_Context : DbContext
    {
        public DbSet<Analyser_Output_Data> Analyser_Output_Datas { get; set; }
        public Anlyser_Context() : base(@"Server=(localdb)\MSSQLLocalDB;Database=Media_Analyser;Trusted_Connection=True")
        {

        }
        public void AddAudioEntries(Guid VideoGuid, string tarnscriptVTT, string transcriptText)
        {
            Console.WriteLine("dataBase " + VideoGuid + "\n" + transcriptText);
            var dataBaseEntrie = new Analyser_Output_Data()
            {
                Video_Guid = VideoGuid,
                TranscriptVtt = tarnscriptVTT,
                TranscriptText = transcriptText,
                Faces_Names = string.Empty, 
            };
            var db = Analyser_Output_Datas.Where(p => p.Video_Guid.ToString().Equals(VideoGuid.ToString())).ToArray();
            if (db.Length > 0)
            {
                db.First().TranscriptText = transcriptText;
                db.First().TranscriptVtt = tarnscriptVTT;
                db.First().Faces_Names = string.Empty;
            }
            else
            {
                Analyser_Output_Datas.Add(dataBaseEntrie);
            }
            SaveChanges();
        }

        public void AddVideoEntries(Guid VideoGuid, string transcriptVtt, string transcriptText, string facesNames)
        {
            var dataBaseEntrie = new Analyser_Output_Data()
            {
                Video_Guid = VideoGuid,
                TranscriptVtt = transcriptVtt,
                TranscriptText = transcriptText,
                Faces_Names = facesNames
            };
            var db = Analyser_Output_Datas.Where(p => p.Video_Guid.ToString().Equals(VideoGuid.ToString())).ToArray();
            if (db.Length > 0)
            {
                Console.WriteLine("Change the Data Base");
                db.First().TranscriptText = transcriptText;
                db.First().TranscriptVtt = transcriptVtt;
                db.First().Faces_Names = facesNames;
            }
            else
            {
                Console.WriteLine("New Entrie in Data Base");
                Analyser_Output_Datas.Add(dataBaseEntrie);
            }
            SaveChanges();
        }
        public string LoadAnalyserData(Guid videoGuid)
        {
            var db = Analyser_Output_Datas.Where(p => p.Video_Guid.ToString().Equals(videoGuid.ToString())).ToArray();
            if (db.Length > 0)
            {
                var dataBaseEntrie = new Analyser_Output_Data()
                {
                    TranscriptVtt = db.First().TranscriptVtt,
                    TranscriptText = db.First().TranscriptText,
                    Faces_Names = db.First().Faces_Names
                };
                return JsonConvert.SerializeObject(dataBaseEntrie);
            }
            return string.Empty;
        }
        public void SaveChangeAnalyserData(Guid videoGuid, string? transcriptVtt = null, string? transcriptText = null, string? facesNames = null)
        {
            var db = Analyser_Output_Datas.Where(p => p.Video_Guid.ToString().Equals(videoGuid.ToString())).ToArray();
            if (db.Length > 0)
            {
                if (!string.IsNullOrEmpty(transcriptVtt))
                {
                    db.First().TranscriptVtt = transcriptVtt;
                }
                if (!string.IsNullOrEmpty(transcriptText))
                {
                    db.First().TranscriptText = transcriptText;
                }
                if (!string.IsNullOrEmpty(facesNames))
                {
                    db.First().Faces_Names = facesNames;
                }
                SaveChanges();
            }
        }
    }
    public class FacesNames
    {
        Guid ImageId { get; set; }
        public string ImageName { get; set; }
        public string PersonName { get; set; }
    }

    public class Analyser_Output_Data
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DataId { get; set; }
        public Guid Video_Guid { get; set; }
        public byte[] Faces { get; set; }
        public string Faces_Names { get; set; }
        public string TranscriptVtt { get; set; }
        public string TranscriptText { get; set; }

    }



}
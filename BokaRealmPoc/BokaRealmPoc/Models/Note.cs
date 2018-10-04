using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Realms;
using Realms.Sync;

namespace BokaRealmPoc.Models
{
    public enum NoteType
    {
        Unknown = 0,
        LandNote = 1,
        EquipmentNote = 2,
        OtherNote = 3
    }

    public class Land : RealmObject
    {
        [PrimaryKey]
        [Required]
        public string Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Backlink(nameof(LandNote.Land))]
        public IQueryable<LandNote> LandNotes { get; }

        public IList<Permission> Permissions { get; }

        public DateTimeOffset DueDate { get; set; }
    }
    
    public class LandNote : RealmObject
    {
        [PrimaryKey]
        public string Id { get; set; }

        public Land Land { get; set; }

        public Note Note { get; set; }

        public IList<Permission> Permissions { get; }
    }

    public class Note : RealmObject
    {
        private string _imageCount;

        [PrimaryKey]
        [Required]
        public string Id { get; set; }

        [Required]
        public string Title { get; set; }

        public int NoteTypeId { get; set; }

        public IList<LandNote> LandNotes { get; }

        [Ignored]
        public string ImageCount
        {
            get => _imageCount;
            set
            {
                _imageCount = value;
                RaisePropertyChanged(nameof(ImageCount));
            }
        }

        //public void Update(string count)
        //{
        //    _imageCount = count;
        //    RaisePropertyChanged(nameof(ImageCount));
        //}

        // Non-persisted properties
        public NoteType NoteType
        {
            get
            {
                switch (NoteTypeId)
                {
                    case (int)NoteType.LandNote:
                        return NoteType.LandNote;
                    case (int)NoteType.EquipmentNote:
                        return NoteType.EquipmentNote;
                    case (int)NoteType.OtherNote:
                        return NoteType.OtherNote;
                    default:
                        return NoteType.Unknown;
                }
            }
        }
        //                 title = $"{title} på {string.Join(", ", lands.Select(l => l.Name))}";

        public string ListTitle => LandNotes != null && LandNotes.Any() ? $"{Title } på {string.Join(", ", LandNotes.Select(l => l.Land.Title))}" : Title;

        public string IconSource
        {
            get
            {
                switch (NoteType)
                {
                    case NoteType.LandNote:
                        return "";
                    case NoteType.EquipmentNote:
                        return "";
                    case NoteType.OtherNote:
                        return "";
                    default:
                        return "";
                }
            }
        }

        public DateTimeOffset CreatedAt { get; set; }

        public IList<Permission> Permissions { get; }
        public DateTimeOffset DueDate { get; set; }
    }
}

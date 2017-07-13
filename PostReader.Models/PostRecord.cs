using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace PostReader.Models
{
    [DataContract]
    public class PostRecord
    {
        [DataMember(Name ="id")]
        public string Id { get; set; }

        [DataMember(Name ="voice")]
        public string Voice { get; set; }

        [DataMember(Name ="text")]
        public string Text { get; set; }

        [DataMember(Name ="status")]
        public string Status { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }
    }
}

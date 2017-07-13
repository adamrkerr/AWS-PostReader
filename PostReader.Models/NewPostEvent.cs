using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace PostReader.Models
{
    [DataContract]
    public class NewPostEvent
    {
        [DataMember(Name ="voice")]
        public string Voice { get; set; }
        
        [DataMember(Name = "text")]
        public string Text { get; set; }

    }
}

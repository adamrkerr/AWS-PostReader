using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace PostReader.Models
{
    [DataContract]
    public class PostRequestEvent
    {
        [DataMember(Name ="postId")]
        public string RecordId { get; set; }
    }
}

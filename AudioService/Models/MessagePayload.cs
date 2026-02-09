using System;
using System.Collections.Generic;
using System.Text;

namespace AudioService.Models
{


    public class MessagePayload
    {
        public int AudioId { get; set; }
        public string FilePath { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> DeviceTokens { get; set; } = [];
    }

}

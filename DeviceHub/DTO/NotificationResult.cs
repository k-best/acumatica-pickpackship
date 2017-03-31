using System;
using System.Collections.Generic;

namespace Acumatica.DeviceHub.DTO
{
    public class NotificationResult
    {
        public PrintJobParameter[] Inserted { get; set; }

        public PrintJobParameter[] Deleted { get; set; }

        public string Query { get; set; }

        public string CompanyId { get; set; }

        public Guid Id { get; set; }

        public long TimeStamp { get; set; }

        public Dictionary<string, object> AdditionalInfo { get; set; }
    }
}
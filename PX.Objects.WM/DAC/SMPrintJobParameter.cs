using System;
using PX.Data;

namespace PX.SM
{
    [Serializable]
    [PXCacheName(PX.Objects.WM.Messages.SMPrintJobParameter)]
    public class SMPrintJobParameter : IBqlTable
    {
        public abstract class jobID : PX.Data.IBqlField { }
        [PXDBInt(IsKey = true)]
        [PXDBDefault(typeof(SMPrintJob.jobID))]
        [PXParent(typeof(Select<SMPrintJob, Where<SMPrintJob.jobID, Equal<Current<SMPrintJobParameter.jobID>>>>))]
        [PXUIField(DisplayName = "Job ID")]
        public virtual int? JobID { get; set; }

        public abstract class parameterName : PX.Data.IBqlField { }
        [PXDBString(255, IsKey = true, IsUnicode = true)]
        [PXUIField(DisplayName = "Parameter Name")]
        public virtual string ParameterName { get; set; }

        public abstract class parameterValue : PX.Data.IBqlField { }
        [PXDBString(255, IsUnicode = true)]
        [PXDefault]
        [PXUIField(DisplayName = "Parameter Value")]
        public virtual string ParameterValue { get; set; }
    }
}

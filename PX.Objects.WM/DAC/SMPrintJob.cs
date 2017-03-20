using System;
using PX.Data;

namespace PX.SM
{
    [Serializable]
    public class SMPrintJob : IBqlTable
    {
        public abstract class jobID : PX.Data.IBqlField { }
        [PXDBIdentity(IsKey = true)]
        [PXDefault]
        [PXUIField(DisplayName = "Job ID", Visibility = PXUIVisibility.SelectorVisible)]
        [PXSelector(typeof(SMPrintJob.jobID))]
        public virtual int? JobID { get; set; }

        public abstract class description : PX.Data.IBqlField { }
        [PXDBString(255, IsUnicode = true)]
        public virtual string Description { get; set; }

        public abstract class printQueue : PX.Data.IBqlField { }
        [PXDBString(10)]
        [PXDefault]
        [PXSelector(typeof(SMPrintQueue.printQueue))]
        [PXUIField(DisplayName = "Print Queue", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual string PrintQueue { get; set; }

        public abstract class reportID : PX.Data.IBqlField { }
        [PXDBString(8, InputMask = "aa.aa.aa.aa")]
        [PXUIField(DisplayName = "Report ID", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual string ReportID { get; set; }
        
        #region System Columns
        public abstract class createdByID : PX.Data.IBqlField { }
        [PXDBCreatedByID]
        public virtual Guid? CreatedByID { get; set; }

        public abstract class createdByScreenID : PX.Data.IBqlField { }
        [PXDBCreatedByScreenID]
        public virtual String CreatedByScreenID { get; set; }

        public abstract class createdDateTime : PX.Data.IBqlField { }
        [PXDBCreatedDateTime]
        public virtual DateTime? CreatedDateTime { get; set; }

        public abstract class lastModifiedByID : PX.Data.IBqlField { }
        [PXDBLastModifiedByID]
        public virtual Guid? LastModifiedByID { get; set; }

        public abstract class lastModifiedByScreenID : PX.Data.IBqlField { }
        [PXDBLastModifiedByScreenID]
        public virtual String LastModifiedByScreenID { get; set; }

        public abstract class lastModifiedDateTime : PX.Data.IBqlField { }
        [PXDBLastModifiedDateTime]
        public virtual DateTime? LastModifiedDateTime { get; set; }

        public abstract class Tstamp : PX.Data.IBqlField { }
        [PXDBTimestamp]
        public virtual Byte[] tstamp { get; set; }
        #endregion
    }
}

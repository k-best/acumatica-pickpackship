using System;
using PX.Data;

namespace PX.SM
{
    [Serializable]
    [PXCacheName(PX.Objects.WM.Messages.SMPrintQueue)]
    public class SMPrintQueue : IBqlTable
    {
        public abstract class printQueue : PX.Data.IBqlField { }
        [PXDBString(10, IsKey = true)]
        [PXDefault]
        [PXUIField(DisplayName = "Print Queue", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual string PrintQueue { get; set; }

        public abstract class descr : PX.Data.IBqlField { }
        [PXDBString(100, IsUnicode = true)]
        [PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual string Descr { get; set; }

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

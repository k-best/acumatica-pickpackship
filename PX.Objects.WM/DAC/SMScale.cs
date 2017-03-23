using System;
using PX.Data;

namespace PX.SM
{
    [Serializable]
    [PXCacheName(PX.Objects.WM.Messages.SMScale)]
    public class SMScale : IBqlTable
    {
        public abstract class scaleID : PX.Data.IBqlField { }
        [PXDBString(10, IsKey = true)]
        [PXDefault]
        [PXSelector(typeof(scaleID))]
        [PXUIField(DisplayName = "Scale ID", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual string ScaleID { get; set; }

        public abstract class descr : PX.Data.IBqlField { }
        [PXDBString(100, IsUnicode = true)]
        [PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual string Descr { get; set; }

        public abstract class lastWeight : PX.Data.IBqlField { }
        [PXDBDecimal(4)]
        [PXUIField(DisplayName = "Last Weight")]
        public virtual decimal? LastWeight { get; set; }
        
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
        [PXUIField(DisplayName = "Last Updated")]
        public virtual DateTime? LastModifiedDateTime { get; set; }

        public abstract class Tstamp : PX.Data.IBqlField { }
        [PXDBTimestamp]
        public virtual Byte[] tstamp { get; set; }
        #endregion
    }
}

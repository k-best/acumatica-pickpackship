using System;
using PX.Data;
using PX.Objects.IN;


namespace PX.Objects.SO
{
    public class SOShipLineSplitExt : PXCacheExtension<SOShipLineSplit>
    {
        public abstract class packageLineNbr : IBqlField { }
        [PXUIField(DisplayName = "Package Line Nbr.", Visible = false)]
        [PXDBInt]
        public virtual int? PackageLineNbr { get; set; }
    }
}

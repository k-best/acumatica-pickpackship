using System;
using PX.Data;
using PX.Objects.IN;


namespace PX.Objects.SO
{
    public class SOShipLineSplitPick : SOShipLineSplit
    {
        public abstract new class shipmentNbr : PX.Data.IBqlField { }
        public abstract new class lineNbr : PX.Data.IBqlField { }

        public abstract class packageLineNbr : IBqlField { }
        [PXUIField(DisplayName = "Package Line Nbr.", Visible = false)]
        [PXInt]
        public virtual int? PackageLineNbr { get; set; }
    }
}

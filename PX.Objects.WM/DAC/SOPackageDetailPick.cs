using System;
using PX.Data;
using PX.Objects.SO;

namespace PX.Objects.SO
{
    public class SOPackageDetailPick : SOPackageDetail
    {
        public abstract new class shipmentNbr : PX.Data.IBqlField { }
        public abstract new class lineNbr : PX.Data.IBqlField { }

        public abstract class isCurrent : PX.Data.IBqlField { }
        [PXBool]
        [PXUIField(DisplayName = "Current")]
        public bool? IsCurrent { get; set; }
    }
}

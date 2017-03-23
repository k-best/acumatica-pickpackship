using System;
using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.CS
{
    public class CarrierExt : PXCacheExtension<Carrier>
    {
        public abstract class returnLabel : PX.Data.IBqlField { }
        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Generate Return Label Automatically")]
        public virtual bool? ReturnLabel { get; set; }
    }
}

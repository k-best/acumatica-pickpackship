using System;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.SO
{
    [Serializable]
    public class SOShipLinePick : SOShipLine
    {
        public abstract class pickedQty : IBqlField { }
        [PXQuantity(typeof(SOShipLine.uOM), typeof(SOShipLine.baseShippedQty))]
        [PXUIField(DisplayName = "Picked Qty.", Enabled = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual Decimal? PickedQty { get; set; }
    }
}

using System;
using PX.Data;

namespace PX.Objects.CS
{
    //TODO: Move to CarrierMaint graph when we integrate into Acumatica code base
    public class CSCarrierMaintExt : PXGraphExtension<CarrierMaint>
    {
        protected virtual void Carrier_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            Carrier row = e.Row as Carrier;
            if (row != null)
            {
                PXUIFieldAttribute.SetVisible<CarrierExt.returnLabel>(sender, row, row.IsExternal == true);
           }
        }
    }
}

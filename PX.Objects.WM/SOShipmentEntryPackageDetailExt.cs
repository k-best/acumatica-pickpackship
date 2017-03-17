using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;


namespace PX.Objects.SO
{
    public class SOShipmentEntryPackageDetailExt : PXGraphExtension<SOShipmentEntry>
    {
        public PXSelect<SOShipLineSplit,
            Where<SOShipLineSplit.shipmentNbr, Equal<Current<SOPackageDetail.shipmentNbr>>,
            And<SOShipLineSplitExt.packageLineNbr, Equal<Current<SOPackageDetail.lineNbr>>>>> PackageDetailSplit;

        protected void SOShipment_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            PackageDetailSplit.Cache.AllowUpdate = false;
            PackageDetailSplit.Cache.AllowInsert = false;
            PackageDetailSplit.Cache.AllowUpdate = false;
        }
    }
}

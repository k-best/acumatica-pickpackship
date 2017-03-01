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
    }
}

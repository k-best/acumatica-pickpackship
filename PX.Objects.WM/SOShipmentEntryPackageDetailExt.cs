using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.SO
{
    //TODO: Move to SOShipmentEntry graph when we integrate into Acumatica code base
    public class SOShipmentEntryPackageDetailExt : PXGraphExtension<SOShipmentEntry>
    {
        public PXSelect<SOPackageDetailSplit,
            Where<SOPackageDetailSplit.shipmentNbr, Equal<Current<SOPackageDetail.shipmentNbr>>,
            And<SOPackageDetailSplit.lineNbr, Equal<Current<SOPackageDetail.lineNbr>>>>> PackageDetailSplit;

        protected void SOShipment_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            //TODO: Update automation rules and remove this code - this is only temporary while this code is being executed as customization
            PackageDetailSplit.AllowDelete = Base.Packages.AllowDelete;
            PackageDetailSplit.AllowInsert = Base.Packages.AllowInsert;
            PackageDetailSplit.AllowSelect = Base.Packages.AllowSelect;
            PackageDetailSplit.AllowUpdate = Base.Packages.AllowUpdate;
        }

        public delegate void ShipPackagesDelegate(SOShipment shiporder);
        [PXOverride]
        public virtual void ShipPackages(SOShipment shiporder, ShipPackagesDelegate baseMethod)
        {
            Carrier carrier = PXSelect<Carrier, Where<Carrier.carrierID, Equal<Required<SOShipment.shipVia>>>>.Select(Base, shiporder.ShipVia);
            if(carrier != null && carrier.IsExternal == true && shiporder.ShippedViaCarrier != true)
            {
                //Automatically print return label if enabled for selected ship via
                var ext = PXCache<Carrier>.GetExtension<CS.CarrierExt>(carrier);
                if (ext.ReturnLabel == true)
                {
                    Base.GetReturnLabels(shiporder);
                }
            }

            baseMethod(shiporder);
        }

        public delegate void ConfirmShipmentDelegate(SOOrderEntry docgraph, SOShipment shiporder);
        [PXOverride]
        public void ConfirmShipment(SOOrderEntry docgraph, SOShipment shiporder, ConfirmShipmentDelegate baseMethod)
        {
            bool packageDetailsValid = true;

            Base.Clear();
            Base.Document.Current = Base.Document.Search<SOShipment.shipmentNbr>(shiporder.ShipmentNbr);

            var packagedQuantities = new Dictionary<Tuple<int?, int?, string, string>, decimal>();

            //Get a summary of all the package details
            foreach (SOPackageDetailSplit ps in PXSelectGroupBy<SOPackageDetailSplit,
                Where<SOPackageDetailSplit.shipmentNbr, Equal<Current<SOShipment.shipmentNbr>>>,
                Aggregate<
                    GroupBy<SOPackageDetailSplit.inventoryID,
                    GroupBy<SOPackageDetailSplit.subItemID,
                    GroupBy<SOPackageDetailSplit.lotSerialNbr,
                    GroupBy<SOPackageDetailSplit.uOM,
                    Sum<SOPackageDetailSplit.baseQty>>>>>>>.Select(Base))
            {
                packagedQuantities.Add(new Tuple<int?, int?, string, string>(ps.InventoryID, ps.SubItemID, ps.LotSerialNbr, ps.UOM), ps.BaseQty.GetValueOrDefault());
            }

            //We run validation only when packaging is used for the order - if left empty, we don't enforce validation
            if (packagedQuantities.Count > 0)
            {
                //Retrieve shipment details and deduct from packaged quantities
                foreach (SOShipLineSplit ls in PXSelectGroupBy<SOShipLineSplit,
                    Where<SOShipLineSplit.shipmentNbr, Equal<Current<SOShipment.shipmentNbr>>,
                        And<SOShipLineSplit.isStockItem, Equal<True>>>,
                    Aggregate<
                        GroupBy<SOShipLineSplit.inventoryID,
                        GroupBy<SOShipLineSplit.subItemID,
                        GroupBy<SOShipLineSplit.lotSerialNbr,
                        GroupBy<SOShipLineSplit.uOM,
                        Sum<SOShipLineSplit.baseQty>>>>>>>.Select(Base))
                {
                    //If item is not in dictionary, it will be automatically initialized and quantity deducted from 0
                    var key = new Tuple<int?, int?, string, string>(ls.InventoryID, ls.SubItemID, ls.LotSerialNbr, ls.UOM);
                    packagedQuantities[key] -= ls.BaseQty.GetValueOrDefault();
                }

                // Anything left with a quantity other than 0 need to be flagged as an error.
                foreach (var key in packagedQuantities.Keys)
                {
                    if (packagedQuantities[key] != 0)
                    {
                        packageDetailsValid = false;
                        break;
                    }
                }
            }

            if (packageDetailsValid)
            {
                baseMethod(docgraph, shiporder);
            }
            else
            {
                throw new PXException(PX.Objects.WM.Messages.PackageDetailsMismatch);
            }
        }
    }
}

using System;
using System.Collections;
using PX.Data;
using PX.Objects.IN;
using System.Collections.Generic;
using PX.Objects.AR;
using PX.SM;
using PX.Objects.CS;

namespace PX.Objects.SO
{
    public static class ScanStatuses
    {
        public const string Success = "OK"; //Causes focus to be sent back to shipment nbr. field
        public const string Clear = "CLR"; //Causes focus to be sent back to shipment nbr. field (same sound as "Scan" status)
        public const string Scan = "SCN";
        public const string Information = "INF";
        public const string Error = "ERR";
    }
    
    public static class ScanModes
    {
        public const string Add = "A";
        public const string Remove = "R";
        public const string Weight = "W";
    }

    public static class ScanCommands
    {
        public const char CommandChar = '*';

        public const string Clear = "Z";
        public const string Confirm = "C";
        public const string ConfirmAll = "CX";
        public const string Add = "A";
        public const string Remove = "R";
        public const string Item = "I";
        public const string LotSerial = "S";
        public const string NewPackage = "P";
        public const string PackageComplete = "PC";
        public const string QuickPackage = "PQ";
    }

    public class PickPackInfo : IBqlTable
    {
        public abstract class shipmentNbr : IBqlField { }
        [PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
        [PXDefault()]
        [PXUIField(DisplayName = "Shipment Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
        [PXSelector(typeof(Search2<SOShipment.shipmentNbr,
            InnerJoin<INSite, On<INSite.siteID, Equal<SOShipment.siteID>>,
            LeftJoinSingleTable<Customer, On<SOShipment.customerID, Equal<Customer.bAccountID>>>>,
            Where2<Match<INSite, Current<AccessInfo.userName>>,
            And<Where2<Where<Customer.bAccountID, IsNull, Or<Match<Customer, Current<AccessInfo.userName>>>>,
            And<SOShipment.status, Equal<SOShipmentStatus.open>,
            And<SOShipment.shipmentType, Equal<SOShipmentType.issue>>>>>>,
            OrderBy<Desc<SOShipment.shipmentNbr>>>))]
        public virtual string ShipmentNbr { get; set; }

        public abstract class barcode : IBqlField { }
        [PXString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Barcode")]
        public virtual string Barcode { get; set; }

        public abstract class quantity : IBqlField { }
        [PXDBQuantity]
        [PXDefault(TypeCode.Decimal, "1.0")]
        [PXUIField(DisplayName = "Quantity")]
        public virtual decimal? Quantity { get; set; }
        
        public abstract class scanMode : IBqlField { }
        [PXString(1, IsFixed = true)]
        [PXStringList(new[] { ScanModes.Add, ScanModes.Remove, ScanModes.Weight }, new[] { PX.Objects.WM.Messages.Add, PX.Objects.WM.Messages.Remove, PX.Objects.WM.Messages.Weight })]
        [PXDefault(ScanModes.Add)]
        [PXUIField(DisplayName = "Scan Mode")]
        public virtual string ScanMode { get; set; }

        public abstract class lotSerialSearch : IBqlField { }
        [PXBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Search Lot/Serial Numbers", FieldClass = "LotSerial")]
        public virtual bool? LotSerialSearch { get; set; }

        public abstract class currentInventoryID : IBqlField { }
        [StockItem]
        public virtual int? CurrentInventoryID { get; set; }

        public abstract class currentSubID : IBqlField { }
        [SubItem]
        public virtual int? CurrentSubID { get; set; }

        public abstract class currentLocationID : IBqlField { }
        [Location]
        public virtual int? CurrentLocationID { get; set; }

        public abstract class currentPackageLineNbr : IBqlField { }
        [PXInt]
        public virtual int? CurrentPackageLineNbr { get; set; }

        public abstract class status : IBqlField { }
        [PXString(3, IsUnicode = true)]
        [PXUIField(DisplayName = "Status", Enabled = false, Visible = false)]
        public virtual string Status { get; set; }

        public abstract class message : IBqlField { }
        [PXString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Message", Enabled = false)]
        public virtual string Message { get; set; }
    }

    public class PickPackShip : PXGraph<PickPackShip>
    {
        public enum ConfirmMode
        {
            PickedItems,
            AllItems
        }

        public const double ScaleWeightValiditySeconds = 30;

        public PXSetup<INSetup> Setup;
        public PXSelect<SOPickPackShipUserSetup, Where<SOPickPackShipUserSetup.userID, Equal<Current<AccessInfo.userID>>>> UserSetup;
        public PXCancel<PickPackInfo> Cancel;
        public PXFilter<PickPackInfo> Document;
        public PXSelect<SOShipment, Where<SOShipment.shipmentNbr, Equal<Current<PickPackInfo.shipmentNbr>>>> Shipment;
        public PXSelect<SOShipLinePick, Where<SOShipLinePick.shipmentNbr, Equal<Current<PickPackInfo.shipmentNbr>>>, OrderBy<Asc<SOShipLinePick.shipmentNbr, Asc<SOShipLine.lineNbr>>>> Transactions;
        public PXSelect<SOShipLineSplit, Where<SOShipLineSplit.shipmentNbr, Equal<Current<SOShipLinePick.shipmentNbr>>, And<SOShipLineSplit.lineNbr, Equal<Current<SOShipLinePick.lineNbr>>>>> Splits;
        public PXSelect<SOPackageDetailPick, Where<SOPackageDetailPick.shipmentNbr, Equal<Current<SOShipment.shipmentNbr>>>> Packages;
        public PXSelect<SOPackageDetailSplit, Where<SOPackageDetailSplit.shipmentNbr, Equal<Current<SOPackageDetailPick.shipmentNbr>>, And<SOPackageDetailSplit.lineNbr, Equal<Current<SOPackageDetailPick.lineNbr>>>>> PackageSplits;

        protected void PickPackInfo_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            EnsureUserSetupExists();
            Transactions.Cache.AllowDelete = false;
            Transactions.Cache.AllowInsert = false;
            Splits.Cache.AllowDelete = false;
            Splits.Cache.AllowInsert = false;
            Splits.Cache.AllowUpdate = false;
            Packages.Cache.AllowInsert = this.Shipment.Current != null;

            var doc = this.Document.Current;
            Confirm.SetEnabled(doc != null && doc.ShipmentNbr != null);
            ConfirmAll.SetEnabled(doc != null && doc.ShipmentNbr != null);
        }

        protected virtual void EnsureUserSetupExists()
        {
            UserSetup.Current = UserSetup.Select();
            if (UserSetup.Current == null)
            {
                UserSetup.Current = UserSetup.Insert((SOPickPackShipUserSetup)UserSetup.Cache.CreateInstance());
            }
        }

        protected void PickPackInfo_ShipmentNbr_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            var doc = e.Row as PickPackInfo;
            if (doc == null) return;

            this.Shipment.Current = this.Shipment.Select();
            if (this.Shipment.Current != null)
            {
                doc.Status = ScanStatuses.Scan;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.ShipmentReady, doc.ShipmentNbr);
            }
            else
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.ShipmentNbrMissing, doc.ShipmentNbr);
            }

            ClearScreen(false);
            this.Document.Update(doc);
        }

        protected void SOShipLinePick_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            PXUIFieldAttribute.SetEnabled(sender, e.Row, false);
            PXUIFieldAttribute.SetEnabled<SOShipLinePick.pickedQty>(sender, e.Row, true);
        }
        
        protected IEnumerable splits()
        {
            //We only use this view as a container for picked lot/serial numbers. We don't care about what's in the DB for this shipment.
            foreach(SOShipLineSplit row in Splits.Cache.Cached)
            {
                if (Shipment.Current != null && row.ShipmentNbr == Shipment.Current.ShipmentNbr &&
                    Transactions.Current != null && row.LineNbr == Transactions.Current.LineNbr)
                {
                    yield return row;
                }
            }
        }

        protected IEnumerable packages()
        {
            //We only use this view as a container for picked packages. We don't care about what's in the DB for this shipment.
            foreach (SOPackageDetailPick row in Packages.Cache.Cached)
            {
                if (this.Shipment.Current != null && row.ShipmentNbr == this.Shipment.Current.ShipmentNbr && Packages.Cache.GetStatus(row) == PXEntryStatus.Inserted)
                {
                    yield return row;
                }
            }
        }

        protected IEnumerable packageSplits()
        {
            //We only use this view as a container for picked package details. We don't care about what's in the DB for this shipment.
            foreach (SOPackageDetailSplit row in PackageSplits.Cache.Cached)
            {
                if (this.Packages.Current != null && row.LineNbr == this.Packages.Current.LineNbr && PackageSplits.Cache.GetStatus(row) == PXEntryStatus.Inserted)
                {
                    yield return row;
                }
            }
        }

        public PXAction<PickPackInfo> allocations;
        [PXUIField(DisplayName = "Allocations")]
        [PXButton]
        protected virtual void Allocations()
        {
            this.Splits.AskExt();
        }

        public PXAction<PickPackInfo> Scan;
        [PXUIField(DisplayName = "Scan")]
        [PXButton]
        protected virtual void scan()
        {
            var doc = this.Document.Current;

            if (String.IsNullOrEmpty(doc.Barcode))
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = WM.Messages.BarcodePrompt;
            }
            else
            {
                switch (doc.ScanMode)
                {
                    case ScanModes.Add:
                    case ScanModes.Remove:
                        if (doc.Barcode[0] == ScanCommands.CommandChar)
                        {
                            ProcessCommands(doc.Barcode);
                        }
                        else
                        {
                            ProcessBarcode(doc.Barcode);
                        }
                        break;
                    case ScanModes.Weight:
                        ProcessWeight(doc.Barcode);
                        break;
                }
            }

            doc.Barcode = String.Empty;
            this.Document.Update(doc);
        }

        protected virtual void ProcessCommands(string barcode)
        {
            var doc = this.Document.Current;
            var commands = barcode.Split(ScanCommands.CommandChar);
           
            int quantity = 0;
            if(int.TryParse(commands[1], out quantity))
            {
                if (IsQuantityEnabled())
                {
                    doc.Quantity = quantity;
                    doc.Status = ScanStatuses.Information;
                    doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.CommandSetQuantity, quantity);

                    if (commands.Length > 2)
                    {
                        ProcessBarcode(commands[2]);
                    }
                }
                else
                {
                    doc.Status = ScanStatuses.Error;
                    doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.CommandAccessRightsError);
                }
            }
            else
            {
                switch(commands[1])
                {
                    case ScanCommands.Add:
                        this.Document.Current.ScanMode = ScanModes.Add;
                        doc.Status = ScanStatuses.Information;
                        doc.Message = WM.Messages.CommandAdd; //TODO: Think of a better message.
                        break;
                    case ScanCommands.Remove:
                        this.Document.Current.ScanMode = ScanModes.Remove;
                        doc.Status = ScanStatuses.Information;
                        doc.Message = WM.Messages.CommandRemove;  //TODO: Think of a better message.
                        break;
                    case ScanCommands.Item:
                        this.Document.Current.LotSerialSearch = false;
                        doc.Status = ScanStatuses.Information;
                        doc.Message = WM.Messages.CommandInventory;
                        break;
                    case ScanCommands.LotSerial:
                        this.Document.Current.LotSerialSearch = true;
                        doc.Status = ScanStatuses.Information;
                        doc.Message = WM.Messages.CommandLot;
                        break;
                    case ScanCommands.Confirm:
                        if (Confirm.GetEnabled())
                        {
                            this.Confirm.Press();
                        }
                        else
                        {
                            doc.Status = ScanStatuses.Error;
                            doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.CommandAccessRightsError);
                        }
                        break;
                    case ScanCommands.ConfirmAll:
                        if (ConfirmAll.GetEnabled())
                        {
                            this.ConfirmAll.Press();
                        }
                        else
                        {
                            doc.Status = ScanStatuses.Error;
                            doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.CommandAccessRightsError);
                        }
                        break;
                    case ScanCommands.Clear:
                        ClearScreen(true);
                        doc.Status = ScanStatuses.Clear;
                        doc.Message = WM.Messages.CommandClear;
                        break;
                    case ScanCommands.NewPackage:
                        ProcessNewPackageCommand(commands);
                        break;
                    case ScanCommands.PackageComplete:
                        ProcessPackageCompleteCommand();
                        break;
                    case ScanCommands.QuickPackage:
                        ProcessNewPackageCommand(commands);
                        ProcessPackageCompleteCommand();
                        break;
                    default:
                        doc.Status = ScanStatuses.Error;
                        doc.Message = WM.Messages.CommandUnknown;
                        break;
                }
            }
        }

        protected virtual void ProcessWeight(string barcode)
        {
            var doc = this.Document.Current;

            decimal weight = 0;
            if(decimal.TryParse(barcode, out weight) && weight >= 0)
            {
                doc.Status = ScanStatuses.Information;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageComplete, weight, Setup.Current.WeightUOM);
                SetCurrentPackageWeight(weight);
                doc.CurrentPackageLineNbr = null;
                doc.ScanMode = ScanModes.Add;
            }
            else
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageInvalidWeight, barcode);
            }
        }

        protected virtual void ClearScreen(bool clearShipmentNbr)
        {
            if(clearShipmentNbr) this.Document.Current.ShipmentNbr = null;
            this.Document.Current.CurrentInventoryID = null;
            this.Document.Current.CurrentSubID = null;
            this.Document.Current.CurrentLocationID = null;
            this.Document.Current.CurrentPackageLineNbr = null;
            this.Transactions.Cache.Clear();
            this.Splits.Cache.Clear();
            this.Packages.Cache.Clear();
            this.PackageSplits.Cache.Clear();
        }

        protected virtual void ProcessBarcode(string barcode)
        {
            var doc = this.Document.Current;

            if (doc.LotSerialSearch == true)
            {
                if (!SetCurrentInventoryIDForLotSerial(barcode))
                {
                    doc.Status = ScanStatuses.Error;
                    doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.LotMissing, barcode);
                    return;
                }
            }

            if (IsCurrentPackageRequiredAndMissing())
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageMissingCurrent);
                return;
            }

            if (doc.CurrentInventoryID == null)
            {
                ProcessItemBarcode(barcode);
            }
            else
            {
                ProcessLotSerialBarcode(barcode);
            }
        }

        protected virtual bool IsCurrentPackageRequiredAndMissing()
        {
            // We don't force users to use them but as soon as they have added one, they have to continue for all their scans.
            // Maybe in the future we can add a setting in SO to control whether this is required or not instead.
            return Packages.SelectSingle() != null && this.Document.Current.CurrentPackageLineNbr == null;
        }

        protected virtual void ProcessItemBarcode(string barcode)
        {
            var doc = this.Document.Current;
            var rec = (PXResult<INItemXRef, InventoryItem, INLotSerClass, INSubItem>)
                          PXSelectJoin<INItemXRef,
                            InnerJoin<InventoryItem,
                                            On<InventoryItem.inventoryID, Equal<INItemXRef.inventoryID>,
                                            And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.inactive>,
                                            And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.noPurchases>,
                                            And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.markedForDeletion>>>>>,
                            InnerJoin<INLotSerClass,
                                         On<InventoryItem.lotSerClassID, Equal<INLotSerClass.lotSerClassID>>,
                            InnerJoin<INSubItem,
                                         On<INSubItem.subItemID, Equal<INItemXRef.subItemID>>>>>,
                            Where<INItemXRef.alternateID, Equal<Required<PickPackInfo.barcode>>,
                                            And<INItemXRef.alternateType, Equal<INAlternateType.barcode>>>>
                            .SelectSingleBound(this, new object[] { doc }, barcode);

            if (rec == null)
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.BarcodeMissing, barcode);
            }
            else
            {
                var inventoryItem = (InventoryItem)rec;
                var sub = (INSubItem)rec;
                var lsclass = (INLotSerClass)rec;

                if (lsclass.LotSerTrack != INLotSerTrack.NotNumbered &&
                    (lsclass.LotSerIssueMethod == INLotSerIssueMethod.UserEnterable ||
                    (lsclass.LotSerAssign == INLotSerAssign.WhenUsed && lsclass.AutoNextNbr == false)))
                {
                    if (lsclass.LotSerAssign == INLotSerAssign.WhenUsed && lsclass.LotSerTrackExpiration == true)
                    {
                        //TODO: Implement support for this.
                        throw new NotImplementedException(WM.Messages.LotNotSupported);
                    }

                    doc.CurrentInventoryID = inventoryItem.InventoryID;
                    doc.CurrentSubID = sub.SubItemID;
                    doc.Status = ScanStatuses.Scan;
                    doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.LotScanPrompt, inventoryItem.InventoryCD.TrimEnd());
                }
                else
                {
                    if (Document.Current.ScanMode == ScanModes.Add && AddPick(inventoryItem.InventoryID, sub.SubItemID, Document.Current.Quantity, null))
                    {
                        doc.Status = ScanStatuses.Scan;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.InventoryAdded, Document.Current.Quantity, inventoryItem.InventoryCD.TrimEnd());
                        doc.Quantity = 1;
                    }
                    else if (Document.Current.ScanMode == ScanModes.Remove && RemovePick(inventoryItem.InventoryID, sub.SubItemID, Document.Current.Quantity, null))
                    {
                        doc.Status = ScanStatuses.Scan;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.InventoryRemoved, Document.Current.Quantity, inventoryItem.InventoryCD.TrimEnd());
                        doc.Quantity = 1;
                        doc.ScanMode = ScanModes.Add;
                    }
                    else
                    {
                        doc.Status = ScanStatuses.Error;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.InventoryMissing, inventoryItem.InventoryCD.TrimEnd());
                    }
                }
            }
        }

        protected virtual void ProcessLotSerialBarcode(string barcode)
        {
            var doc = this.Document.Current;
            var rec = (PXResult<InventoryItem, INLotSerClass>)PXSelectJoin<InventoryItem,
                InnerJoin<INLotSerClass,
                    On<InventoryItem.lotSerClassID, Equal<INLotSerClass.lotSerClassID>>>,
                Where<InventoryItem.inventoryID, Equal<Current<PickPackInfo.currentInventoryID>>>>
                .SelectSingleBound(this, new object[] { doc });
            
            if(rec != null)
            {
                var inventoryItem = (InventoryItem)rec;
                var lsclass = (INLotSerClass)rec;

                if (Document.Current.ScanMode == ScanModes.Add)
                {
                    if(lsclass.LotSerTrack == INLotSerTrack.SerialNumbered && doc.Quantity > 1)
                    {
                        doc.Status = ScanStatuses.Error;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.SerialInvalidQuantity);
                    }
                    else if (lsclass.LotSerTrack == INLotSerTrack.SerialNumbered && GetTotalQuantityPickedForLotSerial(doc.CurrentInventoryID, doc.CurrentSubID, barcode) > 0)
                    {
                        doc.Status = ScanStatuses.Error;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.SerialDuplicateError, barcode);
                    }
                    else if (AddPick(doc.CurrentInventoryID, doc.CurrentSubID, Document.Current.Quantity, barcode))
                    {
                        doc.Status = ScanStatuses.Scan;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.InventoryAddedWithBarcode, Document.Current.Quantity, inventoryItem.InventoryCD.TrimEnd(), barcode);
                        doc.Quantity = 1;
                    }
                    else
                    {
                        doc.Status = ScanStatuses.Error;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.InventoryMissing, inventoryItem.InventoryCD.TrimEnd());
                    }
                }
                else if (Document.Current.ScanMode == ScanModes.Remove)
                {
                    if (GetTotalQuantityPickedForLotSerial(doc.CurrentInventoryID, doc.CurrentSubID, barcode) < doc.Quantity)
                    {
                        doc.Status = ScanStatuses.Error;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.LotInvalidQuantity, barcode);
                    }
                    else if (RemovePick(doc.CurrentInventoryID, doc.CurrentSubID, Document.Current.Quantity, barcode))
                    {
                        doc.Status = ScanStatuses.Scan;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.InventoryRemovedWithBarcode, Document.Current.Quantity, inventoryItem.InventoryCD.TrimEnd(), barcode);
                        doc.Quantity = 1;
                        doc.ScanMode = ScanModes.Add;
                    }
                    else
                    {
                        // We will technically never hit this code, since we pre-validate how much was been picked before calling RemovePick.
                        System.Diagnostics.Debug.Assert(false, "This condition should have been validated by GetTotalQuantityPickedForLotSerial");
                        doc.Status = ScanStatuses.Error;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.InventoryMissing, inventoryItem.InventoryCD.TrimEnd());
                    }
                }
            }
            else
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.InventoryMissing, doc.CurrentInventoryID);
            }

            doc.CurrentInventoryID = null;
            doc.CurrentSubID = null;
            doc.CurrentLocationID = null;
        }

        protected virtual void ProcessNewPackageCommand(string[] commands)
        {
            var doc = this.Document.Current;
           
            if(commands.Length != 3)
            {
                //We're expecting something that looks like *P*LARGE
                doc.Status = ScanStatuses.Error;
                doc.Message = WM.Messages.PackageCommandMissingBoxId;
                return;
            }

            if(doc.CurrentPackageLineNbr != null)
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageIncompleteError, ScanCommands.CommandChar, ScanCommands.PackageComplete);
                return;
            }

            string boxID = commands[2];
            var box = (CSBox) PXSelect<CSBox, Where<CSBox.boxID, Equal<Required<CSBox.boxID>>>>.Select(this, boxID);
            if(box == null)
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.BoxMissing, boxID);
            }
            else
            {
                var newPackage = (SOPackageDetailPick)this.Packages.Cache.CreateInstance();
                newPackage.BoxID = box.BoxID;
                newPackage = this.Packages.Insert(newPackage);

                doc.CurrentPackageLineNbr = newPackage.LineNbr;
                doc.Status = ScanStatuses.Information;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.BoxAdded, boxID);
            }
        }

        protected virtual void ProcessPackageCompleteCommand()
        {
            var doc = this.Document.Current;

            if (doc.CurrentPackageLineNbr == null)
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = WM.Messages.PackageMissingCurrent;
            }
            else
            {
                if(this.UserSetup.Current.UseScale == true)
                {
                    var scale = (SMScale)PXSelect<SMScale, Where<SMScale.scaleID, Equal<Required<SOPickPackShipUserSetup.scaleID>>>>.Select(this, this.UserSetup.Current.ScaleID);
                    if(scale == null)
                    {
                        throw new PXException(PXMessages.LocalizeFormatNoPrefix(WM.Messages.ScaleMissing, this.UserSetup.Current.ScaleID));
                    }

                    if (scale.LastModifiedDateTime.Value.AddSeconds(ScaleWeightValiditySeconds) < DateTime.Now)
                    {
                        doc.Status = ScanStatuses.Error;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.ScaleTimeout, this.UserSetup.Current.ScaleID, ScaleWeightValiditySeconds);
                    }
                    else
                    {
                        doc.Status = ScanStatuses.Information;
                        doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageComplete, scale.LastWeight.GetValueOrDefault(), Setup.Current.WeightUOM);
                        SetCurrentPackageWeight(scale.LastWeight.GetValueOrDefault());
                        doc.CurrentPackageLineNbr = null;
                    }
                }
                else
                {
                    doc.Status = ScanStatuses.Information;
                    doc.Message = WM.Messages.PackageWeightPrompt;
                    doc.ScanMode = ScanModes.Weight;
                }
            }
        }

        protected virtual void SetCurrentPackageWeight(decimal weight)
        {
            var package = (SOPackageDetailPick)this.Packages.Search<SOPackageDetailPick.lineNbr>(this.Document.Current.CurrentPackageLineNbr);
            if (package == null)
            {
                throw new PXException(PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageLineNbrMissing, this.Document.Current.CurrentPackageLineNbr));
            }
            package.Weight = weight;
            this.Packages.Update(package);
        }

        protected virtual bool SetCurrentInventoryIDForLotSerial(string barcode)
        {
            var doc = this.Document.Current;
            INLotSerialStatus firstMatch = null;

            foreach(INLotSerialStatus ls in PXSelect<INLotSerialStatus, 
                Where<INLotSerialStatus.qtyOnHand, Greater<Zero>, 
                And<INLotSerialStatus.siteID, Equal<Current<SOShipment.siteID>>,
                And<INLotSerialStatus.lotSerialNbr, Equal<Required<INLotSerialStatus.lotSerialNbr>>>>>>.Select(this, barcode))
            {
                if(firstMatch == null)
                {
                    firstMatch = ls;
                }
                else
                {
                    throw new PXException(WM.Messages.LotUniquenessError);
                }
            }

            if(firstMatch != null)
            {
                doc.CurrentInventoryID = firstMatch.InventoryID;
                doc.CurrentSubID = firstMatch.SubItemID;
                doc.CurrentLocationID = firstMatch.LocationID;
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual bool AddPick(int? inventoryID, int? subID, decimal? quantity, string lotSerialNumber)
        {
            SOShipLinePick firstLine = null;
            foreach (SOShipLinePick pickLine in this.Transactions.Select())
            {
                if (pickLine.InventoryID == inventoryID && (pickLine.SubItemID == subID || Setup.Current.UseInventorySubItem == false))
                {
                    if (firstLine == null) firstLine = pickLine;

                    if (pickLine.PickedQty.GetValueOrDefault() < pickLine.ShippedQty.GetValueOrDefault())
                    {
                        //We first try to fill all the lines sequentially - item may be present multiple times on the shipment
                        decimal quantityForCurrentPickLine = Math.Min(quantity.GetValueOrDefault(), pickLine.ShippedQty.GetValueOrDefault() - pickLine.PickedQty.GetValueOrDefault());
                        pickLine.PickedQty = pickLine.PickedQty.GetValueOrDefault() + quantityForCurrentPickLine;
                        this.Transactions.Update(pickLine);
                        if (this.Document.Current.CurrentPackageLineNbr != null)
                        {
                            AddPickToCurrentPackageDetails(lotSerialNumber, quantityForCurrentPickLine);
                        }
                        if (lotSerialNumber != null)
                        {
                            AddLotSerialToCurrentLineSplits(lotSerialNumber, quantityForCurrentPickLine);
                        }
                        quantity = quantity - quantityForCurrentPickLine;
                    }

                    if(quantity == 0)
                    {
                        return true;
                    }
                }
            }

            if (firstLine != null)
            {
                //All the lines are already filled; just over-pick the first one.
                firstLine.PickedQty = firstLine.PickedQty.GetValueOrDefault() + quantity;
                this.Transactions.Update(firstLine);
                if (this.Document.Current.CurrentPackageLineNbr != null)
                {
                    AddPickToCurrentPackageDetails(lotSerialNumber, quantity.GetValueOrDefault());
                }
                if (lotSerialNumber != null)
                {
                    AddLotSerialToCurrentLineSplits(lotSerialNumber, quantity.GetValueOrDefault());
                }
                return true;
            }
            else
            {
                //Item not found.
                return false;
            }
        }

        protected virtual void AddLotSerialToCurrentLineSplits(string lotSerial, decimal quantity)
        {
            //TODO: See if it's possible to set quantity > 1 in some cases with lot/numered (shipment 000201 didn't work out in this way, hence the loop)
            for (int i = 0; i < quantity; i++)
            {
                var split = (SOShipLineSplit)this.Splits.Cache.CreateInstance();
                split.Qty = 1;
                split.LocationID = this.Document.Current.CurrentLocationID;
                split.LotSerialNbr = lotSerial;
                this.Splits.Insert(split);
            }
        }

        protected virtual void AddPickToCurrentPackageDetails(string lotSerial, decimal quantity)
        {
            var package = (SOPackageDetailPick)this.Packages.Search<SOPackageDetailPick.lineNbr>(this.Document.Current.CurrentPackageLineNbr);
            if (package == null)
            {
                throw new PXException(PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageLineNbrMissing, this.Document.Current.CurrentPackageLineNbr));
            }

            // First try to update corresponding item/subitem/lot/serial in current package if it exists
            this.Packages.Current = package;
            foreach (SOPackageDetailSplit split in this.PackageSplits.Select())
            {
                if (split.InventoryID == this.Transactions.Current.InventoryID && split.SubItemID == this.Transactions.Current.SubItemID && split.LotSerialNbr == lotSerial)
                {
                    split.Qty += quantity;
                    this.PackageSplits.Update(split);
                    return;
                }
            }

            // No match found, insert new row
            SOPackageDetailSplit newSplit = (SOPackageDetailSplit)this.PackageSplits.Cache.CreateInstance();
            newSplit.LineNbr = this.Document.Current.CurrentPackageLineNbr;
            newSplit.InventoryID = this.Transactions.Current.InventoryID;
            newSplit.SubItemID = this.Transactions.Current.SubItemID;
            newSplit.Qty = quantity;
            newSplit.QtyUOM = this.Transactions.Current.UOM;
            newSplit.LotSerialNbr = lotSerial;
            this.PackageSplits.Insert(newSplit);
        }

        protected virtual decimal GetTotalQuantityPickedForLotSerial(int? inventoryID, int? subID, string lotSerialNumber)
        {
            decimal total = 0;

            foreach (SOShipLinePick pickLine in this.Transactions.Select())
            {
                if(pickLine.InventoryID == inventoryID && pickLine.SubItemID == subID)
                {
                    this.Transactions.Current = pickLine;
                    foreach(SOShipLineSplit split in this.Splits.Select())
                    {
                        if(split.LotSerialNbr == lotSerialNumber)
                        {
                            total = total + split.Qty.GetValueOrDefault();
                        }
                    }
                }
            }

            return total;
        }

        protected virtual bool RemovePick(int? inventoryID, int? subID, decimal? quantity, string lotSerialNumber)
        {
            if (!ValidateRemovePick(inventoryID, subID, lotSerialNumber))
                return false;

            SOShipLinePick firstLine = null;
            foreach (SOShipLinePick pickLine in this.Transactions.Select())
            {
                if (pickLine.InventoryID == inventoryID && (pickLine.SubItemID == subID || Setup.Current.UseInventorySubItem == false))
                {
                    if (firstLine == null) firstLine = pickLine;

                    if (pickLine.PickedQty.GetValueOrDefault() > 0)
                    {
                        //We first try to remove the quantities from already scanned quantities sequentially - item may be present multiple times on the shipment
                        decimal quantityForCurrentPickLine = Math.Min(quantity.GetValueOrDefault(), pickLine.PickedQty.GetValueOrDefault());
                        pickLine.PickedQty = pickLine.PickedQty.GetValueOrDefault() - quantityForCurrentPickLine;
                        if (pickLine.PickedQty == 0) pickLine.PickedQty = null;

                        this.Transactions.Update(pickLine);
                        if (this.Document.Current.CurrentPackageLineNbr != null)
                        {
                            RemovePickFromPackageDetails(lotSerialNumber, quantityForCurrentPickLine);
                        }
                        if (lotSerialNumber != null)
                        {
                            RemoveLotSerialFromCurrentLineSplits(lotSerialNumber, quantityForCurrentPickLine);
                        }

                        quantity = quantity - quantityForCurrentPickLine;
                    }

                    if (quantity == 0)
                    {
                        return true;
                    }
                }
            }

            if (firstLine != null)
            {
                //All the lines are already cleared; deduct from the first one which will go negative (this will be validated during Confirm step).
                firstLine.PickedQty = firstLine.PickedQty.GetValueOrDefault() - quantity;
                if (firstLine.PickedQty == 0) firstLine.PickedQty = null;
                this.Transactions.Update(firstLine);
                if (this.Document.Current.CurrentPackageLineNbr != null)
                {
                    RemovePickFromPackageDetails(lotSerialNumber, quantity.GetValueOrDefault());
                }
                if (lotSerialNumber != null)
                {
                    RemoveLotSerialFromCurrentLineSplits(lotSerialNumber, quantity.GetValueOrDefault());
                }
                return true;
            }
            else
            {
                //Item not found.
                return false;
            }
        }

        private bool ValidateRemovePick(int? inventoryID, int? subID, string lotSerialNumber)
        {
            int itemsCount = 0;

            if (Packages.Select().Count == 0)
                return true;
            
            // Validation: 
            //  a) item is not in current package, but is present in ONE and ONLY one package - we can safely remove it.
            //  b) item is not in current package, but is present in MULTIPLE other packages - we can't decide on behalf of user and need to return FALSE
            //  c) item is not in current package, and not in any other package - we need to return FALSE
            foreach (SOPackageDetailSplit split in PackageSplits.Cache.Cached)
            {
                if (split.InventoryID == inventoryID &&
                    (split.SubItemID == subID || Setup.Current.UseInventorySubItem == false) &&
                    split.LotSerialNbr == lotSerialNumber)
                {
                    if (split.LineNbr == this.Document.Current.CurrentPackageLineNbr)
                    {
                        return true;
                    }

                    itemsCount++;
                }
            }

            return itemsCount == 1;
        }

        protected virtual void RemoveLotSerialFromCurrentLineSplits(string lotSerial, decimal quantity)
        {
            foreach(SOShipLineSplit split in this.Splits.Select())
            {
                if(split.LotSerialNbr == lotSerial)
                {
                    if (split.Qty != 1)
                    {
                        throw new PXException(PXMessages.LocalizeFormatNoPrefix(WM.Messages.LotSplitQuantityError, lotSerial, split.Qty));
                    }

                    this.Splits.Delete(split);
                    quantity = quantity - 1;
                }

                if (quantity == 0) break;
            }
            
            if(quantity != 0)
            {
                // This condition is validated in RemovePick, so we should never get to this point.
                throw new PXException(PXMessages.LocalizeFormatNoPrefix(WM.Messages.LotMissingWithQuantity, lotSerial, quantity));
            }
        }

        protected virtual void RemovePickFromPackageDetails(string lotSerial, decimal quantity)
        {
            var package = (SOPackageDetailPick)this.Packages.Search<SOPackageDetailPick.lineNbr>(this.Document.Current.CurrentPackageLineNbr);
            if (package == null)
            {
                throw new PXException(PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageLineNbrMissing, this.Document.Current.CurrentPackageLineNbr));
            }

            this.Packages.Current = package;

            // Try removing in current package first
            foreach (SOPackageDetailSplit split in this.PackageSplits.Select())
                if (RemoveSplitFromPackageDetails(split, lotSerial, quantity))
                    return;

            // Remove in another package
            foreach (SOPackageDetailSplit split in this.PackageSplits.Cache.Cached)
                if (RemoveSplitFromPackageDetails(split, lotSerial, quantity))
                    return;
             
            throw new PXException(PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageRemoveInventoryError));
        }

        protected virtual bool RemoveSplitFromPackageDetails(SOPackageDetailSplit split, string lotSerial, decimal quantity)
        {
            if (split.InventoryID == this.Transactions.Current.InventoryID &&
                (split.SubItemID == this.Transactions.Current.SubItemID || Setup.Current.UseInventorySubItem == false) &&
                split.LotSerialNbr == lotSerial)
            {
                if (quantity >= split.Qty)
                {
                    this.PackageSplits.Delete(split);
                }
                else
                {
                    split.Qty -= quantity;
                    this.PackageSplits.Update(split);
                }

                return true;
            }

            return false;
        }

        public PXAction<PickPackInfo> Confirm;
        [PXUIField(DisplayName = "Confirm Picked")]
        [PXButton]
        protected virtual void confirm()
        {
            ConfirmShipment(ConfirmMode.PickedItems);
        }

        public PXAction<PickPackInfo> ConfirmAll;
        [PXUIField(DisplayName = "Confirm All")]
        [PXButton]
        protected virtual void confirmAll()
        {
            ConfirmShipment(ConfirmMode.AllItems);
        }

        protected virtual void ConfirmShipment(ConfirmMode confirmMode)
        {
            var doc = this.Document.Current;
            doc.Status = ScanStatuses.Information;
            doc.Message = String.Empty;
            SOShipmentEntry graph = PXGraph.CreateInstance<SOShipmentEntry>();
            SOShipment shipment = null;
            
            if(doc.CurrentPackageLineNbr != null)
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageCompletePrompt, ScanCommands.CommandChar, ScanCommands.PackageComplete);
                this.Document.Update(doc);
                return;
            }

            shipment = graph.Document.Search<SOShipment.shipmentNbr>(doc.ShipmentNbr);
            if (shipment == null)
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = WM.Messages.ShipmentMissing;
                this.Document.Update(doc);
                return;
            }

            if (confirmMode == ConfirmMode.AllItems || !IsConfirmationNeeded() ||
                this.Document.Ask(WM.Messages.ShipmentQuantityMismatchPrompt, MessageButtons.YesNo) == PX.Data.WebDialogResult.Yes)
            {
                PXLongOperation.StartOperation(this, () =>
                {
                    try
                    {
                        graph.Document.Current = shipment;

                        if (confirmMode == ConfirmMode.PickedItems)
                        {
                            UpdateShipmentLinesWithPickResults(graph);
                        }

                        UpdateShipmentPackages(graph);

                        PXAction confAction = graph.Actions["Action"];
                        var adapter = new PXAdapter(new DummyView(graph, graph.Document.View.BqlSelect, new List<object> { graph.Document.Current }));
                        adapter.Menu = SOShipmentEntryActionsAttribute.Messages.ConfirmShipment;
                        confAction.PressButton(adapter);

                        PreparePrintJobs(graph);

                        doc.Status = ScanStatuses.Success;
                        if(confirmMode == ConfirmMode.AllItems)
                        { 
                            doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.ShipmentConfirmedFull, doc.ShipmentNbr);
                        }
                        else if(confirmMode == ConfirmMode.PickedItems)
                        {
                            doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.ShipmentConfirmedPicked, doc.ShipmentNbr);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false, "ConfirmMode invalid");
                        }

                        ClearScreen(true);
                        this.Document.Update(doc);
                    }
                    catch (Exception e)
                    {
                        doc.Status = ScanStatuses.Error;
                        doc.Message = e.Message;
                        this.Document.Update(doc);
                        throw;
                    }
                });
            }
        }

        public PXAction<PickPackInfo> Settings;
        [PXUIField(DisplayName = "Settings")]
        [PXButton]
        protected virtual void settings()
        {
            if (UserSetup.AskExt() == WebDialogResult.OK)
            {
                PXCache cache = Caches[typeof(SOPickPackShipUserSetup)];
                cache.Persist(PXDBOperation.Insert);
                cache.Persist(PXDBOperation.Update);
                cache.Clear();
            }
        }

        protected virtual void PreparePrintJobs(SOShipmentEntry graph)
        {
            PrintJobMaint jobMaint = null;
            var printSetup = (SOPickPackShipUserSetup)UserSetup.Select();

            if (printSetup.ShipmentConfirmation == true)
            {
                //TODO: SO642000 shouldn't be hardcoded - this needs to be read from notification; see PickListPrintToQueueExtensions for example
                if (jobMaint == null) jobMaint = PXGraph.CreateInstance<PrintJobMaint>();
                jobMaint.AddPrintJob(printSetup.ShipmentConfirmationQueue, "SO642000", new Dictionary<string, string> { { "ShipmentNbr", graph.Document.Current.ShipmentNbr } });
            }
            
            if (printSetup.ShipmentLabels == true)
            {
                if (jobMaint == null) jobMaint = PXGraph.CreateInstance<PrintJobMaint>();
                UploadFileMaintenance ufm = PXGraph.CreateInstance<UploadFileMaintenance>();
                foreach (SOPackageDetail package in graph.Packages.Select())
                {
                    Guid[] files = PXNoteAttribute.GetFileNotes(graph.Packages.Cache, package);
                    foreach (Guid id in files)
                    {
                        FileInfo fileInfo = ufm.GetFile(id);
                        string extension = System.IO.Path.GetExtension(fileInfo.Name).ToLower();
                        if (extension == ".pdf" || extension == ".zpl" || extension == ".zplii" || extension == ".epl" || extension == ".epl2" || extension == ".dpl")
                        {
                            jobMaint.AddPrintJob(printSetup.ShipmentLabelsQueue, "", new Dictionary<string, string> { { "FILEID", id.ToString() } });
                        }
                        else
                        {
                            PXTrace.WriteWarning(PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageInvalidFileExtension, graph.Document.Current.ShipmentNbr, package.LineNbr));
                        }
                    }
                }
            }
        }
        
        protected virtual bool IsConfirmationNeeded()
        {
            foreach (SOShipLinePick pickLine in this.Transactions.Select())
            {
                if (pickLine.PickedQty.GetValueOrDefault() != pickLine.ShippedQty.GetValueOrDefault())
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual bool IsQuantityEnabled()
        {
            foreach (PXEventSubscriberAttribute attribute in Document.Cache.GetAttributesReadonly<PickPackInfo.quantity>())
                if (attribute is PXUIFieldAttribute)
                    return ((PXUIFieldAttribute)attribute).Enabled;

            return false;
        }

        protected virtual void UpdateShipmentLinesWithPickResults(SOShipmentEntry graph)
        {
            foreach(SOShipLinePick pickLine in this.Transactions.Select())
            {
                graph.Transactions.Current = graph.Transactions.Search<SOShipLine.lineNbr>(pickLine.LineNbr);
                if(graph.Transactions.Current != null)
                {
                    //Update shipped quantity to match what was picked
                    if (graph.Transactions.Current.ShippedQty != pickLine.PickedQty)
                    {
                        graph.Transactions.Current.ShippedQty = pickLine.PickedQty.GetValueOrDefault();
                        graph.Transactions.Update(graph.Transactions.Current);
                    }

                    //Set any lot/serial numbers that were assigned
                    bool initialized = false;
                    foreach(SOShipLineSplit split in this.Splits.Select())
                    {
                        if(this.Splits.Cache.GetStatus(split) == PXEntryStatus.Inserted)
                        {
                            if(!initialized)
                            {
                                //Delete any pre-existing split
                                foreach(SOShipLineSplit s in graph.splits.Select())
                                {
                                    graph.splits.Delete(s);
                                }
                                initialized = true;
                            }

                            graph.splits.Insert(split);
                        }
                    }
                }
                else
                {
                    throw new PXException(PXMessages.LocalizeFormatNoPrefix(WM.Messages.ShipmentLineMissing, pickLine.LineNbr));
                }
            }
        }

        protected virtual void UpdateShipmentPackages(SOShipmentEntry graph)
        {
            //Delete any existing package row - we ignore what auto-packaging configured and override with packages that were actually used.
            foreach(SOPackageDetail package in graph.Packages.Select())
            {
                graph.Packages.Delete(package);
            }

            foreach (SOPackageDetail package in this.Packages.Select())
            {
                package.Confirmed = true;
                graph.Packages.Insert(package);
            }

            //TODO: Since our view doesn't exist in the SOShipmentEntry graph, we go straight through caches.
            //If this code is merged into the main Acumatica code base, this section can be rewritten to use the view
            var packageSplitCache = graph.Caches[typeof(SOPackageDetailSplit)];
            foreach (SOPackageDetailSplit split in this.PackageSplits.Cache.Cached)
            {
                packageSplitCache.Insert(split);
            }
        }

        protected virtual void SOPackageDetailPick_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            SOPackageDetailPick row = e.Row as SOPackageDetailPick;
            PackageSplits.Cache.AllowInsert = row != null;
            if (row != null)
            {
                row.WeightUOM = Setup.Current.WeightUOM;
                row.IsCurrent = (this.Document.Current.CurrentPackageLineNbr != null && row.LineNbr == this.Document.Current.CurrentPackageLineNbr);
            }
        }

        protected void SOPackageDetailPick_IsCurrent_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            SOPackageDetailPick row = e.Row as SOPackageDetailPick;
            if (row == null) return;

            if(row.IsCurrent == true)
            {
                this.Document.Current.CurrentPackageLineNbr = row.LineNbr;
                this.Document.Update(this.Document.Current);
                this.Packages.View.RequestRefresh(); //To have previously current row unchecked -- not needed when unchecking current
            }
            else
            {
                this.Document.Current.CurrentPackageLineNbr = null;
                this.Document.Update(this.Document.Current);
            }
        }

        private sealed class DummyView : PXView
        {
            private readonly List<object> _records;
            internal DummyView(PXGraph graph, BqlCommand command, List<object> records)
                : base(graph, true, command)
            {
                _records = records;
            }
            public override List<object> Select(object[] currents, object[] parameters, object[] searches, string[] sortcolumns, bool[] descendings, PXFilterRow[] filters, ref int startRow, int maximumRows, ref int totalRows)
            {
                return _records;
            }
        }
    }
}

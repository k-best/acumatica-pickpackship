using System;
using System.Collections;
using PX.Data;
using PX.Objects.IN;
using System.Collections.Generic;
using PX.Common;
using PX.Objects.AR;
using PX.SM;
using PX.Objects.CS;
using System.Globalization;

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
    }

    public static class ScanStates
    {
        public const string ShipmentNumber = "N";
        public const string Item = "I";
        public const string LotSerialNumber = "S";
        public const string Location = "L";
        public const string Weight = "W";
    }

    public static class ScanCommands
    {
        public const char CommandChar = '*';

        public const string Cancel = "Z";
        public const string Confirm = "C";
        public const string ConfirmAll = "CX";
        public const string Add = "A";
        public const string Remove = "R";
        public const string Item = "I";
        public const string LotSerial = "S";
        public const string NewPackage = "P";
        public const string NewPackageAutoCalcWeight = "PA";
        public const string PackageComplete = "PC";
    }

    public class ScanLog : IBqlTable
    {
        public abstract class logLineDate : IBqlField { }
        [PXDBDateAndTime(InputMask = "dd-MM-yyyy HH:mm:ss", DisplayMask = "dd-MM-yyyy HH:mm:ss", IsKey = true)]
        [PXUIField(DisplayName = "Time", Enabled = false)]
        public virtual DateTime? LogLineDate { get; set; }

        public abstract class logLine : IBqlField { }
        [PXString(256, IsUnicode = true)]
        [PXUIField(DisplayName = "Barcode", Enabled = false)]
        public virtual string LogBarcode { get; set; }

        public abstract class logMessage : IBqlField { }
        [PXString(256, IsUnicode = true)]
        [PXUIField(DisplayName = "Message", Enabled = false)]
        public virtual string LogMessage { get; set; }
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
        [PXStringList(new[] { ScanModes.Add, ScanModes.Remove }, new[] { PX.Objects.WM.Messages.Add, PX.Objects.WM.Messages.Remove })]
        [PXDefault(ScanModes.Add)]
        [PXUIField(DisplayName = "Scan Mode")]
        public virtual string ScanMode { get; set; }

        public abstract class scanState : IBqlField { }
        [PXString(1, IsFixed = true)]
        [PXDefault(ScanStates.ShipmentNumber)]
        [PXUIField(DisplayName = "Scan State")]
        public virtual string ScanState { get; set; }

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

        public abstract class currentLotSerialNumber : IBqlField { }
        [PXString]
        public virtual string CurrentLotSerialNumber { get; set; }

        public abstract class currentExpirationDate : IBqlField { }
        [PXDate]
        public virtual DateTime? CurrentExpirationDate { get; set; }

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

        public readonly Dictionary<string, decimal> kgToWeightUnit = new Dictionary<string, decimal>
        {
            { "KG", 1m },
            { "LB", 0.453592m }
        };

        public const double ScaleWeightValiditySeconds = 30;

        public PXSetup<INSetup> Setup;
        public PXSelect<SOPickPackShipUserSetup, Where<SOPickPackShipUserSetup.userID, Equal<Current<AccessInfo.userID>>>> UserSetup;
        public PXCancel<PickPackInfo> Cancel;
        public PXFilter<PickPackInfo> Document;
        public PXSelect<SOShipment, Where<SOShipment.shipmentNbr, Equal<Current<PickPackInfo.shipmentNbr>>>> Shipment;
        public PXSelect<SOShipLinePick, Where<SOShipLinePick.shipmentNbr, Equal<Current<PickPackInfo.shipmentNbr>>>, OrderBy<Asc<SOShipLinePick.shipmentNbr, Asc<SOShipLine.lineNbr>>>> Transactions;
        public PXSelect<SOShipLineSplit, Where<SOShipLineSplit.shipmentNbr, Equal<Current<SOShipLinePick.shipmentNbr>>, And<SOShipLineSplit.lineNbr, Equal<Current<SOShipLinePick.lineNbr>>>>> Splits;
        public PXSelect<SOPackageDetailPick, Where<SOPackageDetailPick.shipmentNbr, Equal<Current<SOShipment.shipmentNbr>>>> Packages;
        public PXSelect<SOShipLineSplit, Where<SOShipLineSplit.shipmentNbr, Equal<Current<SOPackageDetailPick.shipmentNbr>>>> PackageSplits;
        public PXSelectOrderBy<ScanLog, OrderBy<Desc<ScanLog.logLineDate>>> ScanLogs;

        public PickPackShip()
        {
            //TODO: Remove when licence handling has been implemented
            if (DateTime.Now > new DateTime(2017, 4, 30))
            {
                throw new PXException("Pick, Pack and Ship: Evaluation period expired.");
            }
        }

        protected void PickPackInfo_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            EnsureUserSetupExists();
            Transactions.Cache.AllowDelete = false;
            Transactions.Cache.AllowInsert = false;
            Transactions.Cache.AllowUpdate = false;
            Splits.Cache.AllowDelete = false;
            Splits.Cache.AllowInsert = false;
            Splits.Cache.AllowUpdate = false;
            ScanLogs.Cache.AllowDelete = false;
            ScanLogs.Cache.AllowInsert = false;
            ScanLogs.Cache.AllowUpdate = false;
            Packages.Cache.AllowInsert = false; //Manual deletion and edit of weight/value is possible
            PackageSplits.Cache.AllowUpdate = false;
            PackageSplits.Cache.AllowInsert = false;
            PackageSplits.Cache.AllowUpdate = false;

            var doc = this.Document.Current;
            Confirm.SetEnabled(doc != null && doc.ShipmentNbr != null);
            ConfirmAll.SetEnabled(doc != null && doc.ShipmentNbr != null);
        }


        protected void PickPackInfo_ShipmentNbr_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            var doc = e.Row as PickPackInfo;
            if (doc == null) return;

            SelectShipment(doc);
        }

        protected virtual void EnsureUserSetupExists()
        {
            UserSetup.Current = UserSetup.Select();
            if (UserSetup.Current == null)
            {
                UserSetup.Current = UserSetup.Insert((SOPickPackShipUserSetup)UserSetup.Cache.CreateInstance());
            }
        }

        private void SelectShipment(PickPackInfo doc)
        {
            this.Shipment.Current = this.Shipment.Select();
            if (this.Shipment.Current != null)
            {
                doc.Status = ScanStatuses.Scan;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.ShipmentReady, doc.ShipmentNbr);
                SetScanState(ScanStates.Item);
            }
            else
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.ShipmentNbrMissing, doc.ShipmentNbr);
                SetScanState(ScanStates.ShipmentNumber);
            }

            ClearScreen(false);
            this.Document.Update(doc);
        }


        protected IEnumerable scanLogs()
        {
            foreach (ScanLog row in ScanLogs.Cache.Cached)
            {
                yield return row;
            }
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
            foreach (SOShipLineSplit row in PackageSplits.Cache.Cached)
            {
                var ext = PackageSplits.Cache.GetExtension<SOShipLineSplitExt>(row);
                if (this.Packages.Current != null && ext.PackageLineNbr == this.Packages.Current.LineNbr)
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
                doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.BarcodePrompt);
            }
            else
            {
                if (doc.Barcode[0] == ScanCommands.CommandChar)
                {
                    ProcessCommands(doc.Barcode);
                }
                else
                {
                    switch (doc.ScanState)
                    {
                        case ScanStates.ShipmentNumber:
                            ProcessShipmentNumber(doc.Barcode);
                            break;
                        case ScanStates.Item:
                            ProcessItemBarcode(doc.Barcode);
                            break;
                        case ScanStates.LotSerialNumber:
                            ProcessLotSerialBarcode(doc.Barcode);
                            break;
                        case ScanStates.Location:
                            ProcessLocationBarcode(doc.Barcode);
                            break;
                        case ScanStates.Weight:
                            ProcessWeight(doc.Barcode);
                            break;
                    }
                }

                InsertScanLog();
            }

            doc.Barcode = String.Empty;
            this.Document.Update(doc);
        }

        protected virtual void ProcessShipmentNumber(string barcode)
        {
            var doc = this.Document.Current;
            doc.ShipmentNbr = barcode.Trim();
            SelectShipment(doc);
        }

        protected virtual void ProcessCommands(string barcode)
        {
            var doc = this.Document.Current;
            string[] commands = barcode.Split(ScanCommands.CommandChar);

            int quantity = 0;
            if (int.TryParse(commands[1].ToUpperInvariant(), out quantity))
            {
                if (IsQuantityEnabled())
                {
                    doc.Quantity = quantity;
                    doc.Status = ScanStatuses.Information;
                    doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.CommandSetQuantity, quantity);
                }
                else
                {
                    doc.Status = ScanStatuses.Error;
                    doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.CommandAccessRightsError);
                }
            }
            else
            {
                switch (commands[1].ToUpperInvariant())
                {
                    case ScanCommands.Add:
                        this.Document.Current.ScanMode = ScanModes.Add;
                        doc.Status = ScanStatuses.Information;
                        doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.CommandAdd);
                        break;
                    case ScanCommands.Remove:
                        this.Document.Current.ScanMode = ScanModes.Remove;
                        doc.Status = ScanStatuses.Information;
                        doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.CommandRemove);
                        break;
                    case ScanCommands.Item:
                        this.Document.Current.LotSerialSearch = false;
                        doc.Status = ScanStatuses.Information;
                        doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.CommandInventory);
                        break;
                    case ScanCommands.LotSerial:
                        this.Document.Current.LotSerialSearch = true;
                        doc.Status = ScanStatuses.Information;
                        doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.CommandLot);
                        break;
                    case ScanCommands.Confirm:
                        if (Confirm.GetEnabled())
                        {
                            this.Confirm.Press();
                        }
                        else
                        {
                            doc.Status = ScanStatuses.Error;
                            doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.CommandAccessRightsError);
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
                            doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.CommandAccessRightsError);
                        }
                        break;
                    case ScanCommands.Cancel:
                        if (doc.ScanState == ScanStates.Item)
                        {
                            ClearScreen(true);
                            doc.Status = ScanStatuses.Clear;
                            doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.CommandClear);
                        }
                        else if (doc.ScanState != ScanStates.ShipmentNumber)
                        {
                            SetScanState(ScanStates.Item);
                            doc.Status = ScanStatuses.Information;
                            doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.BarcodePrompt);
                        }
                        break;
                    case ScanCommands.NewPackage:
                        ProcessNewPackageCommand(commands, false);
                        break;
                    case ScanCommands.NewPackageAutoCalcWeight:
                        ProcessNewPackageCommand(commands, true);
                        break;
                    case ScanCommands.PackageComplete:
                        ProcessPackageCompleteCommand(false);
                        break;
                    default:
                        doc.Status = ScanStatuses.Error;
                        doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.CommandUnknown);
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
                SetCurrentPackageWeight(weight);
            }
            else
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageInvalidWeight, barcode);
            }
        }

        protected virtual decimal ConvertKilogramToWeightUnit(decimal weight, string weightUnit)
        {
            decimal conversionFactor;

            if (kgToWeightUnit.TryGetValue(weightUnit.Trim().ToUpperInvariant(), out conversionFactor))
            {
                return weight / conversionFactor;
            }
            else
            {
                throw new PXException(WM.Messages.PackageWrongWeightUnit, weightUnit);
            }
        }

        protected virtual void ClearScreen(bool clearShipmentNbr)
        {
            if (clearShipmentNbr)
            {
                this.Document.Current.ShipmentNbr = null;
                SetScanState(ScanStates.ShipmentNumber);
            }

            this.Document.Current.CurrentInventoryID = null;
            this.Document.Current.CurrentSubID = null;
            this.Document.Current.CurrentLocationID = null;
            this.Document.Current.CurrentLotSerialNumber = null;
            this.Document.Current.CurrentExpirationDate = null;
            this.Document.Current.CurrentPackageLineNbr = null;
            this.Transactions.Cache.Clear();
            this.Splits.Cache.Clear();
            this.ScanLogs.Cache.Clear();
            this.Packages.Cache.Clear();
            this.PackageSplits.Cache.Clear();
        }

        protected virtual void ProcessItemBarcode(string barcode)
        {
            var doc = this.Document.Current;
            
            if (doc.LotSerialSearch == true)
            {
                INLotSerialStatus lotSerialStatus = GetLotSerialStatus(barcode);
                INLotSerClass lotSerialClass = (lotSerialStatus != null ? GetLotSerialClass(lotSerialStatus.InventoryID) : null);

                if (ValidateLotSerialStatus(barcode, lotSerialStatus, lotSerialClass))
                {
                    SetCurrentInventoryIDByLotSerial(lotSerialStatus);
                }
                else
                {
                    return;
                }
            }
            else
            {
                bool lotSerialNumbered = false;
                if(!SetCurrentInventoryIDByItemBarcode(barcode, out lotSerialNumbered))
                {
                    doc.Status = ScanStatuses.Error;
                    doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.BarcodeMissing, barcode);
                    return;
                }

                if(lotSerialNumbered)
                {
                    doc.Status = ScanStatuses.Scan;
                    doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.LotScanPrompt);
                    SetScanState(ScanStates.LotSerialNumber);
                    return;
                }
            }
            
            if(IsLocationRequired())
            {
                doc.Status = ScanStatuses.Scan;
                doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.LocationPrompt);
                SetScanState(ScanStates.Location);
                return;
            }

            ProcessPick();
        }

        protected virtual void ProcessLotSerialBarcode(string barcode)
        {
            var doc = this.Document.Current;

            INLotSerialStatus lotSerialStatus = GetLotSerialStatus(barcode);
            INLotSerClass lotSerialClass = GetLotSerialClass(doc.CurrentInventoryID);
            
            if (ValidateLotSerialStatus(barcode, lotSerialStatus, lotSerialClass))
            {
                doc.CurrentLotSerialNumber = barcode;
                doc.CurrentExpirationDate = lotSerialStatus.ExpireDate;

                if (IsLocationRequired())
                {
                    doc.Status = ScanStatuses.Scan;
                    doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.LocationPrompt);
                    SetScanState(ScanStates.Location);
                    return;
                }

                ProcessPick();
            }
        }

        protected virtual void ProcessLocationBarcode(string barcode)
        {
            var doc = this.Document.Current;

            INLocation location = PXSelect<INLocation,
                                 Where<INLocation.siteID, Equal<Current<SOShipment.siteID>>,
                                 And<INLocation.locationCD, Equal<Required<INLocation.locationCD>>>>>.Select(this, barcode);

            if(location == null)
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.LocationInvalid, barcode);
                return;
            }

            doc.CurrentLocationID = location.LocationID;

            ProcessPick();
        }
        
        protected virtual void InsertScanLog()
        {
            const int maxCharLength = 256;
            var doc = this.Document.Current;
            
            ScanLog scanLog = (ScanLog)this.ScanLogs.Cache.CreateInstance();
            scanLog.LogLineDate = PXTimeZoneInfo.Now;
            scanLog.LogBarcode = doc.Barcode.Length <= maxCharLength ? doc.Barcode : doc.Barcode.Substring(0, maxCharLength);
            scanLog.LogMessage = doc.Message.Length <= maxCharLength ? doc.Message : doc.Message.Substring(0, maxCharLength);
            ScanLogs.Cache.Insert(scanLog);
        }

        protected virtual bool IsLocationRequired()
        {
			return PXAccess.FeatureInstalled<FeaturesSet.warehouseLocation>();
        }
        
        protected virtual bool SetCurrentInventoryIDByItemBarcode(string barcode, out bool lotSerialNumbered)
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

            lotSerialNumbered = false;

            if (rec == null)
            {
                return false;
            }
            else
            {
                var inventoryItem = (InventoryItem)rec;
                var sub = (INSubItem)rec;
                var lsclass = (INLotSerClass)rec;

                if (lsclass.LotSerTrack != INLotSerTrack.NotNumbered)
                {
                    if (lsclass.LotSerAssign == INLotSerAssign.WhenUsed && lsclass.LotSerTrackExpiration == true)
                    {
                        //TODO: Implement support for this by prompting for expiration date (ScanStates.ExpirationDate)
                        throw new NotImplementedException(WM.Messages.LotNotSupported);
                    }

                    if(inventoryItem.BaseUnit != inventoryItem.SalesUnit)
                    {
                        //TODO: Implement support for this by prompting user to enter as many serial/lot as what's included in the SaleUnit.
                        throw new NotImplementedException("Items which are lot/serial tracked must use the same base and sale unit of measures.");
                    }

                    lotSerialNumbered = true;
                }

                doc.CurrentInventoryID = inventoryItem.InventoryID;
                doc.CurrentSubID = sub.SubItemID;
                return true;
            }
        }

        protected virtual void SetScanState(string state)
        {
            var doc = this.Document.Current;

            //Add any state transition logic to this switch case
            switch (state)
            {
                case ScanStates.Item:
                    doc.Quantity = 1;
                    doc.ScanMode = ScanModes.Add;
                    doc.CurrentInventoryID = null;
                    doc.CurrentSubID = null;
                    doc.CurrentLocationID = null;
                    doc.CurrentLotSerialNumber = null;
                    doc.CurrentExpirationDate = null;
                    break;
            }

            this.Document.Current.ScanState = state;
        }

        protected virtual void ProcessPick()
        {
            var doc = this.Document.Current;

            if (Document.Current.ScanMode == ScanModes.Add && AddPick(doc.CurrentInventoryID, doc.CurrentSubID, doc.Quantity, doc.CurrentLocationID, doc.CurrentLotSerialNumber, doc.CurrentExpirationDate))
            {
                doc.Status = ScanStatuses.Scan;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.InventoryAdded, Document.Current.Quantity, ((InventoryItem)PXSelectorAttribute.Select<PickPackInfo.currentInventoryID>(this.Document.Cache, doc)).InventoryCD.TrimEnd());
                SetScanState(ScanStates.Item);
            }
            else if (Document.Current.ScanMode == ScanModes.Remove && RemovePick(doc.CurrentInventoryID, doc.CurrentSubID, doc.Quantity, doc.CurrentLocationID, doc.CurrentLotSerialNumber))
            {
                doc.Status = ScanStatuses.Scan;
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.InventoryRemoved, Document.Current.Quantity, ((InventoryItem)PXSelectorAttribute.Select<PickPackInfo.currentInventoryID>(this.Document.Cache, doc)).InventoryCD.TrimEnd());
                SetScanState(ScanStates.Item);
            }
            else
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.InventoryMissing);
            }
        }

        protected virtual void ProcessNewPackageCommand(string[] commands, bool autoCalcWeight)
        {
            var doc = this.Document.Current;
           
            if(commands.Length != 3)
            {
                //We're expecting something that looks like *P*LARGE
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.PackageCommandMissingBoxId);
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
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageBoxMissing, boxID);
            }
            else
            {
                var newPackage = (SOPackageDetailPick)this.Packages.Cache.CreateInstance();
                newPackage.BoxID = box.BoxID;
                newPackage = this.Packages.Insert(newPackage);
                doc.CurrentPackageLineNbr = newPackage.LineNbr;

                ProcessPackageCompleteCommand(autoCalcWeight);
            }
        }

        protected virtual void ProcessPackageCompleteCommand(bool autoCalcWeight)
        {
            var doc = this.Document.Current;

            if (doc.CurrentPackageLineNbr == null)
            {
                doc.Status = ScanStatuses.Error;
                doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.PackageMissingCurrent);
            }
            else
            {
                //Attach any unlinked splits to newly inserted package
                foreach (SOShipLineSplit split in this.Splits.Cache.Cached)
                {
                    var ext = this.Splits.Cache.GetExtension<SOShipLineSplitExt>(split);
                    if (ext.PackageLineNbr == null)
                    {
                        ext.PackageLineNbr = doc.CurrentPackageLineNbr;
                        this.Splits.Update(split);
                    }
                }

                if (autoCalcWeight)
                {
                    ProcessAutoCalcWeight();
                }
                else if (this.UserSetup.Current.UseScale == true)
                {
                    ProcessScaleWeight();
                }
                else
                {
                    PromptForPackageWeight(false);
                }
            }
        }

        protected virtual void ProcessAutoCalcWeight()
        {
            var doc = this.Document.Current;
            decimal weight = 0M;

            if (!CalculatePackageWeightFromItemsAndBoxConfiguration(out weight))
            {
                PromptForPackageWeight(true);
            }
            else
            {
                SetCurrentPackageWeight(weight);
            }
        }

        protected virtual void ProcessScaleWeight()
        {
            var doc = this.Document.Current;
            var scale = (SMScale)PXSelect<SMScale, Where<SMScale.scaleID, Equal<Required<SOPickPackShipUserSetup.scaleID>>>>.Select(this, this.UserSetup.Current.ScaleID);

            if (scale == null)
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
                decimal convertedWeight = ConvertKilogramToWeightUnit(scale.LastWeight.GetValueOrDefault(), Setup.Current.WeightUOM);
                SetCurrentPackageWeight(convertedWeight);
            }
        }

        protected virtual void PromptForPackageWeight(bool autoCalcFailed)
        {
            var doc = this.Document.Current;
            doc.Status = ScanStatuses.Information;

            if (autoCalcFailed)
            {
                doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageWeightAutoCalcFailedPrompt);
            }
            else
            {
                doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.PackageWeightPrompt);
            }

            SetScanState(ScanStates.Weight);
        }

        protected virtual bool CalculatePackageWeightFromItemsAndBoxConfiguration(out decimal weight)
        {
            weight = 0M;

            // Add items weight
            foreach (SOShipLineSplit split in this.Splits.Cache.Cached)
            {
                SOShipLineSplitExt ext = this.Splits.Cache.GetExtension<SOShipLineSplitExt>(split);
                if (ext.PackageLineNbr != this.Document.Current.CurrentPackageLineNbr) continue;

                SOShipLinePick currentShipLine = (SOShipLinePick)this.Transactions.Search<SOShipLinePick.lineNbr>(split.LineNbr);

                if (currentShipLine != null)
                    weight += split.BaseQty.GetValueOrDefault() * currentShipLine.UnitWeigth.GetValueOrDefault();
            }

            if (weight == 0)
            {
                return false;
            }

            // Add box weight
            CSBox box = PXSelect<CSBox, Where<CSBox.boxID, Equal<Required<CSBox.boxID>>>>.Select(this, GetCurrentPackageDetailPick().BoxID);
            if (box == null)
            {
                //Shouldn't happen
                return false;
            }
            else
            {
                weight = decimal.Round(weight + box.BoxWeight.Value, SOPackageInfo.BoxWeightPrecision);
            }

            return true;
        }

        protected virtual void SetCurrentPackageWeight(decimal weight)
        {
            var doc = this.Document.Current;
            SOPackageDetailPick package = GetCurrentPackageDetailPick();

            package.Weight = weight;
            this.Packages.Update(package);

            doc.Status = ScanStatuses.Information;
            doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageComplete, weight, Setup.Current.WeightUOM);
            doc.CurrentPackageLineNbr = null;
            SetScanState(ScanStates.Item);
        }

        protected virtual SOPackageDetailPick GetCurrentPackageDetailPick()
        {
            SOPackageDetailPick package = (SOPackageDetailPick)this.Packages.Search<SOPackageDetailPick.lineNbr>(this.Document.Current.CurrentPackageLineNbr);

            if (package == null)
            {
                throw new PXException(PXMessages.LocalizeFormatNoPrefix(WM.Messages.PackageLineNbrMissing, this.Document.Current.CurrentPackageLineNbr));
            }

            return package;
        }

        protected virtual INLotSerClass GetLotSerialClass(int? inventoryID)
        {
            return (INLotSerClass)PXSelectJoin<INLotSerClass,
                    InnerJoin<InventoryItem, On<INLotSerClass.lotSerClassID, Equal<INLotSerClass.lotSerClassID>>>,
                    Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>.Select(this, inventoryID);
        }

        protected virtual INLotSerialStatus GetLotSerialStatus(string barcode)
        {
            INLotSerialStatus lotSerialStatus = null;

            foreach (INLotSerialStatus ls in PXSelect<INLotSerialStatus,
                                             Where<INLotSerialStatus.qtyOnHand, Greater<Zero>,
                                             And<INLotSerialStatus.siteID, Equal<Current<SOShipment.siteID>>,
                                             And<INLotSerialStatus.lotSerialNbr, Equal<Required<INLotSerialStatus.lotSerialNbr>>>>>>.Select(this, barcode))
            {
                if (lotSerialStatus == null)
                {
                    lotSerialStatus = ls;
                }
                else
                {
                    throw new PXException(WM.Messages.LotUniquenessError);
                }
            }

            return lotSerialStatus;
        }

        protected virtual void SetCurrentInventoryIDByLotSerial(INLotSerialStatus lotSerialStatus)
        {
            var doc = this.Document.Current;
            doc.CurrentInventoryID = lotSerialStatus.InventoryID;
            doc.CurrentSubID = lotSerialStatus.SubItemID;
            doc.CurrentLocationID = lotSerialStatus.LocationID;
            doc.CurrentLotSerialNumber = lotSerialStatus.LotSerialNbr;
            doc.CurrentExpirationDate = lotSerialStatus.ExpireDate;
        }

        protected virtual bool ValidateLotSerialStatus(string barcode, INLotSerialStatus lotSerialStatus, INLotSerClass lotSerialClass)
        {
            var doc = this.Document.Current;

            if (lotSerialClass != null &&
                lotSerialClass.LotSerTrack != INLotSerTrack.NotNumbered &&
                lotSerialClass.LotSerAssign == INLotSerAssign.WhenReceived)
            {
                if (lotSerialStatus == null)
                {
                    doc.Status = ScanStatuses.Error;
                    doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.LotMissing, barcode);
                    return false;
                }
                else if (lotSerialClass.LotSerTrackExpiration == true &&
                         IsLotExpired(lotSerialStatus))
                {
                    doc.Status = ScanStatuses.Error;
                    doc.Message = PXMessages.LocalizeFormatNoPrefix(WM.Messages.LotExpired, barcode);
                    return false;
                }
            }

            return true;
        }

        protected virtual bool IsLotExpired(INLotSerialStatus lotSerialStatus)
        {
            return lotSerialStatus != null && lotSerialStatus.ExpireDate <= PXTimeZoneInfo.Now;
        }

        protected virtual bool AddPick(int? inventoryID, int? subID, decimal? quantity, int? locationID, string lotSerialNumber, DateTime? expirationDate)
        {
            SOShipLinePick firstMatchingLine = null;
            foreach (SOShipLinePick pickLine in this.Transactions.Select())
            {
                if (pickLine.InventoryID != inventoryID || (pickLine.SubItemID != subID && Setup.Current.UseInventorySubItem == true)) continue;
                if (firstMatchingLine == null) firstMatchingLine = pickLine;
                if (pickLine.PickedQty.GetValueOrDefault() >= pickLine.ShippedQty.GetValueOrDefault()) continue;

                //We first try to fill all the lines sequentially - item may be present multiple times on the shipment
                decimal quantityForCurrentPickLine = Math.Min(quantity.GetValueOrDefault(), pickLine.ShippedQty.GetValueOrDefault() - pickLine.PickedQty.GetValueOrDefault());
                pickLine.PickedQty = pickLine.PickedQty.GetValueOrDefault() + quantityForCurrentPickLine;
                this.Transactions.Update(pickLine);
                
                AddPickToCurrentLineSplits(locationID, lotSerialNumber, expirationDate, quantityForCurrentPickLine);

                quantity = quantity - quantityForCurrentPickLine;
            
                if(quantity == 0)
                {
                    return true;
                }
            }

            if (firstMatchingLine != null)
            {
                //All the lines are already filled; just over-pick the first one.
                firstMatchingLine.PickedQty = firstMatchingLine.PickedQty.GetValueOrDefault() + quantity;
                this.Transactions.Update(firstMatchingLine);
                AddPickToCurrentLineSplits(locationID, lotSerialNumber, expirationDate, quantity.GetValueOrDefault());

                return true;
            }
            else
            {
                //Item not found.
                return false;
            }
        }

        protected virtual void AddPickToCurrentLineSplits(int? locationID, string lotSerialNumber, DateTime? expirationDate, decimal quantity)
        {
            if (String.IsNullOrEmpty(lotSerialNumber))
            {
                //This is not a serialized item, we can add quantity to existing split.
                bool foundMatchingSplit = false;
                foreach(SOShipLineSplit split in this.Splits.Select())
                {
                    // Splits are linked to the corresponding package line number. If this is a new package, PackageLineNbr will be null.
                    var ext = this.Splits.Cache.GetExtension<SOShipLineSplitExt>(split);
                    if (split.LocationID == locationID && ext.PackageLineNbr == this.Document.Current.CurrentPackageLineNbr)
                    {
                        split.Qty += quantity;
                        this.Splits.Update(split);
                        foundMatchingSplit = true;
                        break;
                    }
                }

                if(!foundMatchingSplit)
                {
                    InsertSplit(quantity, locationID, lotSerialNumber, expirationDate);
                }
            }
            else
            {
                //Each lot/serial split needs to be inserted as a separate line.
                for (int i = 0; i < quantity; i++)
                {
                    InsertSplit(1, locationID, lotSerialNumber, expirationDate);
                }
            }
        }

        protected virtual void InsertSplit(decimal quantity, int? locationID, string lotSerialNumber, DateTime? expirationDate)
        {
            var split = (SOShipLineSplit)this.Splits.Cache.CreateInstance();
            split.Qty = quantity;
            split.LocationID = locationID;
            split.LotSerialNbr = lotSerialNumber;
            split.ExpireDate = expirationDate;
            this.Splits.Insert(split);

            if(this.Document.Current.CurrentPackageLineNbr != null)
            { 
                var ext = this.Splits.Cache.GetExtension<SOShipLineSplitExt>(split);
                ext.PackageLineNbr = this.Document.Current.CurrentPackageLineNbr;
                this.Splits.Update(split);
            }
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

        protected virtual bool RemovePick(int? inventoryID, int? subID, decimal? quantity, int? locationID, string lotSerialNumber)
        {
            foreach (SOShipLinePick pickLine in this.Transactions.Select())
            {
                if (pickLine.InventoryID != inventoryID || (pickLine.SubItemID != subID && Setup.Current.UseInventorySubItem == true)) continue;
                if (pickLine.PickedQty.GetValueOrDefault() <= 0) continue;

                this.Transactions.Current = pickLine;
                foreach (SOShipLineSplit split in this.Splits.Select())
                {
                    var ext = this.Splits.Cache.GetExtension<SOShipLineSplitExt>(split);

                    if (split.LocationID != locationID && locationID != null) continue;
                    if (ext.PackageLineNbr != this.Document.Current.CurrentPackageLineNbr) continue;
                    if (split.LotSerialNbr != lotSerialNumber && !String.IsNullOrEmpty(lotSerialNumber)) continue;

                    decimal quantityToRemoveForSplit = Math.Min(split.Qty.GetValueOrDefault(), quantity.GetValueOrDefault());
                    quantity -= quantityToRemoveForSplit;
                    split.Qty -= quantityToRemoveForSplit;

                    if (split.Qty == 0)
                    {
                        this.Splits.Delete(split);
                    }
                    else
                    {
                        this.Splits.Update(split);
                    }

                    pickLine.PickedQty -= quantityToRemoveForSplit;
                    if (pickLine.PickedQty == 0) pickLine.PickedQty = null;
                    this.Transactions.Update(pickLine);
                    
                    if (quantity == 0) break;
                }

                if (quantity == 0) break;
            }
            
            if(quantity == 0)
            {
                return true;
            }
            else
            {
                //TODO: Handle situation where we were able to partially remove a pick
                //returning false will show InventoryMissing message which is inaccurate
                return false;
            }
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
                doc.Message = PXMessages.LocalizeNoPrefix(WM.Messages.ShipmentMissing);
                this.Document.Update(doc);
                return;
            }

            if (confirmMode == ConfirmMode.AllItems || !IsConfirmationNeeded() ||
                this.Document.Ask(PXMessages.LocalizeNoPrefix(WM.Messages.ShipmentQuantityMismatchPrompt), MessageButtons.YesNo) == PX.Data.WebDialogResult.Yes)
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
            var printSetup = (SOPickPackShipUserSetup) UserSetup.Select();

            if (printSetup.ShipmentConfirmation == true)
            {
                //TODO: SO642000 shouldn't be hardcoded - this needs to be read from notification; see PickListPrintToQueueExtensions for example
                if (jobMaint == null) jobMaint = PXGraph.CreateInstance<PrintJobMaint>();
                jobMaint.AddPrintJob(PXMessages.LocalizeFormatNoPrefix(WM.Messages.PrintShipmentConfirmation, graph.Document.Current.ShipmentNbr), printSetup.ShipmentConfirmationQueue, "SO642000", new Dictionary<string, string> { { "ShipmentNbr", graph.Document.Current.ShipmentNbr } });
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
                            jobMaint.AddPrintJob(PXMessages.LocalizeFormatNoPrefix(WM.Messages.PrintShipmentlabel, id.ToString()), printSetup.ShipmentLabelsQueue, "", new Dictionary<string, string> { { "FILEID", id.ToString() } });
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

                    //Set any lot/serial numbers as well as locations that were assigned
                    this.Transactions.Current = pickLine;
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
        }

        protected virtual void SOPackageDetailPick_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
        {
            var row = e.Row as SOPackageDetailPick;
            if (row == null) return;

            //Detach any splits that was linked to the just deleted package
            foreach(SOShipLineSplit split in this.Splits.Cache.Cached)
            {
                var ext = this.Splits.Cache.GetExtension<SOShipLineSplitExt>(split);
                if (ext.PackageLineNbr == row.LineNbr)
                {
                    ext.PackageLineNbr = null;
                    this.Splits.Update(split);
                }
            }
        }

        protected virtual void SOPackageDetailPick_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            SOPackageDetailPick row = e.Row as SOPackageDetailPick;
            if (row == null) return;

            row.WeightUOM = Setup.Current.WeightUOM;
            row.IsCurrent = (this.Document.Current.CurrentPackageLineNbr != null && row.LineNbr == this.Document.Current.CurrentPackageLineNbr);
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
        
        protected void SOShipLinePick_PickedQty_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            SOShipLinePick soShipLinePick = e.Row as SOShipLinePick;

            if (soShipLinePick != null)
            {
                const string quantityDisplayFormat = "{0:0.00}";

                sender.RaiseExceptionHandling<SOShipLinePick.pickedQty>(soShipLinePick, 
                                                                        soShipLinePick.PickedQty,
                                                                        new PXSetPropertyException<SOShipLinePick.pickedQty>(PXMessages.LocalizeFormatNoPrefix(WM.Messages.InventoryUpdated,
                                                                                                                                                               string.Format(CultureInfo.InvariantCulture, quantityDisplayFormat, e.OldValue != null ? e.OldValue : 0M),
                                                                                                                                                               string.Format(CultureInfo.InvariantCulture, quantityDisplayFormat, soShipLinePick.PickedQty.HasValue ? soShipLinePick.PickedQty.Value : 0M),
                                                                                                                                                               soShipLinePick.UOM.Trim()),
                                                                                                                             PXErrorLevel.RowInfo));
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

using System;
using PX.Data;
using PX.Common;

namespace PX.Objects.WM
{
    [PXLocalizable]
    public static class Messages
    {
        public const string Add = "Add";
        public const string Remove = "Remove";

        public const string MissingFileIdError = "Print job has no 'Report ID' or 'FILEID' parameter defined.";
        
        #region Barcode
        public const string BarcodeMissing = "Barcode {0} not found in database.";
        public const string BarcodePrompt = "Please scan a barcode.";
        #endregion
        
        #region Command
        public const string CommandAdd = "Add mode set.";
        public const string CommandClear = "Screen cleared.";
        public const string CommandInventory = "Ready to search by item barcode.";
        public const string CommandLot = "Ready to search by lot/serial number.";
        public const string CommandRemove = "Remove mode set.";
        public const string CommandUnknown = "Unknown command.";
        public const string CommandSetQuantity = "Quantity set to {0}.";
        public const string CommandAccessRightsError = "Insufficient access rights to perform this command.";
        #endregion

        #region Lot/Serial
        public const string LotExpired = "Lot/serial {0} is expired.";
        public const string LotInvalidQuantity = "Lot/serial {0} not found in sufficient quantity on shipment.";
        public const string LotMissing = "Lot/serial {0} not found in database.";
        public const string LotNotSupported = "Lot/serial numbers that are assigned when used and which require tracking of expiration date are not supported with this tool.";
        public const string LotNotSupportedUOM = "Items which are lot/serial tracked must use the same base and sale unit of measures.";
        public const string LotScanPrompt = "Please scan lot/serial number.";
        public const string LotSplitQuantityError = "Unexpected split quantity for lot/serial {0} (Quantity: {1}).";
        public const string LotUniquenessError = "More than one lot/serial entry was found. This is not yet supported, please search by Inventory ID.";
        public const string SerialDuplicateError = "Serial {0} has already been picked.";
        public const string SerialInvalidQuantity = "Quantity for serial numbered items must be 1.";
        #endregion

        #region Location
        public const string LocationPrompt = "Please scan location.";
        public const string LocationInvalid = "Location {0} not found in database.";

        #endregion

        #region Inventory
        public const string InventoryAdded = "Added {0} x {1}.";
        public const string InventoryRemoved = "Removed {0} x {1}.";
        public const string InventoryMissing = "Item not found on shipment or in current package.";
        public const string InventoryUpdated = "Picked quantity updated from {0} to {1} {2}.";
        #endregion

        #region Package
        public const string PackageBoxMissing = "Box {0} not found in database.";
        public const string PackageComplete = "Package is complete. Weight: {0:0.0000} {1}";
        public const string PackageCompletePrompt = "Please complete the current package using the {0}{1} command.";
        public const string PackageCommandMissingBoxId = "The New Package command must be followed by a Box ID.";
        public const string PackageIncompleteError = "Please complete the current package using the {0}{1} command.";
        public const string PackageInvalidFileExtension = "Unsupported file extension attached to the package for Shipment {0}/{1}";
        public const string PackageInvalidWeight = "{0} is not a valid weight.";
        public const string PackageLineNbrMissing = "Unable to find package line {0} - was it deleted manually?";
        public const string PackageMissingCurrent = "There is no package currently selected or in process.";
        public const string PackageRemoveInventoryError = "The system was not able to locate package details for the item you just removed.";
        public const string PackageWeightAutoCalcFailedPrompt = "Weight couldn't be calculated automatically. Please enter package total weight and press enter.";
        public const string PackageWeightPrompt = "Please enter package total weight and press enter.";
        public const string PackageWrongWeightUnit = "Wrong weight unit: {0}, only KG and LB are supported.";
        #endregion

        #region Print
        public const string PrintPickList = "Pick List #{0}";
        public const string PrintShipmentConfirmation = "Shipment Confirmation #{0}";
        public const string PrintShipmentlabel = "Shipment Label #{0}";
        #endregion

        #region Scale
        public const string ScaleMissing = "Scale {0} not found in database.";
        public const string ScaleTimeout = "Measurement on scale {0} is more than {1} seconds old. Remove package from the scale and weigh it again.";
        #endregion

        #region Shipment
        public const string ShipmentConfirmedFull = "Shipment {0} confirmed in full.";
        public const string ShipmentConfirmedPicked = "Shipment {0} confirmed as picked.";
        public const string ShipmentLineMissing = "Line {0} not found in shipment.";
        public const string ShipmentMissing = "Shipment not found.";
        public const string ShipmentNbrMissing = "Shipment {0} not found.";
        public const string ShipmentQuantityMismatchPrompt = "The quantity picked for one or more lines doesn't match with the shipment. Do you want to continue?";
        public const string ShipmentReady = "Shipment {0} loaded and ready to pick.";
        #endregion
    }
}

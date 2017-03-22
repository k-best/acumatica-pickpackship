namespace PX.Objects.SO
{
    using System;
    using PX.Data;
    using PX.Objects.IN;
    using PX.Objects.CS;
    using PX.Objects.GL;

    [System.SerializableAttribute()]
    [PXCacheName(PX.Objects.WM.Messages.SOPackageDetailSplit)]
    public partial class SOPackageDetailSplit : PX.Data.IBqlTable
    {
        #region ShipmentNbr
        public abstract class shipmentNbr : PX.Data.IBqlField { }
        [PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
        [PXDBDefault(typeof(SOShipment.shipmentNbr))]
        [PXParent(typeof(Select<SOPackageDetail, Where<SOPackageDetail.shipmentNbr, Equal<Current<SOPackageDetailSplit.shipmentNbr>>, And<SOPackageDetail.lineNbr, Equal<Current<SOPackageDetailSplit.lineNbr>>>>>))]
        public virtual String ShipmentNbr { get; set; }
        #endregion
        #region LineNbr
        public abstract class lineNbr : PX.Data.IBqlField { }
        [PXDBInt(IsKey = true)]
        [PXDefault(typeof(SOPackageDetail.lineNbr))]
        public virtual Int32? LineNbr { get; set; }
        #endregion
        #region SplitLineNbr
        public abstract class splitLineNbr : PX.Data.IBqlField { }
        [PXDBInt(IsKey = true)]
        [PXDefault()]
        [PXLineNbr(typeof(SOShipment.lineCntr))]
        public virtual Int32? SplitLineNbr { get; set; }
        #endregion
        #region InventoryID
        public abstract class inventoryID : PX.Data.IBqlField { }
        [Inventory]
        [PXDefault]
        public virtual Int32? InventoryID { get; set; }
        #endregion
        #region SubItemID
        public abstract class subItemID : PX.Data.IBqlField { }
        [IN.SubItem(typeof(SOPackageDetailSplit.inventoryID))]
        [PXDefault]
        public virtual Int32? SubItemID { get; set; }
        #endregion
        #region LotSerialNbr
        public abstract class lotSerialNbr : PX.Data.IBqlField { }
        [PXDBString(100, IsUnicode = true)]
        [PXUIField(DisplayName = "Lot/Serial Nbr.", FieldClass = "LotSerial")]
        public virtual String LotSerialNbr { get; set; }
        #endregion
        #region ExpireDate
        public abstract class expireDate : PX.Data.IBqlField { }
        [PXDBDate]
        [PXUIField(DisplayName = "Expiration Date", FieldClass = "LotSerial")]
        public virtual DateTime? ExpireDate { get; set; }
        #endregion
        #region UOM
        public abstract class uOM : PX.Data.IBqlField { }
        [INUnit(typeof(SOPackageDetailSplit.inventoryID), DisplayName = "UOM")]
        [PXDefault(typeof(SOShipLine.uOM))]
        public virtual String UOM { get; set; }
        #endregion
        #region Qty
        public abstract class qty : PX.Data.IBqlField { }
        [PXDBQuantity(typeof(SOPackageDetailSplit.uOM), typeof(SOPackageDetailSplit.baseQty))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Quantity")]
        public virtual Decimal? Qty { get; set; }
        #endregion
        #region BaseQty
        public abstract class baseQty : PX.Data.IBqlField { }
        [PXDBDecimal(6)]
        public virtual Decimal? BaseQty { get; set; }
        #endregion

        #region System Columns
        public abstract class createdByID : PX.Data.IBqlField { }
        [PXDBCreatedByID]
        public virtual Guid? CreatedByID { get; set; }

        public abstract class createdByScreenID : PX.Data.IBqlField { }
        [PXDBCreatedByScreenID]
        public virtual String CreatedByScreenID { get; set; }

        public abstract class createdDateTime : PX.Data.IBqlField { }
        [PXDBCreatedDateTime]
        public virtual DateTime? CreatedDateTime { get; set; }

        public abstract class lastModifiedByID : PX.Data.IBqlField { }
        [PXDBLastModifiedByID]
        public virtual Guid? LastModifiedByID { get; set; }

        public abstract class lastModifiedByScreenID : PX.Data.IBqlField { }
        [PXDBLastModifiedByScreenID]
        public virtual String LastModifiedByScreenID { get; set; }

        public abstract class lastModifiedDateTime : PX.Data.IBqlField { }
        [PXDBLastModifiedDateTime]
        public virtual DateTime? LastModifiedDateTime { get; set; }

        public abstract class Tstamp : PX.Data.IBqlField { }
        [PXDBTimestamp]
        public virtual Byte[] tstamp { get; set; }
        #endregion
    }
}

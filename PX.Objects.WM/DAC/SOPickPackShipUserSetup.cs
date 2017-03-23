using System;
using PX.Data;

namespace PX.SM
{
    [Serializable]
    [PXCacheName(PX.Objects.WM.Messages.SOPickPackShipUserSetup)]
    public class SOPickPackShipUserSetup : IBqlTable
    {
        public abstract class userID : PX.Data.IBqlField { }
        [PXDBGuid(IsKey = true)]
        [PXDefault(typeof(Search<Users.pKID, Where<Users.pKID, Equal<Current<AccessInfo.userID>>>>))]
        [PXUIField(DisplayName = "User")]
        public virtual Guid? UserID { get; set; }

        public abstract class shipmentConfirmation : PX.Data.IBqlField { }
        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Print Shipment Confirmation Automatically")]
        public virtual bool? ShipmentConfirmation { get; set; }

        public abstract class shipmentConfirmationQueue : PX.Data.IBqlField { }
        [PXDBString(10)]
        [PXSelector(typeof(SMPrintQueue.printQueue))]
        [PXUIEnabled(typeof(shipmentConfirmation))]
        [PXUIField(DisplayName = "Print Queue")]
        public virtual string ShipmentConfirmationQueue { get; set; }

        public abstract class shipmentLabels : PX.Data.IBqlField { }
        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Print Shipment Labels Automatically")]
        public virtual bool? ShipmentLabels { get; set; }

        public abstract class shipmentLabelsQueue : PX.Data.IBqlField { }
        [PXDBString(10)]
        [PXSelector(typeof(SMPrintQueue.printQueue))]
        [PXUIEnabled(typeof(shipmentLabels))]
        [PXUIField(DisplayName = "Print Queue")]
        public virtual string ShipmentLabelsQueue { get; set; }

        public abstract class useScale : PX.Data.IBqlField { }
        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Use Digital Scale")]
        public virtual bool? UseScale { get; set; }

        public abstract class scaleID : PX.Data.IBqlField { }
        [PXDBString(10)]
        [PXSelector(typeof(SMScale.scaleID))]
        [PXUIEnabled(typeof(useScale))]
        [PXUIField(DisplayName = "Scale")]
        public virtual string ScaleID { get; set; }
    }
}

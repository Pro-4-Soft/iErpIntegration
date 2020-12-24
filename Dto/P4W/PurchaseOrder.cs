﻿using System;
using System.Collections.Generic;

namespace Pro4Soft.iErpIntegration.Dto.P4W
{
    public class PurchaseOrder : IdObject
    {
        public string PurchaseOrderNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public string PurchaseOrderState { get; set; }
        public Client Client { get; set; }
        public Guid? ClientId { get; set; }
        public List<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
        public DateTimeOffset? UploadDate { get; set; }
        public bool IsWarehouseTransfer { get; set; }
    }

    public class PurchaseOrderLine : IdObject
    {
        public int LineNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public Product Product { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public List<PurchaseOrderLineDetail> LineDetails { get; set; } = new List<PurchaseOrderLineDetail>();
    }

    public class PurchaseOrderLineDetail : IdObject
    {
        public int? PacksizeEachCount { get; set; }
        public int? ReceivedQuantity { get; set; }
        public string LotNumber { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public string SerialNumber { get; set; }
    }
}

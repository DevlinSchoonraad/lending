using System;

namespace Core.BorrowItem
{
    public class BorrowItemRequest
    {
        public Guid RequestorId { get; set; }
        public Guid OwnershipId { get; set; }
    }
}
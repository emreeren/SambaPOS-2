﻿using System.ComponentModel.DataAnnotations;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Settings
{
    public class PrinterTemplate : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] LastUpdateTime { get; set; }
        [StringLength(500)]
        public string HeaderTemplate { get; set; }
        [StringLength(500)]
        public string LineTemplate { get; set; }
        [StringLength(500)]
        public string VoidedLineTemplate { get; set; }
        [StringLength(500)]
        public string GiftLineTemplate { get; set; }
        [StringLength(1000)]
        public string FooterTemplate { get; set; }
        [StringLength(500)]
        public string GroupTemplate { get; set; }
        public bool MergeLines { get; set; }
    }
}

using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
public class DocumentLibrary
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LoanApplicationId { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; }

    public String Description { get; set; }

    public bool IsUsingDefaultTemplate { get; set; } = true;

    public string MasterTemplate { get; set; } = "Master Default Template";

    public byte[] MasterTemplateBytes { get; set; }

    public List<LiquidDocsData.Models.Document> Documents { get; set; } = new();

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;
}

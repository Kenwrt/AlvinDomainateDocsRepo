using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidDocsData.Models.Stripe;
public class SubscriptionItems
{
    public List<SubscriptionItem> Data { get; set; } = new();
}


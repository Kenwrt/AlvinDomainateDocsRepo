using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidDocsData.Models.Stripe;

public class EventData
{
    public WebhookEventObject Object { get; set; } = default!;
}




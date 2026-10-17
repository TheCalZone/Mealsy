# Family pricing estimate — 17 October 2026

**£3/month looks viable for modest household usage. £24/year can also cover a light household, but leaves a smaller allowance for free community users, photos, support and fixed costs. This is a scenario model, not a reading of your invoices.**

## What remains after illustrative UK tax and store fees

Assume the displayed price includes 20% UK VAT and the subscription store fee is 15% of the VAT-exclusive price. Actual settlement depends on storefront, tax treatment, developer programme and refunds.

| Product | Customer pays | Approximate proceeds |
|---|---:|---:|
| Monthly Family | £3/month | £2.125/month |
| Annual Family | £24/year | £17/year, or £1.417/month |
| Extra household place | £0.50/month | £0.354/month |

These proceeds are before infrastructure, support, payment adjustments, corporation/income tax, development time and developer account fees. Annual Family is a 33.3% discount against twelve monthly payments. Four people includes the owner.

Sources: [UK VAT rates](https://www.gov.uk/vat-rates), [subscription service fee](https://support.google.com/googleplay/android-developer/answer/112622?hl=en). Apple fees are not assumed identical; these figures model the Android subscription route.

## Usage model

Use 30 days/month, four people per paying household, each keeping the shared list open for 30 minutes/day. The existing foreground sync polls every five seconds: 43,200 callable requests/month. A shared-list read currently reads the membership index, household, paid entitlement, support grant and shopping list: about **216,000 document reads/month** before edits, status screens and transaction retries. It does not poll while the page is closed.

For a community scenario, assume each person loads three pages/day, each with 20 posts and 100 KB photo payloads. This transfers roughly **720 MB/month per household** for photos alone, before protocol overhead, repeated refreshes and private account sync. Page sizes may be up to 30 posts; actual photo sizes vary. Pagination currently reads candidate posts, likes, promotion records and business metadata, so budget several reads per candidate, rather than one read per screen.

For moderation, assume 2,000 input tokens and the configured maximum 128 output tokens per submission. At standard Gemini 2.5 Flash-Lite pricing of $0.10/M input and $0.40/M output, this is **$0.0002512/submission**, or **$2.512 per 10,000 submissions**. Image tokens, longer recipes and repeated rejected attempts change the input count. This uses standard pricing, not the more expensive priority tier. Our moderation sends no search-grounding requests.

Sources: [model pricing](https://cloud.google.com/gemini-enterprise-agent-platform/generative-ai/pricing), [document billing and free quotas](https://firebase.google.com/docs/firestore/pricing), [regional database prices](https://cloud.google.com/firestore/pricing).

## What can be concluded

The document reads and short moderation checks alone are unlikely to consume the net Family revenue under those assumptions. For planning, reserve **£0.30–£1 per moderately active household/month** for variable infrastructure, then validate that allowance against actual billing. This is an engineering allowance, not a quoted provider tariff: runtime duration, regional egress, images and the free-user audience have not been measured. Heavy photo browsing or keeping lists open for hours can exceed it.

At a £0.50 variable allowance, the monthly plan leaves about £1.63/month and the annual plan about £0.92/month toward fixed costs and supporting free users. At £1 variable cost, those contributions fall to £1.13 and £0.42. At £1.50, the annual plan loses about 8p before fixed costs. Every extra place contributes about 35p/month before its own use.

Free quotas are shared by the entire project, **not per subscriber**. Standard database quotas currently include 50,000 reads/day, 20,000 writes/day, 1 GiB storage and 10 GiB/month outbound transfer. Do not subtract those quotas separately for every household. Moderation is used by non-paying community users too; subscription revenue must cover their use or another revenue source must pay for it. Crash reporting has no standalone usage charge, but the application's server execution and logging can still incur costs. TTL deletions are billed.

## Practical pricing decision

Keep £3/month and £24/year for the initial test, with four included people and independently verified 50p/month extra places. Measure monthly infrastructure cost per paying household **and per active free user** before committing to those prices at scale. No subscription prices were changed remotely by this implementation; the app-store catalogue must be configured to match.

In the [project billing reports](https://console.cloud.google.com/billing/projects?project=mealsy-b732b), group by service/SKU and check document operations, runtime, outbound transfer, model tokens and logging. Compare with active users, subscriber count, photo traffic and rejected moderation volume over at least two representative weeks. This estimate does not include an actual billing export or measured production traffic.

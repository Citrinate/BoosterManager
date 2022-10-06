# Inventory History Documentation

I feel the need to provide unofficial documentation here, because doing anything with Steam's [Inventory History](https://steamcommunity.com/my/inventoryhistory/) is a bit of a hassle.

## API Details

#### Rate Limit

1200 requests per 12 hours, per IP address

#### Request
> **Method**: `GET`
>
> **Host**: `steamcommunity.com`
>
> **Path**: `/my/inventoryhistory/`
>
> **Query Parameters**:
>
> Name | Required | Description
> --- | --- | ---
> `ajax`|Yes|With this parameter set to anything, Steam will return the inventory history page as a JSON object
> `app[]`|No|Filters the history to only include events belonging to the specified `appID`.  This parameter can be used multiple times to filter for multiple apps.
> `cursor[time]`|No|Unix timestamp.  Filters the history to only show events older than the specified time.
> `cursor[time_frac]`|No|A whole number representing the fractional part of `cursor[time]`, giving it greater precision. Allows for 9 digits of precision, although only the first 3 seem to be used.
> `cursor[s]`|No|An unknown number.  Seems to be used to differentiate between history events in the case where multiple events might share the same timestamp.  As you go further back into your account's history, this number gets bigger.  It seems to be counting something, but I'm not sure what.
> `sessionid`|No|Unncessary.  It's not possible to view an account's inventory history with this parameter alone, you still need to send the Session ID in the request header as a cookie.
> `start_time`|No|Seems to be the same as `cursor[time]`.

#### Response

> **Content-Type**: `application/json`
>
> Name | Type | Required | Description
> --- | --- | --- |  ---
> `success`|`bool`|Yes|Success status
> `html`|`string`|Yes|The inventory history events
> `num`|`uint`|Yes|Number of events in `html`
> `descriptions`|`JObject/JArray`|Yes|Contains information about the Steam Community Items found in `html`.  If `num` is `0`, this will be an empty array.
> `apps`|`JArray`|Yes|Contains information about the Steam Apps referenced in `html`.
> `cursor`|`JObject`|No|An object used to uniquely identify the last event in `html`.  This will only be defined if older events exist.

## Possible Event Descriptions

Much of the inventory history is delivered as `html` that needs to be parsed.  This plugin current does not, and likely never will, support parsing of `html`.  The best I can do is to offer an incomplete list of possible history event descriptions.

Be aware that each of these descriptions describes a unique type of event.  For example, "Listed on the Community Market", "Listed on the Steam Community Market", and "You listed an item on the Community Market." are all different types of events, and not 3 different ways to describe the same event.

- Crafted
- Earned
- Earned a booster pack
- Earned because you own `<GameName>`
- Earned by completing your Store Discovery Queue
- Earned by crafting
- Earned by joining a team in the Summer Adventure
- Earned by participating in an event
- Earned by participating in the Monster Summer Game
- Earned by participating in the Salien Game
- Earned by redeeming Steam Points
- Earned by sale purchases
- Earned by voting
- Earned due to a Steam error in your favor
- Earned due to game play time
- Expired
- Gift sent to and redeemed by `<UserName>`
- Granted by Steam Support
- Listed on the Community Market
- Listed on the Steam Community Market
- Moved to Storage Unit
- Packed Gems into a Sack
- Purchased a gift
- Purchased from the store
- Purchased on the Community Market
- Purchased with Gems
- Received a gift
- Received a gift from `<UserName>`
- Received a guest pass
- Received by entering product code
- Received from completing tasks during an event
- Received from the Community Market
- Received from the Steam Community Market
- Redeemed a gift in your inventory
- Returned by the Community Market
- Traded
- Turned into Gems
- Unpacked Gems from Sack
- Unpacked a booster pack
- Used
- Won in a giveaway
- You canceled a listing on the Community Market. The item was returned to you.
- You listed an item on the Community Market.
- You purchased an item on the Community Market.
- You traded with `<UserName>`
- Your trade with `<UserName>` failed.

## Unknown Asset Bug

Occassionaly `html` will contain "Unknown Asset #[0-9]+" instead of the appropriate item name.  The item will still have a defined `appid`, `classid`, `instanceid`, `contextid`, and `amount`.  However, the item will be missing from `descriptions`.

This bug will resolve itself, all you need to do is to keep re-attempting to fetch the page until it does.

## Cursor Bug

Below is a description of a bug in the Inventory History API.  Dates are used for simplicity, in reality we're working with unix timestamps.

### Description of the bug

The API provides no way to fetch specific pages, instead we specify a time, and get results older than that time. The API returns a maximum of 50 history events.  If more events exist, it will return a `cursor` object we can use to find the very next event.  Ideally, we can use `cursor` to generate a chain of requests from the very first event to the very last event.  Sometimes however, the `cursor` object returned by the API will be missing, even if more events exist; breaking the chain.

For example, a chain of requests starting at `5/2/21`, but ending on `4/30/21` with a missing `cursor` object at the end might look like this:

> `5/2/21` &rarr; `5/1/21` &rarr; `4/30/21` &rarr; `Nothing`

If using Steam's inventory history page, this bug expresses itself as the "Load More History" button disappearing prematurely.

**As far as I can tell, this bug only appears when searching for history older than ~1.5 years and/or ~100,000 events.**

### How to fix the bug

There's no nice way to fix this bug.  The plugin will detect when the bug may have occurred, and provide a link to the page where it occurred.  On the `InventoryHistoryAPI` side of things, the value for `data[cursor]` will be `null` when it recieves a page with this bug on it.

This will need to be fixed in the browser.  Take the link the plugin provided and remove the `ajax=1` parameter from it.  Here we should see that there's no "Load More History" button at the bottom of the page.  It's possible that this is really just the end of your account's history; you'll need to determine that yourself.

We'll first need to "remind" Steam that older history events exist; we can do this a few different ways:

 - If you have any `app[]` filters, try removing them (it's better to remove them from the url, rather than using the "Filter options")
 - Try using the "Jump to date" feature to jump to an older date

One we've got Steam to display older events, it's likely that there's now a gap between where the history ended before, and where the history continues now.  Using our previous example, the gap might look like this:

> `5/2/21` &rarr; `5/1/21` &rarr; `4/30/21` &rarr; `1/5/21` &rarr; &hellip;

To fill this gap, we need to search for history events within the gap.  Above that would be between `4/28/21` and `1/5/21`.  Preferably as close to where the history stopped as is possible.  We can do this by, again from the link the plugin provided, gradually decreasing the `cursor[time]` parameter until new events begin to appear.  Once they do start to appear, our chain should be properly restored:

> `5/2/21` &rarr; `5/1/21` &rarr; `4/30/21` &rarr; `4/29/21` &rarr; `4/28/21` &rarr; &hellip; &rarr; `1/5/21` &rarr; &hellip;

You can now resume using the plugin per the `!loginventoryhistory` command.

**Warning:** It's possible from this point that the chain may break again, this time somewhere between `4/28/21` and `1/5/21`:

> `5/2/21` &rarr; `5/1/21` &rarr; `4/30/21` &rarr; `4/29/21` &rarr; `4/28/21` &rarr; &hellip; &rarr; `3/14/21` &rarr; `3/13/21` &rarr; `1/5/21` &rarr; &hellip;

If this happens the plugin will not detect it, as the `cursor` object will not be missing.  You'll need to monitor the plugin's activity yourself to ensure that there are no abnormal gaps in your history.  If gaps do exist, you can fill them the same way as before.

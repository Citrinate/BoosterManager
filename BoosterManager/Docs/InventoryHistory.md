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
> `ajax`|No|With this parameter set to anything, Steam will return the inventory history page as a JSON object
> `app[]`|No|Filters the history to only include events belonging to the specified `appID`.  This parameter can be used multiple times to filter for multiple apps.
> `cursor[time]`|No|Unix timestamp.  Filters the history to only show events older than the specified time.
> `cursor[time_frac]`|No|A whole number representing the fractional part of `cursor[time]`, giving it greater precision. Allows for 9 digits of precision, although only the first 3 seem to be used.
> `cursor[s]`|No|An unknown number.  Seems to be used to differentiate between history events in the case where multiple events might share the same timestamp.  As you go further back into your account's history, this number gets bigger.  It seems to be counting something, but I'm not sure what.
> `sessionid`|No|Unnecessary.  It's not possible to view an account's inventory history with this parameter alone, you still need to send the Session ID in the request header as a cookie.
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
> `cursor`|`JObject`|No|An object used to uniquely identify the last event in `html`.  Will not be present if older events do not exist, or sometimes due to [a bug](#history-ends-early-bug).

## Possible Event Descriptions

Much of the inventory history is delivered as `html` that needs to be parsed.  

> The BoosterManager plugin does not, and likely never will, support parsing of `html`.  The best I can do is to offer this incomplete list of possible history event descriptions.

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

## Missing History Bug

It's possible that Steam will skip over parts of your history.  In my experience, when it skips, it's a big skip, creating a gap several months long.  These big gaps tend to be very noticable, but I can't guarantee that the gaps will always be that large.

As an example, assuming you know there should be history on your account between `4/30/21` and `1/5/21`, your history might look like this:

```
… → 5/2/21 → 5/1/21 → 4/30/21 → 1/5/21 → 1/6/21 → …
```

This bug can be fixed [in the browser](https://steamcommunity.com/my/inventoryhistory/) by searching for history within the gap.  You can use Steam's "Jump to date" feature, or try setting the `start_date` parameter yourself.  It may take several attempts to find a value for `start_date` that causes the missing history to re-appear.

When you find a missing history at `start_date = x`, history older than `x` should also re-appear, but history newer than `x` will not.  Looking at the previous example and assuming there's history between `4/28/21` and `3/14/21`; if `x = 3/14/21` then the gap will shrink, but not disappear:

```
… → 5/2/21 → 5/1/21 → 4/30/21 → 3/14/21 → 3/13/21 → … → 1/5/21 → 1/6/21 → …
```

For this reason it's important to start your search right where the gap begins and proceed gradually.  Setting the `start_date` parameter yourself allows you to move backward 1 second at a time, while the "Jump to date" feature moves in increments of 24 hours.  It's also possible to use the `cursor[time]` and `cursor[time_frac]` parameters to move in increments of 1 millisecond.

> The BoosterManager plugin cannot detect this bug.  You'll need to monitor the plugin's activity yourself to ensure there's no gaps.  Within your `InventoryHistoryAPI`, `page - data["cursor"]["time"]` represents the size of the gap in seconds between the current page and the next page.  Be aware, both `page` and `data["cursor"]` can be `null`.

## Unknown Asset Bug

Occasionally some items in `html` will be labeled as "Unknown Asset" instead of the appropriate item name.  The affected items will still have defined `appid`, `classid`, `instanceid`, `contextid`, and `amount`, but the items will be missing from `descriptions`.

This bug is likely caused by Steam servers being down, just keep reloading the page until everything appears properly.

## History Ends Early Bug

Sometimes Steam will say there's no more history, when really there is.  When using the [Inventory History page](https://steamcommunity.com/my/inventoryhistory/), this bug expresses itself as the "Load More History" button disappearing prematurely.  When that happens, the `cursor` object returned by Steam's API is `null`.  As far as I can tell, this bug only appears when searching for history older than ~1.5 years and/or ~100,000 events.

For example, assuming we have history older than `4/30/21`, this can happen:

```
… → 5/2/21 → 5/1/21 → 4/30/21 → Nothing
```

This is resolved the same way as the [Missing History Bug](#missing-history-bug): by searching for history events at times past the cutoff.

> The BoosterManager plugin will detect when the bug may have occurred and provide a link to the page where it occurred.  On the `InventoryHistoryAPI` side of things, the value for `data[cursor]` will be `null` when it receives a page with this bug on it.

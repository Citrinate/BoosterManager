# Inventory History Documentation

I feel the need to provide unofficial documentation here, because doing anything with Steam's [Inventory History](https://steamcommunity.com/my/inventoryhistory/) is a bit of a hassle.

## API Details

#### Rate Limit

~25 requests per 1 minute, per IP address

600 requests per 12 hours, per IP address

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
> `ajax`|No|With this parameter set to anything other than `0`, Steam will return the inventory history page as a JSON object described below.
> `app[]`|No|Filters the history to only include events belonging to the specified `appID`.  This parameter can be used multiple times to filter for multiple apps.
> `cursor[time]`|No|Unix timestamp.  Filters the history to only show events older than the specified time.
> `cursor[time_frac]`|No|A whole number representing the fractional part of `cursor[time]`, giving it greater precision. Allows for 9 digits of precision, although only the first 3 seem to be used.
> `cursor[s]`|No|An unknown number.  Seems to be used to differentiate between history events in the case where multiple events might share the same timestamp.  As you go further back into your account's history, this number gets bigger.  It seems to be counting something, but I'm not sure what.
> `sessionid`|No|Unnecessary.  It's not possible to view an account's inventory history with this parameter alone, you still need to send the Session ID in the request header as a cookie.
> `start_time`|No|Seems to be the same as `cursor[time]`

#### Response

> **Content-Type**: `application/json`
>
> Name | Type | Description
> --- | --- | ---
> `success`|`bool`|Success status
> `error`|`string`|An error message for when `success` is `false`
> `html`|`string`|The inventory history events
> `num`|`uint`|Number of events in `html`
> `descriptions`|`JObject/JArray`|Contains information about the Steam Community Items found in `html`.  If `num` is `0`, this will be an empty array.  This can also be an empty array due to [a bug](#missing-descriptions-bugs).
> `apps`|`JArray`|Contains information about the Steam Apps referenced in `html`
> `cursor`|`JObject`|An object used to uniquely identify the last event in `html`.  Will not be present if older events do not exist, or sometimes due to [a bug](#history-ends-early-bug).

## Possible Event Descriptions

Much of the inventory history is delivered as `html` that needs to be parsed.  

> [!NOTE]
> The BoosterManager plugin does not, and likely never will, support parsing of `html`.  The best I can do is to offer this incomplete list of possible history event descriptions.

Be aware that each of these descriptions describes a unique type of event.  For example, "Listed on the Community Market" and "You listed an item on the Community Market." are different types of events, and not two different ways to describe the same event.

#### Steam Events

- Auction bid returned
- Crafted
- Earned
- Earned a booster pack
- Earned because you own `<GameName>`
- Earned by claiming a free sale reward.
- Earned by completing your Store Discovery Queue
- Earned by crafting
- Earned by joining a team in the Summer Adventure
- Earned by participating in an event
- Earned by participating in the Monster Summer Game
- Earned by participating in the Salien Game
- Earned by participating in the Summer Adventure
- Earned by redeeming Steam Points
- Earned by sale purchases
- Earned by voting
- Earned due to a Steam error in your favor
- Earned due to game play time
- Exchanged Gems
- Expired
- Gift sent to and redeemed by `<UserName>`
- Granted by Steam Support
- Guest pass sent to and redeemed by `<UserName>`
- Listed on the Community Market
- Packed Gems into a Sack
- Placed a bid in an auction
- Purchased a gift
- Purchased from the store
- Purchased on the Community Market
- Purchased with Gems
- Received a gift
- Received a gift from `<UserName>`
- Received a guest pass
- Received by entering product code
- Received from completing tasks during an event
- Redeemed a gift in your inventory
- Redeemed to make a purchase
- Returned by the Community Market
- Revoked
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

#### Valve Game Events

Event descriptions for Valve games can be found in the following files as strings starting with `ItemHistory_Action`

- Artifact Classic: [game/dcg/resource/dcg_common_english.txt](https://github.com/SteamDatabase/GameTracking-Artifact/blob/master/game/dcg/resource/dcg_common_english.txt)
- Counter-Strike: Global Offensive: [csgo/resource/csgo_english.txt](https://github.com/SteamDatabase/GameTracking-CS2/blob/master/game/csgo/resource/csgo_english.txt)
- Dota 2: [game/dota/pak01_dir/resource/localization/dota_english.txt](https://github.com/SteamDatabase/GameTracking-Dota2/blob/6abdd9d13de2f4330ca748082467b9ff6e6cd928/game/dota/pak01_dir/resource/localization/dota_english.txt)
- Portal 2: [portal2/portal2/resource/portal2_english.txt](https://github.com/SteamDatabase/GameTracking/blob/master/portal2/portal2/resource/portal2_english.txt)
- Team Fortress 2: [tf/resource/tf_english.txt](https://github.com/SteamDatabase/GameTracking-TF2/blob/master/tf/resource/tf_english.txt)

## Missing History Bug

The Inventory History API provides no way to fetch specific pages, instead we specify a time, and get results older than that time. It can return a maximum of 50 history events. If more events exist, it will also return a `cursor` object that we can use to find the very next event.  It's possible however that as you go through your history, it will unexpectedly jump ahead, skipping over events.

As an example, assuming you know there should be history on your account between `4/30/21` and `1/5/21`, your history might look like this:

```
… → 5/2/21 → 5/1/21 → 4/30/21 → 1/5/21 → 1/4/21 → …
```

This bug can be addressed by searching for history within the gap.  You can use the in-browser "Jump to date" feature, or try setting the `start_time` parameter yourself.  It may take several attempts to find a value for `start_time` that causes the missing history to re-appear.

If you search at date `x` and find missing history there, then history older than `x` should also re-appear, but history newer than `x` might not.  Looking at the previous example and assuming there's history between `4/30/21` and `3/14/21`; if `x = 3/14/21` then the gap may shrink, but not disappear, and some of the missing history may also show up right before `3/14/21`:

```
… → 5/2/21 → 5/1/21 → 4/30/21 → 3/15/21 → 3/14/21 → 3/13/21 → … → 1/5/21 → 1/4/21 → …
```

For this reason it's better to start your search right where the gap begins and proceed gradually.  Setting the `start_time` parameter yourself allows you to move in increments of 1 second.  The "Jump to date" feature moves in increments of 24 hours.  You can also use the `cursor[time]` and `cursor[time_frac]` parameters to move in increments of 1 millisecond.

Not all gaps are as large as in the examples above.  It's very common to have lots of small gaps when numerous events share the same time (ex: confirming multiple market listings at once).  Here the gaps length can be shorter than a second, and may skip as few as 1 event.  These gaps can be addressed in the same way as large gaps, but because of how small they are, they're very hard to identify and correct for.

In my experience, because small gaps tend to only happen due to market-related activity, they can be safely ignored, as market history is more accurately collected using the Market History API.

> [!NOTE]
> The BoosterManager plugin cannot detect this bug.  You'll need to monitor the plugin's activity yourself to ensure there's no gaps.  Within your `InventoryHistoryAPI`, `page - data["cursor"]["time"]` represents the size of the gap in seconds between the current page and the next page.  Be aware that `data["cursor"]` [can be](#history-ends-early-bug) `null`.  You can attempt to address this bug with your API by setting the `next_page` or `next_cursor` response parameters, telling the plugin which page you'd like it to fetch next.

## History Ends Early Bug

Sometimes Steam will say there's no more history, when really there is.  When `ajax=0`, this bug expresses itself as the "Load More History" button disappearing prematurely.  When that happens, the `cursor` object returned by the API will be missing.

For example, assuming we have history older than `4/30/21`, this can happen:

```
… → 5/2/21 → 5/1/21 → 4/30/21 → Nothing
```

This is resolved the same way as the [Missing History Bug](#missing-history-bug): by searching for history events at times past the cutoff.

> [!NOTE]
> The BoosterManager plugin will detect when the bug may have occurred.  On the `InventoryHistoryAPI` side of things, the value for `data[cursor]` will be `null` when it receives a page with this bug on it.  If your API does not send a `next_page` or `next_cursor` response parameter, then the plugin will stop running and send a link in the Steam Chat to the page where the bug occurred.

## Missing Descriptions Bugs

Every item appearing in `html` is meant to have an entry in `descriptions` located at `descriptions[appid][classid_instanceid]`

There are several conditions in which this is not true:

- Occasionally some items in `html` will have a name starting with "Unknown Asset" instead of the appropriate item name.  The affected items will still have defined `appid`, `classid`, `instanceid`, `contextid`, and `amount`, but they'll have no entry in `descriptions`.
- It can sometimes happen that all of the item names do appear properly, but `descriptions` is just an empty array.
- It can also sometimes happen that all of the item names do appear properly and `descriptions` is filled with entries, but some items will just be missing from `descriptions` for no apparent reason.

These sorts of bugs are likely caused by Steam servers being down, and will eventually go away on their own if you refresh the page.

There's at least one instance where this type of error will never go away by reloading the page:

- Most events involving Steam Gems on or before December 12, 2014 will appear as "Unknown Asset" in place of the gems, and will have no `descriptions` entry.  This is likely due to a gem duplication exploit and the resulting rollbacks.

> [!NOTE]
> The BoosterManager plugin does not detect these bugs.  You can tell the plugin to refresh the page by sending back `cursor` and `page` in the `next_cursor` and `next_page` response parameters respectively.

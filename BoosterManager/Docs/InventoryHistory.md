## Inventory History Cursor Bug

Below is a description of a bug in the [Inventory History API](https://steamcommunity.com/my/inventoryhistory/?ajax=1).  Dates are used for simplicity, in reality we're working with unix timestamps.

The Inventory History API provides no way to fetch specific pages, instead we specify a time, and get results older than that time. The API returns a maximum of 50 items.  If more items exist, it will return a `cursor` object containing the exact time of the very next item.  Ideally, we can use `cursor` to generate a chain of requests from the very first item to the very last item.  Sometimes however, the `cursor` object returned by the API will be missing, even if more items exist; breaking the chain.  Repeated calls will not fix this error.

For example, a chain of requests starting at `5/2/21`, but ending on `4/30/21` with a missing `cursor` object at the end might look like this:

> `5/2/21` &rarr; `5/1/21` &rarr; `4/30/21` &rarr; `Nothing`

If using Steam's [inventory history page](https://steamcommunity.com/my/inventoryhistory/), this bug expresses itself as the "Load more" button disappearing prematurely.

When this happens, we can sort of "remind" Steam that results older than `4/30/21` exist by using the `start_time` parameter with a date older than `4/30/21`.  If we make a request with `start_time = 4/29/21`, we can then go back to the `4/30/21` page, and the previously broken chain will be restored. Once the chain is restored it will continue on normally for a while.

> `5/2/21` &rarr; `5/1/21` &rarr; `4/30/21` &rarr; `4/29/21` &rarr; `4/28/21` &rarr; `...`

Special care needs to be taken when selecting a value for `start_time`, or else we can skip over some items.  If the chain breaks at `4/30/21`, but we then make a request with `start_time = 1/10/20` instead of `start_time = 4/29/21`, the chain will still be restored, but it will have a gap in it.

> `5/2/21` &rarr; `5/1/21` &rarr; `4/30/21` &rarr; `1/10/20` &rarr; `1/9/20` &rarr; `...`

The gap can be filled in by using `start_time` to request results now between `4/30/21` and `1/1/20`.  For example, with `start_time = 4/29/21`

> `5/2/21` &rarr; `5/1/21` &rarr; `4/30/21` &rarr; `4/29/21` &rarr; `4/28/21` &rarr; `...` &rarr; `1/10/20` &rarr; `1/9/20` &rarr; `...`

It's possible that the chain could break again between `4/28/21` and `1/10/20`.  Instead of pointing to nothing, it will point to `1/10/20`

> `5/2/21` &rarr; `5/1/21` &rarr; `4/30/21` &rarr; `4/29/21` &rarr; `4/28/21` &rarr; `4/27/21` &rarr; `4/26/21` &rarr; `1/10/20` &rarr; `1/9/20` &rarr; `...`

There's no good way to detect and fix these gaps.  It's best to avoid creating them in the first place by picking a good value for `start_time`.  The ideal value for `start_time` is the largest possible value less than or equal to where the chain was broken that will give us new results we haven't seen before.

This issue was first discovered when trying to fetch history sequentially on a single account with an app filter of `[753, 730]` and starting from `9/30/22`.  The issue arose after about 3,380 pages of history, when it reached `4/30/21`.

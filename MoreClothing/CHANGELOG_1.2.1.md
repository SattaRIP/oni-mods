# More Clothing v1.2.1 — Suit compatibility & a smarter Soft Suit

A bug-fix and polish pass on top of 1.2.0, mostly around how the mod's gear
plays with vanilla Atmo/Lead/Jet Suits.

## Fixes

- **Worn clothing & footwear no longer break Atmo Suits.** 1.2.0's visible
  Snazzy boots/shoes and the swimwear recolour could poke through a suit
  (boots drawn on the suit's feet, gold bits at the waist, the suit looking
  wrong or "invisible"). All of More Clothing's worn art is now pinned to the
  normal clothing layer **and** fully hidden whenever a real suit is worn on
  top — so Atmo/Lead/Jet Suits render exactly as they should, and the mod's
  garments reappear when the suit comes off.

- **Bionic dupes stay water-shock immune in a Soft Suit.** The Soft Suit's
  airtight seal is now re-asserted continuously while worn, so it can't be
  silently dropped by an equipment refresh or a save-load — bionic dupes in
  Soft Suits no longer get zapped in water.

- **Soft Suit now blocks Popped Eardrums** (over-pressure damage), matching
  the vanilla suits.

- **Soft Suit + real suit now render correctly.** If a dupe wears an
  Atmo/Lead/Jet Suit over a Soft Suit, the *suit's own* helmet shows and the
  Soft Suit's mask is dropped (previously the Soft Suit's retract hid the
  suit's helmet too, so no helmet appeared at all).

- **Snazzy gear hides under suits.** The Snazzy Swimwear recolour carried the
  vanilla wetsuit's foot and waist symbols, which the Atmo Suit body has no
  art for — so they poked through and the suit looked invisible. Those symbols
  are dropped now, so the swimwear hides cleanly under any suit, like a normal
  vest.

- **Soft Suit mask is now just the cup** — the oxygen hose and air-puff on the
  mask have been trimmed off, leaving only the clean mask over the face.

## Soft Suit: faster to get back on your feet

The Soft Suit trades an Atmo Suit's oxygen tank for a big breath reserve, but
recovering that whole reserve used to happen at the normal breathing rate —
so after a long dive a dupe spent an age topping back up. Now the refill is
**two-tier**: it catches up to a normal dupe's lungful at full speed (so
they're mission-ready fast), then backfills the bonus reserve passively at a
gentler pace while in breathable oxygen.

---

*More Clothing is standalone. The Soft Suit and Snazzy upgrades use Bionic
Booster Pack garments as ingredients; the Winter Coat, Shoes, and Mannequin
are DLC-free.*
